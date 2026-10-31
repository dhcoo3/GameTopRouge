using System;
using System.Collections.Generic;
using cfg.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameFramework.Resource;

namespace AAAGame.ScriptsHotfix.UI.Module.Dialogue
{
    /// <summary>头像对话表单，负责打字、分页、历史和键鼠输入。</summary>
    public sealed class DialoguePanel : UGF_Panel
    {
        /// <summary>每秒显示的可见字符数。</summary>
        private const float CharactersPerSecond = 30;
        /// <summary>安全区容器。</summary>
        public RectTransform SafeArea;
        /// <summary>头像图像。</summary>
        public Image Portrait;
        /// <summary>统一占位头像。</summary>
        public Sprite Placeholder;
        /// <summary>说话者名称。</summary>
        public TMP_Text Speaker;
        /// <summary>正文区域，使用 TMP 实际排版分页。</summary>
        public TMP_Text Body;
        /// <summary>正文继续按钮。</summary>
        public Button ContinueButton;
        /// <summary>推进或选择提示。</summary>
        public TMP_Text Prompt;
        /// <summary>最多四个独立选项按钮。</summary>
        public Button[] OptionButtons;
        /// <summary>历史入口。</summary>
        public Button HistoryButton;
        /// <summary>历史返回按钮。</summary>
        public Button HistoryBackButton;
        /// <summary>历史遮罩根节点。</summary>
        public GameObject HistoryRoot;
        /// <summary>可滚动的历史正文。</summary>
        public TMP_Text HistoryText;
        /// <summary>正式资源模式使用 GF 本地化，演示注入已生成的数据。</summary>
        internal Func<string, string> Localize;
        /// <summary>当前控制器。</summary>
        private DialogueController _controller;
        /// <summary>当前配置集合。</summary>
        private DialogueData _data;
        /// <summary>本次释放动作，演示销毁实例，正式入口关闭表单。</summary>
        internal Action CloseAction;
        /// <summary>当前页编号，从一开始。</summary>
        private int _page;
        /// <summary>当前页第一个可见字符的索引。</summary>
        private int _firstCharacter;
        /// <summary>当前页末尾后的字符索引。</summary>
        private int _lastCharacter;
        /// <summary>当前页已经显示的字符浮点计数。</summary>
        private float _visible;
        /// <summary>当前页对应的历史条目索引。</summary>
        private int _historyIndex;
        /// <summary>选项显示的帧，阻止同帧确认。</summary>
        private int _choiceFrame;
        /// <summary>当前键盘选项索引。</summary>
        private int _selected;
        /// <summary>当前节点选项。</summary>
        private DialogueOption[] _options = Array.Empty<DialogueOption>();
        /// <summary>防止异步头像回调污染新节点的版本号。</summary>
        private int _portraitVersion;
        /// <summary>当前由资源组件持有的头像。</summary>
        private Sprite _loadedPortrait;
        /// <summary>避免重复绑定按钮。</summary>
        private bool _wired;
        /// <summary>当前是否已经展示选项。</summary>
        private bool _choicesVisible;
        /// <summary>控制器绑定后才接受输入。</summary>
        private bool _bound;
        /// <summary>读取本地化文案。</summary>
        /// <param name="key">稳定文案键。</param>
        /// <returns>对应的玩家文案。</returns>
        internal string Text(string key) => Localize != null ? Localize(key) : GF.Localization.GetText(key);
        /// <summary>在演示和正式表单打开时共用初始化。</summary>
        /// <param name="controller">当前会话控制器。</param>
        /// <param name="data">已校验的配置。</param>
        internal void Bind(DialogueController controller, DialogueData data)
        {
            _controller = controller;
            _data = data;
            _bound = true;
            WireButtons();
            HistoryRoot.SetActive(false);
            HistoryButton.GetComponentInChildren<TMP_Text>().text = Text(DialogueConst.LocalizationKey.History);
            HistoryBackButton.GetComponentInChildren<TMP_Text>().text = Text(DialogueConst.LocalizationKey.CloseHistory);
            FitSafeArea();
        }
        /// <summary>绑定一次按钮回调，避免对象池重开重复监听。</summary>
        private void WireButtons()
        {
            if (_wired) return;
            _wired = true;
            ContinueButton.onClick.AddListener(AdvancePage);
            HistoryButton.onClick.AddListener(OpenHistory);
            HistoryBackButton.onClick.AddListener(CloseHistory);
            for (int i = 0; i < OptionButtons.Length; i++)
            {
                int index = i;
                OptionButtons[i].onClick.AddListener(() => Choose(index));
            }
        }
        /// <summary>显示可见节点，初始化头像、名称和第一页。</summary>
        /// <param name="node">台词或选项节点。</param>
        internal void ShowNode(DialogueNode node)
        {
            _choicesVisible = false;
            foreach (var button in OptionButtons) button.gameObject.SetActive(false);
            _options = _data.GetOptions(node.Id);
            var speaker = _data.Npcs.GetOrDefault(node.NpcId);
            Speaker.text = speaker == null ? string.Empty : Text(speaker.Name);
            Speaker.gameObject.SetActive(speaker != null);
            Portrait.gameObject.SetActive(speaker != null);
            var bodyRect = Body.rectTransform;
            bodyRect.offsetMin = new Vector2(speaker == null ? 40 : 248, 52);
            bodyRect.offsetMax = new Vector2(-40, speaker == null ? -40 : -82);
            SetPortrait(speaker?.PortraitAsset);
            Body.richText = false;
            Body.text = Text(node.TextKey);
            Body.maxVisibleCharacters = int.MaxValue;
            Body.ForceMeshUpdate();
            _page = 1;
            BeginPage();
        }
        /// <summary>设置占位头像，并在有配置时通过正式资源组件加载。</summary>
        /// <param name="path">允许为空的 Sprite 资源路径。</param>
        private void SetPortrait(string path)
        {
            ReleasePortrait();
            Portrait.sprite = Placeholder;
            int version = _portraitVersion;
            if (string.IsNullOrEmpty(path)) return;
            GF.Resource.LoadAsset(path, typeof(Sprite), new LoadAssetCallbacks((name, asset, duration, context) =>
            {
                if (this == null || !_bound || version != _portraitVersion)
                {
                    GF.Resource.UnloadAsset(asset);
                    return;
                }
                _loadedPortrait = asset as Sprite;
                Portrait.sprite = _loadedPortrait != null ? _loadedPortrait : Placeholder;
            }, (name, status, message, context) => Debug.LogWarning($"Dialogue portrait unavailable: {name}: {message}")));
        }
        /// <summary>清理头像资源并使旧加载回调失效。</summary>
        private void ReleasePortrait()
        {
            _portraitVersion++;
            if (_loadedPortrait != null) GF.Resource.UnloadAsset(_loadedPortrait);
            _loadedPortrait = null;
        }
        /// <summary>按 TMP 实际分页信息启动当前页。</summary>
        private void BeginPage()
        {
            var page = Body.textInfo.pageInfo[_page - 1];
            _firstCharacter = page.firstCharacterIndex;
            _lastCharacter = Body.textInfo.characterCount == 0 ? 0 : page.lastCharacterIndex + 1;
            _visible = _firstCharacter;
            Body.pageToDisplay = _page;
            Body.maxVisibleCharacters = _firstCharacter;
            _historyIndex = DialogueModel.Instance.AddHistory(string.Empty);
            Prompt.text = string.Empty;
        }
        /// <summary>每帧推进文字并处理不依赖 EventSystem 提交的键盘输入。</summary>
        private void Update()
        {
            if (!_bound) return;
            FitSafeArea();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (HistoryRoot.activeSelf) CloseHistory();
                else _controller.Finish(DialogueStatus.Cancelled);
                return;
            }
            if (HistoryRoot.activeSelf) return;
            if (!_choicesVisible)
            {
                _visible = Mathf.Min(_lastCharacter, _visible + Time.unscaledDeltaTime * CharactersPerSecond);
                Body.maxVisibleCharacters = Mathf.FloorToInt(_visible);
                UpdateHistory();
                if (Body.maxVisibleCharacters >= _lastCharacter)
                {
                    if (_page == Body.textInfo.pageCount && _options.Length > 0) ShowChoices();
                    else Prompt.text = Text(DialogueConst.LocalizationKey.Continue);
                }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) AdvancePage();
            }
            else if (Time.frameCount > _choiceFrame)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) SetSelected((_selected + _options.Length - 1) % _options.Length);
                if (Input.GetKeyDown(KeyCode.DownArrow)) SetSelected((_selected + 1) % _options.Length);
                if (Input.GetKeyDown(KeyCode.Return)) Choose(_selected);
            }
        }
        /// <summary>记录已经实际显示的字符，取消时不暴露后续台词。</summary>
        private void UpdateHistory()
        {
            int count = Body.maxVisibleCharacters;
            if (count <= _firstCharacter) return;
            var first = Body.textInfo.characterInfo[_firstCharacter];
            var last = Body.textInfo.characterInfo[Math.Min(count, _lastCharacter) - 1];
            string prefix = string.IsNullOrEmpty(Speaker.text) ? string.Empty : Speaker.text + ": ";
            DialogueModel.Instance.History[_historyIndex] = prefix + Body.text.Substring(first.index, last.index + last.stringLength - first.index);
        }
        /// <summary>补全、翻页或推进；选项出现后不推进台词。</summary>
        private void AdvancePage()
        {
            if (!_bound || HistoryRoot.activeSelf || _choicesVisible) return;
            if (Body.maxVisibleCharacters < _lastCharacter)
            {
                _visible = _lastCharacter;
                Body.maxVisibleCharacters = _lastCharacter;
                UpdateHistory();
                if (_page == Body.textInfo.pageCount && _options.Length > 0) ShowChoices();
                return;
            }
            if (_page < Body.textInfo.pageCount) { _page++; BeginPage(); }
            else _controller.Advance();
        }
        /// <summary>正文显示完毕后创建选项焦点，阻止同帧确认。</summary>
        private void ShowChoices()
        {
            _choicesVisible = true;
            _choiceFrame = Time.frameCount;
            Prompt.text = Text(DialogueConst.LocalizationKey.Choose);
            for (int i = 0; i < _options.Length; i++)
            {
                OptionButtons[i].gameObject.SetActive(true);
                OptionButtons[i].GetComponentInChildren<TMP_Text>().text = Text(_options[i].TextKey);
            }
            SetSelected(0);
        }
        /// <summary>更新显式键盘焦点颜色和边框。</summary>
        /// <param name="index">合法选项序号。</param>
        private void SetSelected(int index)
        {
            _selected = index;
            for (int i = 0; i < _options.Length; i++)
            {
                OptionButtons[i].GetComponent<Image>().color = i == index ? Color.white : new Color(.8f, .8f, .8f, 1);
                var outline = OptionButtons[i].GetComponent<Outline>();
                if (outline != null) outline.enabled = i == index;
                OptionButtons[i].GetComponentInChildren<TMP_Text>().fontStyle = i == index ? FontStyles.Bold : FontStyles.Normal;
            }
            // 所有提交由本组件消费，避免 EventSystem 再次提交到下一节点。
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        /// <summary>消费合法且不在出现帧的选项点击。</summary>
        /// <param name="index">选项数组索引。</param>
        private void Choose(int index)
        {
            if (!_bound || !_choicesVisible || HistoryRoot.activeSelf || Time.frameCount <= _choiceFrame || index < 0 || index >= _options.Length) return;
            _choicesVisible = false;
            _controller.Choose(_options[index].Id);
        }
        /// <summary>显示可滚动历史并停止当前页打字。</summary>
        private void OpenHistory()
        {
            if (!_bound) return;
            UpdateHistory();
            HistoryText.text = string.Join("\n\n", DialogueModel.Instance.History);
            HistoryRoot.SetActive(true);
        }
        /// <summary>返回正在播放的页面。</summary>
        private void CloseHistory() => HistoryRoot.SetActive(false);
        /// <summary>将所有内容限定在设备安全区内。</summary>
        private void FitSafeArea()
        {
            Rect safe = Screen.safeArea;
            SafeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            SafeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            SafeArea.offsetMin = SafeArea.offsetMax = Vector2.zero;
        }
        /// <summary>由控制器统一关闭界面。</summary>
        internal void Release()
        {
            _bound = false;
            ReleasePortrait();
            CloseAction?.Invoke();
            CloseAction = null;
        }
        /// <summary>表单被外部关闭时通知控制器取消。</summary>
        /// <param name="isShutdown">是否正在关闭应用。</param>
        /// <param name="userData">框架关闭参数。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            OnDisabled();
            base.OnClose(isShutdown, userData);
        }
        /// <summary>演示实例和池化表单失活都结束会话。</summary>
        private void OnDisable() => OnDisabled();
        /// <summary>使界面回调失效并解除资源持有。</summary>
        private void OnDisabled()
        {
            if (_bound) _controller?.Finish(DialogueStatus.Cancelled);
            _bound = false;
            ReleasePortrait();
        }
    }
}
