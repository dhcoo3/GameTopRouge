using AAAGame.ScriptsHotfix.UI.Core;

namespace AAAGame.ScriptsHotfix.UI.Module.Dialogue
{
    /// <summary>集中定义对话模块的本地化文案入口。</summary>
    public static class DialogueConst
    {
        /// <summary>集中定义对话模块使用的本地化资源键。</summary>
        public static class LocalizationKey
        {
            /// <summary>开始演示按钮文案键。</summary>
            public const string Start = "Dialogue.UI.Start";
            /// <summary>重置演示按钮文案键。</summary>
            public const string Reset = "Dialogue.UI.Reset";
            /// <summary>演示标题文案键。</summary>
            public const string Title = "Dialogue.UI.Title";
            /// <summary>演示操作说明文案键。</summary>
            public const string Hint = "Dialogue.UI.Hint";
            /// <summary>查看历史记录按钮文案键。</summary>
            public const string History = "Dialogue.UI.History";
            /// <summary>关闭历史记录按钮文案键。</summary>
            public const string CloseHistory = "Dialogue.UI.CloseHistory";
            /// <summary>继续对话提示文案键。</summary>
            public const string Continue = "Dialogue.UI.Continue";
            /// <summary>选择选项提示文案键。</summary>
            public const string Choose = "Dialogue.UI.Choose";
            /// <summary>剧情标记重置结果文案键。</summary>
            public const string ResetDone = "Dialogue.UI.ResetDone";
            /// <summary>说明账号剧情进度不能由演示重置的文案键。</summary>
            public const string ResetAccountOwned = "Dialogue.UI.ResetAccountOwned";
            /// <summary>正常结束状态文案键。</summary>
            public const string StatusCompleted = "Dialogue.UI.Completed";
            /// <summary>取消状态文案键。</summary>
            public const string StatusCancelled = "Dialogue.UI.Cancelled";
            /// <summary>失败状态文案键。</summary>
            public const string StatusFailed = "Dialogue.UI.Failed";
            /// <summary>忙碌状态文案键。</summary>
            public const string StatusBusy = "Dialogue.UI.Busy";
        }

        /// <summary>读取对话模块展示文案。</summary>
        public static class Text
        {
            /// <summary>地图 NPC 的交谈提示。</summary>
            public static string Interact => GF.Localization.GetString("Dialogue.UI.Interact");
            /// <summary>选择对话结束状态对应的本地化键。</summary>
            /// <param name="status">对话会话结束状态。</param>
            /// <returns>状态对应的稳定资源键。</returns>
            public static string GetStatusKey(DialogueStatus status)
            {
                switch (status)
                {
                    case DialogueStatus.Completed: return LocalizationKey.StatusCompleted;
                    case DialogueStatus.Cancelled: return LocalizationKey.StatusCancelled;
                    case DialogueStatus.Failed: return LocalizationKey.StatusFailed;
                    default: return LocalizationKey.StatusBusy;
                }
            }
        }
    }
}
