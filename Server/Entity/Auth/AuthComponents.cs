using Fantasy.Network;
using MongoDB.Driver;

namespace Entity.Auth;

/// <summary>每个认证相关 Scene 的数据库引用及启动就绪状态。</summary>
public sealed class AuthStorageComponent : Fantasy.Entitas.Entity
{
    /// <summary>当前 World 已建立的原生 MongoDB 实例。</summary>
    public IMongoDatabase Database { get; set; } = null!;
    /// <summary>索引与编号计数器初始化完成后才允许认证。</summary>
    public bool IsReady { get; set; }
}

/// <summary>Gate 中当前玩家到唯一有效会话的索引。</summary>
public sealed class GateAuthManagerComponent : Fantasy.Entitas.Entity
{
    /// <summary>已经完成票据认证的会话；替换会话时先切换此索引。</summary>
    public Dictionary<int, Session> Sessions { get; } = new();
}

/// <summary>网络会话绑定的服务端认证身份。</summary>
public sealed class SessionAuthComponent : Fantasy.Entitas.Entity
{
    /// <summary>票据确认的玩家编号。</summary>
    public int PlayerId { get; set; }
    /// <summary>当前 Gate 的唯一会话管理器。</summary>
    public GateAuthManagerComponent Manager { get; set; } = null!;
}
