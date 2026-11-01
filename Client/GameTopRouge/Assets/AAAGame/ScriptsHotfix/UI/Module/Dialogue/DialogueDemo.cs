using System;
using System.Threading;
using cfg;
using cfg.Dialogue;
using cfg.Map;
using Cysharp.Threading.Tasks;
using Luban;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.UI.Module.Dialogue
{
    /// <summary>独立演示入口，使用与正式入口相同的 Luban 产物，不启动登录或网络。</summary>
    public sealed class DialogueDemo : MonoBehaviour
    {
        /// <summary>对话界面预制体。</summary>
        public DialoguePanel PanelPrefab;
        /// <summary>演示画布。</summary>
        public Transform CanvasRoot;
        /// <summary>剧情入口二进制。</summary>
        public TextAsset DialogueBytes;
        /// <summary>统一 NPC 表的二进制配置。</summary>
        public TextAsset NpcBytes;
        /// <summary>节点二进制。</summary>
        public TextAsset NodeBytes;
        /// <summary>选项二进制。</summary>
        public TextAsset OptionBytes;
        /// <summary>本地化二进制。</summary>
        public TextAsset LocalizationBytes;
        /// <summary>开始按钮。</summary>
        public Button StartButton;
        /// <summary>重置按钮。</summary>
        public Button ResetButton;
        /// <summary>标题。</summary>
        public TMP_Text Title;
        /// <summary>操作说明。</summary>
        public TMP_Text Hint;
        /// <summary>结束状态。</summary>
        public TMP_Text Status;
        /// <summary>生成的本地化表。</summary>
        private TbLocalization _localization;
        /// <summary>当前场景取消源。</summary>
        private CancellationTokenSource _lifetime;
        /// <summary>解码配置并绑定演示按钮。</summary>
        private void Start()
        {
            _lifetime = new CancellationTokenSource();
            _localization = new TbLocalization();
            _localization._LoadData(new ByteBuf(LocalizationBytes.bytes));
            var localization = GameFramework.GameFrameworkEntry.GetModule<GameFramework.Localization.ILocalizationManager>();
            foreach (var entry in _localization.DataList)
                if (!localization.HasRawString(entry.Id)) localization.AddRawString(entry.Id, entry.ChineseSimplified);
            StartButton.GetComponentInChildren<TMP_Text>().text = GF.Localization.GetText(DialogueConst.LocalizationKey.Start);
            ResetButton.GetComponentInChildren<TMP_Text>().text = GF.Localization.GetText(DialogueConst.LocalizationKey.Reset);
            Title.text = GF.Localization.GetText(DialogueConst.LocalizationKey.Title);
            Hint.text = GF.Localization.GetText(DialogueConst.LocalizationKey.Hint);
            StartButton.onClick.AddListener(StartConversation);
            ResetButton.onClick.AddListener(ShowResetUnavailableMessage);
        }
        /// <summary>按钮入口负责观察异步任务。</summary>
        private void StartConversation() => RunAsync().Forget(Debug.LogException);
        /// <summary>等待会话完成后恢复演示按钮。</summary>
        /// <returns>整个演示会话任务。</returns>
        private async UniTask RunAsync()
        {
            StartButton.interactable = ResetButton.interactable = false;
            var result = await DialogueController.Instance.PlayAsync(1, OpenPanelAsync, LoadDataAsync, _lifetime.Token);
            if (this == null || _lifetime == null) return;
            Status.text = GF.Localization.GetText(DialogueConst.Text.GetStatusKey(result.Status));
            StartButton.interactable = ResetButton.interactable = true;
        }
        /// <summary>实例化正式对话 Prefab，注入演示本地化读取器。</summary>
        /// <returns>可绑定的对话界面。</returns>
        private UniTask<DialoguePanel> OpenPanelAsync()
        {
            var panel = Instantiate(PanelPrefab, CanvasRoot);
            panel.CloseAction = () => { if (panel != null) Destroy(panel.gameObject); };
            return UniTask.FromResult(panel);
        }
        /// <summary>将场景引用的原始 Luban 二进制转换为相同的配置集合。</summary>
        /// <returns>已解码的剧情数据。</returns>
        private UniTask<DialogueData> LoadDataAsync()
        {
            var data = new DialogueData { Dialogues = new TbDialogue(), Npcs = new TbNpc(), Nodes = new TbDialogueNode(), Options = new TbDialogueOption() };
            data.Dialogues._LoadData(new ByteBuf(DialogueBytes.bytes));
            data.Npcs._LoadData(new ByteBuf(NpcBytes.bytes));
            data.Nodes._LoadData(new ByteBuf(NodeBytes.bytes));
            data.Options._LoadData(new ByteBuf(OptionBytes.bytes));
            data.Validate();
            return UniTask.FromResult(data);
        }
        /// <summary>说明剧情标记由账号进度管理，演示入口不修改存档。</summary>
        private void ShowResetUnavailableMessage()
        {
            Status.text = GF.Localization.GetText(DialogueConst.LocalizationKey.ResetAccountOwned);
        }
        /// <summary>离开演示场景时取消会话，解除按钮监听。</summary>
        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;
            StartButton?.onClick.RemoveListener(StartConversation);
            ResetButton?.onClick.RemoveListener(ShowResetUnavailableMessage);
        }
    }
}
