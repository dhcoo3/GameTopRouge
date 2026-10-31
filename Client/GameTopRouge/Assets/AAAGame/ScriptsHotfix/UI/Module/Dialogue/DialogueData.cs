using System;
using System.Collections.Generic;
using System.Linq;
using cfg.Dialogue;
using cfg.Map;
using Cysharp.Threading.Tasks;

namespace AAAGame.ScriptsHotfix.UI.Module.Dialogue
{
    /// <summary>来自 Luban 的对话配置集合；演示和正式入口共用。</summary>
    public sealed class DialogueData
    {
        /// <summary>剧情入口表。</summary>
        public TbDialogue Dialogues;
        /// <summary>统一的 NPC 配置表，节点编号为 0 时使用旁白。</summary>
        public TbNpc Npcs;
        /// <summary>节点表。</summary>
        public TbDialogueNode Nodes;
        /// <summary>选项表。</summary>
        public TbDialogueOption Options;
        /// <summary>按项目已有配置组件顺序加载并校验。</summary>
        /// <returns>可播放的配置集合。</returns>
        public static async UniTask<DialogueData> LoadAsync()
        {
            var data = new DialogueData
            {
                Dialogues = await GF.LuBan.Get<TbDialogue>(cfg.Tables.dialogue_tbdialogue),
                Npcs = await GF.LuBan.Get<TbNpc>(cfg.Tables.map_tbnpc),
                Nodes = await GF.LuBan.Get<TbDialogueNode>(cfg.Tables.dialogue_tbdialoguenode),
                Options = await GF.LuBan.Get<TbDialogueOption>(cfg.Tables.dialogue_tbdialogueoption)
            };
            data.Validate();
            return data;
        }
        /// <summary>获取按排序字段排列的节点选项。</summary>
        /// <param name="nodeId">选项节点编号。</param>
        /// <returns>独立的有序选项数组。</returns>
        public DialogueOption[] GetOptions(int nodeId) => Options.DataList.Where(x => x.NodeId == nodeId).OrderBy(x => x.Order).ToArray();
        /// <summary>验证引用、节点类型、选项数量及结束可达性。</summary>
        public void Validate()
        {
            foreach (var node in Nodes.DataList)
            {
                if (Dialogues.GetOrDefault(node.DialogueId) == null) throw new InvalidOperationException($"Unknown dialogue: {node.Id}");
                if (node.NpcId != 0 && Npcs.GetOrDefault(node.NpcId) == null) throw new InvalidOperationException($"Unknown NPC: {node.Id}");
                if (!Enum.IsDefined(typeof(DialogueNodeType), node.Type)) throw new InvalidOperationException($"Unknown node type: {node.Id}");
                if ((node.Type == DialogueNodeType.Line || node.Type == DialogueNodeType.Choice) && string.IsNullOrEmpty(node.TextKey)) throw new InvalidOperationException($"Missing text: {node.Id}");
                if ((node.Type == DialogueNodeType.Condition || node.Type == DialogueNodeType.SetFlag) && string.IsNullOrEmpty(node.FlagKey)) throw new InvalidOperationException($"Missing flag: {node.Id}");
                var options = GetOptions(node.Id);
                if (node.Type == DialogueNodeType.Choice && (options.Length < 1 || options.Length > 4 || options.Select(x => x.Order).Distinct().Count() != options.Length)) throw new InvalidOperationException($"Invalid options: {node.Id}");
                foreach (int target in Edges(node)) CheckTarget(target, node.DialogueId);
            }
            foreach (var option in Options.DataList)
                if (Nodes.GetOrDefault(option.NodeId)?.Type != DialogueNodeType.Choice) throw new InvalidOperationException($"Orphan option: {option.Id}");
            foreach (var dialogue in Dialogues.DataList)
            {
                CheckTarget(dialogue.EntryNodeId, dialogue.Id);
                var canEnd = new HashSet<int>(Nodes.DataList.Where(n => n.DialogueId == dialogue.Id && n.Type == DialogueNodeType.End).Select(n => n.Id));
                bool changed;
                do
                {
                    changed = false;
                    foreach (var node in Nodes.DataList.Where(n => n.DialogueId == dialogue.Id))
                        if (!canEnd.Contains(node.Id) && Edges(node).Any(canEnd.Contains)) changed |= canEnd.Add(node.Id);
                } while (changed);
                if (Nodes.DataList.Any(n => n.DialogueId == dialogue.Id && !canEnd.Contains(n.Id))) throw new InvalidOperationException($"No reachable end: {dialogue.Id}");
            }
        }
        /// <summary>检查跳转不能跨越剧情。</summary>
        /// <param name="id">目标节点编号。</param>
        /// <param name="dialogueId">所属剧情编号。</param>
        private void CheckTarget(int id, int dialogueId)
        {
            if (Nodes.GetOrDefault(id)?.DialogueId != dialogueId) throw new InvalidOperationException($"Invalid target: {id} in {dialogueId}");
        }
        /// <summary>枚举节点真实使用的跳转。</summary>
        /// <param name="node">需要检查的节点。</param>
        /// <returns>目标编号集合。</returns>
        private IEnumerable<int> Edges(DialogueNode node)
        {
            switch (node.Type)
            {
                case DialogueNodeType.Line:
                case DialogueNodeType.SetFlag: yield return node.NextNodeId; break;
                case DialogueNodeType.Condition: yield return node.TrueNodeId; yield return node.FalseNodeId; break;
                case DialogueNodeType.Choice:
                    foreach (var option in GetOptions(node.Id)) yield return option.TargetNodeId;
                    break;
            }
        }
    }
}
