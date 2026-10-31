using System;
using System.Collections.Generic;

namespace AAAGame.ScriptsHotfix.UI.Module.Login
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    /// <summary>
    /// 该模块常量定义
    /// </summary>
    public class LoginConst
    {

        /// <summary>认证失败的语义原因，用于将网络错误映射成模块文案。</summary>
        public enum ErrorMessageId
        {
            /// <summary>登录服务地址格式错误。</summary>
            InvalidAuthServiceUrl,
            /// <summary>本地游客凭据不可用。</summary>
            GuestCredentialsUnavailable,
            /// <summary>当前登录授权过期。</summary>
            AuthorizationExpired,
            /// <summary>客户端 WebRequest 组件未初始化。</summary>
            WebRequestUnavailable,
            /// <summary>游客身份验证失败。</summary>
            GuestLoginRejected,
            /// <summary>登录授权已被替换。</summary>
            AuthorizationReplaced,
            /// <summary>认证服务返回 HTTP 错误。</summary>
            HttpStatusFailure,
            /// <summary>无法连接认证服务。</summary>
            AuthServiceUnreachable,
            /// <summary>认证响应格式错误。</summary>
            InvalidResponseFormat,
            /// <summary>认证服务没有返回有效 Gate 信息。</summary>
            InvalidGateInformation,
            /// <summary>本地游客凭据损坏。</summary>
            GuestCredentialsCorrupt,
            /// <summary>Gate 没有返回有效玩家身份。</summary>
            InvalidPlayerIdentity,
            /// <summary>重连时返回了其他玩家身份。</summary>
            ReconnectIdentityMismatch,
            /// <summary>游客凭据无法保存到本地。</summary>
            GuestCredentialsSaveFailed,
            /// <summary>客户端正在关闭。</summary>
            ClientShuttingDown,
            /// <summary>自动重连次数已用完。</summary>
            ReconnectExhausted,
            /// <summary>Gate 登录被服务端拒绝。</summary>
            ServerRejected,
            /// <summary>Gate 登录请求超时。</summary>
            GateLoginTimeout,
        }

        /// <summary>集中管理 Login 模块面向玩家展示的中文文案。</summary>
        public static class Text
        {
            /// <summary>登录失败提示框的标题：“登录失败”。</summary>
            public const string LoginFailedTitle = "登录失败";
            /// <summary>无法分类的登录失败提示。</summary>
            public const string LoginFailedGeneric = "登录失败，请稍后重试。";
            /// <summary>普通网络错误提示玩家检查网络后重试。</summary>
            public const string LoginNetworkRetry = "登录失败，请检查网络后重试。";
            /// <summary>登录失效后返回入口时的提示框标题：“登录已断开”。</summary>
            public const string LoginDisconnectedTitle = "登录已断开";
            /// <summary>登录入口中账号密码功能的占位提示。</summary>
            public const string AccountLoginUnavailable = "账号登录暂未开放";
            /// <summary>通用确认按钮文字。</summary>
            public const string Confirm = "确定";
            /// <summary>本地游客凭据损坏时的提示。</summary>
            public const string GuestCredentialsCorrupt = "本地游客凭据损坏，已保留原数据，请检查本地存档。";
            /// <summary>Gate 未返回有效玩家身份时的提示。</summary>
            public const string InvalidPlayerIdentity = "游戏服务器未返回有效身份。";
            /// <summary>重连身份与本地当前玩家不一致时的提示。</summary>
            public const string ReconnectIdentityMismatch = "重连身份与当前玩家不一致。";
            /// <summary>游客身份无法保存时的提示。</summary>
            public const string GuestCredentialsSaveFailed = "游客身份保存失败，请检查存储空间后重试。";
            /// <summary>登录服务地址无效时的提示。</summary>
            public const string InvalidAuthServiceUrl = "登录服务地址配置不正确。";
            /// <summary>本地游客身份不可用时的提示。</summary>
            public const string GuestCredentialsUnavailable = "本地游客凭据不可用。";
            /// <summary>登录授权过期时要求玩家重新登录。</summary>
            public const string AuthorizationExpired = "登录授权已失效，请重新登录。";
            /// <summary>客户端网络组件未就绪时的提示。</summary>
            public const string WebRequestUnavailable = "客户端网络服务尚未就绪，请稍后重试。";
            /// <summary>游客凭据验证失败时的提示。</summary>
            public const string GuestLoginRejected = "游客身份验证失败，请保留本地身份信息并重试。";
            /// <summary>授权被另一次登录替换时的提示。</summary>
            public const string AuthorizationReplaced = "登录授权已失效或已被替换，请重新登录。";
            /// <summary>认证服务返回 HTTP 错误时的提示。</summary>
            public const string HttpStatusFailure = "登录服务暂时无法处理请求，请稍后重试。";
            /// <summary>认证服务不可达时的提示。</summary>
            public const string AuthServiceUnreachable = "无法连接登录服务，请检查服务端是否启动或网络地址是否可达。";
            /// <summary>认证响应无法解析时的提示。</summary>
            public const string InvalidResponseFormat = "认证服务返回的数据格式不正确。";
            /// <summary>认证响应缺少有效游戏服信息时的提示。</summary>
            public const string InvalidGateInformation = "认证服务未返回有效的游戏服信息。";
            /// <summary>认证请求无效时的提示。</summary>
            public const string InvalidAuthRequest = "认证请求无效，请重新登录。";
            /// <summary>认证服务未提供可展示错误原因时的兜底提示。</summary>
            public const string ServerRejected = "登录请求失败，请稍后重试。";
            /// <summary>客户端即将关闭时的内部提示。</summary>
            public const string ClientShuttingDown = "客户端正在关闭。";
            /// <summary>被顶号时服务端未提供退出原因的提示。</summary>
            public const string LoginRevoked = "此账号已在其他位置登录。";
            /// <summary>自动重连耗尽时要求玩家重新登录的提示。</summary>
            public const string ReconnectExhausted = "连接游戏服务器失败，请重新登录。";
            /// <summary>Gate 连接或认证超时时的提示。</summary>
            public const string GateLoginTimeout = "游戏服务器连接或认证超时。";

            /// <summary>按认证错误原因及服务端错误码解析玩家可见文案。</summary>
            /// <param name="exception">携带语义原因、服务端错误码和可选服务端消息的认证异常。</param>
            /// <returns>当前认证错误对应的中文文案。</returns>
            public static string GetAuthErrorMessage(GuestAuthException exception)
            {
                if (exception == null) return LoginFailedGeneric;

                switch (exception.ErrorMessageId)
                {
                    case ErrorMessageId.InvalidAuthServiceUrl: return InvalidAuthServiceUrl;
                    case ErrorMessageId.GuestCredentialsUnavailable: return GuestCredentialsUnavailable;
                    case ErrorMessageId.AuthorizationExpired: return AuthorizationExpired;
                    case ErrorMessageId.WebRequestUnavailable: return WebRequestUnavailable;
                    case ErrorMessageId.GuestLoginRejected: return GuestLoginRejected;
                    case ErrorMessageId.AuthorizationReplaced: return AuthorizationReplaced;
                    case ErrorMessageId.HttpStatusFailure: return HttpStatusFailure;
                    case ErrorMessageId.AuthServiceUnreachable: return AuthServiceUnreachable;
                    case ErrorMessageId.InvalidResponseFormat: return InvalidResponseFormat;
                    case ErrorMessageId.InvalidGateInformation: return InvalidGateInformation;
                    case ErrorMessageId.GuestCredentialsCorrupt: return GuestCredentialsCorrupt;
                    case ErrorMessageId.InvalidPlayerIdentity: return InvalidPlayerIdentity;
                    case ErrorMessageId.ReconnectIdentityMismatch: return ReconnectIdentityMismatch;
                    case ErrorMessageId.GuestCredentialsSaveFailed: return GuestCredentialsSaveFailed;
                    case ErrorMessageId.ClientShuttingDown: return ClientShuttingDown;
                    case ErrorMessageId.ReconnectExhausted: return ReconnectExhausted;
                    case ErrorMessageId.GateLoginTimeout: return GateLoginTimeout;
                    case ErrorMessageId.ServerRejected:
                        switch (exception.Code)
                        {
                            case GuestAuthErrorCodes.InvalidCredentials: return GuestLoginRejected;
                            case GuestAuthErrorCodes.SessionReplaced: return AuthorizationReplaced;
                            case GuestAuthErrorCodes.InvalidRequest: return InvalidAuthRequest;
                            case GuestAuthErrorCodes.SessionExpired: return AuthorizationExpired;
                        }
                        return string.IsNullOrWhiteSpace(exception.RemoteMessage) ? ServerRejected : exception.RemoteMessage;
                    default: return LoginFailedGeneric;
                }
            }
        }

        /// <summary>
        /// 该模块的弹窗定义
        /// </summary>
        public enum OverlayUI
        {
            LoginTips,
            LoginWindow,
        }
        
    
        /// <summary>Login 模块的 MVC 事件。</summary>
        public static class Event
        {
            /// <summary>游客登录状态变化，由 LoginController 发布给 LoginPanel。</summary>
            public static readonly Int16 ELoginStateChanged = ++GameEvent.UIEventId;
        }
    }
}
