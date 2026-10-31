using TMPro;
using System.Threading;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.UI.Module.Login
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    /// <summary>游客认证入口；账号密码登录将在后续版本开放。</summary>
    public class LoginPanel : UGF_Panel
    {
        #region Auto Create
        /// <summary>暂未开放的账号输入框。</summary>
        private TMP_InputField m_AccountInputField;
        /// <summary>暂未开放的密码输入框。</summary>
        private TMP_InputField m_PasswordInputField;
        /// <summary>暂未开放的账号登录按钮。</summary>
        private UGF_Button m_StartButton;
        /// <summary>请求 Login 控制器执行游客登录的按钮。</summary>
        private UGF_Button m_GuestLoginButton;
        #endregion

        /// <summary>面板当前打开周期的取消源。</summary>
        private CancellationTokenSource _panelLifetime;

        /// <summary>绑定现有登录界面控件。</summary>
        protected override void RegisterUI()
        {
            #region Auto Bind
            m_AccountInputField = PanelUIContainer.Get<TMP_InputField>("m_AccountInputField");
            m_PasswordInputField = PanelUIContainer.Get<TMP_InputField>("m_PasswordInputField");
            m_StartButton = PanelUIContainer.Get<UGF_Button>("m_StartButton");
            m_GuestLoginButton = PanelUIContainer.Get<UGF_Button>("m_GuestLoginButton");
            #endregion
        }

        /// <summary>只启用第一版游客认证入口。</summary>
        protected override void RegisterEvent()
        {
            #region Auto Event
            m_GuestLoginButton.SetClickListener(OnGuestLoginClicked);
            #endregion
            
            Subscribe(LoginConst.Event.ELoginStateChanged, SetGuestLoginState);
        }


        /// <summary>打开时创建独立取消令牌并显示当前版本支持的登录方式。</summary>
        /// <param name="userData">界面打开参数。</param>
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            _panelLifetime?.Cancel();
            _panelLifetime?.Dispose();
            _panelLifetime = new CancellationTokenSource();
            m_AccountInputField.interactable = false;
            m_PasswordInputField.interactable = false;
            m_AccountInputField.text = string.Empty;
            m_PasswordInputField.text = string.Empty;
            m_StartButton.interactable = false;
            ApplyGuestLoginState(LoginModel.Instance.LoggingIn, LoginModel.Instance.LoginCompleted);
            if (m_AccountInputField.placeholder is TMP_Text placeholder) placeholder.text = LoginConst.Text.AccountLoginUnavailable;
            TMP_Text label = m_StartButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = LoginConst.Text.AccountLoginUnavailable;
        }

        /// <summary>按钮回调直接调用 LoginController，由控制器统一编排登录流程。</summary>
        private void OnGuestLoginClicked()
        {
            if (_panelLifetime == null) return;
            LoginController.Instance.HandleGuestLoginAsync(_panelLifetime.Token).Forget();
        }

        /// <summary>更新登录按钮状态。</summary>
        private void SetGuestLoginState(object sender, GameEvent e)
        {
            ApplyGuestLoginState(e.GetParam1<bool>(), e.GetParam2<bool>());
        }

        /// <summary>根据 Login 登录状态更新游客登录按钮。</summary>
        private void ApplyGuestLoginState(bool pending, bool completed)
        {
            if (m_GuestLoginButton != null)
                m_GuestLoginButton.interactable = !pending && !completed;
        }

        /// <summary>离开面板时取消尚未完成的登录，已成功的游戏会话有独立生命周期。</summary>
        /// <param name="isShutdown">是否由应用关闭触发。</param>
        /// <param name="userData">关闭界面的上下文。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            _panelLifetime?.Cancel();
            _panelLifetime?.Dispose();
            _panelLifetime = null;
            base.OnClose(isShutdown, userData);
        }
    }
}
