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
    /// <summary>仅在绑定目标当前进行时开放固定地图任务物件。</summary>
    public sealed class TaskMarkerInteraction : MonoBehaviour
    {
        /// <summary>绑定到该固定物件的地图目标数据。</summary>
        private MapObjectMarker _marker;
        /// <summary>组件启用期间用于取消加载、对话和进度提交的令牌。</summary>
        private CancellationTokenSource _lifetime;
        /// <summary>显示在任务标记上方的调查提示。</summary>
        private TextMeshPro _prompt;
        /// <summary>防止连续输入重复调查或提交目标事件。</summary>
        private bool _pending;

        /// <summary>绑定地图物件目标并加载显示提示所需资源。</summary>
        /// <param name="marker">任务固定物件的地图摆放配置。</param>
        public void Configure(MapObjectMarker marker)
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = new CancellationTokenSource();
            _marker = marker;
            LoadAsync(_lifetime.Token).Forget(Debug.LogException);
        }

        /// <summary>加载任务表并初始化任务交互提示。</summary>
        /// <param name="token">组件失活或地图卸载时取消加载的令牌。</param>
        /// <returns>任务配置和提示初始化完成后的异步任务。</returns>
        private async UniTask LoadAsync(CancellationToken token)
        {
            try { await QuestData.LoadAsync().AttachExternalCancellation(token); }
            catch (OperationCanceledException) { return; }
            if (token.IsCancellationRequested || this == null) return;
            if (_prompt == null)
            {
                var child = new GameObject("TaskMarkerPrompt");
                child.transform.SetParent(transform, false);
                child.transform.localPosition = new Vector3(0, 1.45f, 0);
                _prompt = child.AddComponent<TextMeshPro>();
                _prompt.font = TMP_Settings.defaultFontAsset;
                _prompt.fontSize = 2.6f;
                _prompt.color = new Color32(238, 205, 136, 255);
                _prompt.alignment = TextAlignmentOptions.Center;
                _prompt.rectTransform.sizeDelta = new Vector2(7, 1);
                _prompt.GetComponent<MeshRenderer>().sortingOrder = 1000;
            }
            _prompt.text = QuestConst.Text.Interact;
            _prompt.gameObject.SetActive(false);
        }

        /// <summary>按账号任务进度和玩家距离显示提示并响应调查键。</summary>
        private void Update()
        {
            if (_marker == null || _prompt == null || !LoginModel.Instance.IsAuthenticated) return;
            if (QuestView.Instance.IsBlockingMapInteraction || DialogueView.Instance.IsPlaying)
            { _prompt.gameObject.SetActive(false); return; }
            var role = GameProxyManger.Instance.GetProxy<UnitProxy>()?.MainRole;
            bool alive = role?.Data?.Resource != null && role.Data.Resource.hp > 0;
            if (_pending && !alive) _lifetime?.Cancel();
            bool active = PlayerProgressService.IsObjectiveActive(_marker.objectiveId, out _);
            bool near = false;
            if (alive && role.RoleBehaviour != null)
            {
                var position = role.RoleBehaviour.Position;
                near = Vector2.Distance(new Vector2((float)position.x, (float)position.y), transform.position) <= _marker.radius;
            }
            bool available = active && near && !_pending && !DialogueView.Instance.IsPlaying;
            _prompt.gameObject.SetActive(available);
            if (available && Input.GetKeyDown(KeyCode.E)) InspectAsync(_lifetime.Token).Forget(Debug.LogException);
        }

        /// <summary>播放调查反馈对话并提交已完成的目标事件。</summary>
        /// <param name="token">组件失活或地图卸载时取消调查的令牌。</param>
        /// <returns>调查对话及进度提交完成后的异步任务。</returns>
        private async UniTask InspectAsync(CancellationToken token)
        {
            if (_pending || !PlayerProgressService.IsObjectiveActive(_marker.objectiveId, out int questId)) return;
            QuestData data = await QuestData.LoadAsync();
            QuestObjective objective = data.Objectives.GetOrDefault(_marker.objectiveId);
            if (objective == null || objective.Type != ObjectiveType.InspectObject) return;
            _pending = true;
            InputProxy input = GameProxyManger.Instance.GetProxy<InputProxy>();
            input?.SetDialogueInputBlocked(true);
            try
            {
                if (objective.DialogueId > 0)
                {
                    DialogueResult dialogue = await DialogueView.Instance.PlayAsync(objective.DialogueId, token);
                    if (dialogue.Status != DialogueStatus.Completed) return;
                }
                await PlayerProgressService.SubmitEventAsync(new PlayerProgressEvent
                {
                    Type = PlayerProgressEventType.ObjectiveProgress,
                    QuestId = questId,
                    ObjectiveId = objective.Id,
                    MapId = _marker.mapId,
                    MapInstanceId = _marker.instanceId,
                    SourceId = _marker.objectId,
                    Count = 1,
                    DialogueId = objective.DialogueId
                }, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogWarning($"Task marker interaction failed: {exception.Message}"); }
            finally
            {
                input?.SetDialogueInputBlocked(false);
                _pending = false;
            }
        }

        /// <summary>组件失活时取消调查流程并隐藏交互提示。</summary>
        private void OnDisable()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
            if (_prompt != null) _prompt.gameObject.SetActive(false);
        }
    }
}
