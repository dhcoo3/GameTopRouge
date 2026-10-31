using System;
using System.Linq;
using AAAGame.ScriptsHotfix.GamePlay.Quest;
using AAAGame.ScriptsHotfix.UI.Core;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>任务日志当前显示的分类。</summary>
    public enum QuestJournalTab
    {
        /// <summary>显示当前开放的主线任务。</summary>
        Main,
        /// <summary>显示当前开放的支线任务。</summary>
        Side,
        /// <summary>显示已经完成的任务。</summary>
        Completed
    }

    /// <summary>保存客户端任务列表选择和按账号隔离的本地追踪偏好。</summary>
    public sealed class QuestModel : ModuleModel<QuestModel>, IModel
    {
        /// <summary>本地追踪偏好的设置键前缀，玩家编号追加在其后。</summary>
        private const string TrackKeyPrefix = "Quest.Track.";
        /// <summary>当前账号追踪的任务编号；无可追踪任务时为 0。</summary>
        public int TrackedQuestId { get; private set; }
        /// <summary>日志中当前选中的任务编号；未选择时为 0。</summary>
        public int SelectedQuestId { get; set; }
        /// <summary>当前任务列表分类。</summary>
        public QuestJournalTab SelectedTab { get; set; }

        /// <summary>校验当前追踪任务并在账号状态变化后恢复默认追踪项。</summary>
        public void ReconcileTrack()
        {
            PlayerProgressSnapshot snapshot = PlayerProgressService.Snapshot;
            if (snapshot == null) { TrackedQuestId = 0; return; }
            string key = TrackKeyPrefix + GF.UserId;
            int saved = int.TryParse(GF.Setting.GetString(key, "0"), out int value) ? value : 0;
            if (saved > 0 && snapshot.GetStatus(saved) == QuestStatus.Active) TrackedQuestId = saved;
            else
            {
                TrackedQuestId = snapshot.Quests.Values.Where(quest => quest.Status == QuestStatus.Active)
                    .Select(quest => quest.QuestId).OrderBy(id => id).FirstOrDefault();
                SaveTrack();
            }
            if (SelectedQuestId == 0 || snapshot.GetStatus(SelectedQuestId) == QuestStatus.Locked)
                SelectedQuestId = TrackedQuestId;
        }

        /// <summary>保存玩家对进行中任务的追踪选择。</summary>
        /// <param name="questId">玩家选择追踪的任务编号。</param>
        public void Track(int questId)
        {
            PlayerProgressSnapshot snapshot = PlayerProgressService.Snapshot;
            if (snapshot == null || snapshot.GetStatus(questId) != QuestStatus.Active) return;
            TrackedQuestId = questId;
            SelectedQuestId = questId;
            SaveTrack();
        }

        /// <summary>按当前账号编号持久化追踪偏好。</summary>
        private void SaveTrack()
        {
            GF.Setting.SetString(TrackKeyPrefix + GF.UserId, TrackedQuestId.ToString());
            GF.Setting.Save();
        }
    }
}
