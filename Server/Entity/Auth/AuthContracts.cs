namespace Entity.Auth;

/// <summary>认证协议使用的业务错误码和时限。</summary>
public static class AuthConstants
{
    /// <summary>操作成功。</summary>
    public const uint Success = 0;
    /// <summary>游客凭据不正确。</summary>
    public const uint InvalidCredentials = 1001;
    /// <summary>票据不存在、过期或已经使用。</summary>
    public const uint InvalidTicket = 1002;
    /// <summary>认证存储或目标服务尚未就绪。</summary>
    public const uint Unavailable = 1003;
    /// <summary>登录授权已被另一次登录替换。</summary>
    public const uint SessionReplaced = 1004;
    /// <summary>请求格式不正确。</summary>
    public const uint InvalidRequest = 1005;
    /// <summary>当前登录授权已过期。</summary>
    public const uint SessionExpired = 1006;
    /// <summary>随机密钥长度，单位为字节。</summary>
    public const int SecretByteCount = 32;
    /// <summary>一次性游戏服票据有效秒数。</summary>
    public const int TicketLifetimeSeconds = 60;
    /// <summary>当前登录授权有效小时数。</summary>
    public const int SessionLifetimeHours = 24;
}

/// <summary>游客首次登录或再次登录提交的持久凭据。</summary>
public sealed class GuestLoginRequest
{
    /// <summary>客户端生成并持久保存的随机 GUID，不是玩家编号。</summary>
    public string GuestId { get; set; } = string.Empty;
    /// <summary>客户端持久保存的 256 位随机密钥，采用 Base64 编码。</summary>
    public string GuestSecret { get; set; } = string.Empty;
}

/// <summary>HTTP 认证接口的错误信息。</summary>
public sealed class AuthErrorResponse
{
    /// <summary>可供客户端判断是否继续重试的业务错误码。</summary>
    public uint Code { get; set; }
    /// <summary>面向玩家的错误描述，不包含凭据。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>HTTP 接口返回的游戏服连接信息和一次性票据。</summary>
public class AuthTicketResponse
{
    /// <summary>目标 Gate 的场景配置编号。</summary>
    public uint GateId { get; set; }
    /// <summary>客户端可达的游戏服地址。</summary>
    public string Host { get; set; } = string.Empty;
    /// <summary>KCP 外网端口。</summary>
    public int Port { get; set; }
    /// <summary>游戏服传输协议，当前固定为 KCP。</summary>
    public string Protocol { get; set; } = "KCP";
    /// <summary>服务端分配的正整数玩家编号。</summary>
    public int PlayerId { get; set; }
    /// <summary>玩家显示名称。</summary>
    public string PlayerName { get; set; } = string.Empty;
    /// <summary>仅能使用一次的游戏服登录票据。</summary>
    public string Ticket { get; set; } = string.Empty;
    /// <summary>票据失效的 UTC 时间。</summary>
    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>游客认证成功后返回的当前登录授权。</summary>
public sealed class GuestLoginResponse : AuthTicketResponse
{
    /// <summary>仅存放在客户端内存中，用于重新申请票据的随机授权。</summary>
    public string SessionToken { get; set; } = string.Empty;
    /// <summary>当前登录授权的 UTC 失效时间。</summary>
    public DateTime SessionExpiresAtUtc { get; set; }
}

/// <summary>票据验证后供 Gate 使用的身份，不接收客户端玩家编号。</summary>
public sealed class VerifiedGuest
{
    /// <summary>游客记录的随机主键。</summary>
    public string GuestId { get; init; } = string.Empty;
    /// <summary>已验证的玩家编号。</summary>
    public int PlayerId { get; init; }
    /// <summary>玩家显示名称。</summary>
    public string PlayerName { get; init; } = string.Empty;
    /// <summary>本次登录授权的哈希，用于验证是否已被替换。</summary>
    public string SessionHash { get; init; } = string.Empty;
}
