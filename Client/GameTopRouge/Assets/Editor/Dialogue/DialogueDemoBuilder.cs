using System.IO;
using AAAGame.ScriptsHotfix.UI.Module.Dialogue;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>生成可编辑的对话素材、Prefab 和独立演示场景。</summary>
public static class DialogueDemoBuilder
{
    /// <summary>模块资源根目录。</summary>
    private const string ArtRoot = "Assets/AAAGame/Sprites/UI/Dialogue";
    /// <summary>对话 Prefab 的正式模块路径。</summary>
    private const string PrefabPath = "Assets/AAAGame/Prefabs/UI/Dialogue/DialoguePanel.prefab";
    /// <summary>独立演示场景路径。</summary>
    public const string ScenePath = "Assets/AAAGame/Scene/Dialogue/DialogueDemo.unity";
    /// <summary>统一字体。</summary>
    private static TMP_FontAsset _font;
    /// <summary>九宫格面板素材。</summary>
    private static Sprite _panelSprite;
    /// <summary>默认按钮素材。</summary>
    private static Sprite _buttonSprite;
    /// <summary>旧铜色。</summary>
    private static readonly Color Copper = new Color32(141, 115, 80, 255);
    /// <summary>正文颜色。</summary>
    private static readonly Color Ink = new Color32(216, 204, 182, 255);

    /// <summary>以附加场景方式制作资产，不保存或关闭用户场景。</summary>
    [MenuItem("Tools/Dialogue/Rebuild Demo Assets")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode before building dialogue assets.");
        Directory.CreateDirectory(ArtRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/AAAGame/Font/Common/思源黑体 SDF.asset");
        _panelSprite = MakeSprite("Panel", false, false);
        _buttonSprite = MakeSprite("Option", true, false);
        var portrait = MakeSprite("PortraitPlaceholder", false, true);
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var canvas = RootCanvas("DialogueDemoCanvas");
            var root = Rect("DialoguePanel", canvas.transform);
            Stretch(root);
            root.gameObject.AddComponent<Canvas>();
            root.gameObject.AddComponent<GraphicRaycaster>();
            var panel = root.gameObject.AddComponent<DialoguePanel>();
            panel.OpenFade = false;
            panel.Placeholder = portrait;
            var block = root.gameObject.AddComponent<UGF_Image>();
            block.color = new Color(0, 0, 0, .12f);
            var safe = Rect("SafeArea", root);
            Stretch(safe);
            panel.PanelUIContainer.AddNode("m_SafeArea", (safe).gameObject, "RectTransform", "private");
            var frame = Rect("DialogueFrame", safe);
            frame.anchorMin = new Vector2(.0625f, 0);
            frame.anchorMax = new Vector2(.9375f, 0);
            frame.pivot = new Vector2(.5f, 0);
            frame.anchoredPosition = new Vector2(0, 48);
            frame.sizeDelta = new Vector2(0, 280);
            var background = frame.gameObject.AddComponent<UGF_Image>();
            background.sprite = _panelSprite;
            background.type = Image.Type.Sliced;
            panel.PanelUIContainer.AddNode("m_ContinueButton", (frame.gameObject.AddComponent<UGF_Button>()).gameObject, "UGF_Button", "private");
            panel.PanelUIContainer.Get<UGF_Button>("m_ContinueButton").transition = Selectable.Transition.None;
            panel.PanelUIContainer.Get<UGF_Button>("m_ContinueButton").navigation = new Navigation { mode = Navigation.Mode.None };
            var portraitRect = Rect("Portrait", frame);
            Fixed(portraitRect, new Vector2(40, -52), new Vector2(176, 176), new Vector2(0, 1));
            panel.PanelUIContainer.AddNode("m_Portrait", (portraitRect.gameObject.AddComponent<UGF_Image>()).gameObject, "UGF_Image", "private");
            panel.PanelUIContainer.Get<UGF_Image>("m_Portrait").sprite = portrait;
            panel.PanelUIContainer.Get<UGF_Image>("m_Portrait").preserveAspect = true;
            panel.PanelUIContainer.Get<UGF_Image>("m_Portrait").raycastTarget = false;
            panel.PanelUIContainer.AddNode("m_Speaker", (Label("Speaker", frame, 30)).gameObject, "TMP_Text", "private");
            Fixed(panel.PanelUIContainer.Get<TMP_Text>("m_Speaker").rectTransform, new Vector2(248, -30), new Vector2(1000, 42), new Vector2(0, 1));
            panel.PanelUIContainer.Get<TMP_Text>("m_Speaker").color = new Color32(201, 149, 76, 255);
            panel.PanelUIContainer.AddNode("m_Body", (Label("Body", frame, 28)).gameObject, "TMP_Text", "private");
            Stretch(panel.PanelUIContainer.Get<TMP_Text>("m_Body").rectTransform);
            panel.PanelUIContainer.Get<TMP_Text>("m_Body").rectTransform.offsetMin = new Vector2(248, 52);
            panel.PanelUIContainer.Get<TMP_Text>("m_Body").rectTransform.offsetMax = new Vector2(-40, -82);
            panel.PanelUIContainer.Get<TMP_Text>("m_Body").overflowMode = TextOverflowModes.Page;
            panel.PanelUIContainer.Get<TMP_Text>("m_Body").lineSpacing = 3;
            panel.PanelUIContainer.AddNode("m_Prompt", (Label("Prompt", frame, 20)).gameObject, "TMP_Text", "private");
            Fixed(panel.PanelUIContainer.Get<TMP_Text>("m_Prompt").rectTransform, new Vector2(-40, 18), new Vector2(220, 30), new Vector2(1, 0));
            panel.PanelUIContainer.Get<TMP_Text>("m_Prompt").alignment = TextAlignmentOptions.Right;
            var optionButtons = new UGF_Button[4];
            for (int i = 0; i < 4; i++)
            {
                var option = Button("Option" + (i + 1), safe, 26);
                var rect = (RectTransform)option.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
                rect.pivot = new Vector2(.5f, 0);
                rect.sizeDelta = new Vector2(840, 64);
                rect.anchoredPosition = new Vector2(0, 348 + (3 - i) * 76);
                optionButtons[i] = option;
                var outline = option.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(201, 149, 76, 255);
                outline.effectDistance = new Vector2(2, -2);
                outline.enabled = false;
                option.gameObject.SetActive(false);
            }
            panel.PanelUIContainer.AddNode("m_HistoryButton", (Button("History", safe, 24)).gameObject, "UGF_Button", "private");
            Fixed((RectTransform)panel.PanelUIContainer.Get<UGF_Button>("m_HistoryButton").transform, new Vector2(-48, -40), new Vector2(144, 56), new Vector2(1, 1));
            BuildHistory(panel, safe);
            var container = panel.PanelUIContainer;
            container.AddNode("m_BodyRect", container.Get<TMP_Text>("m_Body").gameObject, "RectTransform", "private");
            foreach (var key in new[] { "HistoryButton", "HistoryBackButton" })
                container.AddNode("m_" + key + "Text", container.Get<UGF_Button>("m_" + key).GetComponentInChildren<TMP_Text>(true).gameObject, "TMP_Text", "private");
            for (int i = 0; i < 4; i++)
            {
                var option = optionButtons[i];
                container.AddNode("m_OptionButtons" + i, option.gameObject, "UGF_Button", "private");
                container.AddNode("m_OptionText" + i, option.GetComponentInChildren<TMP_Text>(true).gameObject, "TMP_Text", "private");
                container.AddNode("m_OptionImage" + i, option.gameObject, "UGF_Image", "private");
                container.AddNode("m_OptionOutline" + i, option.gameObject, "Outline", "private");
            }
            // 容器引用的实际节点也必须使用 m_ 前缀。
            foreach (var entry in container.UIContainerDict)
                if (entry.Value.NodeObj != null && !entry.Value.NodeObj.name.StartsWith("m_"))
                    entry.Value.NodeObj.name = "m_" + entry.Value.NodeObj.name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath).GetComponent<DialoguePanel>();
            Object.DestroyImmediate(root.gameObject);
            BuildDemo(canvas, prefab);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>生成像素九宫格或透明头像，每个素材为独立文件。</summary>
    /// <param name="name">素材文件名。</param>
    /// <param name="button">是否使用按钮尺寸。</param>
    /// <param name="portrait">是否绘制占位头像。</param>
    /// <returns>导入后的 Sprite。</returns>
    private static Sprite MakeSprite(string name, bool button, bool portrait)
    {
        int size = portrait ? 64 : 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int edge = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                Color color = edge == 1 ? Copper : edge == 3 ? new Color32(81, 70, 56, 255) : new Color32(button ? (byte)36 : (byte)27, button ? (byte)34 : (byte)26, button ? (byte)30 : (byte)24, 255);
                if (portrait)
                {
                    color = new Color32(36, 34, 30, 255);
                    bool shoulders = y < 23 && x > 9 && x < 55;
                    bool hood = y >= 20 && y < 55 && x > 17 && x < 47;
                    bool face = y >= 27 && y < 45 && x > 24 && x < 40;
                    if (shoulders || hood) color = new Color32(81, 70, 56, 255);
                    if (face) color = new Color32(167, 155, 134, 255);
                    if (y == 38 && (x == 28 || x == 35)) color = Color.black;
                }
                texture.SetPixel(x, y, color);
            }
        texture.Apply();
        string path = ArtRoot + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = portrait ? Vector4.zero : new Vector4(6, 6, 6, 6);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    /// <summary>创建自适应画布。</summary>
    /// <param name="name">画布名称。</param>
    /// <returns>根画布。</returns>
    private static Canvas RootCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        return canvas;
    }
    /// <summary>创建 UI 变换节点。</summary>
    /// <param name="name">节点名。</param>
    /// <param name="parent">所属父节点。</param>
    /// <returns>新节点。</returns>
    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
    /// <summary>拉伸填满父节点。</summary>
    /// <param name="rect">目标节点。</param>
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    /// <summary>设置固定大小的锚点布局。</summary>
    /// <param name="rect">目标节点。</param>
    /// <param name="position">相对锚点的位置。</param>
    /// <param name="size">基准分辨率尺寸。</param>
    /// <param name="anchor">锚点和轴心。</param>
    private static void Fixed(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
    /// <summary>创建正文文本，全部文案由运行时本地化赋值。</summary>
    /// <param name="name">节点名。</param>
    /// <param name="parent">父节点。</param>
    /// <param name="size">字号。</param>
    /// <returns>可编辑的文本组件。</returns>
    private static TMP_Text Label(string name, Transform parent, float size)
    {
        var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.fontSize = size;
        text.color = Ink;
        text.raycastTarget = false;
        text.richText = false;
        text.text = string.Empty;
        return text;
    }
    /// <summary>创建独立按钮和文字层。</summary>
    /// <param name="name">按钮名。</param>
    /// <param name="parent">父节点。</param>
    /// <param name="fontSize">文字字号。</param>
    /// <returns>按钮。</returns>
    private static UGF_Button Button(string name, Transform parent, int fontSize)
    {
        var rect = Rect(name, parent);
        var image = rect.gameObject.AddComponent<UGF_Image>();
        image.sprite = _buttonSprite;
        image.type = Image.Type.Sliced;
        var button = rect.gameObject.AddComponent<UGF_Button>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var text = Label("Label", rect, fontSize);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(24, 8);
        text.rectTransform.offsetMax = new Vector2(-24, -8);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }
    /// <summary>创建带滚动视口的历史覆盖层。</summary>
    /// <param name="panel">目标表单。</param>
    /// <param name="parent">安全区容器。</param>
    private static void BuildHistory(DialoguePanel panel, RectTransform parent)
    {
        var overlay = Rect("HistoryOverlay", parent);
        Stretch(overlay);
        overlay.gameObject.AddComponent<UGF_Image>().color = new Color(0, 0, 0, .88f);
        panel.PanelUIContainer.AddNode("m_HistoryRoot", overlay.gameObject, "Transform", "private");
        var viewport = Rect("Viewport", overlay);
        Stretch(viewport);
        viewport.offsetMin = new Vector2(180, 150);
        viewport.offsetMax = new Vector2(-180, -100);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        viewport.gameObject.AddComponent<UGF_Image>().color = new Color(0, 0, 0, .01f);
        panel.PanelUIContainer.AddNode("m_HistoryText", (Label("HistoryText", viewport, 28)).gameObject, "TMP_Text", "private");
        var content = panel.PanelUIContainer.Get<TMP_Text>("m_HistoryText").rectTransform;
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1);
        content.sizeDelta = Vector2.zero;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        panel.PanelUIContainer.AddNode("m_HistoryBackButton", (Button("Back", overlay, 24)).gameObject, "UGF_Button", "private");
        Fixed((RectTransform)panel.PanelUIContainer.Get<UGF_Button>("m_HistoryBackButton").transform, new Vector2(0, 48), new Vector2(240, 64), new Vector2(.5f, 0));
        overlay.gameObject.SetActive(false);
    }
    /// <summary>构建没有登录或网络依赖的独立演示入口。</summary>
    /// <param name="canvas">演示画布。</param>
    /// <param name="prefab">正式对话预制体。</param>
    private static void BuildDemo(Canvas canvas, DialoguePanel prefab)
    {
        var backdrop = Rect("Backdrop", canvas.transform);
        Stretch(backdrop);
        backdrop.gameObject.AddComponent<UGF_Image>().color = new Color32(22, 23, 21, 255);
        var demo = new GameObject("DialogueDemo").AddComponent<DialogueDemo>();
        demo.PanelPrefab = prefab;
        demo.CanvasRoot = canvas.transform;
        demo.Title = Label("Title", canvas.transform, 48);
        Fixed(demo.Title.rectTransform, new Vector2(120, -120), new Vector2(1200, 90), new Vector2(0, 1));
        demo.Hint = Label("Hint", canvas.transform, 24);
        Fixed(demo.Hint.rectTransform, new Vector2(120, -220), new Vector2(1500, 60), new Vector2(0, 1));
        demo.StartButton = Button("Start", canvas.transform, 28);
        Fixed((RectTransform)demo.StartButton.transform, new Vector2(120, -340), new Vector2(320, 76), new Vector2(0, 1));
        demo.ResetButton = Button("Reset", canvas.transform, 28);
        Fixed((RectTransform)demo.ResetButton.transform, new Vector2(480, -340), new Vector2(320, 76), new Vector2(0, 1));
        demo.Status = Label("Status", canvas.transform, 24);
        Fixed(demo.Status.rectTransform, new Vector2(120, -450), new Vector2(1400, 70), new Vector2(0, 1));
        const string dataRoot = "Assets/AAAGame/DataTable/";
        demo.DialogueBytes = AssetDatabase.LoadAssetAtPath<TextAsset>(dataRoot + "dialogue_tbdialogue.bytes");
        demo.NpcBytes = AssetDatabase.LoadAssetAtPath<TextAsset>(dataRoot + "map_tbnpc.bytes");
        demo.NodeBytes = AssetDatabase.LoadAssetAtPath<TextAsset>(dataRoot + "dialogue_tbdialoguenode.bytes");
        demo.OptionBytes = AssetDatabase.LoadAssetAtPath<TextAsset>(dataRoot + "dialogue_tbdialogueoption.bytes");
        demo.LocalizationBytes = AssetDatabase.LoadAssetAtPath<TextAsset>(dataRoot + "tblocalization.bytes");
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(22, 23, 21, 255);
    }
}
