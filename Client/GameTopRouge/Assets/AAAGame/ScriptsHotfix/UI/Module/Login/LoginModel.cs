using System;
using System.Security.Cryptography;
using GameFramework;
using Newtonsoft.Json;
using AAAGame.ScriptsHotfix.UI.Core;

namespace AAAGame.ScriptsHotfix.UI.Module.Login
{
    /// <summary>持久化的随机游客凭据，与服务端分配的玩家编号独立。</summary>
    public sealed class GuestCredentials
    {
        /// <summary>随机游客记录标识。</summary>
        [JsonProperty("guestId")]
        public string GuestId { get; set; }
        /// <summary>Base64 编码的 256 位随机密钥。</summary>
        [JsonProperty("guestSecret")]
        public string GuestSecret { get; set; }
    }

    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    /// <summary>保存 Login 模块游客凭据、授权、路由和 Gate 确认后的玩家资料。</summary>
    public sealed class LoginModel : ModuleModel<LoginModel>, IModel
    {
        /// <summary>凭据作为单一 JSON 值持久化，避免只保存部分身份。</summary>
        private const string CredentialsSettingKey = "Auth.GuestCredentials.v1";
        /// <summary>游客密钥长度，单位为字节。</summary>
        private const int SecretByteCount = 32;
        /// <summary>HTTP 服务最近确认的玩家身份，用于 Gate 响应一致性校验。</summary>
        private int _expectedPlayerId;

        /// <summary>当前授权只存在内存中，重启后需要重新完成游客登录。</summary>
        public string SessionToken { get; private set; }
        /// <summary>最近一次 HTTP 认证返回的 Gate 地址与票据。</summary>
        public GuestAuthResponse Route { get; private set; }
        /// <summary>Gate 确认后的玩家编号。</summary>
        public int PlayerId { get; private set; }
        /// <summary>Gate 确认后的玩家显示名称。</summary>
        public string PlayerName { get; private set; }
        /// <summary>Gate 已确认身份且当前连接有效。</summary>
        public bool IsAuthenticated { get; private set; }
        
        /// <summary>是否正在执行首次登录。</summary>
        public bool LoggingIn { get;  set; } = false;
        /// <summary>当前客户端是否已完成登录并启动主线。</summary>
        public bool LoginCompleted { get;  set; } = false;
        
        /// <summary>读取本地游客身份；首次使用时生成并确认持久化成功。</summary>
        /// <returns>可重复使用的游客凭据。</returns>
        public GuestCredentials GetOrCreateGuestCredentials()
        {
            string json = GF.Setting.GetString(CredentialsSettingKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    GuestCredentials saved = JsonConvert.DeserializeObject<GuestCredentials>(json);
                    if (saved != null && Guid.TryParse(saved.GuestId, out Guid id) && id != Guid.Empty &&
                        Convert.FromBase64String(saved.GuestSecret).Length == SecretByteCount)
                    {
                        SaveCredentials();
                        return saved;
                    }
                }
                catch (Exception exception) when (exception is JsonException || exception is FormatException || exception is ArgumentException) { }
                throw new GuestAuthException(LoginConst.ErrorMessageId.GuestCredentialsCorrupt);
            }

            byte[] secret = new byte[SecretByteCount];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create()) { random.GetBytes(secret); }
            GuestCredentials credentials = new GuestCredentials
            {
                GuestId = Guid.NewGuid().ToString("N"),
                GuestSecret = Convert.ToBase64String(secret)
            };
            GF.Setting.SetString(CredentialsSettingKey, JsonConvert.SerializeObject(credentials));
            SaveCredentials();
            return credentials;
        }

        /// <summary>缓存 HTTP 授权和路由，保留旧授权用于后续票据续签。</summary>
        /// <param name="route">认证服务返回的 Gate 路由及票据。</param>
        public void ApplyAuthResponse(GuestAuthResponse route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            Route = route;
            if (route.PlayerId > 0) _expectedPlayerId = route.PlayerId;
            if (!string.IsNullOrEmpty(route.SessionToken)) SessionToken = route.SessionToken;
        }

        /// <summary>Gate 验证成功后设置权威玩家资料并更新现有玩法全局 ID。</summary>
        /// <param name="playerId">Gate 确认的玩家编号。</param>
        /// <param name="playerName">Gate 确认的玩家名称。</param>
        public void ConfirmGateIdentity(int playerId, string playerName)
        {
            if (playerId <= 0) throw new GuestAuthException(LoginConst.ErrorMessageId.InvalidPlayerIdentity);
            if (_expectedPlayerId > 0 && _expectedPlayerId != playerId)
                throw new GuestAuthException(LoginConst.ErrorMessageId.ReconnectIdentityMismatch, GuestAuthErrorCodes.SessionReplaced);
            PlayerId = playerId;
            PlayerName = playerName;
            IsAuthenticated = true;
            GF.UserId = playerId;
        }

        /// <summary>标记当前网络暂时失联，保留已确认玩家 ID 用于后续身份一致性校验。</summary>
        public void MarkTransportDisconnected() => IsAuthenticated = false;

        /// <summary>清除内存授权、路由和玩家资料，不删除本地游客凭据。</summary>
        public void ClearAuthorization()
        {
            SessionToken = null;
            Route = null;
            PlayerId = 0;
            _expectedPlayerId = 0;
            PlayerName = null;
            IsAuthenticated = false;
            GF.UserId = 0;
        }

        /// <summary>读取 Game Framework 底层保存结果；失败时禁止请求服务端建号。</summary>
        private static void SaveCredentials()
        {
            if (!GameFrameworkEntry.GetModule<GameFramework.Setting.ISettingManager>().Save())
                throw new GuestAuthException(LoginConst.ErrorMessageId.GuestCredentialsSaveFailed);
        }
    }
}
