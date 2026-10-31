using System;
using System.Linq;
using System.Threading;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Quest;
using AAAGame.ScriptsHotfix.UI.Core;
using cfg.Quest;
using Cysharp.Threading.Tasks;
using cfg.UI;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>连接账号进度、任务界面、简报与地图生命周期。</summary>
    public sealed class QuestController : ModuleController<QuestController>, IController
    {
        /// <summary>当前打开的任务日志面板。</summary>
        private QuestPanel _panel;
        /// <summary>当前地图任务追踪 HUD。</summary>
        private QuestHudPanel _hud;
        /// <summary>当前等待玩家确认的任务简报。</summary>
        private QuestBriefingPanel _briefing;
        /// <summary>等待简报确认或地图取消的异步完成源。</summary>
        private UniTaskCompletionSource<bool> _briefingCompletion;
        /// <summary>当前地图会话的取消令牌。</summary>
        private CancellationToken _mapToken;
        /// <summary>防止任务日志异步打开期间重复打开。</summary>
        private bool _openingPanel;
        /// <summary>标记任务简报正在占用地图操作。</summary>
        private bool _briefingBusy;
        /// <summary>标记地图进度加载或简报流程正在执行。</summary>
        private bool _mapEntryInProgress;
        /// <summary>地图进入流程代数，用于忽略已经过期的异步回调。</summary>
        private int _mapEntryGeneration;
        /// <summary>简报会话代数，用于隔离迟到的确认响应。</summary>
        private int _briefingGeneration;
        /// <summary>地图切换或任务简报尚未完成时，阻止地图交互。</summary>
        internal bool IsBlockingMapInteraction => _mapEntryInProgress || _briefingBusy;

        /// <summary>任务进度协议通过共享进度服务发送，不需要模块独立注册协议回调。</summary>
        public override void RegisterProto() { }

        /// <summary>任务模块当前没有需要单独注册的全局事件。</summary>
        public override void RegisterEvent() { }

        /// <summary>加载当前账号任务并在必要时展示尚未确认的任务简报。</summary>
        /// <param name="mapId">已进入地图的配置编号。</param>
        /// <param name="token">地图生命周期取消令牌。</param>
        /// <returns>进度加载及简报处理完成后的异步任务。</returns>
        internal async UniTask OnMapEnteredAsync(int mapId, CancellationToken token)
        {
            _mapToken = token;
            int generation = ++_mapEntryGeneration;
            _mapEntryInProgress = true;
            var input = GameProxyManger.Instance.GetProxy<InputProxy>();
            input?.SetDialogueInputBlocked(true);
            try
            {
                PlayerProgressSnapshot snapshot = await PlayerProgressService.LoadForMapAsync(mapId, token);
                token.ThrowIfCancellationRequested();
                QuestData data = await QuestData.LoadAsync().AttachExternalCancellation(token);
                QuestModel.Instance.ReconcileTrack();
                await QuestView.Instance.EnsureHudAsync();
                foreach (QuestProgressEntry progress in snapshot.Quests.Values.OrderBy(value => value.QuestId))
                {
                    cfg.Quest.QuestDefinition quest = data.Quests.GetOrDefault(progress.QuestId);
                    if (progress.Status != QuestStatus.Active || progress.BriefingAcknowledged || quest == null || string.IsNullOrEmpty(quest.BriefingTextKey)) continue;
                    await ShowBriefingAsync(quest, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                if (generation == _mapEntryGeneration)
                {
                    _mapEntryInProgress = false;
                    input?.SetDialogueInputBlocked(false);
                }
            }
        }

        /// <summary>打开任务日志；进度尚未加载或地图简报占用输入时忽略请求。</summary>
        /// <returns>面板打开并绑定任务数据后的异步任务。</returns>
        internal async UniTask OpenJournalAsync()
        {
            if (_mapEntryInProgress || _briefingBusy) return;
            if (_panel != null || _openingPanel) return;
            if (!PlayerProgressService.IsLoaded)
            {
                Debug.LogWarning("Quest journal is not ready until account progress loads.");
                return;
            }
            _openingPanel = true;
            try
            {
                _panel = (QuestPanel)await UIModuleManager.Instance.GoToModuleAsync(ModuleType.Quest, UIParams.Create(false));
                _panel.Bind(this, QuestModel.Instance, await QuestData.LoadAsync());
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { _openingPanel = false; }
        }

        /// <summary>打开任务简报并等待玩家确认或地图生命周期取消。</summary>
        /// <param name="quest">需要展示简报的任务配置。</param>
        /// <param name="token">地图离开或销毁时取消简报的令牌。</param>
        /// <returns>简报关闭后的异步任务。</returns>
        internal async UniTask ShowBriefingAsync(cfg.Quest.QuestDefinition quest, CancellationToken token)
        {
            if (_briefingBusy) return;
            _briefingBusy = true;
            int generation = ++_briefingGeneration;
            UniTaskCompletionSource<bool> completion = new UniTaskCompletionSource<bool>();
            QuestBriefingPanel openedPanel = null;
            try
            {
                _briefingCompletion = completion;
                openedPanel = await UIService.Instance.OpenAsync<QuestBriefingPanel>(ModuleType.Quest, QuestConst.Asset.BriefingPanel, UIParams.Create(false));
                if (generation != _briefingGeneration || token.IsCancellationRequested)
                {
                    if (!ReferenceEquals(_briefing, openedPanel)) openedPanel.Close(false);
                    return;
                }
                _briefing = openedPanel;
                _briefing.Bind(this, quest);
                using (token.Register(() => completion.TrySetCanceled(token)))
                    await completion.Task;
            }
            finally
            {
                if (generation == _briefingGeneration)
                {
                    if (ReferenceEquals(_briefing, openedPanel)) _briefing?.Close(false);
                    _briefing = null;
                    _briefingCompletion = null;
                    _briefingBusy = false;
                }
            }
        }

        /// <summary>提交服务端简报确认，成功后解除阻挡并刷新任务追踪。</summary>
        /// <param name="quest">玩家当前确认的任务配置。</param>
        /// <returns>确认请求完成后的异步任务。</returns>
        internal async UniTask ConfirmBriefingAsync(cfg.Quest.QuestDefinition quest)
        {
            QuestBriefingPanel panel = _briefing;
            int generation = _briefingGeneration;
            UniTaskCompletionSource<bool> completion = _briefingCompletion;
            if (panel == null) return;
            panel.SetBusy(true);
            try
            {
                await PlayerProgressService.SubmitEventAsync(new PlayerProgressEvent
                {
                    Type = PlayerProgressEventType.AcknowledgeBriefing,
                    QuestId = quest.Id,
                    MapId = quest.StartMapId
                }, _mapToken);
                if (generation == _briefingGeneration) completion?.TrySetResult(true);
                QuestModel.Instance.ReconcileTrack();
                _hud?.Refresh();
            }
            catch (Exception exception)
            {
                if (generation == _briefingGeneration && ReferenceEquals(_briefing, panel))
                    panel.SetError(QuestConst.Text.OperationFailed);
                Debug.LogWarning($"Quest briefing confirmation failed: {exception.Message}");
            }
        }

        /// <summary>切换追踪任务并刷新任务日志和地图 HUD。</summary>
        /// <param name="questId">需要追踪的任务编号。</param>
        internal void TrackQuest(int questId)
        {
            QuestModel.Instance.Track(questId);
            _panel?.Refresh();
            _hud?.Refresh();
        }

        /// <summary>关闭当前任务日志并解除控制器对面板的引用。</summary>
        internal void CloseJournal()
        {
            QuestPanel panel = _panel;
            _panel = null;
            panel?.Close(false);
        }

        /// <summary>仅当传入面板仍为当前任务日志时解除绑定。</summary>
        /// <param name="panel">即将关闭的任务日志面板。</param>
        internal void DetachJournal(QuestPanel panel)
        {
            if (ReferenceEquals(_panel, panel)) _panel = null;
        }

        /// <summary>绑定地图 HUD，并使用最新账号进度更新显示。</summary>
        /// <param name="hud">已由 UI 模块系统打开的 HUD 面板。</param>
        /// <param name="data">已加载的任务配置快照。</param>
        internal void AttachHud(QuestHudPanel hud, QuestData data)
        {
            _hud = hud;
            _hud.Bind(data);
            _hud.Refresh();
        }

        /// <summary>仅当传入面板仍为当前 HUD 时解除绑定。</summary>
        /// <param name="hud">即将关闭的地图 HUD 面板。</param>
        internal void DetachHud(QuestHudPanel hud)
        {
            if (ReferenceEquals(_hud, hud)) _hud = null;
        }

        /// <summary>离开地图时取消简报、关闭任务界面并清理模块会话。</summary>
        internal void CloseMapUI()
        {
            _mapEntryGeneration++;
            _mapEntryInProgress = false;
            _briefingGeneration++;
            _briefingBusy = false;
            _briefingCompletion?.TrySetCanceled();
            _briefingCompletion = null;
            QuestBriefingPanel briefing = _briefing;
            _briefing = null;
            briefing?.Close(false);
            _mapToken = default;
            CloseJournal();
            if (_hud != null)
            {
                QuestHudPanel hud = _hud;
                _hud = null;
                hud.Close(false);
            }
            base.Clear();
        }

        /// <summary>清理任务模块；关闭面板并释放地图会话状态。</summary>
        public override void Clear()
        {
            CloseMapUI();
        }
    }
}
