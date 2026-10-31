using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.UI.Module.Login
{
    /// <summary>与服务端 HTTP 认证契约一致的永久失败错误码。</summary>
    internal static class GuestAuthErrorCodes
    {
        /// <summary>长期游客凭据错误。</summary>
        public const uint InvalidCredentials = 1001;
        /// <summary>当前授权已被新登录替换。</summary>
        public const uint SessionReplaced = 1004;
        /// <summary>请求格式错误。</summary>
        public const uint InvalidRequest = 1005;
        /// <summary>当前授权已过期。</summary>
        public const uint SessionExpired = 1006;
    }

    /// <summary>HTTP 认证返回的玩家资料、Gate 路由及短时票据。</summary>
    public sealed class GuestAuthResponse
    {
        /// <summary>服务端分配的玩家编号，最终仍由 Gate 确认。</summary>
        public int PlayerId { get; set; }
        /// <summary>玩家显示名称。</summary>
        public string PlayerName { get; set; }
        /// <summary>目标 Gate 场景编号。</summary>
        public uint GateId { get; set; }
        /// <summary>客户端可达的游戏服地址。</summary>
        public string Host { get; set; }
        /// <summary>KCP 端口。</summary>
        public int Port { get; set; }
        /// <summary>服务端传输协议。</summary>
        public string Protocol { get; set; }
        /// <summary>一次性票据，使用后不可复用。</summary>
        public string Ticket { get; set; }
        /// <summary>票据的 UTC 过期时间。</summary>
        public DateTime ExpiresAtUtc { get; set; }
        /// <summary>仅首次登录返回的当前授权，保存在 LoginModel 内存中。</summary>
        public string SessionToken { get; set; }
    }

    /// <summary>可展示给玩家并判断是否停止重试的认证错误。</summary>
    public sealed class GuestAuthException : Exception
    {
        /// <summary>服务端业务码或从 HTTP 状态推导的认证错误码；网络错误时为零。</summary>
        public uint Code { get; }
        /// <summary>认证失败的语义原因，由 LoginConst 映射成玩家可见文案。</summary>
        public LoginConst.ErrorMessageId ErrorMessageId { get; }
        /// <summary>未知业务码时作为兜底展示的服务端错误描述。</summary>
        public string RemoteMessage { get; }
        /// <summary>身份已失效或被替换时必须停止自动重连。</summary>
        public bool IsTerminal => Code == GuestAuthErrorCodes.InvalidCredentials || Code == GuestAuthErrorCodes.SessionReplaced ||
            Code == GuestAuthErrorCodes.InvalidRequest || Code == GuestAuthErrorCodes.SessionExpired;

        /// <summary>创建带有语义错误原因的认证异常，不在请求服务中定义玩家文案。</summary>
        /// <param name="errorMessageId">模块文案对应的语义错误原因。</param>
        /// <param name="code">服务端认证错误码。</param>
        /// <param name="remoteMessage">服务端错误描述，未知错误码时作为兜底。</param>
        public GuestAuthException(LoginConst.ErrorMessageId errorMessageId, uint code = 0, string remoteMessage = null)
            : base(errorMessageId.ToString())
        {
            ErrorMessageId = errorMessageId;
            Code = code;
            RemoteMessage = remoteMessage;
        }
    }

    /// <summary>只负责发送 Login 模块的认证 HTTP 请求，不保存凭据或登录状态。</summary>
    public sealed class GuestAuthClient
    {
        /// <summary>仅用于串联客户端认证 HTTP 请求日志。</summary>
        private static int _requestSequence;
        /// <summary>认证 API 基础地址。</summary>
        private readonly string _baseUrl;

        /// <summary>创建对应环境的认证 HTTP 客户端。</summary>
        /// <param name="baseUrl">HTTP 或 HTTPS API 基础地址。</param>
        public GuestAuthClient(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new GuestAuthException(LoginConst.ErrorMessageId.InvalidAuthServiceUrl);
            _baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>使用 LoginModel 提供的游客凭据申请新登录授权。</summary>
        /// <param name="credentials">已持久化的随机游客凭据。</param>
        /// <param name="cancellationToken">取消当前登录的令牌。</param>
        /// <returns>Gate 地址、一次性票据及当前授权。</returns>
        public Task<GuestAuthResponse> LoginAsync(GuestCredentials credentials, CancellationToken cancellationToken)
        {
            if (credentials == null || string.IsNullOrEmpty(credentials.GuestId) || string.IsNullOrEmpty(credentials.GuestSecret))
                throw new GuestAuthException(LoginConst.ErrorMessageId.GuestCredentialsUnavailable);
            return PostAsync("/api/auth/guest/login", JsonConvert.SerializeObject(credentials), null, cancellationToken);
        }

        /// <summary>只使用当前内存授权签发新票据，旧授权被替换时停止重试。</summary>
        /// <param name="sessionToken">LoginModel 保存的当前授权。</param>
        /// <param name="cancellationToken">取消重连的令牌。</param>
        /// <returns>新的 Gate 连接信息。</returns>
        public Task<GuestAuthResponse> GetTicketAsync(string sessionToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
                throw new GuestAuthException(LoginConst.ErrorMessageId.AuthorizationExpired, GuestAuthErrorCodes.SessionReplaced);
            return PostAsync("/api/auth/ticket", "{}", sessionToken, cancellationToken);
        }

        /// <summary>通过 GF.WebRequest 发送认证请求并解析认证响应或传输错误。</summary>
        /// <param name="path">认证 API 相对路径。</param>
        /// <param name="json">JSON 请求体。</param>
        /// <param name="sessionToken">重连时使用的 Bearer 授权。</param>
        /// <param name="cancellationToken">请求取消令牌。</param>
        /// <returns>经过基本格式验证的认证响应。</returns>
        private async Task<GuestAuthResponse> PostAsync(string path, string json, string sessionToken, CancellationToken cancellationToken)
        {
            int requestId = System.Threading.Interlocked.Increment(ref _requestSequence);
            Uri endpoint = new Uri(_baseUrl + path, UriKind.Absolute);
            string safeEndpoint = $"{endpoint.Scheme}://{endpoint.Authority}{endpoint.AbsolutePath}";
            Stopwatch stopwatch = Stopwatch.StartNew();
            WebRequestComponent webRequest = GF.WebRequest;
            if (webRequest == null)
            {
                Log.Error("认证 HTTP 请求失败：GF.WebRequest 组件尚未初始化。Endpoint={0}", safeEndpoint);
                throw new GuestAuthException(LoginConst.ErrorMessageId.WebRequestUnavailable);
            }

            float timeoutSeconds = webRequest.Timeout;
            Log.Info("认证 HTTP 请求开始 Request={0}, Endpoint={1}, Timeout={2:0.##}s, Agents={3}, Working={4}, Waiting={5}",
                requestId, safeEndpoint, timeoutSeconds, webRequest.TotalAgentCount, webRequest.WorkingAgentCount, webRequest.WaitingTaskCount);
            WebRequestResult result;
            try
            {
                Log.Info("认证 HTTP 发送中 Request={0}, Endpoint={1}", requestId, safeEndpoint);
                UniTask<WebRequestResult> requestTask;
                if (sessionToken == null)
                {
                    requestTask = webRequest.AddWebRequestAwait(_baseUrl + path, Encoding.UTF8.GetBytes(json));
                }
                else
                {
                    // GF 的表单请求会带出本次表单请求头；不把授权写入共享组件或 URL。
                    WWWForm ticketForm = new WWWForm();
                    ticketForm.AddField("request", "ticket");
                    ticketForm.headers["Authorization"] = "Bearer " + sessionToken;
                    requestTask = webRequest.AddWebRequestAwait(_baseUrl + path, ticketForm);
                }

                result = await requestTask.AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Log.Warning("认证 HTTP 请求已取消 Request={0}, Endpoint={1}, ElapsedMs={2}",
                    requestId, safeEndpoint, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                Log.Error("认证 HTTP 请求发生未处理异常 Request={0}, Endpoint={1}, Type={2}, Message={3}, ElapsedMs={4}",
                    requestId, safeEndpoint, exception.GetType().Name, exception.Message, stopwatch.ElapsedMilliseconds);
                throw;
            }

            stopwatch.Stop();
            string responseText = result.Bytes == null ? string.Empty : Encoding.UTF8.GetString(result.Bytes);
            int statusCode = result.IsError ? ParseHttpStatusCode(result.ErrorMessage) : 0;
            string statusDescription = !result.IsError
                ? "success"
                : statusCode > 0 ? statusCode.ToString() : "transport-error";
            Log.Info("认证 HTTP 响应 Request={0}, Status={1}, IsError={2}, Error={3}, ElapsedMs={4}, ResponseBytes={5}",
                requestId, statusDescription, result.IsError,
                result.IsError ? result.ErrorMessage : string.Empty,
                stopwatch.ElapsedMilliseconds, result.Bytes?.Length ?? 0);

            if (result.IsError)
            {
                uint errorCode = 0;
                LoginConst.ErrorMessageId errorMessageId;
                if (statusCode == 401)
                {
                    errorCode = sessionToken == null ? GuestAuthErrorCodes.InvalidCredentials : GuestAuthErrorCodes.SessionReplaced;
                    errorMessageId = sessionToken == null
                        ? LoginConst.ErrorMessageId.GuestLoginRejected
                        : LoginConst.ErrorMessageId.AuthorizationReplaced;
                }
                else
                {
                    errorMessageId = statusCode > 0
                        ? LoginConst.ErrorMessageId.HttpStatusFailure
                        : LoginConst.ErrorMessageId.AuthServiceUnreachable;
                }
                Log.Warning("认证 HTTP 请求失败 Request={0}, Endpoint={1}, Status={2}, ErrorCode={3}, Error={4}",
                    requestId, safeEndpoint, statusDescription, errorCode, result.ErrorMessage);
                throw new GuestAuthException(errorMessageId, errorCode);
            }

            cancellationToken.ThrowIfCancellationRequested();
            GuestAuthResponse response;
            try { response = JsonConvert.DeserializeObject<GuestAuthResponse>(responseText); }
            catch (JsonException exception)
            {
                Log.Error("认证 HTTP 响应解析失败 Request={0}, Endpoint={1}, ResponseBytes={2}, Type={3}",
                    requestId, safeEndpoint, result.Bytes?.Length ?? 0, exception.GetType().Name);
                throw new GuestAuthException(LoginConst.ErrorMessageId.InvalidResponseFormat);
            }
            if (response == null || response.GateId == 0 || string.IsNullOrWhiteSpace(response.Host) ||
                response.Port <= 0 || response.Port > ushort.MaxValue || response.Protocol != "KCP" || string.IsNullOrEmpty(response.Ticket))
            {
                Log.Warning("认证 HTTP 响应缺少有效 Gate 信息 Request={0}, Endpoint={1}, HasBody={2}, HasGateId={3}, HasHost={4}, Port={5}, Protocol={6}, HasTicket={7}",
                    requestId, safeEndpoint, !string.IsNullOrWhiteSpace(responseText), response?.GateId > 0,
                    !string.IsNullOrWhiteSpace(response?.Host), response?.Port, response?.Protocol,
                    !string.IsNullOrEmpty(response?.Ticket));
                throw new GuestAuthException(LoginConst.ErrorMessageId.InvalidGateInformation);
            }
            return response;
        }

        /// <summary>从 Game Framework 的传输错误文本中提取 HTTP 4xx 或 5xx 状态码。</summary>
        /// <param name="errorMessage">WebRequest 组件返回的错误文本。</param>
        /// <returns>错误文本中的 HTTP 状态码；未包含状态码时返回零。</returns>
        private static int ParseHttpStatusCode(string errorMessage)
        {
            Match match = Regex.Match(errorMessage ?? string.Empty, @"\b([45]\d{2})\b");
            if (!match.Success)
            {
                return 0;
            }

            return int.TryParse(match.Groups[1].Value, out int statusCode) ? statusCode : 0;
        }
    }
}
