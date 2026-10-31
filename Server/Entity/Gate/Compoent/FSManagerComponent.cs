using Fantasy;
using Fantasy.Gate.Entity;

namespace Entity.Gate.Compoent;

/// <summary>
/// 单个帧同步对局的服务端权威状态。
/// </summary>
public sealed class FSManagerComponent : Fantasy.Entitas.Entity
{
    /// <summary>每秒逻辑帧数。</summary>
    public const int LogicFrameRate = 30;

    /// <summary>一秒包含的毫秒数。</summary>
    public const int MillisecondsPerSecond = 1000;

    /// <summary>服务端已经封存的最新逻辑帧号。</summary>
    public int FrameId { get; set; }

    /// <summary>本局开始前必须加入的玩家人数。</summary>
    public int StartCount { get; set; }

    /// <summary>参与本局帧同步的单位，键为服务端实体标识。</summary>
    public Dictionary<long, Unit> Units { get; } = new();

    /// <summary>下一次逻辑帧定时器标识，零表示尚未调度。</summary>
    public long TimerId { get; set; }

    /// <summary>本次固定频率调度的起始服务器时间，单位为毫秒。</summary>
    public long FrameScheduleStartTime { get; set; }

    /// <summary>当前调度周期已经安排的逻辑帧数量。</summary>
    public int ScheduledFrameCount { get; set; }

    /// <summary>当前尚未封帧的一次性命令容器。</summary>
    public OneFrameCommand? NextCommand { get; set; }

    /// <summary>从第一帧开始保存到对局结束的不可变权威输入历史。</summary>
    public SortedDictionary<int, OneFrameCommand> FrameHistory { get; } = new();

    /// <summary>每位玩家最近一次有效移动命令。</summary>
    public Dictionary<int, FrameCommand> LatestMoveCommands { get; } = new();

    /// <summary>每位玩家最近一次移动命令抵达时间，单位为毫秒。</summary>
    public Dictionary<int, long> LastMoveCommandTimes { get; } = new();

    /// <summary>当前未封帧内每位玩家的一次性技能命令。</summary>
    public Dictionary<int, List<FrameCommand>> PendingSkillCommands { get; } = new();

    /// <summary>每位玩家最后确认已执行的连续帧号，仅用于诊断与重连位置记录。</summary>
    public Dictionary<int, int> LastAckFrameIds { get; } = new();

    /// <summary>每个玩家当前连接内最后处理的输入序号。</summary>
    public Dictionary<int, int> LastInputSequences { get; } = new();

    /// <summary>本局固定且可复现的初始化描述。</summary>
    public FrameMatchDescriptor? MatchDescriptor { get; set; }
}

/// <summary>
/// 对局开始前固定的确定性初始化数据。
/// </summary>
public sealed class FrameMatchDescriptor
{
    /// <summary>本局唯一标识，同时作为房间标识。</summary>
    public int MatchId { get; init; }

    /// <summary>本局确定性随机种子。</summary>
    public int RandomSeed { get; init; }

    /// <summary>本局副本配置标识。</summary>
    public int DungeonId { get; init; }

    /// <summary>本局副本类型枚举值。</summary>
    public int DungeonType { get; init; }

    /// <summary>按稳定玩家标识排序的参与者配置。</summary>
    public IReadOnlyList<FrameMatchPlayerDescriptor> Players { get; init; } = Array.Empty<FrameMatchPlayerDescriptor>();
}

/// <summary>
/// 对局中单个玩家的固定初始化配置。
/// </summary>
public sealed class FrameMatchPlayerDescriptor
{
    /// <summary>认证后的玩家标识。</summary>
    public int PlayerId { get; init; }

    /// <summary>出生点配置标识。</summary>
    public int Position { get; init; }

    /// <summary>本局携带的英雄配置标识列表。</summary>
    public IReadOnlyList<int> HeroIds { get; init; } = Array.Empty<int>();
}
