using System.Linq;
using AAAGame.ScriptsHotfix.GamePlay.Quest;
using AAAGame.ScriptsHotfix.UI.Module.Dialogue;
using Cysharp.Threading.Tasks;
using cfg.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>地图中的当前追踪目标与任务记录快捷入口。</summary>
    public sealed class QuestHudPanel : UGF_Panel
    {
        /// <summary>当前任务标题和追踪目标文本控件。</summary>
        private TMP_Text _text;
        /// <summary>当前 HUD 使用的任务数据表快照。</summary>
        private QuestData _data;
        /// <summary>标记动态控件是否已经创建。</summary>
        private bool _built;

        /// <summary>首次打开表单时创建 HUD 并订阅账号进度变化。</summary>
        /// <param name="userData">UI 框架传入的表单数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (_built) return;
            _built = true;
            Build();
            PlayerProgressService.SnapshotChanged += OnSnapshotChanged;
        }

        /// <summary>绑定地图流程已经加载的任务配置。</summary>
        /// <param name="data">任务 HUD 读取标题和目标时使用的数据表集合。</param>
        internal void Bind(QuestData data)
        {
            _data = data;
        }

        /// <summary>根据当前追踪偏好和账号快照刷新 HUD 文本。</summary>
        internal void Refresh()
        {
            if (_text == null || _data == null) return;
            QuestModel.Instance.ReconcileTrack();
            PlayerProgressSnapshot snapshot = PlayerProgressService.Snapshot;
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
                _text.text = string.Empty;
                return;
            }

            QuestObjectiveGroup group = _data.GetGroups(quest.Id).FirstOrDefault(value =>
                _data.GetObjectives(value.Id).Count(objective =>
                    snapshot.GetObjectiveCount(quest.Id, objective.Id) >= objective.TargetCount) < value.RequiredObjectiveCount);
            QuestObjective objective = group == null ? null : _data.GetObjectives(group.Id)
                .FirstOrDefault(value => snapshot.GetObjectiveCount(quest.Id, value.Id) < value.TargetCount);
            string objectiveText = objective == null
                ? QuestConst.Text.NoObjectives
                : QuestUiFactory.Localize(objective.DescriptionKey);
            _text.text = QuestUiFactory.Localize(quest.TitleKey) + "\n" + objectiveText;
        }

        /// <summary>创建任务追踪卡片和任务日志快捷按钮。</summary>
        private void Build()
        {
            RectTransform rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            Image panel = QuestUiFactory.Image("TrackedQuestCard", transform, new Vector2(570, 128),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -36), new Color32(20, 20, 18, 220));
            QuestUiFactory.Image("TrackedQuestAccent", panel.transform, new Vector2(4, 128),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Color32(154, 113, 57, 255));
            _text = QuestUiFactory.Text("TrackedQuestText", panel.transform, string.Empty, 20,
                new Color32(229, 218, 193, 255), new Vector2(500, 102),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0));
            QuestUiFactory.Button("QuestJournalShortcut", panel.transform, QuestConst.Text.Title, 16,
                new Vector2(210, 34), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 10), OpenJournal);
        }

        /// <summary>请求打开任务日志；对话期间忽略快捷键。</summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J) && !DialogueView.Instance.IsPlaying)
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
            PlayerProgressService.SnapshotChanged -= OnSnapshotChanged;
            QuestController.Instance.DetachHud(this);
            base.OnClose(isShutdown, userData);
        }
    }
}
