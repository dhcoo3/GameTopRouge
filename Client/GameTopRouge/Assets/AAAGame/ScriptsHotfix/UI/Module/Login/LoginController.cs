using System;
using System.Threading;
using System.Threading.Tasks;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.UI.Core;
using AAAGame.ScriptsHotfix.UI.Module.FightShare;
using Cysharp.Threading.Tasks;
using cfg.Fight;
using Fantasy;
using Fantasy.Network;
using cfg.UI;
using Log = UnityGameFramework.Runtime.Log;

namespace AAAGame.ScriptsHotfix.UI.Module.Login
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    /// <summary>编排 Login 游客认证、Gate 登录协议、认证续签和断线重连。</summary>
    public sealed class LoginController : ModuleController<LoginController>, IController
    {
        /// <summary>断线后最多重试次数。</summary>
        private const int MaximumReconnectAttempts = 5;
        /// <summary>首次重连等待时间，单位为毫秒。</summary>
        private const int InitialReconnectDelayMilliseconds = 500;
        /// <summary>重连等待上限，单位为毫秒。</summary>
        private const int MaximumReconnectDelayMilliseconds = 5000;
        /// <summary>Gate 登录协议等待上限，单位为毫秒。</summary>
        private const int GateLoginTimeoutMilliseconds = 5000;

        /// <summary>Login 模块是否已经订阅网络传输断开事件。</summary>
        private bool _networkSubscribed;
        /// <summary>是否正在执行唯一的自动重连循环。</summary>
        private bool _reconnecting;
        /// <summary>当前授权会话是否还允许连接和重连。</summary>
        private bool _active;
        /// <summary>组件或应用正在关闭时禁止迟到回调启动新流程。</summary>
        private bool _shuttingDown;
        /// <summary>是否正在恢复登录首页。</summary>
        private bool _returningLogin;
        /// <summary>当前授权会话的生命周期。</summary>
        private CancellationTokenSource _lifetime;
        /// <summary>无状态的 HTTP 认证请求客户端。</summary>
        private GuestAuthClient _authClient;

        /// <summary>Fantasy 按协议类型自动发现 G2C_LoginRevokedHandler。</summary>
        public override void RegisterProto()
        {
        }

        /// <summary>监听底层传输断开，由 Login 认证流程决定是否续签和重连。</summary>
        public override void RegisterEvent()
        {
            EnsureNetworkSubscription();
        }


        /// <summary>统一编排认证、界面状态、错误提示和登录后的主线入口。</summary>
        public async UniTaskVoid HandleGuestLoginAsync(CancellationToken cancellationToken)
        {
            bool loggingIn = LoginModel.Instance.LoggingIn;
            bool loginCompleted = LoginModel.Instance.LoginCompleted;
            if (loggingIn || _shuttingDown)
            {
                Fire(LoginConst.Event.ELoginStateChanged, loggingIn, loginCompleted);
                return;
            }
            if (loginCompleted)
            {
                Fire(LoginConst.Event.ELoginStateChanged, false, true);
                return;
            }

            LoginModel.Instance.LoggingIn = true;
            Log.Info("游客登录流程开始。");
            Fire(LoginConst.Event.ELoginStateChanged, true, false);
            try
            {
                Log.Info("正在加载游客登录服务配置。");
                AppConfigs configs = await AppConfigs.GetInstanceSync();
                cancellationToken.ThrowIfCancellationRequested();
                Log.Info("登录服务配置已加载，开始 HTTP 与 Gate 认证。");
                await AuthenticateGuestAsync(configs.AuthBaseUrl, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                LoginModel.Instance.LoginCompleted = true;
                Log.Info("游客登录认证成功，玩家 ID={0}。", LoginModel.Instance.PlayerId);
                Fire(LoginConst.Event.ELoginStateChanged, false, true);
                FightShareController.Instance.EnterFight(DungeonType.Main);
            }
            catch (OperationCanceledException)
            {
                Log.Info("游客登录流程已取消。");
                if (!LoginModel.Instance.LoginCompleted && _active) StopCurrentSession(true);
            }
            catch (Exception exception)
            {
                uint errorCode = exception is GuestAuthException authException ? authException.Code : 0;
                Log.Error("游客登录流程失败 Type={0}, Code={1}", exception.GetType().Name, errorCode);
                if (!cancellationToken.IsCancellationRequested)
                {
                    LoginView.Instance.ShowMessage(LoginConst.Text.LoginFailedTitle, exception is GuestAuthException guestAuthException
                        ? LoginConst.Text.GetAuthErrorMessage(guestAuthException) : LoginConst.Text.LoginNetworkRetry);
                }
            }
            finally
            {
                LoginModel.Instance.LoggingIn = false;
                if (!LoginModel.Instance.LoginCompleted) Fire(LoginConst.Event.ELoginStateChanged, false, false);
            }
        }

        /// <summary>使用持久游客身份完成 HTTP 授权与 Gate 票据登录。</summary>
        /// <param name="authBaseUrl">当前环境的认证服务地址。</param>
        /// <param name="cancellationToken">关闭登录面板时取消尚未完成的登录。</param>
        /// <returns>Gate 确认身份后完成的任务。</returns>
        private async Task AuthenticateGuestAsync(string authBaseUrl, CancellationToken cancellationToken)
        {
            if (_shuttingDown) throw new GuestAuthException(LoginConst.ErrorMessageId.ClientShuttingDown);

            EnsureNetworkSubscription();
            StopCurrentSession(true);
            CancellationTokenSource lifetime = new CancellationTokenSource();
            _lifetime = lifetime;
            _active = true;
            try
            {
                _authClient = new GuestAuthClient(authBaseUrl);
                using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellationToken))
                {
                    GuestCredentials credentials = LoginModel.Instance.GetOrCreateGuestCredentials();
                    GuestAuthResponse route = await _authClient.LoginAsync(credentials, linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    if (!ReferenceEquals(_lifetime, lifetime)) throw new OperationCanceledException();
                    LoginModel.Instance.ApplyAuthResponse(route);
                    await ConnectAndAuthenticateAsync(route, linked.Token);
                }
            }
            catch
            {
                if (ReferenceEquals(_lifetime, lifetime)) StopCurrentSession(true);
                throw;
            }
        }

        /// <summary>帧层报告网络停顿后只启动一个自动续签循环。</summary>
        public void BeginReconnect()
        {
            if (!_active || LoginModel.Instance.LoggingIn || _reconnecting || _lifetime == null || _shuttingDown ||
                !LoginModel.Instance.IsAuthenticated)
                return;
            _reconnecting = true;
            LoginModel.Instance.MarkTransportDisconnected();
            ReconnectAsync(_lifetime).Forget();
        }

        /// <summary>只响应当前 Gate Session 的顶号通知。</summary>
        /// <param name="session">接收通知的连接。</param>
        /// <param name="message">服务端提供的退出原因。</param>
        public void HandleLoginRevoked(Session session, string message)
        {
            if (!_active || !ReferenceEquals(GF.GameNetwork.Session, session)) return;
            FailSessionAsync(string.IsNullOrWhiteSpace(message) ? LoginConst.Text.LoginRevoked : message, _lifetime).Forget();
        }

        /// <summary>因授权失效或退出而释放当前连接状态，保留持久游客凭据。</summary>
        private void StopCurrentSession(bool clearAuthorization)
        {
            _active = false;
            _reconnecting = false;
            CancellationTokenSource lifetime = _lifetime;
            _lifetime = null;
            lifetime?.Cancel();
            lifetime?.Dispose();
            GF.GameNetwork?.Disconnect();
            _authClient = null;
            if (clearAuthorization) LoginModel.Instance.ClearAuthorization();
        }

        /// <summary>连接认证服务返回的 Gate 并提交一次性登录票据。</summary>
        /// <param name="route">Gate 地址和票据。</param>
        /// <param name="cancellationToken">当前授权会话取消令牌。</param>
        /// <returns>Gate 确认身份后的认证任务。</returns>
        private async Task ConnectAndAuthenticateAsync(GuestAuthResponse route, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Session session = await GF.GameNetwork.ConnectAsync(route.Host, route.Port, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            (int playerId, string playerName) identity = await WithTimeoutAsync(
                LoginGateAsync(session, route.Ticket), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!_active || session.IsDisposed || !ReferenceEquals(GF.GameNetwork.Session, session))
                throw new OperationCanceledException();
            LoginModel.Instance.ConfirmGateIdentity(identity.playerId, identity.playerName);
        }

        /// <summary>发送 Gate 登录协议并复制响应资料，避免消息池回收后继续持有响应。</summary>
        /// <param name="session">当前 KCP 会话。</param>
        /// <param name="ticket">未使用的短时票据。</param>
        /// <returns>Gate 确认的玩家编号和名称。</returns>
        private static async Task<(int PlayerId, string PlayerName)> LoginGateAsync(Session session, string ticket)
        {
            using (C2G_LoginRequest request = C2G_LoginRequest.Create())
            {
                request.Ticket = ticket;
                using (G2C_LoginResponse response = (G2C_LoginResponse)await session.Call(request))
                {
                    if (response.ErrorCode != 0)
                        throw new GuestAuthException(LoginConst.ErrorMessageId.ServerRejected, response.ErrorCode, response.Tag);
                    if (response.PlayerId <= 0)
                        throw new GuestAuthException(LoginConst.ErrorMessageId.InvalidPlayerIdentity);
                    return (response.PlayerId, response.PlayerName);
                }
            }
        }

        /// <summary>只处理当前连接断开；登录和重连阶段的失败由对应等待任务收敛。</summary>
        /// <param name="session">断开的 Session。</param>
        private void OnTransportDisconnected(Session session)
        {
            if (!_active || LoginModel.Instance.LoggingIn || _reconnecting || !LoginModel.Instance.IsAuthenticated ||
                !ReferenceEquals(GF.GameNetwork.Session, session))
                return;
            BeginReconnect();
        }

        /// <summary>使用当前授权申请新票据，并按指数退避重连。</summary>
        /// <param name="lifetime">本轮所属的授权生命周期。</param>
        private async UniTaskVoid ReconnectAsync(CancellationTokenSource lifetime)
        {
            CancellationToken cancellationToken = lifetime.Token;
            try
            {
                for (int attempt = 0; attempt < MaximumReconnectAttempts; attempt++)
                {
                    int delay = Math.Min(MaximumReconnectDelayMilliseconds, InitialReconnectDelayMilliseconds << attempt);
                    await UniTask.Delay(delay, cancellationToken: cancellationToken);
                    try
                    {
                        GuestAuthResponse route = await _authClient.GetTicketAsync(LoginModel.Instance.SessionToken, cancellationToken);
                        if (!ReferenceEquals(_lifetime, lifetime)) return;
                        LoginModel.Instance.ApplyAuthResponse(route);
                        await ConnectAndAuthenticateAsync(route, cancellationToken);
                        return;
                    }
                    catch (GuestAuthException exception) when (exception.IsTerminal)
                    {
                        await FailSessionAsync(LoginConst.Text.GetAuthErrorMessage(exception), lifetime);
                        return;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception exception)
                    {
                        Log.Warning("自动重连第 {0} 次失败：{1}", attempt + 1, exception.GetType().Name);
                        if (ReferenceEquals(_lifetime, lifetime)) GF.GameNetwork.Disconnect();
                    }
                }
                await FailSessionAsync(LoginConst.Text.ReconnectExhausted, lifetime);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (ReferenceEquals(_lifetime, lifetime)) _reconnecting = false;
            }
        }

        /// <summary>停止重试并在下一帧结束战斗、恢复登录首页。</summary>
        /// <param name="message">退出原因。</param>
        /// <param name="lifetime">失败所属的授权生命周期。</param>
        private async UniTask FailSessionAsync(string message, CancellationTokenSource lifetime)
        {
            if (!ReferenceEquals(_lifetime, lifetime) || _shuttingDown) return;
            _active = false;
            LoginModel.Instance.ClearAuthorization();
            lifetime.Cancel();
            await UniTask.Yield();
            if (!ReferenceEquals(_lifetime, lifetime) || _shuttingDown) return;
            StopCurrentSession(false);
            ReturnLoginAsync(message).Forget();
        }

        /// <summary>停止旧连接并为新一轮认证订阅传输断开事件。</summary>
        private void EnsureNetworkSubscription()
        {
            if (_networkSubscribed || GF.GameNetwork == null) return;
            GF.GameNetwork.TransportDisconnected += OnTransportDisconnected;
            GF.GameNetwork.TransportShuttingDown += OnTransportShuttingDown;
            _networkSubscribed = true;
        }

        /// <summary>网络组件退出时取消认证任务，避免应用关闭后有迟到重连。</summary>
        private void OnTransportShuttingDown()
        {
            _shuttingDown = true;
            StopCurrentSession(true);
        }

        /// <summary>释放当前战斗并恢复登录入口；保留本地游客凭据。</summary>
        /// <param name="message">向玩家展示的退出原因。</param>
        /// <returns>界面恢复任务。</returns>
        private async UniTask ReturnLoginAsync(string message)
        {
            if (_returningLogin) return;
            _returningLogin = true;
            try
            {
                UIToGamePlayerBridge.Instance.EndGamePlay();
                FightShareModel.Instance.ResetAfterLogout();
                GF.UI.CloseAllLoadingUIForms();
                GF.UI.CloseAllLoadedUIForms();
                await UIModuleManager.Instance.GoToModuleAsync(ModuleType.Login);
                LoginModel.Instance.LoginCompleted = false;
                Fire(LoginConst.Event.ELoginStateChanged, false, false);
                LoginView.Instance.ShowMessage(LoginConst.Text.LoginDisconnectedTitle, message);
            }
            catch (Exception exception)
            {
                Log.Error("返回登录页失败：{0}", exception);
            }
            finally { _returningLogin = false; }
        }

        /// <summary>恢复登录界面并显示认证断开原因。</summary>
        /// <param name="message">需要显示给玩家的原因。</param>
        /// <returns>登录界面恢复完成后的异步任务。</returns>
        public UniTask ReturnToLoginAsync(string message)
        {
            return ReturnLoginAsync(message);
        }

        /// <summary>为底层 RPC 设置等待上限，并观察超时后迟到任务的异常。</summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="task">Gate 登录 RPC。</param>
        /// <param name="cancellationToken">当前登录取消令牌。</param>
        /// <returns>Gate 登录响应资料。</returns>
        private static async Task<T> WithTimeoutAsync<T>(Task<T> task, CancellationToken cancellationToken)
        {
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                Task delay = Task.Delay(GateLoginTimeoutMilliseconds, timeout.Token);
                if (await Task.WhenAny(task, delay) != task)
                {
                    _ = task.ContinueWith(failed => { _ = failed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new GuestAuthException(LoginConst.ErrorMessageId.GateLoginTimeout);
                }
                timeout.Cancel();
                return await task;
            }
        }
    }
}
