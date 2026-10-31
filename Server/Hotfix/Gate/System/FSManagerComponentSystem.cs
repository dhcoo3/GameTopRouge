using Entity.Gate.Compoent;
using Fantasy;
using Fantasy.Gate.Entity;
using Fantasy.Helper;
using Fantasy.Network;

namespace Hotfix.Gate.Component;

/// <summary>
/// 单局帧同步状态的输入收集、定时封帧和历史补取逻辑。
/// </summary>
public static class FSManagerComponentSystem
{
    /// <summary>移动命令类型。</summary>
    private const int MovementCommandType = 1;

    /// <summary>技能命令类型。</summary>
    private const int SkillCommandType = 2;

    /// <summary>客户端归一化输入的整数缩放倍数。</summary>
    private const int InputScale = 10000;

    /// <summary>单位在一个逻辑帧内允许缓存的最多技能命令数。</summary>
    private const int MaxSkillCommandsPerFrame = 16;

    /// <summary>单次缺帧响应最多返回的连续帧数量。</summary>
    private const int MaxFramesPerFetchResponse = 30;

    /// <summary>允许沿用最近移动状态的最长时间，单位为毫秒。</summary>
    private const int MovementInputTimeoutMilliseconds = 300;

    /// <summary>
    /// 固定本局可复现的初始化描述。
    /// </summary>
    /// <param name="self">需要配置的单局帧同步实体。</param>
    /// <param name="descriptor">开局后不可变的对局描述。</param>
    public static void ConfigureMatch(this FSManagerComponent self, FrameMatchDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (self.TimerId != 0 || self.FrameId != 0 || self.Units.Count != 0)
        {
            throw new InvalidOperationException("逻辑帧已开始，不能替换对局描述。");
        }

        if (descriptor.MatchId <= 0 || descriptor.Players.Count == 0)
        {
            throw new ArgumentException("对局描述必须包含有效 MatchId 和至少一名玩家。", nameof(descriptor));
        }

        self.MatchDescriptor = descriptor;
        self.StartCount = descriptor.Players.Count;
    }

    /// <summary>
    /// 将一个房间成员加入本局帧同步状态。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="player">已认证且已绑定会话的玩家单位。</param>
    /// <returns>加入成功时返回 true。</returns>
    public static bool Join(this FSManagerComponent self, Unit player)
    {
        if (self.MatchDescriptor == null || !self.MatchDescriptor.Players.Any(item => item.PlayerId == player.UnitId))
        {
            Log.Error($"玩家不属于当前帧同步对局 {player.UnitId}");
            return false;
        }

        if (self.Units.ContainsKey(player.Id))
        {
            Log.Error($"帧同步单位已存在 {player.Id}");
            return false;
        }

        self.Units.Add(player.Id, player);
        self.LastAckFrameIds[player.UnitId] = 0;
        self.LastInputSequences[player.UnitId] = 0;
        self.Start();
        return true;
    }

    /// <summary>
    /// 在所有固定成员完成绑定后开始 30Hz 逻辑帧。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    public static void Start(this FSManagerComponent self)
    {
        if (self.TimerId != 0 || self.Units.Count != self.StartCount)
        {
            return;
        }

        self.NextCommand = new OneFrameCommand();
        self.FrameScheduleStartTime = TimeHelper.Now;
        self.ScheduledFrameCount = 0;
        self.ScheduleNextLogicFrame();
    }

    /// <summary>
    /// 停止本局定时器，后续不再生成新帧。
    /// </summary>
    /// <param name="self">需要停止的帧同步实体。</param>
    public static void Stop(this FSManagerComponent self)
    {
        if (self.TimerId != 0)
        {
            self.Scene.TimerComponent.Net.Remove(self.TimerId);
        }

        self.TimerId = 0;
        self.FrameScheduleStartTime = 0;
        self.ScheduledFrameCount = 0;
    }

    /// <summary>
    /// 释放本局持有的输入状态和帧历史。
    /// </summary>
    /// <param name="self">需要清理的帧同步实体。</param>
    public static void ClearMatchState(this FSManagerComponent self)
    {
        self.Stop();
        self.Units.Clear();
        self.FrameHistory.Clear();
        self.LatestMoveCommands.Clear();
        self.LastMoveCommandTimes.Clear();
        self.PendingSkillCommands.Clear();
        self.LastAckFrameIds.Clear();
        self.LastInputSequences.Clear();
        self.NextCommand = null;
        self.MatchDescriptor = null;
    }

    /// <summary>
    /// 根据绝对目标时刻调度下一帧，避免固定间隔造成长期频率漂移。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    private static void ScheduleNextLogicFrame(this FSManagerComponent self)
    {
        int scheduledFrameCount = ++self.ScheduledFrameCount;
        long targetTime = self.FrameScheduleStartTime +
                          (long)scheduledFrameCount * FSManagerComponent.MillisecondsPerSecond /
                          FSManagerComponent.LogicFrameRate;
        if (targetTime < TimeHelper.Now)
        {
            targetTime = TimeHelper.Now;
        }

        self.TimerId = self.Scene.TimerComponent.Net.OnceTillTimer(targetTime, self.LogicFrameTimer);
    }

    /// <summary>
    /// 封存当前输入、写入历史并向在线成员广播本局的新权威帧。
    /// </summary>
    /// <param name="self">触发定时器的帧同步实体。</param>
    private static void LogicFrameTimer(this FSManagerComponent self)
    {
        self.TimerId = 0;
        self.DetachDisposedSessions();
        int frameId = ++self.FrameId;
        OneFrameCommand frameCommand = self.NextCommand ?? new OneFrameCommand();
        frameCommand.FrameId = frameId;
        self.AppendFrameCommands(frameCommand);
        OneFrameCommand historyFrame = CopyFrame(frameCommand);
        self.FrameHistory.Add(frameId, historyFrame);
        self.BroadcastLiveFrame(historyFrame);
        self.PendingSkillCommands.Clear();
        self.NextCommand = new OneFrameCommand();
        self.ScheduleNextLogicFrame();
    }

    /// <summary>
    /// 根据连接会话查找所属玩家单位。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="session">发送消息的网络会话。</param>
    /// <param name="unit">找到时返回的房间单位。</param>
    /// <returns>会话属于本局时返回 true。</returns>
    public static bool TryGetUnitBySession(this FSManagerComponent self, Session session, out Unit unit)
    {
        foreach (Unit candidate in self.Units.Values)
        {
            if (candidate.Session == session)
            {
                unit = candidate;
                return true;
            }
        }

        unit = null!;
        return false;
    }

    /// <summary>
    /// 重新绑定同一玩家的新会话并重置该连接的输入序号。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="playerId">已认证的玩家标识。</param>
    /// <param name="session">重连后建立的新会话。</param>
    /// <param name="unit">重绑成功时返回的单位。</param>
    /// <returns>本局存在该玩家时返回 true。</returns>
    public static bool TryRebindSession(this FSManagerComponent self, int playerId, Session session, out Unit unit)
    {
        foreach (Unit candidate in self.Units.Values)
        {
            if (candidate.UnitId != playerId)
            {
                continue;
            }

            candidate.Session = session;
            self.LastInputSequences[playerId] = 0;
            unit = candidate;
            self.SendMatchBootstrap(unit);
            return true;
        }

        unit = null!;
        return false;
    }

    /// <summary>
    /// 记录客户端已完成模拟的连续帧号，确认只能向前推进。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="unit">发送确认的玩家单位。</param>
    /// <param name="ackFrameId">客户端最后实际执行的连续帧号。</param>
    public static void AcknowledgeFrame(this FSManagerComponent self, Unit unit, int ackFrameId)
    {
        if (ackFrameId < 0 || ackFrameId > self.FrameId)
        {
            return;
        }

        if (!self.LastAckFrameIds.TryGetValue(unit.UnitId, out int previousAck) || ackFrameId > previousAck)
        {
            self.LastAckFrameIds[unit.UnitId] = ackFrameId;
        }
    }

    /// <summary>
    /// 接收一个客户端输入包，并将其写入当前尚未封帧的输入状态。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="unit">经会话确认的发送者单位。</param>
    /// <param name="ackFrameId">客户端最后已执行的连续帧号。</param>
    /// <param name="inputSequence">连接内单调递增的输入序号。</param>
    /// <param name="commands">客户端提交的移动和技能命令。</param>
    public static void InputFrameCommands(this FSManagerComponent self, Unit unit, int ackFrameId, int inputSequence,
        List<FrameCommand> commands)
    {
        self.AcknowledgeFrame(unit, ackFrameId);
        if (inputSequence <= 0 || self.LastInputSequences.TryGetValue(unit.UnitId, out int lastInputSequence) &&
            inputSequence <= lastInputSequence)
        {
            return;
        }

        self.LastInputSequences[unit.UnitId] = inputSequence;
        if (commands == null)
        {
            return;
        }

        foreach (FrameCommand command in commands)
        {
            if (command == null)
            {
                continue;
            }

            if (command.CommandType == MovementCommandType)
            {
                if (command.x < -InputScale || command.x > InputScale || command.y < -InputScale || command.y > InputScale)
                {
                    continue;
                }

                self.LatestMoveCommands[unit.UnitId] = CreateCommand(unit.UnitId, MovementCommandType, command.x, command.y);
                self.LastMoveCommandTimes[unit.UnitId] = TimeHelper.Now;
                continue;
            }

            if (command.CommandType != SkillCommandType)
            {
                continue;
            }

            if (!self.PendingSkillCommands.TryGetValue(unit.UnitId, out List<FrameCommand>? skills))
            {
                skills = new List<FrameCommand>();
                self.PendingSkillCommands.Add(unit.UnitId, skills);
            }

            if (skills.Count < MaxSkillCommandsPerFrame)
            {
                skills.Add(CreateCommand(unit.UnitId, SkillCommandType, command.x, command.y));
            }
        }
    }

    /// <summary>
    /// 将每个参与者的移动状态和当前帧技能按稳定顺序写入权威帧。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="frameCommand">正在封存的权威帧。</param>
    private static void AppendFrameCommands(this FSManagerComponent self, OneFrameCommand frameCommand)
    {
        List<Unit> units = self.Units.Values.OrderBy(unit => unit.UnitId).ToList();
        long now = TimeHelper.Now;
        foreach (Unit unit in units)
        {
            int playerId = unit.UnitId;
            frameCommand.UnitFrameCommand.Add(self.GetCurrentMoveCommand(unit, now));
            if (!self.PendingSkillCommands.TryGetValue(playerId, out List<FrameCommand>? skills))
            {
                continue;
            }

            foreach (FrameCommand skill in skills)
            {
                frameCommand.UnitFrameCommand.Add(skill);
            }
        }
    }

    /// <summary>
    /// 获取指定玩家本帧需要执行的移动命令。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="unit">需要计算移动状态的玩家单位。</param>
    /// <param name="now">当前服务端毫秒时间。</param>
    /// <returns>服务端归属玩家标识后的移动命令。</returns>
    private static FrameCommand GetCurrentMoveCommand(this FSManagerComponent self, Unit unit, long now)
    {
        int playerId = unit.UnitId;
        if (unit.Session == null || unit.Session.IsDisposed ||
            !self.LatestMoveCommands.TryGetValue(playerId, out FrameCommand? moveCommand) ||
            !self.LastMoveCommandTimes.TryGetValue(playerId, out long lastMoveTime) ||
            now - lastMoveTime > MovementInputTimeoutMilliseconds)
        {
            return CreateCommand(playerId, MovementCommandType, 0, 0);
        }

        return moveCommand;
    }

    /// <summary>
    /// 发送本局刚封存的一帧给全部在线成员。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="frame">已写入历史的不可变权威帧。</param>
    private static void BroadcastLiveFrame(this FSManagerComponent self, OneFrameCommand frame)
    {
        foreach (Unit unit in self.Units.Values)
        {
            Session? session = unit.Session;
            if (session == null || session.IsDisposed)
            {
                continue;
            }

            using G2C_FrameCommand message = G2C_FrameCommand.Create();
            message.FrameId = self.FrameId;
            message.Commands.Add(CopyFrame(frame));
            session.Send(message);
        }
    }

    /// <summary>
    /// 向指定会话返回从起始帧开始的一段连续历史帧。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="unit">请求补帧的已认证单位。</param>
    /// <param name="startFrameId">需要补取的第一帧。</param>
    public static void SendFrameHistory(this FSManagerComponent self, Unit unit, int startFrameId)
    {
        Session? session = unit.Session;
        if (session == null || session.IsDisposed || startFrameId < 1 || startFrameId > self.FrameId)
        {
            return;
        }

        using G2C_FrameCommand message = G2C_FrameCommand.Create();
        message.FrameId = self.FrameId;
        int endFrameId = Math.Min(self.FrameId, startFrameId + MaxFramesPerFetchResponse - 1);
        for (int frameId = startFrameId; frameId <= endFrameId; frameId++)
        {
            if (!self.FrameHistory.TryGetValue(frameId, out OneFrameCommand? frame))
            {
                Log.Error($"对局 {self.MatchDescriptor?.MatchId} 缺少历史帧 {frameId}");
                return;
            }

            message.Commands.Add(CopyFrame(frame));
        }

        session.Send(message);
    }

    /// <summary>
    /// 向指定成员下发不可变的对局初始化描述。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    /// <param name="unit">接收描述的在线单位。</param>
    public static void SendMatchBootstrap(this FSManagerComponent self, Unit unit)
    {
        Session? session = unit.Session;
        FrameMatchDescriptor? descriptor = self.MatchDescriptor;
        if (session == null || session.IsDisposed || descriptor == null)
        {
            return;
        }

        using G2C_FrameMatchBootstrap message = G2C_FrameMatchBootstrap.Create();
        message.MatchId = descriptor.MatchId;
        message.RandomSeed = descriptor.RandomSeed;
        message.DungeonId = descriptor.DungeonId;
        message.DungeonType = descriptor.DungeonType;
        message.CurrentFrameId = self.FrameId;
        foreach (FrameMatchPlayerDescriptor player in descriptor.Players.OrderBy(item => item.PlayerId))
        {
            FrameMatchPlayer networkPlayer = FrameMatchPlayer.Create();
            networkPlayer.PlayerId = player.PlayerId;
            networkPlayer.Position = player.Position;
            networkPlayer.HeroIds.AddRange(player.HeroIds);
            message.Players.Add(networkPlayer);
        }

        session.Send(message);
    }

    /// <summary>
    /// 清除已经释放的会话引用，等待同一玩家重新登录绑定。
    /// </summary>
    /// <param name="self">目标对局的帧同步实体。</param>
    private static void DetachDisposedSessions(this FSManagerComponent self)
    {
        foreach (Unit unit in self.Units.Values)
        {
            if (unit.Session != null && unit.Session.IsDisposed)
            {
                unit.Session = null;
            }
        }
    }

    /// <summary>
    /// 创建服务端拥有的帧命令，禁止保留客户端协议对象引用。
    /// </summary>
    /// <param name="unitId">命令归属玩家标识。</param>
    /// <param name="commandType">命令类型。</param>
    /// <param name="x">命令横向参数。</param>
    /// <param name="y">命令纵向参数。</param>
    /// <returns>独立可保存的命令对象。</returns>
    private static FrameCommand CreateCommand(int unitId, int commandType, int x, int y)
    {
        return new FrameCommand { Uid = unitId, CommandType = commandType, x = x, y = y };
    }

    /// <summary>
    /// 深拷贝权威帧，确保网络发送和历史帧不共享可变对象。
    /// </summary>
    /// <param name="source">需要复制的权威帧。</param>
    /// <returns>可独立修改或发送的深拷贝。</returns>
    private static OneFrameCommand CopyFrame(OneFrameCommand source)
    {
        OneFrameCommand copy = new() { FrameId = source.FrameId };
        foreach (FrameCommand command in source.UnitFrameCommand)
        {
            copy.UnitFrameCommand.Add(new FrameCommand
            {
                Uid = command.Uid,
                CommandType = command.CommandType,
                x = command.x,
                y = command.y,
                z = command.z,
            });
        }

        return copy;
    }
}
