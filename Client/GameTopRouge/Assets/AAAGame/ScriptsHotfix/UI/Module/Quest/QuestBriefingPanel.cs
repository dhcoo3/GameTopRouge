using System;
using AAAGame.ScriptsHotfix.Bridge;
using TMPro;
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
        #region Auto Create
        /// <summary>确认失败时显示的错误文案控件。</summary>
        private TMP_Text m_Error;
        /// <summary>显示任务简报标题的控件。</summary>
        private TMP_Text m_Title;
        /// <summary>显示任务简报正文的控件。</summary>
        private TMP_Text m_Body;
        /// <summary>提交简报确认的按钮。</summary>
        private UGF_Button m_Confirm;
        /// <summary>确认按钮的文本控件。</summary>
        private TMP_Text m_ConfirmLabel;
        #endregion
        /// <summary>确认请求尚未返回时屏蔽重复输入。</summary>
        private bool _busy;
        /// <summary>Prefab 控件是否已成功绑定。</summary>
        private bool _built;
        /// <summary>当前简报面板独立持有的玩家输入锁。</summary>
        private IDisposable _inputLock;

        /// <summary>为本次打开绑定交互。</summary>
        protected override void RegisterEvent()
        {
            m_Confirm.SetClickListener(Confirm);
        }

        /// <summary>从面板容器绑定显示和交互控件。</summary>
        protected override void RegisterUI()
        {
            #region Auto Bind
            m_Error = PanelUIContainer.Get<TMP_Text>("m_Error");
            m_Title = PanelUIContainer.Get<TMP_Text>("m_Title");
            m_Body = PanelUIContainer.Get<TMP_Text>("m_Body");
            m_Confirm = PanelUIContainer.Get<UGF_Button>("m_Confirm");
            m_ConfirmLabel = PanelUIContainer.Get<TMP_Text>("m_ConfirmLabel");
                _built = true;
            #endregion
        }

        /// <summary>每次打开简报时注册确认按钮。</summary>
        /// <param name="userData">UI 框架传入的表单数据。</param>
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (!_built) return;

            _inputLock?.Dispose();
            _inputLock = UIToGamePlayerBridge.Instance.AcquirePlayerInputLock();
            _controller = null;
            _quest = null;
            _busy = false;
            m_Confirm.interactable = true;
            m_Error.text = string.Empty;
        }

        /// <summary>绑定当前任务并刷新简报标题和正文。</summary>
        /// <param name="controller">处理简报确认请求的任务控制器。</param>
        /// <param name="quest">当前需要确认的任务配置。</param>
        internal void Bind(QuestController controller, cfg.Quest.QuestDefinition quest)
        {
            _controller = controller;
            _quest = quest;
            m_Title.text = GF.Localization.GetText("Quest.UI.BriefingTitle");
            m_Body.text = GF.Localization.GetText(quest.BriefingTextKey);
            m_Error.text = string.Empty;
            m_ConfirmLabel.text = GF.Localization.GetText("Quest.UI.Confirm");
        }

        /// <summary>设置确认按钮忙碌状态，避免重复提交。</summary>
        /// <param name="value">正在提交时为真，否则为假。</param>
        internal void SetBusy(bool value)
        {
            _busy = value;
            if (m_Confirm != null) m_Confirm.interactable = !value;
        }
        /// <summary>显示服务端确认失败的提示并恢复按钮。</summary>
        /// <param name="value">由任务模块本地化入口提供的错误文案。</param>
        internal void SetError(string value)
        {
            if (m_Error != null) m_Error.text = value;
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

        /// <summary>关闭简报时移除确认按钮监听。</summary>
        /// <param name="isShutdown">是否由 UI 系统关闭整个应用。</param>
        /// <param name="userData">UI 框架传入的关闭数据。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            _inputLock?.Dispose();
            _inputLock = null;
            _controller = null;
            _quest = null;
            base.OnClose(isShutdown, userData);
        }
    }
}
