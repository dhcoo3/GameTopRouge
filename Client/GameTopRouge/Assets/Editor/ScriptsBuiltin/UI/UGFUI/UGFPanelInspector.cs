using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UGF_Panel),true)]
public class EditorUGFPanel : Editor
{
    UGF_Panel _target;
    private string m_AssetPath = "";
    private SerializedProperty m_UIModel;
    private SerializedProperty m_SynTextFont;
    private SerializedProperty m_ShowBgMask;
    private SerializedProperty m_MaskAlpha;
    private SerializedProperty m_HintText;
    private SerializedProperty m_HintTextPos;
    private SerializedProperty m_HintTextId;
    private SerializedProperty m_Layers;
    private SerializedProperty m_OpenFade;
    private SerializedProperty m_AutoDepthChanged;
    private SerializedProperty m_UseLocalization;
    private SerializedProperty m_OpenAnim;
    private SerializedProperty m_CloseAnim;
    private SerializedProperty m_OpenSoundId;
    private SerializedProperty m_CloseSoundId;
    private SerializedProperty m_BackgrounpMaskUseFade;
    private SerializedProperty m_AddCanvasGroup;
    private SerializedProperty m_AddGraphicRaycaster;
    
    private void OnEnable()
    {
        _target = (UGF_Panel)serializedObject.targetObject;
        
        if (string.IsNullOrEmpty(m_AssetPath))
        {
            m_AssetPath = AssetDatabase.GetAssetPath(_target.GetInstanceID());
        }
      
        m_UIModel = serializedObject.FindProperty("UIModel");
        m_SynTextFont = serializedObject.FindProperty("SynTextFont");
        m_ShowBgMask = serializedObject.FindProperty("ShowBgMask");
        m_MaskAlpha = serializedObject.FindProperty("MaskAlpha");
        m_HintText = serializedObject.FindProperty("HintText");
        m_HintTextPos = serializedObject.FindProperty("HintTextPos");
        m_HintTextId = serializedObject.FindProperty("HintTextId");
        m_Layers = serializedObject.FindProperty("Layers");
        m_OpenFade = serializedObject.FindProperty("OpenFade");
        m_AutoDepthChanged = serializedObject.FindProperty("AutoDepthChanged");
        m_OpenAnim = serializedObject.FindProperty("OpenAnim");
        m_CloseAnim = serializedObject.FindProperty("CloseAnim");
        m_OpenSoundId = serializedObject.FindProperty("OpenSoundId");
        m_CloseSoundId = serializedObject.FindProperty("CloseSoundId");
        m_BackgrounpMaskUseFade = serializedObject.FindProperty("BackgrounpMaskUseFade");    
        m_AddCanvasGroup = serializedObject.FindProperty("AddCanvasGroup");    
        m_AddGraphicRaycaster = serializedObject.FindProperty("AddGraphicRaycaster");

        CheckPanelImage();
        CheckLocalizationText();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        // 添加按钮
        if (GUILayout.Button("同步UI容器"))
        {
            SynUIContainer();
        }
        
        EditorGUILayout.PropertyField(m_UIModel, new GUIContent("UI显示模式"));
        EditorGUILayout.PropertyField(m_Layers, new GUIContent("UISortingLayer"));
        EditorGUILayout.PropertyField(m_AddCanvasGroup, new GUIContent("添加CanvasGroup"));
        EditorGUILayout.PropertyField(m_AddGraphicRaycaster, new GUIContent("添加GraphicRaycaster"));
        EditorGUILayout.PropertyField(m_OpenAnim, new GUIContent("开启UI动效类型"));
        EditorGUILayout.PropertyField(m_CloseAnim, new GUIContent("关闭UI动效类型"));
        EditorGUILayout.PropertyField(m_OpenSoundId, new GUIContent("开启UI音效"));          
        EditorGUILayout.PropertyField(m_CloseSoundId, new GUIContent("关闭UI音效"));
        EditorGUILayout.PropertyField(m_OpenFade, new GUIContent("开启淡入淡出"));
        /*EditorGUILayout.PropertyField(m_ShowBgMask, new GUIContent("背景Mask"));
        
        if(m_ShowBgMask.boolValue)
        {
            EditorGUILayout.PropertyField(m_MaskAlpha,new GUIContent("背景Mask透明度"));
            EditorGUILayout.PropertyField(m_BackgrounpMaskUseFade,new GUIContent("背景渐进效果"));
            
            EditorGUILayout.PropertyField(m_HintText, new GUIContent("是否开启提示文本"));
            if(m_HintText.boolValue)
            {
                EditorGUILayout.PropertyField(m_HintTextPos,new GUIContent("坐标"));
                EditorGUILayout.PropertyField(m_HintTextId, new GUIContent("文本Id"));
            }
        }*/
          
                
        EditorGUILayout.PropertyField(m_SynTextFont, new GUIContent("同步子对象字体"));
        EditorGUILayout.PropertyField(m_AutoDepthChanged, new GUIContent("同步子对象深度"));

        //修改序列化对象的属性
        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("GameObject/UGF_UI/UGF_Panel", priority = 1)]
    static void AddUGFPanel()
    {
        CreateUGFPanel(Selection.activeTransform);
    }

    private static void CreateUGFPanel(Transform parent)
    {
        GameObject obj = new GameObject("UGFPanel");

        if (parent)
        {
            obj.transform.SetParent(parent, false);
        }

        obj.transform.localPosition = Vector3.zero;
        obj.AddComponent<RectTransform>();
        obj.AddComponent<UGF_Panel>();
        Selection.activeGameObject = obj;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            if (!string.IsNullOrEmpty(m_AssetPath))
            {
                //SpriteAtlasBinderEditor.BindSpriteAtlas(m_AssetPath);
            }
        }
    }

    /// <summary>
    /// 检查错误使用的图片组件
    /// </summary>
    private void CheckPanelImage()
    {
        UGF_Image[] imges = _target.GetComponentsInChildren<UGF_Image>();
        for (int i = 0; i < imges.Length; i++)
        {
            UGF_Image img = imges[i];
            if (!img.DynamicLoad&&img.sprite != null)
            {
                string path = AssetDatabase.GetAssetPath(img.sprite);
                if (path.Contains("Assets/Res/Sprite/"))
                {
                    Debug.LogError("优化:使用非图集图片,但没有使用动态加载,会造成依赖冗余及资源缺失:"+img.name,img.gameObject);
                }
            }
        }
        
        /*UGF_RawImage[] rawImges = _target.GetComponentsInChildren<UGF_RawImage>();
        for (int i = 0; i < rawImges.Length; i++)
        {
            UGF_RawImage rawImge = rawImges[i];
            if (!rawImge.DynamicLoad&&rawImge.texture != null)
            {
                string path = AssetDatabase.GetAssetPath(rawImge.texture);
                if (path.Contains("Assets/Res/Sprite/"))
                {
                    Debug.LogError("优化:使用非图集图片,但没有使用动态加载,会造成依赖冗余及资源缺失:"+rawImge.name,rawImge.gameObject);
                }
            }
        }*/
        
    }

    /// <summary>
    /// 检测本地化文本是否正确使用
    /// </summary>
    public void CheckLocalizationText()
    {
        UGF_Text[] ugfTexts = _target.GetComponentsInChildren<UGF_Text>(true);
        for (int i = 0; i < ugfTexts.Length; i++)
        {
            ugfTexts[i].CheckLocalization();
        }
    }

    /// <summary>
    /// 同步容器类型
    /// </summary>
    public void SynUIContainer()
    {
        
        // 获取当前选中的对象（支持多选）
        foreach (var obj in targets)
        {
            // 获取实际子类类型
            Type childType = obj.GetType();
            string path = GetScriptPath(childType);
            Debug.Log($"当前对象类型: {childType.Name} {path}");
            string content = File.ReadAllText(path);
            string defineText = UIContainerAutoCreate.GetDefineText(_target.PanelUIContainer,true);
            string bindText = UIContainerAutoCreate.GetBindText(_target.PanelUIContainer,"PanelUIContainer",true);
        
            content = Regex.Replace(
                content,
                @"#region\s+Auto\s+Create(.+?)#endregion",
                defineText,
                RegexOptions.Singleline
            );

            content = Regex.Replace(
                content,
                @"#region\s+Auto\s+Bind(.+?)#endregion",
                bindText,
                RegexOptions.Singleline
            );
       
            File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            Debug.Log("LoginPanel脚本更新完成！");
            
            return;
        }
    }
    
    private string GetScriptPath(System.Type type)
    {
        // 获取MonoScript（仅适用于继承MonoBehaviour的脚本）
        MonoScript monoScript = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance(type));
        if (monoScript == null)
        {
            monoScript = MonoScript.FromMonoBehaviour(new GameObject().AddComponent(type) as MonoBehaviour);
        }

        if (monoScript != null)
        {
            // 通过AssetDatabase获取资源路径
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);
            return scriptPath;
        }
        return null;
    }
}