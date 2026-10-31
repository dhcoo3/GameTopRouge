using AAAGame.ScriptsHotfix.GamePlay.Quest;
using AAAGame.ScriptsHotfix.UI.Core;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>集中定义任务模块的本地化文案入口。</summary>
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

        /// <summary>读取任务模块展示文案。</summary>
        public static class Text
        {
            /// <summary>任务日志标题。</summary>
            public static string Title => GF.Localization.GetString("Quest.UI.Title");
            /// <summary>主线任务分类标签。</summary>
            public static string Main => GF.Localization.GetString("Quest.UI.Main");
            /// <summary>支线任务分类标签。</summary>
            public static string Side => GF.Localization.GetString("Quest.UI.Side");
            /// <summary>已完成任务分类标签。</summary>
            public static string Completed => GF.Localization.GetString("Quest.UI.Completed");
            /// <summary>任务追踪按钮文字。</summary>
            public static string Track => GF.Localization.GetString("Quest.UI.Track");
            /// <summary>当前已追踪状态文字。</summary>
            public static string Tracked => GF.Localization.GetString("Quest.UI.Tracked");
            /// <summary>无可显示目标时的提示文字。</summary>
            public static string NoObjectives => GF.Localization.GetString("Quest.UI.NoObjectives");
            /// <summary>任务简报标题。</summary>
            public static string BriefingTitle => GF.Localization.GetString("Quest.UI.BriefingTitle");
            /// <summary>通用确认按钮文字。</summary>
            public static string Confirm => GF.Localization.GetString("Quest.UI.Confirm");
            /// <summary>任务进度操作失败时的玩家提示。</summary>
            public static string OperationFailed => GF.Localization.GetString("Quest.UI.Error");
            /// <summary>地图任务物件的交互提示。</summary>
            public static string Interact => GF.Localization.GetString("Quest.UI.Interact");

            /// <summary>按任务状态读取任务列表中的本地化状态文字。</summary>
            /// <param name="status">服务端返回的任务状态编号。</param>
            /// <returns>对应状态的本地化文案。</returns>
            public static string GetStatus(QuestStatus status)
            {
                switch (status)
                {
                    case QuestStatus.Available: return GF.Localization.GetString("Quest.UI.Status.Available");
                    case QuestStatus.Active: return GF.Localization.GetString("Quest.UI.Status.Active");
                    case QuestStatus.Completed: return GF.Localization.GetString("Quest.UI.Status.Completed");
                    default: return GF.Localization.GetString("Quest.UI.Status.Locked");
                }
            }
        }
    }
}
