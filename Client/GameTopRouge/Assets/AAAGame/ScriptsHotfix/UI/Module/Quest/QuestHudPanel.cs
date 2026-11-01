using System.Linq;
using AAAGame.ScriptsHotfix.Bridge;
using Cysharp.Threading.Tasks;
using cfg.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QuestStatus = AAAGame.ScriptsHotfix.Bridge.UIToGamePlayerBridge.QuestStatus;
using PlayerProgressSnapshot = AAAGame.ScriptsHotfix.Bridge.UIToGamePlayerBridge.PlayerProgressSnapshot;
using QuestProgressEntry = AAAGame.ScriptsHotfix.Bridge.UIToGamePlayerBridge.QuestProgressEntry;
using QuestDataSnapshot = AAAGame.ScriptsHotfix.Bridge.UIToGamePlayerBridge.QuestDataSnapshot;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>地图中的当前追踪目标与任务记录快捷入口。</summary>
    public sealed class QuestHudPanel : UGF_Panel
    {
        #region Auto Create
        /// <summary>当前任务标题和追踪目标文本控件。</summary>
        private TMP_Text m_Text;
        /// <summary>打开任务日志的按钮。</summary>
        private UGF_Button m_JournalButton;
        /// <summary>打开任务日志按钮的本地化文本。</summary>
        private TMP_Text m_JournalLabel;
        #endregion
        /// <summary>当前 HUD 使用的任务数据表快照。</summary>
        private QuestDataSnapshot _data;
        /// <summary>Prefab 控件是否已成功绑定。</summary>
        private bool _built;

        /// <summary>为本次打开绑定交互。</summary>
        protected override void RegisterEvent()
        {
            m_JournalButton.SetClickListener(OpenJournal);
        }

        /// <summary>从面板容器绑定显示和交互控件。</summary>
        protected override void RegisterUI()
        {
            #region Auto Bind
            m_Text = PanelUIContainer.Get<TMP_Text>("m_Text");
            m_JournalButton = PanelUIContainer.Get<UGF_Button>("m_JournalButton");
            m_JournalLabel = PanelUIContainer.Get<TMP_Text>("m_JournalLabel");
                _built = true;
            #endregion
        }

        /// <summary>每次打开 HUD 时注册快捷按钮并恢复进度更新订阅。</summary>
        /// <param name="userData">UI 框架传入的表单数据。</param>
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_JournalLabel.text = GF.Localization.GetText("Quest.UI.Title");
            if (!_built) return;

            UIToGamePlayerBridge.Instance.ProgressSnapshotChanged -= OnSnapshotChanged;
            UIToGamePlayerBridge.Instance.ProgressSnapshotChanged += OnSnapshotChanged;
            Refresh();
        }

        /// <summary>绑定地图流程已经加载的任务配置。</summary>
        /// <param name="data">任务 HUD 读取标题和目标时使用的数据表集合。</param>
        internal void Bind(QuestDataSnapshot data)
        {
            _data = data;
        }

        /// <summary>根据当前追踪偏好和账号快照刷新 HUD 文本。</summary>
        internal void Refresh()
        {
            if (m_Text == null || _data == null) return;
            QuestModel.Instance.ReconcileTrack();
            PlayerProgressSnapshot snapshot = UIToGamePlayerBridge.Instance.CurrentProgress;
            cfg.Quest.QuestDefinition quest = _data.Quests.GetOrDefault(QuestModel.Instance.TrackedQuestId);
            if (quest == null || snapshot == null || snapshot.GetStatus(quest.Id) != QuestStatus.Active)
            {
                QuestProgressEntry next = snapshot?.Quests.Values
                    .Where(value => value.Status == QuestStatus.Active)
                    .OrderBy(value => value.QuestId)
                    .FirstOrDefault();
                quest = next == null ? null : _data.Quests.GetOrDefault(next.QuestId);
            }
            if (quest == null)
            {
                m_Text.text = string.Empty;
                return;
            }

            QuestObjectiveGroup group = _data.GetGroups(quest.Id).FirstOrDefault(value =>
                _data.GetObjectives(value.Id).Count(objective =>
                    snapshot.GetObjectiveCount(quest.Id, objective.Id) >= objective.TargetCount) < value.RequiredObjectiveCount);
            QuestObjective objective = group == null ? null : _data.GetObjectives(group.Id)
                .FirstOrDefault(value => snapshot.GetObjectiveCount(quest.Id, value.Id) < value.TargetCount);
            string objectiveText = objective == null
                ? GF.Localization.GetText("Quest.UI.NoObjectives")
                : GF.Localization.GetText(objective.DescriptionKey);
            m_Text.text = GF.Localization.GetText(quest.TitleKey) + "\n" + objectiveText;
        }

        /// <summary>请求打开任务日志；对话期间忽略快捷键。</summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J) && !UIToGamePlayerBridge.Instance.IsPlayerInputLocked)
                QuestView.Instance.OpenJournalAsync().Forget(Debug.LogException);
        }

        /// <summary>打开任务日志的按钮操作。</summary>
        private void OpenJournal()
        {
            QuestView.Instance.OpenJournalAsync().Forget(Debug.LogException);
        }

        /// <summary>进度服务通知账号快照发生变化时重新渲染。</summary>
        /// <param name="snapshot">服务端返回的最新账号进度快照。</param>
        private void OnSnapshotChanged(PlayerProgressSnapshot snapshot)
        {
            Refresh();
        }

        /// <summary>关闭 HUD 时解除进度事件订阅并释放控制器引用。</summary>
        /// <param name="isShutdown">是否由 UI 系统关闭整个应用。</param>
        /// <param name="userData">UI 框架传入的关闭数据。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            UIToGamePlayerBridge.Instance.ProgressSnapshotChanged -= OnSnapshotChanged;
            QuestController.Instance.DetachHud(this);
            base.OnClose(isShutdown, userData);
        }
    }
}
