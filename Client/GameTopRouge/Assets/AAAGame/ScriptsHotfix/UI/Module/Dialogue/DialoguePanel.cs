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
        private readonly TMP_Text[] m_OptionTexts = new TMP_Text[4];
        private readonly UGF_Image[] m_OptionImages = new UGF_Image[4];
        private readonly Outline[] m_OptionOutlines = new Outline[4];
        private TMP_Text m_HistoryButtonText;
        private TMP_Text m_HistoryBackButtonText;
        private RectTransform m_BodyRect;
        /// <summary>仅从容器绑定面板控件。</summary>
        /// <summary>从面板容器绑定显示和交互控件。</summary>
        protected override void RegisterUI()
        {
            #region Auto Bind
            m_SafeArea = PanelUIContainer.Get<RectTransform>("m_SafeArea");
            m_Portrait = PanelUIContainer.Get<UGF_Image>("m_Portrait");
            m_Speaker = PanelUIContainer.Get<TMP_Text>("m_Speaker");
            m_Body = PanelUIContainer.Get<TMP_Text>("m_Body");
            m_ContinueButton = PanelUIContainer.Get<UGF_Button>("m_ContinueButton");
            m_Prompt = PanelUIContainer.Get<TMP_Text>("m_Prompt");
            m_OptionButtons = new UGF_Button[4];
            for (int i = 0; i < 4; i++)
            {
                m_OptionButtons[i] = PanelUIContainer.Get<UGF_Button>("m_OptionButtons" + i);
                m_OptionTexts[i] = PanelUIContainer.Get<TMP_Text>("m_OptionText" + i);
                m_OptionImages[i] = PanelUIContainer.Get<UGF_Image>("m_OptionImage" + i);
                m_OptionOutlines[i] = PanelUIContainer.Get<Outline>("m_OptionOutline" + i);
            }
            m_HistoryButton = PanelUIContainer.Get<UGF_Button>("m_HistoryButton");
            m_HistoryBackButton = PanelUIContainer.Get<UGF_Button>("m_HistoryBackButton");
            m_HistoryRoot = PanelUIContainer.Get<Transform>("m_HistoryRoot");
            m_HistoryText = PanelUIContainer.Get<TMP_Text>("m_HistoryText");
            m_HistoryButtonText = PanelUIContainer.Get<TMP_Text>("m_HistoryButtonText");
            m_HistoryBackButtonText = PanelUIContainer.Get<TMP_Text>("m_HistoryBackButtonText");
            m_BodyRect = PanelUIContainer.Get<RectTransform>("m_BodyRect");
            #endregion
        }
        protected override void RegisterEvent() => WireButtons();

        /// <summary>安全区容器。</summary>
        private RectTransform m_SafeArea;
        /// <summary>头像图像。</summary>
        private UGF_Image m_Portrait;
        /// <summary>统一占位头像。</summary>
        public Sprite Placeholder;
        /// <summary>说话者名称。</summary>
        private TMP_Text m_Speaker;
        /// <summary>正文区域，使用 TMP 实际排版分页。</summary>
        private TMP_Text m_Body;
        /// <summary>正文继续按钮。</summary>
        private UGF_Button m_ContinueButton;
        /// <summary>推进或选择提示。</summary>
        private TMP_Text m_Prompt;
        /// <summary>最多四个独立选项按钮。</summary>
        private UGF_Button[] m_OptionButtons;
        /// <summary>历史入口。</summary>
        private UGF_Button m_HistoryButton;
        /// <summary>历史返回按钮。</summary>
        private UGF_Button m_HistoryBackButton;
        /// <summary>历史遮罩根节点。</summary>
        private Transform m_HistoryRoot;
        /// <summary>可滚动的历史正文。</summary>
        private TMP_Text m_HistoryText;
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
        /// <summary>在演示和正式表单打开时共用初始化。</summary>
        /// <param name="controller">当前会话控制器。</param>
        /// <param name="data">已校验的配置。</param>
        internal void Bind(DialogueController controller, DialogueData data)
        {
            _controller = controller;
            _data = data;
            _bound = true;
            if (m_Body == null)
            {
                RegisterUI();
                RegisterEvent();
            }
            m_HistoryRoot.gameObject.SetActive(false);
            m_HistoryButtonText.text = GF.Localization.GetText(DialogueConst.LocalizationKey.History);
            m_HistoryBackButtonText.text = GF.Localization.GetText(DialogueConst.LocalizationKey.CloseHistory);
            FitSafeArea();
        }
        /// <summary>绑定一次按钮回调，避免对象池重开重复监听。</summary>
        private void WireButtons()
        {
            if (_wired) return;
            _wired = true;
            m_ContinueButton.SetClickListener(AdvancePage);
            m_HistoryButton.SetClickListener(OpenHistory);
            m_HistoryBackButton.SetClickListener(CloseHistory);
            for (int i = 0; i < m_OptionButtons.Length; i++)
            {
                int index = i;
                m_OptionButtons[i].SetClickListener(() => Choose(index));
            }
        }
        /// <summary>显示可见节点，初始化头像、名称和第一页。</summary>
        /// <param name="node">台词或选项节点。</param>
        internal void ShowNode(DialogueNode node)
        {
            _choicesVisible = false;
            foreach (var button in m_OptionButtons) button.gameObject.SetActive(false);
            _options = _data.GetOptions(node.Id);
            var speaker = _data.Npcs.GetOrDefault(node.NpcId);
            m_Speaker.text = speaker == null ? string.Empty : GF.Localization.GetText(speaker.Name);
            m_Speaker.gameObject.SetActive(speaker != null);
            m_Portrait.gameObject.SetActive(speaker != null);
            var bodyRect = m_BodyRect;
            bodyRect.offsetMin = new Vector2(speaker == null ? 40 : 248, 52);
            bodyRect.offsetMax = new Vector2(-40, speaker == null ? -40 : -82);
            SetPortrait(speaker?.PortraitAsset);
            m_Body.richText = false;
            m_Body.text = GF.Localization.GetText(node.TextKey);
            m_Body.maxVisibleCharacters = int.MaxValue;
            m_Body.ForceMeshUpdate();
            _page = 1;
            BeginPage();
        }
        /// <summary>设置占位头像，并在有配置时通过正式资源组件加载。</summary>
        /// <param name="path">允许为空的 Sprite 资源路径。</param>
        private void SetPortrait(string path)
        {
            ReleasePortrait();
            m_Portrait.sprite = Placeholder;
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
                m_Portrait.sprite = _loadedPortrait != null ? _loadedPortrait : Placeholder;
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
            var page = m_Body.textInfo.pageInfo[_page - 1];
            _firstCharacter = page.firstCharacterIndex;
            _lastCharacter = m_Body.textInfo.characterCount == 0 ? 0 : page.lastCharacterIndex + 1;
            _visible = _firstCharacter;
            m_Body.pageToDisplay = _page;
            m_Body.maxVisibleCharacters = _firstCharacter;
            _historyIndex = DialogueModel.Instance.AddHistory(string.Empty);
            m_Prompt.text = string.Empty;
        }
        /// <summary>每帧推进文字并处理不依赖 EventSystem 提交的键盘输入。</summary>
        private void Update()
        {
            if (!_bound) return;
            FitSafeArea();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (m_HistoryRoot.gameObject.activeSelf) CloseHistory();
                else _controller.Finish(DialogueStatus.Cancelled);
                return;
            }
            if (m_HistoryRoot.gameObject.activeSelf) return;
            if (!_choicesVisible)
            {
                _visible = Mathf.Min(_lastCharacter, _visible + Time.unscaledDeltaTime * CharactersPerSecond);
                m_Body.maxVisibleCharacters = Mathf.FloorToInt(_visible);
                UpdateHistory();
                if (m_Body.maxVisibleCharacters >= _lastCharacter)
                {
                    if (_page == m_Body.textInfo.pageCount && _options.Length > 0) ShowChoices();
                    else m_Prompt.text = GF.Localization.GetText(DialogueConst.LocalizationKey.Continue);
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
            int count = m_Body.maxVisibleCharacters;
            if (count <= _firstCharacter) return;
            var first = m_Body.textInfo.characterInfo[_firstCharacter];
            var last = m_Body.textInfo.characterInfo[Math.Min(count, _lastCharacter) - 1];
            string prefix = string.IsNullOrEmpty(m_Speaker.text) ? string.Empty : m_Speaker.text + ": ";
            DialogueModel.Instance.History[_historyIndex] = prefix + m_Body.text.Substring(first.index, last.index + last.stringLength - first.index);
        }
        /// <summary>补全、翻页或推进；选项出现后不推进台词。</summary>
        private void AdvancePage()
        {
            if (!_bound || m_HistoryRoot.gameObject.activeSelf || _choicesVisible) return;
            if (m_Body.maxVisibleCharacters < _lastCharacter)
            {
                _visible = _lastCharacter;
                m_Body.maxVisibleCharacters = _lastCharacter;
                UpdateHistory();
                if (_page == m_Body.textInfo.pageCount && _options.Length > 0) ShowChoices();
                return;
            }
            if (_page < m_Body.textInfo.pageCount) { _page++; BeginPage(); }
            else _controller.Advance();
        }
        /// <summary>正文显示完毕后创建选项焦点，阻止同帧确认。</summary>
        private void ShowChoices()
        {
            _choicesVisible = true;
            _choiceFrame = Time.frameCount;
            m_Prompt.text = GF.Localization.GetText(DialogueConst.LocalizationKey.Choose);
            for (int i = 0; i < _options.Length; i++)
            {
                m_OptionButtons[i].gameObject.SetActive(true);
                m_OptionTexts[i].text = GF.Localization.GetText(_options[i].TextKey);
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
                m_OptionImages[i].color = i == index ? Color.white : new Color(.8f, .8f, .8f, 1);
                var outline = m_OptionOutlines[i];
                if (outline != null) outline.enabled = i == index;
                m_OptionTexts[i].fontStyle = i == index ? FontStyles.Bold : FontStyles.Normal;
            }
            // 所有提交由本组件消费，避免 EventSystem 再次提交到下一节点。
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        /// <summary>消费合法且不在出现帧的选项点击。</summary>
        /// <param name="index">选项数组索引。</param>
        private void Choose(int index)
        {
            if (!_bound || !_choicesVisible || m_HistoryRoot.gameObject.activeSelf || Time.frameCount <= _choiceFrame || index < 0 || index >= _options.Length) return;
            _choicesVisible = false;
            _controller.Choose(_options[index].Id);
        }
        /// <summary>显示可滚动历史并停止当前页打字。</summary>
        private void OpenHistory()
        {
            if (!_bound) return;
            UpdateHistory();
            m_HistoryText.text = string.Join("\n\n", DialogueModel.Instance.History);
            m_HistoryRoot.gameObject.SetActive(true);
        }
        /// <summary>返回正在播放的页面。</summary>
        private void CloseHistory() => m_HistoryRoot.gameObject.SetActive(false);
        /// <summary>将所有内容限定在设备安全区内。</summary>
        private void FitSafeArea()
        {
            Rect safe = Screen.safeArea;
            m_SafeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            m_SafeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            m_SafeArea.offsetMin = m_SafeArea.offsetMax = Vector2.zero;
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
