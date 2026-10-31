using System;
using System.Threading;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using AAAGame.ScriptsHotfix.GamePlay.Quest;
using AAAGame.ScriptsHotfix.UI.Module.Dialogue;
using AAAGame.ScriptsHotfix.UI.Module.Login;
using AAAGame.ScriptsHotfix.UI.Module.Quest;
using cfg.Map;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using cfg.Quest;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>在地图 NPC 附近显示交谈提示，并提交配置绑定的任务对话目标。</summary>
    public sealed class NpcDialogueInteraction : MonoBehaviour
    {
        /// <summary>当前地图 NPC 的显示及交互配置。</summary>
        private MapPlacementMarker _marker;
        /// <summary>NPC 配置表中绑定的对话编号。</summary>
        private int _dialogueId;
        /// <summary>组件启用期间用于取消异步加载和会话的生命周期令牌。</summary>
        private CancellationTokenSource _lifetime;
        /// <summary>显示在 NPC 上方的交谈提示。</summary>
        private TextMeshPro _prompt;
        /// <summary>防止同一 NPC 重复启动对话或提交事件。</summary>
        private bool _pending;

        /// <summary>绑定地图摆放数据并启动 NPC 对话配置加载。</summary>
        /// <param name="marker">当前 NPC 的地图实例及交互范围。</param>
        public void Configure(MapPlacementMarker marker)
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = new CancellationTokenSource();
            _marker = marker;
            _dialogueId = 0;
            LoadAsync(_lifetime.Token).Forget(Debug.LogException);
        }

        /// <summary>异步加载 NPC 与任务配置并初始化交谈提示。</summary>
        /// <param name="token">组件离开地图或失活时取消加载的令牌。</param>
        /// <returns>配置与提示初始化完成后的异步任务。</returns>
        private async UniTask LoadAsync(CancellationToken token)
        {
            TbNpc table;
            try
            {
                table = await GF.LuBan.Get<TbNpc>(cfg.Tables.map_tbnpc);
                await QuestData.LoadAsync().AttachExternalCancellation(token);
            }
            catch (OperationCanceledException) { return; }
            if (token.IsCancellationRequested || this == null) return;
            _dialogueId = table.GetOrDefault(_marker.npcId)?.DialogueId ?? 0;
            if (_prompt == null)
            {
                var child = new GameObject("DialogueInteractionPrompt");
                child.transform.SetParent(transform, false);
                child.transform.localPosition = new Vector3(0, 1.5f, 0);
                _prompt = child.AddComponent<TextMeshPro>();
                _prompt.font = TMP_Settings.defaultFontAsset;
                _prompt.fontSize = 3;
                _prompt.alignment = TextAlignmentOptions.Center;
                _prompt.rectTransform.sizeDelta = new Vector2(6, 1);
                _prompt.GetComponent<MeshRenderer>().sortingOrder = 1000;
            }
            _prompt.text = DialogueConst.Text.Interact;
            _prompt.gameObject.SetActive(false);
        }

        /// <summary>更新交谈提示，并在玩家靠近时响应交互键。</summary>
        private void Update()
        {
            if (_marker == null || _dialogueId == 0 || _prompt == null) return;
            if (QuestView.Instance.IsBlockingMapInteraction)
            { _prompt.gameObject.SetActive(false); return; }
            bool questBound = PlayerProgressService.HasQuestDialogueInteraction(_marker.mapId, _marker.npcId, _dialogueId);
            bool questObjectiveActive = !questBound ||
                PlayerProgressService.FindActiveDialogueObjective(_marker.mapId, _marker.npcId, _dialogueId) != null ||
                PlayerProgressService.FindAcceptableQuestForNpc(_marker.mapId, _marker.npcId) > 0;
            var role = GameProxyManger.Instance.GetProxy<UnitProxy>()?.MainRole;
            bool alive = role?.Data?.Resource != null && role.Data.Resource.hp > 0;
            if (_pending && (!alive || !LoginModel.Instance.IsAuthenticated)) _lifetime?.Cancel();
            bool near = false;
            if (alive && role.RoleBehaviour != null)
            {
                var position = role.RoleBehaviour.Position;
                near = Vector2.Distance(new Vector2((float)position.x, (float)position.y), transform.position) <= _marker.radius;
            }
            bool available = questObjectiveActive && near && !_pending && !DialogueView.Instance.IsPlaying;
            _prompt.gameObject.SetActive(available);
            if (available && Input.GetKeyDown(KeyCode.E)) TalkAsync(_lifetime.Token).Forget(Debug.LogException);
        }

        /// <summary>播放 NPC 对话，完成后提交对应的任务事件。</summary>
        /// <param name="token">组件失活或地图切换时取消交谈的令牌。</param>
        /// <returns>对话及后续进度提交完成后的异步任务。</returns>
        private async UniTask TalkAsync(CancellationToken token)
        {
            if (_pending) return;
            bool questBound = PlayerProgressService.HasQuestDialogueInteraction(_marker.mapId, _marker.npcId, _dialogueId);
            int objectiveQuestId = 0;
            QuestObjective objective = PlayerProgressService.FindActiveDialogueObjective(_marker.mapId, _marker.npcId, _dialogueId, out objectiveQuestId);
            int acceptQuestId = PlayerProgressService.FindAcceptableQuestForNpc(_marker.mapId, _marker.npcId);
            if (questBound && objective == null && acceptQuestId == 0) return;
            _pending = true;
            InputProxy input = GameProxyManger.Instance.GetProxy<InputProxy>();
            input?.SetDialogueInputBlocked(true);
            try
            {
                DialogueResult result = await DialogueView.Instance.PlayAsync(_dialogueId, token);
                if (result.Status != DialogueStatus.Completed) return;
                if (objective != null)
                {
                    await PlayerProgressService.SubmitEventAsync(new PlayerProgressEvent
                    {
                        Type = PlayerProgressEventType.ObjectiveProgress,
                        QuestId = objectiveQuestId,
                        ObjectiveId = objective.Id,
                        MapId = _marker.mapId,
                        SourceId = _marker.npcId,
                        Count = 1,
                        DialogueId = _dialogueId
                    }, token);
                }
                if (acceptQuestId > 0)
                {
                    await PlayerProgressService.SubmitEventAsync(new PlayerProgressEvent
                    {
                        Type = PlayerProgressEventType.AcceptQuest,
                        QuestId = acceptQuestId,
                        SourceId = _marker.npcId
                    }, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogWarning($"NPC dialogue interaction failed: {exception.Message}"); }
            finally
            {
                input?.SetDialogueInputBlocked(false);
                _pending = false;
            }
        }

        /// <summary>组件失活时取消当前交谈并释放生命周期令牌。</summary>
        private void OnDisable()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
            if (_prompt != null) _prompt.gameObject.SetActive(false);
        }
    }
}
