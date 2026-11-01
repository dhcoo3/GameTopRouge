using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AAAGame.ScriptsHotfix.GameNetwork;
using Cysharp.Threading.Tasks;
using Fantasy;
using cfg.Quest;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Quest
{
    /// <summary>玩家进度 RPC 使用的稳定错误编号。</summary>
    public enum ProgressErrorCode : uint
    {
        /// <summary>请求成功。</summary>
        Success = 0,
        /// <summary>当前网络会话未认证。</summary>
        NotAuthenticated = 1,
        /// <summary>请求字段或请求编号无效。</summary>
        InvalidRequest = 2,
        /// <summary>任务尚未开放。</summary>
        QuestLocked = 3,
        /// <summary>任务当前不处于进行中状态。</summary>
        QuestNotActive = 4,
        /// <summary>目标事件来源与配置不匹配。</summary>
        InvalidEventSource = 5,
        /// <summary>目标所属阶段尚未开放。</summary>
        InvalidObjectiveOrder = 6,
        /// <summary>服务器任务配置无效。</summary>
        InvalidContentConfig = 7,
        /// <summary>账号进度并发更新冲突。</summary>
        RevisionConflict = 8,
        /// <summary>网络或存储服务暂时不可用。</summary>
        StorageUnavailable = 9
    }

    /// <summary>客户端能够提交给进度服务的状态变更类型。</summary>
    public enum PlayerProgressEventType
    {
        /// <summary>通过配置中的委托 NPC 接取任务。</summary>
        AcceptQuest = 1,
        /// <summary>提交一次目标交互或击杀进度。</summary>
        ObjectiveProgress = 2,
        /// <summary>确认首次任务简报。</summary>
        AcknowledgeBriefing = 3,
        /// <summary>提交对话配置中指定的剧情标记节点。</summary>
        DialogueSetFlag = 4
    }

    /// <summary>服务端返回的单个任务进度。</summary>
    public sealed class QuestProgressEntry
    {
        /// <summary>任务配置编号。</summary>
        public int QuestId { get; internal set; }
        /// <summary>当前任务状态。</summary>
        public QuestStatus Status { get; internal set; }
        /// <summary>首次简报是否已由玩家确认。</summary>
        public bool BriefingAcknowledged { get; internal set; }
        /// <summary>按目标编号保存的已完成数量。</summary>
        public Dictionary<int, int> ObjectiveCounts { get; } = new Dictionary<int, int>();
    }

    /// <summary>当前账号从服务端加载的任务、剧情标记和物品快照。</summary>
    public sealed class PlayerProgressSnapshot
    {
        /// <summary>用于识别账号进度先后顺序的版本号。</summary>
        public long Revision { get; internal set; }
        /// <summary>按任务编号索引的任务进度。</summary>
        public Dictionary<int, QuestProgressEntry> Quests { get; } = new Dictionary<int, QuestProgressEntry>();
        /// <summary>按稳定名称索引的剧情布尔标记。</summary>
        public Dictionary<string, bool> StoryFlags { get; } = new Dictionary<string, bool>(StringComparer.Ordinal);
        /// <summary>按物品编号索引的道具堆叠数量。</summary>
        public Dictionary<int, int> Items { get; } = new Dictionary<int, int>();

        /// <summary>读取任务状态；账号快照中不存在的任务视为未开放。</summary>
        /// <param name="questId">任务配置编号。</param>
        /// <returns>任务当前状态。</returns>
        public QuestStatus GetStatus(int questId) => Quests.TryGetValue(questId, out var value) ? value.Status : QuestStatus.Locked;

        /// <summary>读取目标完成数量；未记录的目标数量为零。</summary>
        /// <param name="questId">任务配置编号。</param>
        /// <param name="objectiveId">目标配置编号。</param>
        /// <returns>当前目标进度数量。</returns>
        public int GetObjectiveCount(int questId, int objectiveId) => Quests.TryGetValue(questId, out var quest) &&
            quest.ObjectiveCounts.TryGetValue(objectiveId, out int count) ? count : 0;
    }

    /// <summary>账号任务状态，与服务器进度协议中的数值保持一致。</summary>
    public enum QuestStatus
    {
        /// <summary>尚未达到开放条件。</summary>
        Locked = 0,
        /// <summary>满足条件，等待玩家接取。</summary>
        Available = 1,
        /// <summary>任务正在进行。</summary>
        Active = 2,
        /// <summary>所有配置目标均已完成。</summary>
        Completed = 3
    }

    /// <summary>提交给账号进度服务的一次状态变更请求。</summary>
    public sealed class PlayerProgressEvent
    {
        /// <summary>本次状态变更类型。</summary>
        public PlayerProgressEventType Type;
        /// <summary>关联的任务编号；剧情标记事件为零。</summary>
        public int QuestId;
        /// <summary>关联的任务目标编号。</summary>
        public int ObjectiveId;
        /// <summary>事件发生的地图编号。</summary>
        public int MapId;
        /// <summary>事件来源的地图摆放实例编号。</summary>
        public int MapInstanceId;
        /// <summary>NPC、地图物件或敌人配置编号。</summary>
        public int SourceId;
        /// <summary>本次事件报告的数量。</summary>
        public int Count;
        /// <summary>关联的对话配置编号。</summary>
        public int DialogueId;
        /// <summary>关联的对话节点编号。</summary>
        public int DialogueNodeId;
    }

    /// <summary>服务端拒绝进度操作或进度服务不可用时抛出的异常。</summary>
    public sealed class ProgressServiceException : Exception
    {
        /// <summary>服务器返回的稳定错误编号。</summary>
        public uint ErrorCode { get; }

        /// <summary>创建携带服务端错误编号的进度异常。</summary>
        /// <param name="errorCode">服务器返回的稳定错误编号。</param>
        /// <param name="message">便于定位问题的错误描述。</param>
        public ProgressServiceException(uint errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>客户端账号进度入口。快照只接受服务端响应，不做任务状态乐观更新。</summary>
    public static class PlayerProgressService
    {
        /// <summary>每次只允许一个账号进度写入或读取操作占用请求序列。</summary>
        private const int MaximumConcurrentMutations = 1;
        /// <summary>串行化进度加载和变更，避免客户端产生乱序写入。</summary>
        private static readonly SemaphoreSlim MutationGate = new SemaphoreSlim(MaximumConcurrentMutations, MaximumConcurrentMutations);
        /// <summary>保护当前玩家编号与进度快照的同步锁。</summary>
        private static readonly object Sync = new object();
        /// <summary>最近一次由服务端确认的账号进度快照。</summary>
        private static PlayerProgressSnapshot _snapshot;
        /// <summary>当前快照所属的玩家编号，防止账号切换时复用数据。</summary>
        private static int _snapshotPlayerId;
        /// <summary>等待服务端确认的幂等请求，网络超时后用于重试原事件。</summary>
        private static PendingSubmission _pending;

        /// <summary>服务端确认新进度快照后触发；监听方应只刷新表现，不修改快照。</summary>
        public static event Action<PlayerProgressSnapshot> SnapshotChanged;

        /// <summary>读取当前已认证账号的进度快照；未加载或账号切换时返回空值。</summary>
        public static PlayerProgressSnapshot Snapshot { get { lock (Sync) return _snapshotPlayerId == GF.UserId ? _snapshot : null; } }

        /// <summary>当前已认证账号是否已经成功加载进度。</summary>
        public static bool IsLoaded { get { lock (Sync) return _snapshot != null && _snapshotPlayerId == GF.UserId; } }

        /// <summary>读取已加载快照中的剧情标记；缺少标记或进度时按 false 处理。</summary>
        /// <param name="key">剧情配置使用的稳定标记键。</param>
        /// <returns>标记当前值；账号尚未加载时返回 false。</returns>
        public static bool GetStoryFlag(string key)
        {
            lock (Sync) return _snapshot != null && _snapshotPlayerId == GF.UserId &&
                _snapshot.StoryFlags.TryGetValue(key ?? string.Empty, out bool value) && value;
        }

        /// <summary>进入地图时向服务端加载账号进度并更新自动接取状态。</summary>
        /// <param name="mapId">当前地图配置编号，必须为正数。</param>
        /// <param name="token">地图离开时取消等待的令牌。</param>
        /// <returns>服务端确认的账号进度快照。</returns>
        public static async UniTask<PlayerProgressSnapshot> LoadForMapAsync(int mapId, CancellationToken token = default)
        {
            if (mapId <= 0) throw new ArgumentOutOfRangeException(nameof(mapId));
            await MutationGate.WaitAsync(token);
            try
            {
                await QuestData.LoadAsync().AttachExternalCancellation(token);
                if (GF.GameNetwork?.Session == null || GF.GameNetwork.Session.IsDisposed)
                    throw new ProgressServiceException((uint)ProgressErrorCode.StorageUnavailable, "Progress network session is not connected.");
                var session = GF.GameNetwork.Session;
                using (C2G_LoadPlayerProgress request = C2G_LoadPlayerProgress.Create())
                {
                    request.MapId = mapId;
                    G2C_LoadPlayerProgress rpcResponse;
                    try
                    {
                        rpcResponse = (G2C_LoadPlayerProgress)await session.Call(request);
                    }
                    catch (Exception) when (session.IsDisposed || !ReferenceEquals(GF.GameNetwork?.Session, session))
                    {
                        throw new ProgressServiceException((uint)ProgressErrorCode.StorageUnavailable,
                            "Progress request was interrupted because the network session closed.");
                    }

                    using (G2C_LoadPlayerProgress response = rpcResponse)
                    {
                        if (session.IsDisposed || !ReferenceEquals(GF.GameNetwork?.Session, session))
                            throw new ProgressServiceException((uint)ProgressErrorCode.StorageUnavailable,
                                "Progress response arrived after the network session closed.");
                        if (response.ErrorCode != (uint)ProgressErrorCode.Success) throw CreateException(response.ErrorCode, response.ErrorMessage);
                        PlayerProgressSnapshot snapshot = Copy(response);
                        ApplySnapshot(snapshot);
                        _pending = null;
                        return snapshot;
                    }
                }
            }
            finally { MutationGate.Release(); }
        }

        /// <summary>串行提交任务、简报或剧情标记事件，并使用同一请求编号处理重试。</summary>
        /// <param name="progressEvent">需要服务端校验并应用的进度事件。</param>
        /// <param name="token">地图离开或会话关闭时取消等待的令牌。</param>
        /// <returns>服务端确认变更后的完整账号快照。</returns>
        public static async UniTask<PlayerProgressSnapshot> SubmitEventAsync(PlayerProgressEvent progressEvent, CancellationToken token = default)
        {
            if (progressEvent == null) throw new ArgumentNullException(nameof(progressEvent));
            await MutationGate.WaitAsync(token);
            try
            {
                if (!IsLoaded) throw new ProgressServiceException((uint)ProgressErrorCode.StorageUnavailable, "Load player progress before submitting changes.");
                if (_pending != null && !_pending.Matches(progressEvent))
                    throw new ProgressServiceException((uint)ProgressErrorCode.RevisionConflict, "A prior progress request is awaiting confirmation. Reload the account snapshot first.");
                _pending ??= new PendingSubmission(Guid.NewGuid().ToString("N"), progressEvent);
                token.ThrowIfCancellationRequested();
                if (GF.GameNetwork?.Session == null || GF.GameNetwork.Session.IsDisposed)
                    throw new ProgressServiceException((uint)ProgressErrorCode.StorageUnavailable, "Progress network session is not connected.");
                using (C2G_SubmitPlayerProgressEvent request = C2G_SubmitPlayerProgressEvent.Create())
                {
                    request.RequestId = _pending.RequestId;
                    request.EventType = (int)progressEvent.Type;
                    request.QuestId = progressEvent.QuestId;
                    request.ObjectiveId = progressEvent.ObjectiveId;
                    request.MapId = progressEvent.MapId;
                    request.MapInstanceId = progressEvent.MapInstanceId;
                    request.SourceId = progressEvent.SourceId;
                    request.Count = progressEvent.Count;
                    request.DialogueId = progressEvent.DialogueId;
                    request.DialogueNodeId = progressEvent.DialogueNodeId;
                    using (G2C_SubmitPlayerProgressEvent response = (G2C_SubmitPlayerProgressEvent)await GF.GameNetwork.Session.Call(request))
                    {
                        if (response.ErrorCode != (uint)ProgressErrorCode.Success) throw CreateException(response.ErrorCode, response.ErrorMessage);
                        PlayerProgressSnapshot snapshot = Copy(response);
                        ApplySnapshot(snapshot);
                        _pending = null;
                        return snapshot;
                    }
                }
            }
            finally { MutationGate.Release(); }
        }

        /// <summary>查找当前进行中任务中与 NPC 对话匹配的首个未完成目标。</summary>
        /// <param name="mapId">NPC 所在地图编号。</param>
        /// <param name="npcId">NPC 配置编号。</param>
        /// <param name="dialogueId">NPC 当前绑定的对话编号。</param>
        /// <returns>匹配目标；没有可推进目标时返回空值。</returns>
        public static QuestObjective FindActiveDialogueObjective(int mapId, int npcId, int dialogueId)
            => FindActiveDialogueObjective(mapId, npcId, dialogueId, out _);

        /// <summary>查找 NPC 对话目标并返回该目标所属任务编号。</summary>
        /// <param name="mapId">NPC 所在地图编号。</param>
        /// <param name="npcId">NPC 配置编号。</param>
        /// <param name="dialogueId">NPC 当前绑定的对话编号。</param>
        /// <param name="questId">目标所属任务编号；未找到时为 0。</param>
        /// <returns>匹配目标；没有可推进目标时返回空值。</returns>
        public static QuestObjective FindActiveDialogueObjective(int mapId, int npcId, int dialogueId, out int questId)
        {
            questId = 0;
            PlayerProgressSnapshot snapshot = Snapshot;
            if (snapshot == null) return null;
            QuestData data = QuestData.Loaded;
            if (data == null) return null;
            foreach (QuestProgressEntry progress in snapshot.Quests.Values.Where(value => value.Status == QuestStatus.Active))
            {
                QuestObjectiveGroup group = CurrentGroup(data, progress);
                if (group == null) continue;
                QuestObjective match = data.GetObjectives(group.Id).FirstOrDefault(value => value.Type == cfg.Quest.ObjectiveType.CompleteDialogue &&
                    value.SourceMapId == mapId && value.SourceNpcId == npcId && value.DialogueId == dialogueId &&
                    progress.ObjectiveCounts.GetValueOrDefault(value.Id) < value.TargetCount);
                if (match != null) { questId = progress.QuestId; return match; }
            }
            return null;
        }

        /// <summary>检查指定目标是否处于当前开放阶段且尚未完成。</summary>
        /// <param name="objectiveId">目标配置编号。</param>
        /// <param name="questId">目标所属任务编号；不可用时为 0。</param>
        /// <returns>该目标当前可以交互时返回真。</returns>
        public static bool IsObjectiveActive(int objectiveId, out int questId)
        {
            questId = 0;
            PlayerProgressSnapshot snapshot = Snapshot;
            if (snapshot == null) return false;
            QuestData data = QuestData.Loaded;
            if (data == null) return false;
            QuestObjective objective = data.Objectives.GetOrDefault(objectiveId);
            if (objective == null) return false;
            QuestObjectiveGroup group = data.Groups.GetOrDefault(objective.GroupId);
            if (group == null) return false;
            if (!snapshot.Quests.TryGetValue(group.QuestId, out QuestProgressEntry progress) || progress.Status != QuestStatus.Active) return false;
            QuestObjectiveGroup currentGroup = CurrentGroup(data, progress);
            if (currentGroup?.Id != group.Id || progress.ObjectiveCounts.GetValueOrDefault(objectiveId) >= objective.TargetCount) return false;
            questId = group.QuestId;
            return true;
        }

        /// <summary>检查配置中是否存在指定 NPC 对话目标。</summary>
        /// <param name="mapId">目标配置的来源地图编号。</param>
        /// <param name="npcId">目标配置的来源 NPC 编号。</param>
        /// <param name="dialogueId">目标配置绑定的对话编号。</param>
        /// <returns>存在匹配目标时返回真。</returns>
        public static bool HasDialogueObjective(int mapId, int npcId, int dialogueId)
        {
            QuestData data = QuestData.Loaded;
            return data != null && data.Objectives.DataList.Any(value => value.Type == cfg.Quest.ObjectiveType.CompleteDialogue &&
                value.SourceMapId == mapId && value.SourceNpcId == npcId && value.DialogueId == dialogueId);
        }

        /// <summary>检查 NPC 对话是否用于推进任务或接取委托。</summary>
        /// <param name="mapId">NPC 所在地图编号。</param>
        /// <param name="npcId">NPC 配置编号。</param>
        /// <param name="dialogueId">NPC 绑定的对话编号。</param>
        /// <returns>该对话属于任务交互时返回真。</returns>
        public static bool HasQuestDialogueInteraction(int mapId, int npcId, int dialogueId)
        {
            QuestData data = QuestData.Loaded;
            if (data == null) return false;
            return HasDialogueObjective(mapId, npcId, dialogueId) || data.Quests.DataList.Any(value =>
                value.Activation == cfg.Quest.Activation.AcceptFromNpc && value.AcceptNpcId == npcId &&
                (value.StartMapId == 0 || value.StartMapId == mapId));
        }

        /// <summary>查找当前 NPC 可接取且排序最靠前的任务。</summary>
        /// <param name="mapId">NPC 所在地图编号。</param>
        /// <param name="npcId">任务配置中的委托 NPC 编号。</param>
        /// <returns>可接取任务编号；没有可接任务时返回 0。</returns>
        public static int FindAcceptableQuestForNpc(int mapId, int npcId)
        {
            PlayerProgressSnapshot snapshot = Snapshot;
            QuestData data = QuestData.Loaded;
            if (snapshot == null || data == null) return 0;
            return data.Quests.DataList.Where(value => value.Activation == cfg.Quest.Activation.AcceptFromNpc &&
                    value.AcceptNpcId == npcId && (value.StartMapId == 0 || value.StartMapId == mapId) &&
                    snapshot.GetStatus(value.Id) == QuestStatus.Available)
                .OrderBy(value => value.SortOrder).Select(value => value.Id).FirstOrDefault();
        }

        /// <summary>读取指定任务当前尚未完成的最早目标组。</summary>
        /// <param name="data">当前加载的任务配置。</param>
        /// <param name="progress">指定任务的账号进度。</param>
        /// <returns>尚未满足完成数量的首个目标组；全部完成时返回空值。</returns>
        private static QuestObjectiveGroup CurrentGroup(QuestData data, QuestProgressEntry progress)
        {
            foreach (QuestObjectiveGroup group in data.GetGroups(progress.QuestId))
            {
                QuestObjective[] objectives = data.GetObjectives(group.Id);
                int completed = objectives.Count(value => progress.ObjectiveCounts.GetValueOrDefault(value.Id) >= value.TargetCount);
                if (completed < group.RequiredObjectiveCount) return group;
            }
            return null;
        }

        /// <summary>将进度加载响应复制为客户端只读快照对象。</summary>
        /// <param name="response">服务端返回的账号进度加载响应。</param>
        /// <returns>脱离协议对象生命周期的进度快照。</returns>
        private static PlayerProgressSnapshot Copy(G2C_LoadPlayerProgress response)
        {
            var result = new PlayerProgressSnapshot { Revision = response.Revision };
            foreach (Fantasy.PlayerQuestProgress source in response.Quests)
            {
                var quest = new QuestProgressEntry { QuestId = source.QuestId, Status = (QuestStatus)source.Status,
                    BriefingAcknowledged = source.BriefingAcknowledged };
                foreach (Fantasy.PlayerObjectiveProgress objective in source.Objectives)
                    quest.ObjectiveCounts[objective.ObjectiveId] = objective.Count;
                result.Quests[quest.QuestId] = quest;
            }
            foreach (Fantasy.PlayerStoryFlag flag in response.StoryFlags) result.StoryFlags[flag.Key] = flag.Value;
            foreach (Fantasy.PlayerItemStack item in response.Items) result.Items[item.ItemId] = item.Count;
            return result;
        }

        /// <summary>将进度变更响应复制为客户端只读快照对象。</summary>
        /// <param name="response">服务端返回的进度变更响应。</param>
        /// <returns>脱离协议对象生命周期的进度快照。</returns>
        private static PlayerProgressSnapshot Copy(G2C_SubmitPlayerProgressEvent response)
        {
            var result = new PlayerProgressSnapshot { Revision = response.Revision };
            foreach (Fantasy.PlayerQuestProgress source in response.Quests)
            {
                var quest = new QuestProgressEntry { QuestId = source.QuestId, Status = (QuestStatus)source.Status,
                    BriefingAcknowledged = source.BriefingAcknowledged };
                foreach (Fantasy.PlayerObjectiveProgress objective in source.Objectives)
                    quest.ObjectiveCounts[objective.ObjectiveId] = objective.Count;
                result.Quests[quest.QuestId] = quest;
            }
            foreach (Fantasy.PlayerStoryFlag flag in response.StoryFlags) result.StoryFlags[flag.Key] = flag.Value;
            foreach (Fantasy.PlayerItemStack item in response.Items) result.Items[item.ItemId] = item.Count;
            return result;
        }

        /// <summary>按版本顺序替换当前账号快照并通知表现层刷新。</summary>
        /// <param name="snapshot">从服务端复制出的进度快照。</param>
        private static void ApplySnapshot(PlayerProgressSnapshot snapshot)
        {
            lock (Sync)
            {
                if (_snapshot != null && _snapshotPlayerId == GF.UserId && snapshot.Revision < _snapshot.Revision) return;
                _snapshot = snapshot;
                _snapshotPlayerId = GF.UserId;
            }
            Action<PlayerProgressSnapshot> listeners = SnapshotChanged;
            if (listeners == null) return;
            foreach (Action<PlayerProgressSnapshot> listener in listeners.GetInvocationList())
            {
                try
                {
                    listener(snapshot);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }
        }

        /// <summary>将服务器错误码和消息封装为稳定的客户端进度异常。</summary>
        /// <param name="code">服务端返回的错误编号。</param>
        /// <param name="message">服务端返回的诊断说明。</param>
        /// <returns>携带错误码及可读诊断文本的异常对象。</returns>
        private static ProgressServiceException CreateException(uint code, string message)
        {
            if (code > (uint)ProgressErrorCode.StorageUnavailable) code = (uint)ProgressErrorCode.StorageUnavailable;
            return new ProgressServiceException(code, string.IsNullOrWhiteSpace(message) ? "Player progress request failed." : message);
        }

        /// <summary>保留尚未确认的变更和幂等请求号，以便使用相同内容安全重试。</summary>
        private sealed class PendingSubmission
        {
            /// <summary>服务端用于识别重复请求的唯一编号。</summary>
            public readonly string RequestId;
            /// <summary>创建请求时保存的不可变事件副本。</summary>
            private readonly PlayerProgressEvent _event;

            /// <summary>复制事件字段并关联服务端幂等编号。</summary>
            /// <param name="id">本次请求的 GUID 字符串。</param>
            /// <param name="value">需要保存副本的进度事件。</param>
            public PendingSubmission(string id, PlayerProgressEvent value)
            {
                RequestId = id;
                _event = CopyEvent(value);
            }

            /// <summary>判断新事件是否与等待确认的请求内容完全一致。</summary>
            /// <param name="other">准备提交或重试的事件。</param>
            /// <returns>事件字段一致时返回真。</returns>
            public bool Matches(PlayerProgressEvent other) => _event.Type == other.Type && _event.QuestId == other.QuestId &&
                _event.ObjectiveId == other.ObjectiveId && _event.MapId == other.MapId && _event.MapInstanceId == other.MapInstanceId &&
                _event.SourceId == other.SourceId && _event.Count == other.Count && _event.DialogueId == other.DialogueId &&
                _event.DialogueNodeId == other.DialogueNodeId;

            /// <summary>复制提交事件，避免调用方随后修改请求内容。</summary>
            /// <param name="value">调用方提供的进度事件。</param>
            /// <returns>字段值相同的独立事件对象。</returns>
            private static PlayerProgressEvent CopyEvent(PlayerProgressEvent value) => new PlayerProgressEvent
            {
                Type = value.Type, QuestId = value.QuestId, ObjectiveId = value.ObjectiveId, MapId = value.MapId,
                MapInstanceId = value.MapInstanceId, SourceId = value.SourceId, Count = value.Count,
                DialogueId = value.DialogueId, DialogueNodeId = value.DialogueNodeId
            };
        }
    }
}
