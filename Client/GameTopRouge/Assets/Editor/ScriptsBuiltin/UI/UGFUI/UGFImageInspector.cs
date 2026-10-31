using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UGF_Image))]
public class UGFImageInspector : Editor
{
    private bool m_ImageInspector = false;
    private bool m_Extend = true;
    private bool m_ClearLoadPath = false;

    private UGF_Image _target;     

    public SerializedProperty DynamicLoad;
    public SerializedProperty DynamicLoadPath;
    public SerializedProperty SlicedClipMode;
    public SerializedProperty DynamicLoadAlphaVal;
    public SerializedProperty IgnoreAutoAtlas;
    
    private Sprite m_SpriteObj;
    private string m_OldName = "";
    private string m_NewName = "";
    
    private void OnEnable()
    {
        _target = (UGF_Image)serializedObject.targetObject;
        DynamicLoad = serializedObject.FindProperty("DynamicLoad");
        DynamicLoadPath = serializedObject.FindProperty("DynamicLoadPath");
        SlicedClipMode = serializedObject.FindProperty("m_SlicedClipMode");
        DynamicLoadAlphaVal = serializedObject.FindProperty("DynamicLoadAlphaVal");
        IgnoreAutoAtlas = serializedObject.FindProperty("IgnoreAutoAtlas");
        
        if (!Application.isPlaying)
        {
            if (_target.DynamicLoad)
            {
                if (_target.DynamicLoad && !string.IsNullOrEmpty(_target.DynamicLoadPath))
                {
                    m_SpriteObj = AssetDatabase.LoadAssetAtPath<Sprite>(_target.DynamicLoadPath);
                    m_OldName = m_SpriteObj.name;
                    m_NewName = m_OldName;
                }
            }
            else
            {
                if (_target.sprite != null)
                {
                    m_SpriteObj = _target.sprite;
                    m_OldName = m_SpriteObj.name;
                    m_NewName = m_OldName;

                    string path = AssetDatabase.GetAssetPath(m_SpriteObj);
                    if (path.Contains("Assets/Res/Sprite/"))
                    {
                        Debug.LogError("优化:使用非图集图片,但没有使用动态加载,会造成依赖冗余及资源缺失:"+_target.name,_target.gameObject);
                    }
                }
            }
        }
        else
        {
            m_SpriteObj = _target.sprite;
        }
    }
    

    public void OnDestroy()
    {
        if (!Application.isPlaying&&_target &&_target.DynamicLoad)
        {
            if (!string.IsNullOrEmpty(_target.DynamicLoadPath))
            {
                _target.sprite = null;
            }
         
            _target.color = new Color(_target.color.r, _target.color.g, _target.color.b, 0);
            EditorUtility.SetDirty(_target);
        }
    }

    public override void OnInspectorGUI()
    {                 
        serializedObject.Update();
        DrawImageInspector();
        DrawExtend();

        if (!m_OldName.Equals(m_NewName))
        {
            EditorUtility.SetDirty(_target);
            m_OldName = m_NewName;
        }
        
        //修改序列化对象的属性
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawImageInspector()
    {
        m_ImageInspector = EditorGUILayout.Foldout(m_ImageInspector, "图片");
        
        if (m_ImageInspector)
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Set Native Size", GUILayout.Width(200)))
            {
                SetNativeSiz();
            }
        }
    }

    private void DrawExtend()
    {
        m_Extend = EditorGUILayout.Foldout(m_Extend, "扩展");

        if (m_Extend)
        {
            EditorGUILayout.BeginHorizontal();
            string assetFullName = "";
            if (m_SpriteObj != null)
            {
                assetFullName = AssetDatabase.GetAssetPath(_target.sprite);
                if (_target.sprite && string.IsNullOrEmpty(assetFullName))
                {
                    assetFullName = _target.sprite.name;
                }
            }
            EditorGUILayout.LabelField(new GUIContent("图片名称:"+assetFullName));
           
            if (GUILayout.Button(new GUIContent("路径复制","复制图片的资源全路径"))){ CopyPath(); }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            m_SpriteObj = EditorGUILayout.ObjectField(new GUIContent("Sprite图片"),m_SpriteObj,typeof(Sprite),true) as Sprite;
            _target.sprite = m_SpriteObj;
            
            EditorGUILayout.BeginVertical();
         
            if (GUILayout.Button("Set Native Size")) { SetNativeSiz(); }
            EditorGUILayout.EndVertical();
        
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(DynamicLoad, new GUIContent("动态加载","游戏运行时才加载图片,编辑器模式下只显示预览,预览结束后会将透明度调为0"));
            EditorGUILayout.PropertyField(IgnoreAutoAtlas, new GUIContent("忽略自动绑定图集","打包时会自动生成图集，见BuildPrefabAtlas"));
            
            if (_target.DynamicLoad)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(DynamicLoadPath, new GUIContent("加载路径","运行时按此路径加载,若要使用代码加载,请点击清除路径"));
                if (GUILayout.Button("清除路径",GUILayout.Width(100)))
                {
                    m_ClearLoadPath = true;
                    _target.DynamicLoadPath = "";
                    EditorUtility.SetDirty(_target);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.PropertyField(DynamicLoadAlphaVal, new GUIContent("透明度","加载成功后设置的透明度"));
                
                if (_target.sprite != null)
                {
                    string newPath = AssetDatabase.GetAssetPath(_target.sprite);
                    if (!m_ClearLoadPath && !Application.isPlaying &&!_target.DynamicLoadPath.Equals(newPath))
                    {
                        _target.DynamicLoadPath = newPath;
                        EditorUtility.SetDirty(_target);
                    }
                }
                
                if (!Application.isPlaying && !string.IsNullOrEmpty(_target.DynamicLoadPath))
                {
                    m_SpriteObj = AssetDatabase.LoadAssetAtPath<Sprite>(_target.DynamicLoadPath);
                }

                if (!Application.isPlaying)
                {
                    _target.color = new Color(_target.color.r, _target.color.g, _target.color.b, _target.DynamicLoadAlphaVal);
                }
            }
            
            
            if (_target.sprite && _target.sprite.name != m_OldName)
            {
                m_NewName = _target.sprite.name;
            }
            
            EditorGUILayout.PropertyField(SlicedClipMode, new GUIContent("Sliced+Filled","九宫格带裁剪模式"));
        }
    }
    
    private void CopyPath()
    {
        if (_target.sprite != null)
        {
            //Utils.CopyToClipboard(AssetDatabase.GetAssetPath(_target.sprite));
        }
    }
    
    private void SetNativeSiz()
    {
        _target.SetNativeSize();

        UGF_Button uGF_Button = _target.GetComponent<UGF_Button>();
           
        if (uGF_Button && !uGF_Button.CustomPolygon)
        {              
            if (uGF_Button.PolygonCollider2D)
            {
                float width = uGF_Button.UIHelper.GetWidth();
                float height = uGF_Button.UIHelper.GetHeight();
                Vector2 vector1 = new Vector2((width / 2) * -1, height / 2);
                Vector2 vector2 = new Vector2(width / 2, height / 2);
                Vector2 vector3 = new Vector2(width / 2, (height / 2) * -1);
                Vector2 vector4 = new Vector2((width / 2) * -1, (height / 2) * -1);
                uGF_Button.PolygonCollider2D.points = new Vector2[] { vector1, vector2, vector3, vector4 };
            }                
        }
        EditorUtility.SetDirty(_target);
    }

    [MenuItem("GameObject/UGF_UI/UGF_Image", priority = 2)]
    static void AddUGFImage()
    {
        CreateUGFImage(Selection.activeTransform);
    }

    public static UGF_Image CreateUGFImage(Transform parent)
    {
        GameObject obj = new GameObject("UGFImage");

        if (parent)
        {
            obj.transform.SetParent(parent, false);
        }
        obj.transform.localPosition = Vector3.zero;
        UGF_Image ugf_image = obj.AddComponent<UGF_Image>();
        ugf_image.raycastTarget = false;
        ugf_image.maskable = false;
        Selection.activeGameObject = obj;
        return ugf_image;
    }
}