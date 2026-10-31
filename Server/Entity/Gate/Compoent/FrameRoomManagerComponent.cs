using Fantasy;
using Fantasy.Gate.Entity;
using Fantasy.Network;

namespace Entity.Gate.Compoent;

/// <summary>
/// Gate 场景中管理全部双人帧同步房间的组件。
/// </summary>
public sealed class FrameRoomManagerComponent : Fantasy.Entitas.Entity
{
    /// <summary>下一个可分配的房间标识。</summary>
    public int NextRoomId { get; set; } = 1;

    /// <summary>房间标识到房间状态的映射。</summary>
    public Dictionary<int, FrameRoom> Rooms { get; } = new();

    /// <summary>玩家标识到所属房间标识的映射。</summary>
    public Dictionary<int, int> PlayerRoomIds { get; } = new();

}

/// <summary>
/// 一局双人帧同步对局的房间状态。
/// </summary>
public sealed class FrameRoom
{
    /// <summary>房间与对局的唯一标识。</summary>
    public int RoomId { get; init; }

    /// <summary>创建房间的玩家标识。</summary>
    public int OwnerPlayerId { get; init; }

    /// <summary>固定副本配置标识。</summary>
    public int DungeonId { get; init; }

    /// <summary>固定副本类型枚举值。</summary>
    public int DungeonType { get; init; }

    /// <summary>房间成员，按加入顺序保存，第二人加入后不可再变更。</summary>
    public List<FrameRoomMember> Members { get; } = new(2);

    /// <summary>房间是否已锁定并开始逻辑帧。</summary>
    public bool IsStarted { get; set; }

    /// <summary>本局独立的帧同步状态实体，未开局时为空。</summary>
    public FSManagerComponent? FrameManager { get; set; }
}

/// <summary>
/// 房间中一个已认证玩家的连接和英雄选择状态。
/// </summary>
public sealed class FrameRoomMember
{
    /// <summary>认证后的玩家标识。</summary>
    public int PlayerId { get; init; }

    /// <summary>玩家选择的英雄配置标识。</summary>
    public int HeroId { get; init; }

    /// <summary>当前在线会话；断线后为空，重连时重新绑定。</summary>
    public Session? Session { get; set; }

    /// <summary>开局后对应的服务端帧同步单位。</summary>
    public Unit? Unit { get; set; }
}
