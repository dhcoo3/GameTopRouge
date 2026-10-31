using TMPro;
using AAAGame.ScriptsHotfix.GamePlay.Quest;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>首次接取任务时显示的确认简报。</summary>
    public sealed class QuestBriefingPanel : UGF_Panel
    {
        /// <summary>处理简报确认和服务端提交的任务控制器。</summary>
        private QuestController _controller;
        /// <summary>当前正在展示的任务配置。</summary>
        private cfg.Quest.QuestDefinition _quest;
        /// <summary>确认失败时显示的错误文案控件。</summary>
        private TMP_Text _error;
        /// <summary>显示任务简报标题的控件。</summary>
        private TMP_Text _title;
        /// <summary>显示任务简报正文的控件。</summary>
        private TMP_Text _body;
        /// <summary>提交简报确认的按钮。</summary>
        private Button _confirm;
        /// <summary>确认请求尚未返回时屏蔽重复输入。</summary>
        private bool _busy;
        /// <summary>标记简报控件是否已经创建。</summary>
        private bool _built;

        /// <summary>首次打开表单时创建简报内容和确认按钮。</summary>
        /// <param name="userData">UI 框架传入的表单数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (!_built) Build();
        }

        /// <summary>绑定当前任务并刷新简报标题和正文。</summary>
        /// <param name="controller">处理简报确认请求的任务控制器。</param>
        /// <param name="quest">当前需要确认的任务配置。</param>
        internal void Bind(QuestController controller, cfg.Quest.QuestDefinition quest)
        {
            _controller = controller;
            _quest = quest;
            _title.text = QuestConst.Text.BriefingTitle;
            _body.text = QuestUiFactory.Localize(quest.BriefingTextKey);
            _confirm.GetComponentInChildren<TMP_Text>().text = QuestConst.Text.Confirm;
        }

        /// <summary>设置确认按钮忙碌状态，避免重复提交。</summary>
        /// <param name="value">正在提交时为真，否则为假。</param>
        internal void SetBusy(bool value)
        {
            _busy = value;
            if (_confirm != null) _confirm.interactable = !value;
        }
        /// <summary>显示服务端确认失败的提示并恢复按钮。</summary>
        /// <param name="value">由任务模块本地化入口提供的错误文案。</param>
        internal void SetError(string value)
        {
            if (_error != null) _error.text = value;
            SetBusy(false);
        }

        /// <summary>将简报确认操作交给任务控制器处理。</summary>
        private void Confirm()
        {
            if (_busy || _controller == null || _quest == null) return;
            _controller.ConfirmBriefingAsync(_quest).Forget(Debug.LogException);
        }
        /// <summary>消费空格和回车输入，触发简报确认。</summary>
        private void Update()
        {
            if (!_built || _busy) return;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Confirm();
        }

        /// <summary>创建任务简报的遮罩、正文、错误提示和确认按钮。</summary>
        private void Build()
        {
            _built = true;
            var rect = GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var shade = QuestUiFactory.Image("BriefingShade", transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0, 0, 0, 0.84f));
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = Vector2.one; shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            var frame = QuestUiFactory.Image("BriefingFrame", shade.transform, new Vector2(1180, 650), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color32(20, 20, 19, 255));
            QuestUiFactory.Image("BriefingTopRule", frame.transform, new Vector2(1080, 2), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -86), new Color32(154, 113, 57, 255));
            _title = QuestUiFactory.Text("BriefingTitle", frame.transform, string.Empty, 30, new Color32(232, 218, 188, 255), new Vector2(1050, 64), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), TextAlignmentOptions.Center);
            _body = QuestUiFactory.Text("BriefingBody", frame.transform, string.Empty, 24, new Color32(225, 213, 188, 255), new Vector2(1020, 420), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), TextAlignmentOptions.TopLeft);
            _body.verticalAlignment = VerticalAlignmentOptions.Middle;
            _error = QuestUiFactory.Text("BriefingError", frame.transform, string.Empty, 17, new Color32(217, 151, 127, 255), new Vector2(780, 35), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 96), TextAlignmentOptions.Center);
            _confirm = QuestUiFactory.Button("BriefingConfirm", frame.transform, QuestConst.Text.Confirm, 22, new Vector2(280, 62), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), Confirm);
        }
    }
}
