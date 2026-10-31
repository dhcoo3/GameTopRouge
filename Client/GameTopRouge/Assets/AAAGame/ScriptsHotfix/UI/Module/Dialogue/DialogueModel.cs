using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Quest;
using AAAGame.ScriptsHotfix.UI.Core;
using cfg.Dialogue;

namespace AAAGame.ScriptsHotfix.UI.Module.Dialogue
{
    /// <summary>会话的结束原因。</summary>
    public enum DialogueStatus
    {
        /// <summary>正常结束。</summary>
        Completed,
        /// <summary>玩家或调用方取消。</summary>
        Cancelled,
        /// <summary>配置或资源错误。</summary>
        Failed,
        /// <summary>已有会话占用入口。</summary>
        Busy
    }

    /// <summary>不可变的会话返回值，选项为配置中的选项编号。</summary>
    public sealed class DialogueResult
    {
        /// <summary>本次会话状态。</summary>
        public DialogueStatus Status { get; }
        /// <summary>按选择顺序复制的选项编号。</summary>
        public IReadOnlyList<int> Choices { get; }
        /// <summary>创建独立结果快照。</summary>
        /// <param name="status">结束原因。</param>
        /// <param name="choices">允许为空的选项列表。</param>
        public DialogueResult(DialogueStatus status, IEnumerable<int> choices = null)
        {
            Status = status;
            Choices = new List<int>(choices ?? Array.Empty<int>()).AsReadOnly();
        }
    }

    /// <summary>保存本次运行的剧情标记和当前会话状态。</summary>
    public sealed class DialogueModel : ModuleModel<DialogueModel>, IModel
    {
        /// <summary>会话最多保留的历史条目，防止手动循环无限增长。</summary>
        private const int MaximumHistoryEntries = 512;
        /// <summary>本次会话已实际显示的台词及选项。</summary>
        internal readonly List<string> History = new List<string>();
        /// <summary>本次会话已选择的配置编号。</summary>
        internal readonly List<int> Choices = new List<int>();
        /// <summary>当前可见节点。</summary>
        internal DialogueNode Current;
        /// <summary>当前会话是否持有播放入口。</summary>
        public bool IsPlaying { get; internal set; }
        /// <summary>读取标记，不存在时返回 false。</summary>
        /// <param name="key">稳定的标记名称。</param>
        /// <returns>标记当前值。</returns>
        internal bool GetFlag(string key) => PlayerProgressService.GetStoryFlag(key);

        /// <summary>追加有容量限制的历史条目。</summary>
        /// <param name="text">已显示的台词或选项。</param>
        /// <returns>新条目的索引。</returns>
        internal int AddHistory(string text)
        {
            if (History.Count >= MaximumHistoryEntries) History.RemoveAt(0);
            History.Add(text);
            return History.Count - 1;
        }
        /// <summary>清空历史并开始独占会话。</summary>
        internal void Begin()
        {
            History.Clear();
            Choices.Clear();
            Current = null;
            IsPlaying = true;
        }
    }
}
