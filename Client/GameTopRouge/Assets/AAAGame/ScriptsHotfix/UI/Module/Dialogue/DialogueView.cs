using System.Threading;
using AAAGame.ScriptsHotfix.UI.Core;
using cfg.UI;
using Cysharp.Threading.Tasks;

namespace AAAGame.ScriptsHotfix.UI.Module.Dialogue
{
    /// <summary>提供对话模块唯一的正式展示入口。</summary>
    public sealed class DialogueView : ModuleView<DialogueView>, IView
    {
        /// <summary>当前是否存在正在播放的对话会话。</summary>
        public bool IsPlaying => DialogueModel.Instance.IsPlaying;

        /// <summary>加载配置并打开对话；必须在已初始化 GF 的 Unity 主线程调用。</summary>
        /// <param name="dialogueId">剧情表编号。</param>
        /// <param name="cancellationToken">取消整个会话的令牌。</param>
        /// <returns>会话结束状态与选项快照。</returns>
        public UniTask<DialogueResult> PlayAsync(int dialogueId, CancellationToken cancellationToken = default)
        {
            return DialogueController.Instance.PlayAsync(dialogueId, OpenPanelAsync, DialogueData.LoadAsync, cancellationToken);
        }
        /// <summary>通过项目统一模块配置打开正式表单。</summary>
        /// <returns>可供控制器绑定的界面。</returns>
        private async UniTask<DialoguePanel> OpenPanelAsync()
        {
            var panel = (DialoguePanel)await UIModuleManager.Instance.GoToModuleAsync(ModuleType.Dialogue, UIParams.Create(false));
            panel.Localize = null;
            panel.CloseAction = () => panel.Close(false);
            return panel;
        }
    }
}
