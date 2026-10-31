using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(UGF_Text))]
public class UGFTextInspector : UnityEditor.Editor
{
    private bool m_FuncShow = true;

    private UGF_Text _target;

    public SerializedProperty m_SynFont;
    public SerializedProperty m_UseLocalization;
    public SerializedProperty m_UseLinkImage;
    public SerializedProperty m_UseLinkText;
    public SerializedProperty m_UseAutoLayout;
    public SerializedProperty m_LocalizationKey;

    public bool UseSpacing = false;

    private void OnEnable()
    {
        _target = (UGF_Text)serializedObject.targetObject;

        m_SynFont = serializedObject.FindProperty("SynFont");
        m_UseLocalization = serializedObject.FindProperty("UseLocalization");
        m_UseLinkImage = serializedObject.FindProperty("UseLinkImage");
        m_UseLinkText = serializedObject.FindProperty("UseLinkText");
        m_LocalizationKey = serializedObject.FindProperty("LocalizationKey");
        m_UseAutoLayout = serializedObject.FindProperty("UseAutoLayout");

        _target.CheckLocalization();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.OnInspectorGUI();
        m_FuncShow = EditorGUILayout.Foldout(m_FuncShow, "设置");

        if (m_FuncShow)
        {
            EditorGUILayout.PropertyField(m_SynFont, new GUIContent("自动同步主字体"));
            EditorGUILayout.PropertyField(m_UseLocalization, new GUIContent("自动本地化文本"));

            if (_target.UseLocalization)
            {
                EditorGUILayout.PropertyField(m_LocalizationKey, new GUIContent("本地化Key"));
                // UGF_Panel uGF_Panel = FindRootPanel();
                // if (!uGF_Panel.LocalizationTextList.Contains(_target))
                // {
                //     uGF_Panel?.LocalizationTextList.Add(_target);
                // }
            }
            else
            {
                // UGF_Panel uGF_Panel = FindRootPanel();
                // uGF_Panel?.LocalizationTextList.Remove(_target);
            }

            EditorGUILayout.PropertyField(m_UseLinkImage, new GUIContent("开启图文混排功能"));
            EditorGUILayout.PropertyField(m_UseLinkText, new GUIContent("开启文字超链接功能"));
            EditorGUILayout.PropertyField(m_UseAutoLayout, new GUIContent("单行居中，多行左对齐"));
        }

        if (GUILayout.Button("文字效果(字间距,描边..)"))
        {
            AddTextEffectExtend(_target.gameObject);
        }
        
        if (GUILayout.Button("添加字体描边(outlineEx)"))
        {
            AddTextOutlineEx(_target.gameObject);
        }

        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("GameObject/UGF_UI/UGF_Text", priority = 6)]
    public static UGF_Text AddUGFText()
    {
        return CreateUGFText(Selection.activeTransform);
    }

    public static UGF_Text CreateUGFText(Transform parent)
    {
        GameObject obj = new GameObject("UGFText");
        UGFUICreateConfig uGFUICreateConfig = UGFUIConfig.LoadAsset<UGFUICreateConfig>("UGFUICreateConfig");

        if (parent)
        {
            obj.transform.SetParent(parent, false);
        }

        obj.transform.localPosition = Vector3.zero;
        UGF_Text text = obj.AddComponent<UGF_Text>();
        text.color = Color.white;
        text.raycastTarget = false;
        text.fontSize = 30;
        text.font = uGFUICreateConfig.DefaultFont;
        text.text = "Text...";
        text.UIHelper.SetSize(150, 60);
        text.SetAlignment((int)TextAnchor.MiddleCenter);
        text.supportRichText = false;
        text.maskable = false;
        Selection.activeGameObject = obj;

        return text;
    }

    public void AddTextEffectExtend(GameObject gameObject)
    {
        //gameObject.GetOrAddComponent<TextEffectExtend>();
        //gameObject.GetOrAddComponent<Outline>();
    }
    private void AddTextOutlineEx(GameObject gameObject)
    {
        /*while(_target.gameObject.TryGetComponent<Outline>(out var outline))DestroyImmediate(outline);
        while(_target.gameObject.TryGetComponent<OutlineEx>(out var outlineEx)) DestroyImmediate(outlineEx);//字间距需要在OutlineEx之前添加
        gameObject.GetOrAddComponent<TextEffectExtend>();
        gameObject.GetOrAddComponent<OutlineEx>();
        SetTopParent(gameObject.transform);*/
    }
    
    private void SetTopParent(Transform transform)
    {
        while (transform.parent.name != "Canvas (Environment)")
        {
            transform = transform.parent;
        }

        //如果不是panel则不添加Canvas
        if (!transform.TryGetComponent<UGF_Panel>(out _)) return;
        
        if (!transform.TryGetComponent<Canvas>(out var canvas)) canvas = transform.AddComponent<Canvas>();
        if (!transform.GetComponent<GraphicRaycaster>()) transform.AddComponent<GraphicRaycaster>();
        
            
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord3;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
    }


    public UGF_Panel FindRootPanel()
    {
        UGF_Panel uGF_Panel = null;
        Transform transform = _target.transform;

        while (uGF_Panel == null&& transform)
        {
            uGF_Panel = transform.GetComponent<UGF_Panel>();

            if(uGF_Panel == null)
            {
                transform = transform.parent;
            }
        }

        return uGF_Panel;
    }
}