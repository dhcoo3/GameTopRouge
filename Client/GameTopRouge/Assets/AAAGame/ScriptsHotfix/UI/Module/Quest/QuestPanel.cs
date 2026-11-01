using System;
using System.Linq;
using AAAGame.ScriptsHotfix.Bridge;
using cfg.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using QuestStatus = AAAGame.ScriptsHotfix.Bridge.UIToGamePlayerBridge.QuestStatus;
using PlayerProgressSnapshot = AAAGame.ScriptsHotfix.Bridge.UIToGamePlayerBridge.PlayerProgressSnapshot;
using QuestDataSnapshot = AAAGame.ScriptsHotfix.Bridge.UIToGamePlayerBridge.QuestDataSnapshot;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>主线、支线与已完成任务的记录面板。</summary>
    public sealed class QuestPanel : UGF_Panel
    {
        /// <summary>负责任务日志用例与导航的控制器。</summary>
        private QuestController _controller;
        /// <summary>保存当前分类、选中项及追踪偏好的任务模型。</summary>
        private QuestModel _model;
        /// <summary>用于渲染日志内容的任务配置快照。</summary>
        private QuestDataSnapshot _data;
        /// <summary>本面板拥有的玩家输入锁。</summary>
        private IDisposable _inputLock;
        #region Auto Create
        /// <summary>任务列表滚动内容容器。</summary>
        private QuestListView m_QuestList;
        /// <summary>任务日志标题文本。</summary>
        private TMP_Text m_Header;
        /// <summary>关闭按钮文本。</summary>
        private TMP_Text m_CloseLabel;
        /// <summary>主线分类按钮文本。</summary>
        private TMP_Text m_MainTabLabel;
        /// <summary>支线分类按钮文本。</summary>
        private TMP_Text m_SideTabLabel;
        /// <summary>已完成分类按钮文本。</summary>
        private TMP_Text m_CompletedTabLabel;
        /// <summary>选中任务的标题控件。</summary>
        private TMP_Text m_DetailTitle;
        /// <summary>选中任务的说明控件。</summary>
        private TMP_Text m_Description;
        /// <summary>选中任务的目标列表控件。</summary>
        private TMP_Text m_Objectives;
        /// <summary>选中任务的状态控件。</summary>
        private TMP_Text m_Status;
        /// <summary>选中任务的奖励列表控件。</summary>
        private TMP_Text m_Reward;
        /// <summary>将选中任务设为当前追踪任务的按钮。</summary>
        private UGF_Button m_TrackButton;
        /// <summary>追踪按钮文本。</summary>
        private TMP_Text m_TrackLabel;
        /// <summary>关闭按钮。</summary>
        private UGF_Button m_CloseButton;
        /// <summary>主线分类按钮。</summary>
        private UGF_Button m_MainTabButton;
        /// <summary>支线分类按钮。</summary>
        private UGF_Button m_SideTabButton;
        /// <summary>已完成分类按钮。</summary>
        private UGF_Button m_CompletedTabButton;
        #endregion
        /// <summary>Prefab 控件是否已成功绑定。</summary>
        private bool _built;
        /// <summary>当前分类中用于键盘上下选择的任务编号。</summary>
        private int[] _visibleQuestIds = System.Array.Empty<int>();

        /// <summary>为本次打开绑定交互。</summary>
        protected override void RegisterEvent()
        {
            m_QuestList.Selected -= OnQuestSelected;
            m_QuestList.Selected += OnQuestSelected;
            m_CloseButton.SetClickListener(CloseJournal);
            m_MainTabButton.SetClickListener(OnMainTabClicked);
            m_SideTabButton.SetClickListener(OnSideTabClicked);
            m_CompletedTabButton.SetClickListener(OnCompletedTabClicked);
            m_TrackButton.SetClickListener(OnTrackClicked);
        }

        /// <summary>从面板容器绑定显示和交互控件。</summary>
        protected override void RegisterUI()
        {
            #region Auto Bind
            m_QuestList = PanelUIContainer.Get<QuestListView>("m_QuestList");
            m_Header = PanelUIContainer.Get<TMP_Text>("m_Header");
            m_CloseLabel = PanelUIContainer.Get<TMP_Text>("m_CloseLabel");
            m_MainTabLabel = PanelUIContainer.Get<TMP_Text>("m_MainTabLabel");
            m_SideTabLabel = PanelUIContainer.Get<TMP_Text>("m_SideTabLabel");
            m_CompletedTabLabel = PanelUIContainer.Get<TMP_Text>("m_CompletedTabLabel");
            m_DetailTitle = PanelUIContainer.Get<TMP_Text>("m_DetailTitle");
            m_Description = PanelUIContainer.Get<TMP_Text>("m_Description");
            m_Objectives = PanelUIContainer.Get<TMP_Text>("m_Objectives");
            m_Status = PanelUIContainer.Get<TMP_Text>("m_Status");
            m_Reward = PanelUIContainer.Get<TMP_Text>("m_Reward");
            m_TrackButton = PanelUIContainer.Get<UGF_Button>("m_TrackButton");
            m_TrackLabel = PanelUIContainer.Get<TMP_Text>("m_TrackLabel");
            m_CloseButton = PanelUIContainer.Get<UGF_Button>("m_CloseButton");
            m_MainTabButton = PanelUIContainer.Get<UGF_Button>("m_MainTabButton");
            m_SideTabButton = PanelUIContainer.Get<UGF_Button>("m_SideTabButton");
            m_CompletedTabButton = PanelUIContainer.Get<UGF_Button>("m_CompletedTabButton");
            _built = true;
            #endregion
        }

        /// <summary>为当前打开的任务日志注册交互并刷新分类状态。</summary>
        /// <param name="userData">UI 框架传入的表单数据。</param>
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_Header.text = GF.Localization.GetText("Quest.UI.Title");
            m_CloseLabel.text = "×";
            m_MainTabLabel.text = GF.Localization.GetText("Quest.UI.Main");
            m_SideTabLabel.text = GF.Localization.GetText("Quest.UI.Side");
            m_CompletedTabLabel.text = GF.Localization.GetText("Quest.UI.Completed");
            if (!_built) return;

        }

        /// <summary>绑定控制器、模型和配置后刷新日志内容并占用地图输入。</summary>
        /// <param name="controller">本任务模块的控制器。</param>
        /// <param name="model">本任务模块的显示状态模型。</param>
        /// <param name="data">已加载的任务配置快照。</param>
        internal void Bind(QuestController controller, QuestModel model, QuestDataSnapshot data)
        {
            _controller = controller;
            _model = model;
            _data = data;
            UIToGamePlayerBridge.Instance.ProgressSnapshotChanged -= OnSnapshotChanged;
            UIToGamePlayerBridge.Instance.ProgressSnapshotChanged += OnSnapshotChanged;
            QuestModel.Instance.ReconcileTrack();
            Refresh();
            _inputLock?.Dispose();
            _inputLock = UIToGamePlayerBridge.Instance.AcquirePlayerInputLock();
        }

        /// <summary>根据账号进度重建当前分类列表和详情。</summary>
        internal void Refresh() => RefreshList(false);

        private void RefreshList(bool resetPosition)
        {
            if (!_built || _data == null || _model == null) return;
            PlayerProgressSnapshot snapshot = UIToGamePlayerBridge.Instance.CurrentProgress;
            if (snapshot == null)
            {
                _visibleQuestIds = System.Array.Empty<int>();
                _model.SelectedQuestId = 0;
                m_QuestList.SetItems(System.Array.Empty<QuestListItem>(), 0, true);
                RenderDetail();
                return;
            }
            var rows = _data.Quests.DataList.Where(quest =>
            {
                QuestStatus status = snapshot.GetStatus(quest.Id);
                if (_model.SelectedTab == QuestJournalTab.Completed) return status == QuestStatus.Completed;
                if (status == QuestStatus.Completed) return false;
                if (status == QuestStatus.Locked) return false;
                return _model.SelectedTab == QuestJournalTab.Main ? quest.Category == Category.Main : quest.Category == Category.Side;
            }).OrderBy(quest => quest.SortOrder).ToArray();
            _visibleQuestIds = rows.Select(value => value.Id).ToArray();
            if (!_visibleQuestIds.Contains(_model.SelectedQuestId))
                _model.SelectedQuestId = rows.Length == 0 ? 0 : rows[0].Id;
            var items = rows.Select(quest => new QuestListItem(quest.Id, quest.TitleKey,
                QuestConst.Text.GetStatusKey(snapshot.GetStatus(quest.Id)))).ToArray();
            m_QuestList.SetItems(items, _model.SelectedQuestId, resetPosition);
            RenderDetail();
        }

        private void OnQuestSelected(int questId) => SelectQuest(questId, false);

        /// <summary>选择任务仅更新详情和可见行样式。</summary>
        private void SelectQuest(int questId, bool ensureVisible)
        {
            _model.SelectedQuestId = questId;
            m_QuestList.SetSelected(questId, ensureVisible);
            RenderDetail();
        }

        /// <summary>账号进度快照变化时刷新任务列表和当前详情。</summary>
        /// <param name="snapshot">服务端返回的最新账号进度快照。</param>
        private void OnSnapshotChanged(PlayerProgressSnapshot snapshot)
        {
            Refresh();
        }

        /// <summary>渲染当前任务标题、说明、目标进度与奖励。</summary>
        private void RenderDetail()
        {
            PlayerProgressSnapshot snapshot = UIToGamePlayerBridge.Instance.CurrentProgress;
            cfg.Quest.QuestDefinition quest = _data.Quests.GetOrDefault(_model.SelectedQuestId);
            if (snapshot == null || quest == null || snapshot.GetStatus(quest.Id) == QuestStatus.Locked)
            {
                m_DetailTitle.text = GF.Localization.GetText("Quest.UI.NoObjectives");
                m_Description.text = string.Empty;
                m_Objectives.text = string.Empty;
                m_Status.text = string.Empty;
                m_Reward.text = string.Empty;
                m_TrackButton.gameObject.SetActive(false);
                return;
            }
            m_DetailTitle.text = GF.Localization.GetText(quest.TitleKey);
            m_Description.text = GF.Localization.GetText(quest.DescriptionKey) +
                (string.IsNullOrEmpty(quest.BriefingTextKey) ? string.Empty : "\n\n" + GF.Localization.GetText(quest.BriefingTextKey));
            m_Status.text = GF.Localization.GetText(QuestConst.Text.GetStatusKey(snapshot.GetStatus(quest.Id)));
            m_TrackButton.gameObject.SetActive(snapshot.GetStatus(quest.Id) == QuestStatus.Active);
            m_TrackLabel.text = quest.Id == _model.TrackedQuestId
                ? GF.Localization.GetText("Quest.UI.Tracked") : GF.Localization.GetText("Quest.UI.Track");
            var lines = _data.GetGroups(quest.Id).SelectMany(group => _data.GetObjectives(group.Id).Select(objective =>
            {
                int count = snapshot.GetObjectiveCount(quest.Id, objective.Id);
                bool complete = count >= objective.TargetCount;
                string label = complete && !string.IsNullOrEmpty(objective.CompletionTextKey)
                    ? GF.Localization.GetText(objective.CompletionTextKey)
                    : GF.Localization.GetText(objective.DescriptionKey) + $"  {count}/{objective.TargetCount}";
                return (complete ? "✓  " : "•  ") + label;
            })).ToArray();
            m_Objectives.text = string.Join("\n\n", lines);
            var rewards = _data.GetRewards(quest.Id);
            m_Reward.text = rewards.Length == 0 ? string.Empty : "\n" + string.Join("\n", rewards.Select(value =>
                $"{_data.Items.GetOrDefault(value.ItemId)?.Name ?? value.ItemId.ToString()}  ×{value.Count}"));
        }

        /// <summary>关闭任务日志面板。</summary>
        private void CloseJournal()
        {
            _controller?.CloseJournal();
        }

        /// <summary>切换到主线任务分类。</summary>
        private void OnMainTabClicked()
        {
            SetTab(QuestJournalTab.Main);
        }

        /// <summary>切换到支线任务分类。</summary>
        private void OnSideTabClicked()
        {
            SetTab(QuestJournalTab.Side);
        }

        /// <summary>切换到已完成任务分类。</summary>
        private void OnCompletedTabClicked()
        {
            SetTab(QuestJournalTab.Completed);
        }

        /// <summary>将当前选中任务设为追踪目标。</summary>
        private void OnTrackClicked()
        {
            _controller?.TrackQuest(_model?.SelectedQuestId ?? 0);
        }

        /// <summary>切换任务分类并选择新列表中的首项。</summary>
        /// <param name="tab">玩家切换到的任务分类。</param>
        private void SetTab(QuestJournalTab tab)
        {
            _model.SelectedTab = tab;
            _model.SelectedQuestId = 0;
            RefreshList(true);
        }
        /// <summary>处理日志关闭、键盘选择和追踪快捷输入。</summary>
        private void Update()
        {
            if (!_built || _controller == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) _controller.CloseJournal();
            if (_visibleQuestIds.Length == 0) return;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                int current = System.Array.IndexOf(_visibleQuestIds, _model.SelectedQuestId);
                if (current < 0) current = 0;
                int direction = Input.GetKeyDown(KeyCode.DownArrow) ? 1 : -1;
                int next = (current + direction + _visibleQuestIds.Length) % _visibleQuestIds.Length;
                SelectQuest(_visibleQuestIds[next], true);
            }
            else if (Input.GetKeyDown(KeyCode.Return) && _model.SelectedQuestId > 0)
            {
                if (UIToGamePlayerBridge.Instance.CurrentProgress?.GetStatus(_model.SelectedQuestId) == QuestStatus.Active)
                    _controller.TrackQuest(_model.SelectedQuestId);
            }
        }

        /// <summary>关闭任务日志时解除地图输入锁并释放表单资源。</summary>
        /// <param name="isShutdown">是否由 UI 系统关闭整个应用。</param>
        /// <param name="userData">UI 框架传入的关闭数据。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            m_QuestList.Clear();
            UIToGamePlayerBridge.Instance.ProgressSnapshotChanged -= OnSnapshotChanged;
            _controller?.DetachJournal(this);
            _inputLock?.Dispose();
            _inputLock = null;
            base.OnClose(isShutdown, userData);
        }
    }
}
