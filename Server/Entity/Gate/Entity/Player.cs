using Fantasy.Entitas.Interface;
using Fantasy.Network;

namespace Fantasy.Gate.Entity;

/// <summary>可持久化的基础玩家资料，玩家编号由认证服务分配。</summary>
public sealed class Player : Entitas.Entity, ISupportedSerialize
{
    /// <summary>玩家显示名称。</summary>
    public string Name{get;set;} = string.Empty;
    /// <summary>玩家等级。</summary>
    public int Level{get;set;}
    /// <summary>认证服务分配的正整数编号，沿用原数据库字段类型。</summary>
    public long PlayerId{get;set;}
    /// <summary>
    /// 游戏服会话
    /// </summary>
    public Session? Session{get;set;}
}
