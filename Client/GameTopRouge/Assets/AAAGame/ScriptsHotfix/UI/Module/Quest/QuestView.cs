using System.Threading;
using AAAGame.ScriptsHotfix.GamePlay.Quest;
using AAAGame.ScriptsHotfix.UI.Core;
using cfg.UI;
using Cysharp.Threading.Tasks;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>任务模块对外提供日志、HUD 和地图会话入口。</summary>
    public sealed class QuestView : ModuleView<QuestView>, IView
    {
        /// <summary>地图切换时加载进度并处理待确认的任务简报。</summary>
        /// <param name="mapId">当前地图配置编号。</param>
        /// <param name="token">地图生命周期取消令牌。</param>
        /// <returns>任务状态加载完成后的异步任务。</returns>
        public UniTask OnMapEnteredAsync(int mapId, CancellationToken token)
        {
            return QuestController.Instance.OnMapEnteredAsync(mapId, token);
        }

        /// <summary>查询任务简报或地图切换是否正在占用地图输入。</summary>
        public bool IsBlockingMapInteraction => QuestController.Instance.IsBlockingMapInteraction;

        /// <summary>打开任务日志展示面板。</summary>
        /// <returns>日志面板打开后的异步任务。</returns>
        public UniTask OpenJournalAsync()
        {
            return QuestController.Instance.OpenJournalAsync();
        }

        /// <summary>在地图中打开任务追踪 HUD，并绑定当前任务配置。</summary>
        /// <returns>HUD 打开并完成绑定后的异步任务。</returns>
        public async UniTask EnsureHudAsync()
        {
            QuestData data = await QuestData.LoadAsync();
            var panel = await UIService.Instance.OpenAsync<QuestHudPanel>(ModuleType.Quest, QuestConst.Asset.HudPanel, UIParams.Create(false));
            QuestController.Instance.AttachHud(panel, data);
        }

        /// <summary>离开地图时关闭任务界面并清理模块状态。</summary>
        public void CloseMapUI()
        {
            QuestController.Instance.CloseMapUI();
        }
    }
}
