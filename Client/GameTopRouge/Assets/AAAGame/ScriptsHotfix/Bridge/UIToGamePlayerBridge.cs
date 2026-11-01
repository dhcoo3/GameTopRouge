using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AAAGame.ScriptsHotfix.GamePlay;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Player;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Quest;
using AAAGame.ScriptsHotfix.UI.Module.Login;
using Cysharp.Threading.Tasks;
using Fantasy;
using Fantasy.Network;
using UnityGameFramework.Runtime;
using Log = UnityGameFramework.Runtime.Log;
using GameplayProgressEvent = AAAGame.ScriptsHotfix.GamePlay.Logic.Quest.PlayerProgressEvent;
using GameplayProgressEventType = AAAGame.ScriptsHotfix.GamePlay.Logic.Quest.PlayerProgressEventType;
using GameplayProgressSnapshot = AAAGame.ScriptsHotfix.GamePlay.Logic.Quest.PlayerProgressSnapshot;
using GameplayQuestData = AAAGame.ScriptsHotfix.GamePlay.Logic.Quest.QuestData;
using GameplayProgressException = AAAGame.ScriptsHotfix.GamePlay.Logic.Quest.ProgressServiceException;

namespace AAAGame.ScriptsHotfix.Bridge
{
    /// <summary>UI 层调用玩法功能、读取账号进度及占用玩家输入的唯一入口。</summary>
    public sealed class UIToGamePlayerBridge : Singleton<UIToGamePlayerBridge>
    {
        private Action<PlayerProgressSnapshot> _progressSnapshotChanged;
        private bool _progressServiceSubscribed;

        /// <summary>账号进度确认后通知订阅界面刷新显示。</summary>
        public event Action<PlayerProgressSnapshot> ProgressSnapshotChanged
        {
            add
            {
                if (value == null) return;
                _progressSnapshotChanged += value;
                if (_progressServiceSubscribed) return;
                PlayerProgressService.SnapshotChanged += OnGameplayProgressSnapshotChanged;
                _progressServiceSubscribed = true;
            }
            remove
            {
                _progressSnapshotChanged -= value;
                if (_progressSnapshotChanged != null || !_progressServiceSubscribed) return;
                PlayerProgressService.SnapshotChanged -= OnGameplayProgressSnapshotChanged;
                _progressServiceSubscribed = false;
            }
        }

        /// <summary>当前账号是否已经加载服务端进度。</summary>
        public bool IsProgressLoaded => PlayerProgressService.IsLoaded;

        /// <summary>读取当前账号最近确认的进度副本。</summary>
        public PlayerProgressSnapshot CurrentProgress => ConvertSnapshot(PlayerProgressService.Snapshot);

        /// <summary>是否有已认证的游戏服会话。</summary>
        public bool IsAuthenticated => LoginModel.Instance.IsAuthenticated;

        /// <summary>当前是否有对话、简报或其他本地界面占用玩家输入。</summary>
        public bool IsPlayerInputLocked => GameProxyManger.Instance.GetProxy<InputProxy>()?.IsPlayerInputLocked == true;

        /// <summary>开始单人主线战斗。</summary>
        public void GameStart()
        {
            if (!LoginModel.Instance.IsAuthenticated || GF.UserId <= 0)
                throw new InvalidOperationException("进入主线前必须完成游戏服认证。");
            GF.UI.CloseAllLoadingUIForms();
            GF.UI.CloseAllLoadedUIForms();
            List<PlayerData> players = new List<PlayerData>();
            PlayerData player = PlayerData.Create(GF.UserId, LoginModel.Instance.PlayerName);
            player.HeroIds = new List<int> { 1000001 };
            players.Add(player);
            GamePlayFacade.Instance.Run(false, players, 1, cfg.Fight.DungeonType.Main);
        }

        /// <summary>结束当前玩法会话并清理玩法对象。</summary>
        public void EndGamePlay()
        {
            GamePlayFacade.Instance.End();
        }

        /// <summary>获取一份独立的玩家输入锁，多个界面可安全并行持有。</summary>
        /// <returns>释放时只归还本次申请的输入锁。</returns>
        public IDisposable AcquirePlayerInputLock()
        {
            return GameProxyManger.Instance.GetProxy<InputProxy>()?.AcquirePlayerInputLock();
        }

        /// <summary>异步加载指定地图所需的账号进度。</summary>
        /// <param name="mapId">当前地图编号。</param>
        /// <param name="token">取消本次加载的令牌。</param>
        /// <returns>由玩法进度转换而来的只读界面快照。</returns>
        public async UniTask<PlayerProgressSnapshot> LoadPlayerProgressAsync(int mapId, CancellationToken token = default)
        {
            try { return ConvertSnapshot(await PlayerProgressService.LoadForMapAsync(mapId, token)); }
            catch (GameplayProgressException exception) { throw new ProgressOperationException(exception.ErrorCode, exception.Message); }
        }

        /// <summary>提交一次任务或剧情进度事件。</summary>
        /// <param name="progressEvent">不暴露玩法层类型的进度事件数据。</param>
        /// <param name="token">取消本次提交的令牌。</param>
        /// <returns>服务端确认后的账号进度副本。</returns>
        public async UniTask<PlayerProgressSnapshot> SubmitProgressEventAsync(ProgressEvent progressEvent, CancellationToken token = default)
        {
            if (progressEvent == null) throw new ArgumentNullException(nameof(progressEvent));
            GameplayProgressEvent gameplayEvent = new GameplayProgressEvent
            {
                Type = (GameplayProgressEventType)progressEvent.Type,
                QuestId = progressEvent.QuestId,
                ObjectiveId = progressEvent.ObjectiveId,
                MapId = progressEvent.MapId,
                MapInstanceId = progressEvent.MapInstanceId,
                SourceId = progressEvent.SourceId,
                Count = progressEvent.Count,
                DialogueId = progressEvent.DialogueId,
                DialogueNodeId = progressEvent.DialogueNodeId
            };
            try { return ConvertSnapshot(await PlayerProgressService.SubmitEventAsync(gameplayEvent, token)); }
            catch (GameplayProgressException exception) { throw new ProgressOperationException(exception.ErrorCode, exception.Message); }
        }

        /// <summary>读取已经持久化的布尔剧情标记；未设置标记返回 false。</summary>
        /// <param name="key">稳定的剧情标记名称。</param>
        /// <returns>标记当前值。</returns>
        public bool GetStoryFlag(string key)
        {
            return PlayerProgressService.GetStoryFlag(key);
        }

        /// <summary>加载任务 UI 所需的 Luban 配置副本。</summary>
        /// <param name="token">取消配置等待的令牌。</param>
        /// <returns>供 UI 使用的任务表快照。</returns>
        public async UniTask<QuestDataSnapshot> LoadQuestDataAsync(CancellationToken token = default)
        {
            GameplayQuestData data = await GameplayQuestData.LoadAsync().AttachExternalCancellation(token);
            return new QuestDataSnapshot(data);
        }

        /// <summary>创建主线双人帧同步房间。</summary>
        /// <param name="heroId">创建者选择的英雄编号。</param>
        /// <returns>成功时返回房间编号，否则返回零。</returns>
        public async Task<int> CreateFrameRoomAsync(int heroId)
        {
            if (!LoginModel.Instance.IsAuthenticated) return 0;
            Session session = GF.GameNetwork.Session;
            if (session == null || session.IsDisposed)
            {
                LoginController.Instance.BeginReconnect();
                return 0;
            }
            try
            {
                using var response = await session.C2G_CreateFrameRoom(1, (int)cfg.Fight.DungeonType.Main, heroId);
                if (response.ErrorCode == 0) return response.RoomId;
                Log.Warning("创建帧同步房间失败：{0}", response.ErrorMessage);
            }
            catch (Exception)
            {
                LoginController.Instance.BeginReconnect();
            }
            return 0;
        }

        /// <summary>加入指定双人帧同步房间。</summary>
        /// <param name="roomId">要加入的房间编号。</param>
        /// <param name="heroId">加入者选择的英雄编号。</param>
        /// <returns>服务端确认成功时返回 true。</returns>
        public async Task<bool> JoinFrameRoomAsync(int roomId, int heroId)
        {
            if (!LoginModel.Instance.IsAuthenticated) return false;
            Session session = GF.GameNetwork.Session;
            if (session == null || session.IsDisposed)
            {
                LoginController.Instance.BeginReconnect();
                return false;
            }
            try
            {
                using var response = await session.C2G_JoinFrameRoom(roomId, heroId);
                if (response.ErrorCode == 0) return true;
                Log.Warning("加入帧同步房间失败：{0}", response.ErrorMessage);
            }
            catch (Exception)
            {
                LoginController.Instance.BeginReconnect();
            }
            return false;
        }

        private void OnGameplayProgressSnapshotChanged(GameplayProgressSnapshot snapshot)
        {
            _progressSnapshotChanged?.Invoke(ConvertSnapshot(snapshot));
        }

        private static PlayerProgressSnapshot ConvertSnapshot(GameplayProgressSnapshot snapshot)
        {
            if (snapshot == null) return null;
            PlayerProgressSnapshot converted = new PlayerProgressSnapshot { Revision = snapshot.Revision };
            foreach (KeyValuePair<int, AAAGame.ScriptsHotfix.GamePlay.Logic.Quest.QuestProgressEntry> pair in snapshot.Quests)
            {
                QuestProgressEntry entry = new QuestProgressEntry
                {
                    QuestId = pair.Value.QuestId,
                    Status = (QuestStatus)pair.Value.Status,
                    BriefingAcknowledged = pair.Value.BriefingAcknowledged
                };
                foreach (KeyValuePair<int, int> objective in pair.Value.ObjectiveCounts)
                    entry.ObjectiveCounts.Add(objective.Key, objective.Value);
                converted.Quests.Add(pair.Key, entry);
            }
            foreach (KeyValuePair<string, bool> flag in snapshot.StoryFlags) converted.StoryFlags.Add(flag.Key, flag.Value);
            foreach (KeyValuePair<int, int> item in snapshot.Items) converted.Items.Add(item.Key, item.Value);
            return converted;
        }

        /// <summary>UI 可见的任务状态，数值与进度服务状态一一对应。</summary>
        public enum QuestStatus
        {
            /// <summary>尚未开放。</summary>
            Locked = 0,
            /// <summary>满足条件，可接取。</summary>
            Available = 1,
            /// <summary>进行中。</summary>
            Active = 2,
            /// <summary>已经完成。</summary>
            Completed = 3
        }

        /// <summary>UI 可提交的账号进度事件类型。</summary>
        public enum ProgressEventType
        {
            /// <summary>接取任务。</summary>
            AcceptQuest = 1,
            /// <summary>推进任务目标。</summary>
            ObjectiveProgress = 2,
            /// <summary>确认任务简报。</summary>
            AcknowledgeBriefing = 3,
            /// <summary>提交对话剧情标记。</summary>
            DialogueSetFlag = 4
        }

        /// <summary>账号进度服务的稳定错误编号。</summary>
        public enum ProgressErrorCode : uint
        {
            /// <summary>网络或存储服务暂不可用。</summary>
            StorageUnavailable = 9
        }

        /// <summary>与玩法进度对象分离的账号进度副本。</summary>
        public sealed class PlayerProgressSnapshot
        {
            /// <summary>服务端进度版本号。</summary>
            public long Revision { get; internal set; }
            /// <summary>按任务编号索引的进度。</summary>
            public Dictionary<int, QuestProgressEntry> Quests { get; } = new Dictionary<int, QuestProgressEntry>();
            /// <summary>按名称索引的剧情标记。</summary>
            public Dictionary<string, bool> StoryFlags { get; } = new Dictionary<string, bool>(StringComparer.Ordinal);
            /// <summary>按编号索引的道具数量。</summary>
            public Dictionary<int, int> Items { get; } = new Dictionary<int, int>();
            /// <summary>读取任务状态；未记录的任务视为尚未开放。</summary>
            public QuestStatus GetStatus(int questId) => Quests.TryGetValue(questId, out QuestProgressEntry entry) ? entry.Status : QuestStatus.Locked;
            /// <summary>读取目标进度；未记录的目标返回零。</summary>
            public int GetObjectiveCount(int questId, int objectiveId) => Quests.TryGetValue(questId, out QuestProgressEntry quest) &&
                quest.ObjectiveCounts.TryGetValue(objectiveId, out int count) ? count : 0;
        }

        /// <summary>一个任务的 UI 进度副本。</summary>
        public sealed class QuestProgressEntry
        {
            /// <summary>任务编号。</summary>
            public int QuestId { get; internal set; }
            /// <summary>当前状态。</summary>
            public QuestStatus Status { get; internal set; }
            /// <summary>首次简报是否已确认。</summary>
            public bool BriefingAcknowledged { get; internal set; }
            /// <summary>各目标当前完成数量。</summary>
            public Dictionary<int, int> ObjectiveCounts { get; } = new Dictionary<int, int>();
        }

        /// <summary>由 UI 提交的进度事件载荷。</summary>
        public sealed class ProgressEvent
        {
            /// <summary>事件类型。</summary>
            public ProgressEventType Type;
            /// <summary>任务编号。</summary>
            public int QuestId;
            /// <summary>目标编号。</summary>
            public int ObjectiveId;
            /// <summary>地图编号。</summary>
            public int MapId;
            /// <summary>地图摆放实例编号。</summary>
            public int MapInstanceId;
            /// <summary>事件来源配置编号。</summary>
            public int SourceId;
            /// <summary>推进数量。</summary>
            public int Count;
            /// <summary>关联对话编号。</summary>
            public int DialogueId;
            /// <summary>关联对话节点编号。</summary>
            public int DialogueNodeId;
        }

        /// <summary>进度服务失败的 UI 可见异常。</summary>
        public sealed class ProgressOperationException : Exception
        {
            /// <summary>进度服务返回的稳定错误编号。</summary>
            public uint ErrorCode { get; }

            /// <summary>创建保留服务端错误编号的异常。</summary>
            public ProgressOperationException(uint errorCode, string message) : base(message)
            {
                ErrorCode = errorCode;
            }
        }

        /// <summary>任务和奖励表的 UI 查询快照。</summary>
        public sealed class QuestDataSnapshot
        {
            /// <summary>任务定义配置。</summary>
            public cfg.Quest.TbQuestDefinition Quests { get; }
            /// <summary>任务阶段配置。</summary>
            public cfg.Quest.TbQuestObjectiveGroup Groups { get; }
            /// <summary>任务目标配置。</summary>
            public cfg.Quest.TbQuestObjective Objectives { get; }
            /// <summary>任务奖励配置。</summary>
            public cfg.Quest.TbQuestReward Rewards { get; }
            /// <summary>物品显示配置。</summary>
            public cfg.item.TbItem Items { get; }

            internal QuestDataSnapshot(GameplayQuestData data)
            {
                Quests = data.Quests;
                Groups = data.Groups;
                Objectives = data.Objectives;
                Rewards = data.Rewards;
                Items = data.Items;
            }

            /// <summary>取得按顺序排列的任务阶段。</summary>
            /// <param name="questId">任务编号。</param>
            /// <returns>属于该任务的有序阶段。</returns>
            public cfg.Quest.QuestObjectiveGroup[] GetGroups(int questId) => Groups.DataList
                .Where(value => value.QuestId == questId).OrderBy(value => value.Order).ToArray();

            /// <summary>取得指定阶段的目标。</summary>
            /// <param name="groupId">任务阶段编号。</param>
            /// <returns>属于该阶段的目标。</returns>
            public cfg.Quest.QuestObjective[] GetObjectives(int groupId) => Objectives.DataList
                .Where(value => value.GroupId == groupId).ToArray();

            /// <summary>取得按顺序排列的任务奖励。</summary>
            /// <param name="questId">任务编号。</param>
            /// <returns>属于该任务的有序奖励。</returns>
            public cfg.Quest.QuestReward[] GetRewards(int questId) => Rewards.DataList
                .Where(value => value.QuestId == questId).OrderBy(value => value.Order).ToArray();
        }
    }
}
