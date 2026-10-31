using System;
using System.Linq;
using cfg.Quest;
using Cysharp.Threading.Tasks;
using ItemTables = cfg.item;

namespace AAAGame.ScriptsHotfix.GamePlay.Quest
{
    /// <summary>集中加载本地 Luban 任务表，并提供按任务和目标组查询配置的接口。</summary>
    public sealed class QuestData
    {
        /// <summary>任务定义表。</summary>
        public TbQuestDefinition Quests { get; private set; }
        /// <summary>任务目标阶段表。</summary>
        public TbQuestObjectiveGroup Groups { get; private set; }
        /// <summary>任务目标表。</summary>
        public TbQuestObjective Objectives { get; private set; }
        /// <summary>任务奖励表。</summary>
        public TbQuestReward Rewards { get; private set; }
        /// <summary>物品定义表，用于显示奖励名称。</summary>
        public ItemTables.TbItem Items { get; private set; }
        /// <summary>已经加载并验证的数据表集合。</summary>
        private static QuestData _instance;
        /// <summary>正在进行的共享异步加载任务。</summary>
        private static UniTask<QuestData> _loading;
        /// <summary>读取当前已经加载的数据；未加载时返回空值。</summary>
        public static QuestData Loaded => _instance;

        /// <summary>共享加载客户端任务、目标、奖励及物品数据表。</summary>
        /// <returns>成功加载并检查完整性的任务配置集合。</returns>
        public static UniTask<QuestData> LoadAsync()
        {
            if (_instance != null) return UniTask.FromResult(_instance);
            if (_loading.Status == UniTaskStatus.Pending) return _loading;
            _loading = LoadInternalAsync();
            return _loading;
        }

        /// <summary>异步读取所有任务相关 Luban 表并建立缓存。</summary>
        /// <returns>完整的任务配置集合；必要数据缺失时抛出异常。</returns>
        private static async UniTask<QuestData> LoadInternalAsync()
        {
            var data = new QuestData
            {
                Quests = await GF.LuBan.Get<TbQuestDefinition>(cfg.Tables.quest_tbquestdefinition),
                Groups = await GF.LuBan.Get<TbQuestObjectiveGroup>(cfg.Tables.quest_tbquestobjectivegroup),
                Objectives = await GF.LuBan.Get<TbQuestObjective>(cfg.Tables.quest_tbquestobjective),
                Rewards = await GF.LuBan.Get<TbQuestReward>(cfg.Tables.quest_tbquestreward),
                Items = await GF.LuBan.Get<ItemTables.TbItem>(cfg.Tables.item_tbitem)
            };
            if (data.Quests == null || data.Groups == null || data.Objectives == null || data.Rewards == null || data.Items == null)
                throw new InvalidOperationException("Quest tables are unavailable.");
            _instance = data;
            return data;
        }

        /// <summary>取得指定任务按阶段顺序排列的目标组。</summary>
        /// <param name="questId">任务配置编号。</param>
        /// <returns>按阶段顺序排列的目标组副本。</returns>
        public QuestObjectiveGroup[] GetGroups(int questId) => Groups.DataList.Where(value => value.QuestId == questId)
            .OrderBy(value => value.Order).ToArray();

        /// <summary>取得指定目标组中的目标配置。</summary>
        /// <param name="groupId">目标组配置编号。</param>
        /// <returns>属于该目标组的目标数组。</returns>
        public QuestObjective[] GetObjectives(int groupId) => Objectives.DataList.Where(value => value.GroupId == groupId).ToArray();

        /// <summary>取得指定任务按发放顺序排列的奖励。</summary>
        /// <param name="questId">任务配置编号。</param>
        /// <returns>按顺序排列的奖励配置数组。</returns>
        public QuestReward[] GetRewards(int questId) => Rewards.DataList.Where(value => value.QuestId == questId)
            .OrderBy(value => value.Order).ToArray();
    }
}
