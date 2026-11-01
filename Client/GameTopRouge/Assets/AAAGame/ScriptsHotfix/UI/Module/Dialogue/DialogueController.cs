using System;
using System.Threading;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.UI.Core;
using cfg.Dialogue;
using Cysharp.Threading.Tasks;

namespace AAAGame.ScriptsHotfix.UI.Module.Dialogue
{
    /// <summary>编排单个本地对话会话，不修改玩法或网络状态。</summary>
    public sealed class DialogueController : ModuleController<DialogueController>, IController
    {
        /// <summary>连续自动节点的最大执行次数。</summary>
        private const int MaximumAutomaticSteps = 128;
        /// <summary>本次对话校验通过的配置集合。</summary>
        private DialogueData _data;
        /// <summary>等待台词结束、玩家取消或播放失败的会话完成源。</summary>
        private UniTaskCompletionSource<DialogueResult> _completion;
        /// <summary>当前会话使用的对话面板。</summary>
        private DialoguePanel _panel;
        /// <summary>外部调用方提供的会话取消令牌。</summary>
        private CancellationToken _sessionToken;
        /// <summary>当前正在播放的对话配置编号。</summary>
        private int _dialogueId;
        /// <summary>阻止重入自动节点循环的状态。</summary>
        private bool _moving;

        /// <summary>对话节点由本控制器按表配置执行，不需要额外注册协议回调。</summary>
        public override void RegisterProto() { }

        /// <summary>对话输入由面板处理，不需要额外注册全局事件。</summary>
        public override void RegisterEvent() { }

        /// <summary>启动会话；调用者必须在 Unity 主线程使用。</summary>
        /// <param name="id">剧情编号。</param>
        /// <param name="openPanel">打开并返回界面的展示函数。</param>
        /// <param name="loadData">配置加载函数。</param>
        /// <param name="token">会话取消令牌。</param>
        /// <returns>独立的会话结果。</returns>
        internal async UniTask<DialogueResult> PlayAsync(int id, Func<UniTask<DialoguePanel>> openPanel, Func<UniTask<DialogueData>> loadData, CancellationToken token)
        {
            var model = DialogueModel.Instance;
            if (model.IsPlaying) return new DialogueResult(DialogueStatus.Busy);
            using IDisposable inputLock = UIToGamePlayerBridge.Instance.AcquirePlayerInputLock();
            model.Begin();
            _completion = new UniTaskCompletionSource<DialogueResult>();
            try
            {
                token.ThrowIfCancellationRequested();
                _data = await loadData().AttachExternalCancellation(token);
                token.ThrowIfCancellationRequested();
                _data.Validate();
                var dialogue = _data.Dialogues.GetOrDefault(id) ?? throw new InvalidOperationException($"Unknown dialogue: {id}");
                _dialogueId = id;
                _sessionToken = token;
                _panel = await openPanel();
                token.ThrowIfCancellationRequested();
                if (_panel == null) throw new InvalidOperationException("Dialogue panel unavailable");
                _panel.Bind(this, _data);
                await MoveToAsync(dialogue.EntryNodeId);
                while (_completion != null && !_completion.Task.Status.IsCompleted())
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
                return await _completion.Task;
            }
            catch (OperationCanceledException) { return new DialogueResult(DialogueStatus.Cancelled, model.Choices); }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                return new DialogueResult(DialogueStatus.Failed, model.Choices);
            }
            finally
            {
                if (_panel != null) _panel.Release();
                _panel = null;
                _data = null;
                _completion = null;
                _dialogueId = 0;
                _moving = false;
                _sessionToken = default;
                model.Current = null;
                model.IsPlaying = false;
            }
        }
        /// <summary>沿指定节点执行条件和标记节点，遇到台词或选项后返回。</summary>
        /// <param name="nodeId">目标节点编号。</param>
        private async UniTask MoveToAsync(int nodeId)
        {
            if (_moving) return;
            _moving = true;
            try
            {
                for (int step = 0; step < MaximumAutomaticSteps; step++)
                {
                    _sessionToken.ThrowIfCancellationRequested();
                    var node = _data.Nodes.Get(nodeId);
                    DialogueModel.Instance.Current = node;
                    switch (node.Type)
                    {
                        case DialogueNodeType.Condition:
                            nodeId = DialogueModel.Instance.GetFlag(node.FlagKey) == node.FlagValue ? node.TrueNodeId : node.FalseNodeId;
                            continue;
                        case DialogueNodeType.SetFlag:
                            await UIToGamePlayerBridge.Instance.SubmitProgressEventAsync(new UIToGamePlayerBridge.ProgressEvent
                            {
                                Type = UIToGamePlayerBridge.ProgressEventType.DialogueSetFlag,
                                DialogueId = _dialogueId,
                                DialogueNodeId = node.Id
                            }, _sessionToken);
                            nodeId = node.NextNodeId;
                            continue;
                        case DialogueNodeType.End: Finish(DialogueStatus.Completed); return;
                        default: _panel.ShowNode(node); return;
                    }
                }
                throw new InvalidOperationException("Automatic dialogue node limit exceeded");
            }
            catch (OperationCanceledException) { Finish(DialogueStatus.Cancelled); }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                Finish(DialogueStatus.Failed);
            }
            finally { _moving = false; }
        }
        /// <summary>响应面板发出的台词页推进请求。</summary>
        internal void Advance()
        {
            if (!_moving && DialogueModel.Instance.Current?.Type == DialogueNodeType.Line) MoveToAsync(DialogueModel.Instance.Current.NextNodeId).Forget(UnityEngine.Debug.LogException);
        }
        /// <summary>记录合法选项并从选项目标继续播放。</summary>
        /// <param name="optionId">当前选项的配置编号。</param>
        internal void Choose(int optionId)
        {
            var option = _data.Options.GetOrDefault(optionId);
            if (_moving || option == null || option.NodeId != DialogueModel.Instance.Current?.Id) return;
            DialogueModel.Instance.Choices.Add(optionId);
            DialogueModel.Instance.AddHistory("> " + GF.Localization.GetText(option.TextKey));
            MoveToAsync(option.TargetNodeId).Forget(UnityEngine.Debug.LogException);
        }
        /// <summary>统一结束会话；重复调用不改变首次完成结果。</summary>
        /// <param name="status">完成、取消或失败。</param>
        internal void Finish(DialogueStatus status) => _completion?.TrySetResult(new DialogueResult(status, DialogueModel.Instance.Choices));
        /// <summary>清理模块时取消仍未完成的对话会话。</summary>
        public override void Clear()
        {
            Finish(DialogueStatus.Cancelled);
            base.Clear();
        }
    }
}
