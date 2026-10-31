using System.Linq;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Quest;
using cfg.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>主线、支线与已完成任务的记录面板。</summary>
    public sealed class QuestPanel : UGF_Panel
    {
        /// <summary>面板使用的旧铜色装饰色。</summary>
        private static readonly Color Copper = new Color32(154, 113, 57, 255);
        /// <summary>面板正文使用的暖灰文字色。</summary>
        private static readonly Color WarmText = new Color32(225, 213, 188, 255);
        /// <summary>负责任务日志用例与导航的控制器。</summary>
        private QuestController _controller;
        /// <summary>保存当前分类、选中项及追踪偏好的任务模型。</summary>
        private QuestModel _model;
        /// <summary>用于渲染日志内容的任务配置快照。</summary>
        private QuestData _data;
        /// <summary>任务列表滚动内容容器。</summary>
        private RectTransform _list;
        /// <summary>选中任务的标题控件。</summary>
        private TMP_Text _detailTitle;
        /// <summary>选中任务的说明控件。</summary>
        private TMP_Text _description;
        /// <summary>选中任务的目标列表控件。</summary>
        private TMP_Text _objectives;
        /// <summary>选中任务的状态控件。</summary>
        private TMP_Text _status;
        /// <summary>选中任务的奖励列表控件。</summary>
        private TMP_Text _reward;
        /// <summary>将选中任务设为当前追踪任务的按钮。</summary>
        private Button _trackButton;
        /// <summary>标记运行时 UI 层级是否已经创建。</summary>
        private bool _built;
        /// <summary>当前分类中用于键盘上下选择的任务编号。</summary>
        private int[] _visibleQuestIds = System.Array.Empty<int>();

        /// <summary>首次打开表单时创建任务日志控件。</summary>
        /// <param name="userData">UI 框架传入的表单数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (!_built) Build();
        }

        /// <summary>绑定控制器、模型和配置后刷新日志内容并占用地图输入。</summary>
        /// <param name="controller">本任务模块的控制器。</param>
        /// <param name="model">本任务模块的显示状态模型。</param>
        /// <param name="data">已加载的任务配置快照。</param>
        internal void Bind(QuestController controller, QuestModel model, QuestData data)
        {
            _controller = controller;
            _model = model;
            _data = data;
            PlayerProgressService.SnapshotChanged -= OnSnapshotChanged;
            PlayerProgressService.SnapshotChanged += OnSnapshotChanged;
            QuestModel.Instance.ReconcileTrack();
            Refresh();
            GameProxyManger.Instance.GetProxy<InputProxy>()?.SetDialogueInputBlocked(true);
        }

        /// <summary>根据账号进度重建当前分类列表和详情。</summary>
        internal void Refresh()
        {
            if (!_built || _data == null || _model == null) return;
            QuestUiFactory.ClearChildren(_list);
            PlayerProgressSnapshot snapshot = PlayerProgressService.Snapshot;
            if (snapshot == null) return;
            var rows = _data.Quests.DataList.Where(quest =>
            {
                QuestStatus status = snapshot.GetStatus(quest.Id);
                if (_model.SelectedTab == QuestJournalTab.Completed) return status == QuestStatus.Completed;
                if (status == QuestStatus.Completed) return false;
                if (status == QuestStatus.Locked) return false;
                return _model.SelectedTab == QuestJournalTab.Main ? quest.Category == Category.Main : quest.Category == Category.Side;
            }).OrderBy(quest => quest.SortOrder).ToArray();
            _visibleQuestIds = rows.Select(value => value.Id).ToArray();
            if (rows.Length > 0 && !_visibleQuestIds.Contains(_model.SelectedQuestId))
                _model.SelectedQuestId = rows[0].Id;
            for (int i = 0; i < rows.Length; i++)
            {
                cfg.Quest.QuestDefinition quest = rows[i];
                QuestStatus status = snapshot.GetStatus(quest.Id);
                var button = QuestUiFactory.Button("QuestRow_" + quest.Id, _list,
                    QuestUiFactory.Localize(quest.TitleKey), 22, new Vector2(390, 58), new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(4, -6 - i * 66), () => SelectQuest(quest.Id));
                button.targetGraphic.color = quest.Id == _model.SelectedQuestId ? new Color32(119, 91, 51, 255) : new Color32(48, 43, 36, 250);
                QuestUiFactory.Text("QuestRowStatus_" + quest.Id, button.transform,
                    QuestConst.Text.GetStatus(status), 15, new Color32(196, 174, 135, 255), new Vector2(130, 34),
                    new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), TextAlignmentOptions.Right);
            }
            RenderDetail();
            SelectRow(_model.SelectedQuestId);
        }

        /// <summary>选中任务并重绘当前列表和详情。</summary>
        /// <param name="questId">新选中的任务编号。</param>
        private void SelectQuest(int questId)
        {
            _model.SelectedQuestId = questId;
            Refresh();
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
            PlayerProgressSnapshot snapshot = PlayerProgressService.Snapshot;
            cfg.Quest.QuestDefinition quest = _data.Quests.GetOrDefault(_model.SelectedQuestId);
            if (snapshot == null || quest == null || snapshot.GetStatus(quest.Id) == QuestStatus.Locked)
            {
                _detailTitle.text = QuestConst.Text.NoObjectives;
                _description.text = string.Empty;
                _objectives.text = string.Empty;
                _status.text = string.Empty;
                _reward.text = string.Empty;
                _trackButton.gameObject.SetActive(false);
                return;
            }
            _detailTitle.text = QuestUiFactory.Localize(quest.TitleKey);
            _description.text = QuestUiFactory.Localize(quest.DescriptionKey) +
                (string.IsNullOrEmpty(quest.BriefingTextKey) ? string.Empty : "\n\n" + QuestUiFactory.Localize(quest.BriefingTextKey));
            _status.text = QuestConst.Text.GetStatus(snapshot.GetStatus(quest.Id));
            _trackButton.gameObject.SetActive(snapshot.GetStatus(quest.Id) == QuestStatus.Active);
            _trackButton.GetComponentInChildren<TMP_Text>().text = quest.Id == _model.TrackedQuestId
                ? QuestConst.Text.Tracked : QuestConst.Text.Track;
            var lines = _data.GetGroups(quest.Id).SelectMany(group => _data.GetObjectives(group.Id).Select(objective =>
            {
                int count = snapshot.GetObjectiveCount(quest.Id, objective.Id);
                bool complete = count >= objective.TargetCount;
                string label = complete && !string.IsNullOrEmpty(objective.CompletionTextKey)
                    ? QuestUiFactory.Localize(objective.CompletionTextKey)
                    : QuestUiFactory.Localize(objective.DescriptionKey) + $"  {count}/{objective.TargetCount}";
                return (complete ? "✓  " : "•  ") + label;
            })).ToArray();
            _objectives.text = string.Join("\n\n", lines);
            var rewards = _data.GetRewards(quest.Id);
            _reward.text = rewards.Length == 0 ? string.Empty : "\n" + string.Join("\n", rewards.Select(value =>
                $"{_data.Items.GetOrDefault(value.ItemId)?.Name ?? value.ItemId.ToString()}  ×{value.Count}"));
        }

        /// <summary>构建任务日志的背景、分类、列表、详情和操作按钮。</summary>
        private void Build()
        {
            _built = true;
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var shade = QuestUiFactory.Image("QuestShade", transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0, 0, 0, 0.78f));
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = Vector2.one; shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            var frame = QuestUiFactory.Image("QuestFrame", transform, new Vector2(1450, 840), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color32(19, 19, 18, 250));
            Border(frame.rectTransform, new Vector2(1450, 840));
            QuestUiFactory.Text("QuestHeader", frame.transform, QuestConst.Text.Title, 32, new Color32(232, 218, 188, 255), new Vector2(900, 70),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -28), TextAlignmentOptions.Left);
            QuestUiFactory.Button("QuestClose", frame.transform, "×", 30, new Vector2(58, 58), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -24), () => _controller?.CloseJournal());
            var tabs = QuestUiFactory.Rect("QuestTabs", frame.transform, new Vector2(440, 56), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -112));
            QuestUiFactory.Button("QuestTabMain", tabs, QuestConst.Text.Main, 20, new Vector2(135, 50), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, () => SetTab(QuestJournalTab.Main));
            QuestUiFactory.Button("QuestTabSide", tabs, QuestConst.Text.Side, 20, new Vector2(135, 50), new Vector2(0, 1), new Vector2(0, 1), new Vector2(145, 0), () => SetTab(QuestJournalTab.Side));
            QuestUiFactory.Button("QuestTabCompleted", tabs, QuestConst.Text.Completed, 20, new Vector2(150, 50), new Vector2(0, 1), new Vector2(0, 1), new Vector2(290, 0), () => SetTab(QuestJournalTab.Completed));
            var viewport = QuestUiFactory.Image("QuestListViewport", frame.transform, new Vector2(450, 610), new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -180), new Color32(27, 27, 25, 240));
            var viewportRect = viewport.rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            _list = QuestUiFactory.Rect("QuestList", viewport.transform, new Vector2(430, 1200), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -8));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect; scroll.content = _list; scroll.vertical = true; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            QuestUiFactory.Image("QuestDivider", frame.transform, new Vector2(2, 630), new Vector2(0, 1), new Vector2(0, 1), new Vector2(500, -172), Copper);
            _detailTitle = QuestUiFactory.Text("QuestDetailTitle", frame.transform, string.Empty, 31, new Color32(232, 218, 188, 255), new Vector2(860, 56), new Vector2(0, 1), new Vector2(0, 1), new Vector2(548, -118));
            _status = QuestUiFactory.Text("QuestStatus", frame.transform, string.Empty, 19, Copper, new Vector2(320, 40), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-34, -126), TextAlignmentOptions.Right);
            _description = QuestUiFactory.Text("QuestDescription", frame.transform, string.Empty, 20, WarmText, new Vector2(850, 112), new Vector2(0, 1), new Vector2(0, 1), new Vector2(548, -190));
            _objectives = QuestUiFactory.Text("QuestObjectives", frame.transform, string.Empty, 21, WarmText, new Vector2(850, 405), new Vector2(0, 1), new Vector2(0, 1), new Vector2(548, -322));
            _reward = QuestUiFactory.Text("QuestRewards", frame.transform, string.Empty, 18, new Color32(210, 176, 115, 255), new Vector2(620, 64), new Vector2(0, 0), new Vector2(0, 0), new Vector2(548, 78));
            _trackButton = QuestUiFactory.Button("QuestTrack", frame.transform, QuestConst.Text.Track, 20, new Vector2(220, 55),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 42), () => _controller?.TrackQuest(_model?.SelectedQuestId ?? 0));
            GameProxyManger.Instance.GetProxy<InputProxy>()?.SetDialogueInputBlocked(true);
        }

        /// <summary>为任务面板绘制四边旧铜色细框。</summary>
        /// <param name="parent">承载边框图像的矩形变换。</param>
        /// <param name="size">父面板的宽高。</param>
        private void Border(RectTransform parent, Vector2 size)
        {
            QuestUiFactory.Image("BorderTop", parent, new Vector2(size.x - 4, 2), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), Copper);
            QuestUiFactory.Image("BorderBottom", parent, new Vector2(size.x - 4, 2), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 2), Copper);
            QuestUiFactory.Image("BorderLeft", parent, new Vector2(2, size.y - 4), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(2, 0), Copper);
            QuestUiFactory.Image("BorderRight", parent, new Vector2(2, size.y - 4), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-2, 0), Copper);
        }

        /// <summary>切换任务分类并选择新列表中的首项。</summary>
        /// <param name="tab">玩家切换到的任务分类。</param>
        private void SetTab(QuestJournalTab tab)
        {
            _model.SelectedTab = tab;
            _model.SelectedQuestId = 0;
            Refresh();
            SelectRow(_model.SelectedQuestId);
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
                SelectQuest(_visibleQuestIds[next]);
            }
            else if (Input.GetKeyDown(KeyCode.Return) && _model.SelectedQuestId > 0)
            {
                if (PlayerProgressService.Snapshot?.GetStatus(_model.SelectedQuestId) == QuestStatus.Active)
                    _controller.TrackQuest(_model.SelectedQuestId);
            }
        }

        /// <summary>将当前选择同步到 Unity 事件系统焦点。</summary>
        /// <param name="questId">需要聚焦的任务编号。</param>
        private void SelectRow(int questId)
        {
            if (questId <= 0 || _list == null) return;
            for (int i = 0; i < _list.childCount; i++)
            {
                Button button = _list.GetChild(i).GetComponent<Button>();
                if (button != null && button.name == "QuestRow_" + questId)
                {
                    EventSystem.current?.SetSelectedGameObject(button.gameObject);
                    return;
                }
            }
        }

        /// <summary>关闭任务日志时解除地图输入锁并释放表单资源。</summary>
        /// <param name="isShutdown">是否由 UI 系统关闭整个应用。</param>
        /// <param name="userData">UI 框架传入的关闭数据。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            PlayerProgressService.SnapshotChanged -= OnSnapshotChanged;
            _controller?.DetachJournal(this);
            GameProxyManger.Instance.GetProxy<InputProxy>()?.SetDialogueInputBlocked(false);
            base.OnClose(isShutdown, userData);
        }
    }
}
