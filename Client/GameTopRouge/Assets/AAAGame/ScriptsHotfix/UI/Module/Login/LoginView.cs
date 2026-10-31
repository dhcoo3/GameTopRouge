using System.Threading;
using AAAGame.ScriptsHotfix.UI.Core;

namespace AAAGame.ScriptsHotfix.UI.Module.Login
{
    #if ENABLE_OBFUZ
    [Obfuz.ObfuzIgnore]
    #endif
    /// <summary>Login 模块的可选展示门面，负责共用界面提示等展示能力。</summary>
    public sealed class LoginView : ModuleView<LoginView>, IView
    {
        /// <summary>由 Login 控制器请求界面显示业务提示。</summary>
        public void ShowMessage(string title, string message)
        {
            GF.BuiltinView.ShowDialog(title, message, LoginConst.Text.Confirm);
        }
    }
}
