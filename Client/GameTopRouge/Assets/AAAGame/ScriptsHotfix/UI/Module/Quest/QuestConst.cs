using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.UI.Core;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>集中定义任务模块资源与状态键映射。</summary>
    public static class QuestConst
    {
        /// <summary>任务模块使用的 UI 资源名称。</summary>
        public static class Asset
        {
            /// <summary>任务简报预制体资源名称。</summary>
            public const string BriefingPanel = "QuestBriefingPanel";
            /// <summary>任务追踪 HUD 预制体资源名称。</summary>
            public const string HudPanel = "QuestHudPanel";
        }

        /// <summary>任务状态对应的本地化键映射。</summary>
        public static class Text
        {

            /// <summary>按任务状态返回本地化键。</summary>
            /// <param name="status">服务端返回的任务状态编号。</param>
            /// <returns>对应状态的本地化键。</returns>
            public static string GetStatusKey(UIToGamePlayerBridge.QuestStatus status)
            {
                switch (status)
                {
                    case UIToGamePlayerBridge.QuestStatus.Available: return "Quest.UI.Status.Available";
                    case UIToGamePlayerBridge.QuestStatus.Active: return "Quest.UI.Status.Active";
                    case UIToGamePlayerBridge.QuestStatus.Completed: return "Quest.UI.Status.Completed";
                    default: return "Quest.UI.Status.Locked";
                }
            }
        }
    }
}
