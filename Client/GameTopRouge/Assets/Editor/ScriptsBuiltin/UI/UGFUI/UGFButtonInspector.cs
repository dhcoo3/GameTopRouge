using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


[CustomEditor(typeof(UGF_Button))]
public class UGFButtonInspector : UnityEditor.Editor
{

    private bool m_ButtonInspector = false;
    private bool m_Extend = true;
    
    private enum ButtonTypeDes
    {
        点击,
        间隔,
        长按,
        点击加长按,
        长按循环,
        点击加长按循环
    }

    private ButtonTypeDes m_ButtonTypeDes = ButtonTypeDes.点击;
    
    private UGF_Button _target;
    private SerializedProperty m_ClickInterva;
    private SerializedProperty m_IsGray;
    private SerializedProperty m_LongPressOnTime;
    private SerializedProperty m_Sound;
    private SerializedProperty m_CustomPolygon;
    private SerializedProperty m_StateObjList;
    private SerializedProperty m_SoundId;

    private void OnEnable()
    {
        _target = (UGF_Button)serializedObject.targetObject;
        m_ClickInterva = serializedObject.FindProperty("ClickInterva");
        m_IsGray = serializedObject.FindProperty("IsGray");
        m_LongPressOnTime = serializedObject.FindProperty("LongPressOnTime");
        m_CustomPolygon = serializedObject.FindProperty("CustomPolygon");
        m_StateObjList = serializedObject.FindProperty("StateObjList");
        m_SoundId = serializedObject.FindProperty("SoundId");
        
        m_ButtonTypeDes = (ButtonTypeDes)_target.ButtonType;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        m_ButtonInspector = EditorGUILayout.Foldout(m_ButtonInspector, "按钮");
        
        if (m_ButtonInspector)
        {
            base.OnInspectorGUI();
        }
        

        m_Extend = EditorGUILayout.Foldout(m_Extend, new GUIContent("扩展", "按钮功能扩展"));

        if (m_Extend)
        {
            EditorGUILayout.BeginVertical("box");

            m_ButtonTypeDes =
                (ButtonTypeDes)EditorGUILayout.EnumPopup(new GUIContent("类型", "请选择按钮类型"), m_ButtonTypeDes);
            
            _target.ButtonType = (ButtonType)m_ButtonTypeDes;
            
            if (_target.ButtonType == ButtonType.Interva)
            {
                EditorGUILayout.PropertyField(m_ClickInterva, new GUIContent("间隔时间"));
                EditorGUILayout.PropertyField(m_IsGray, new GUIContent("置灰效果","在按钮无法点击的间隔时间内使用置灰效果"));
            }
            else if (_target.ButtonType == ButtonType.LongPressOn 
                     || _target.ButtonType == ButtonType.ClickAndLongPressOn 
                     || _target.ButtonType == ButtonType.LongPressOnLoop
                     || _target.ButtonType == ButtonType.ClickAndLongPressOnLoop)
            {
                EditorGUILayout.PropertyField(m_LongPressOnTime, new GUIContent("长按响应时间"));
            }
     
            EditorGUILayout.PropertyField(m_CustomPolygon, new GUIContent("自定义点击区域","不规则点击时使用该功能"));
            EditorGUILayout.PropertyField(m_SoundId, new GUIContent("点击音效"));   
            EditorGUILayout.PropertyField(m_StateObjList, new GUIContent("显示样式","按钮需要使用多种样式切换时使用"));
            EditorGUILayout.EndVertical();
        }            

        //修改序列化对象的属性
        serializedObject.ApplyModifiedProperties();   
    }

    [MenuItem("GameObject/UGF_UI/UGF_Button", priority = 3)]
    static void AddUGFButton()
    {
        CreateUGFButton(Selection.activeTransform);
    }

    static public UGF_Button CreateUGFButton(Transform parent)
    {
        GameObject obj = new GameObject("UGFButton");
        UGFUICreateConfig uGFUICreateConfig = UGFUIConfig.LoadAsset<UGFUICreateConfig>("UGFUICreateConfig");

        if (parent)
        {
            obj.transform.SetParent(parent, false);
        }

        obj.transform.localPosition = Vector3.zero;

        UGF_Button ugfButton = obj.AddComponent<UGF_Button>();
        ugfButton.UIHelper.SetSize(218, 138);

        GameObject text = new GameObject();
        text.name = "Text";
        text.transform.SetParent(obj.transform);
        text.transform.localPosition = Vector3.zero;
        text.transform.localScale = Vector3.one;
        TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
        tmp.enableAutoSizing = true;

        ugfButton.ButtonText = tmp;
      
        UGF_Image img = obj.GetComponent<UGF_Image>();
        img.maskable = false;
        ugfButton.targetGraphic = img;

        if (uGFUICreateConfig.ButtonDefaultImage2 != null)
        {
            ugfButton.image.sprite = uGFUICreateConfig.ButtonDefaultImage2;
        }

        if (uGFUICreateConfig.ButtonOnClickId != null)
        {
            ugfButton.SoundId = uGFUICreateConfig.ButtonOnClickId;
        }

        if (uGFUICreateConfig.ButtonTransition != null)
        {
            ugfButton.transition = uGFUICreateConfig.ButtonTransition;
            
            if (uGFUICreateConfig.ButtonTransition == Selectable.Transition.Animation)
            {
                Animator animator = ugfButton.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = uGFUICreateConfig.ButtonAnimatorController;
            }
        }

        return ugfButton;
    }
}
