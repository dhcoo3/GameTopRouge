using System;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIContainer))]
public class UIContainerInspector : Editor
{
    private bool Func_Show = false;
    
    protected UIContainer m_Target;
    
    public enum UIType
    {
        UGF_Image = 0,
        UGF_Text = 1,
        UGF_Button = 2,
        UGF_UIBase = 3,
        TMP_InputField = 4
    }
    
    public enum DomainType
    {
        @public,
        @private,
        @protected,
    }
    
    private void OnEnable()
    {
        m_Target = (UIContainer)serializedObject.targetObject;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        DrawTitle();
        
        Func_Show = EditorGUILayout.Foldout(Func_Show, "显示节点详情");
        if (Func_Show)
        {
            DrawNode();
        }
    
        serializedObject.ApplyModifiedProperties();
    }

    void DrawTitle()
    {
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("同步UI节点"))
        {
            SynAllChild();
            EditorUtility.SetDirty(m_Target);
            AssetDatabase.Refresh();
        }
            
        if (GUILayout.Button("清除所有"))
        {
            m_Target.UIContainerDict.Clear();
            EditorUtility.SetDirty(m_Target);
            AssetDatabase.Refresh();
        }
        
        if (GUILayout.Button("复制到剪贴版板"))
        {
            CopyUIContainer();
        }
        
        GUILayout.EndHorizontal();
    }

    void DrawNode()
    {
        foreach (var data in m_Target.UIContainerDict)
        {
            if(data.Value.NodeObj == null) continue;
            
            GUILayout.BeginHorizontal("box", GUILayout.Width(100));
            if (DrawItem(data.Value))
            {
                return;
            };
            GUILayout.EndHorizontal();
        }
    }
    
    private int selectedTool = 0;
    bool DrawItem(UIContainerData data)
    {
        bool changedItem = false;
        
        data.NodeObj = EditorGUILayout.ObjectField(data.NodeObj, typeof(GameObject),true,GUILayout.Width(100)) as GameObject;
     
        GUILayout.Space(10);
        
        if (Enum.TryParse(typeof(DomainType), data.Domain, true, out object? domainResult))
        {
            EditorGUI.BeginChangeCheck();
            DomainType _domain = (DomainType)EditorGUILayout.EnumPopup((DomainType)domainResult,GUILayout.Width(100));
           
            if (EditorGUI.EndChangeCheck())
            {
                data.Domain = _domain.ToString();
                m_Target.UIContainerDict[data.NodeName] = data;
                EditorUtility.SetDirty(m_Target);
                changedItem = true;
            }
        }
        
        if (Enum.TryParse(typeof(UIType), data.NodeType, true, out object? result))
        {
            EditorGUI.BeginChangeCheck();
            UIType uiType = (UIType)EditorGUILayout.EnumPopup((UIType)result,GUILayout.Width(100));
            
            if (EditorGUI.EndChangeCheck())
            {
                Type type = Type.GetType(uiType.ToString());
                
                if (type != null && data.NodeObj.GetComponent(type) != null)
                {
                    data.NodeType = uiType.ToString();
                }
                else
                {
                    Debug.LogError("该节点没有此组件！！");
                }
                
                m_Target.UIContainerDict[data.NodeName] = data;
                EditorUtility.SetDirty(m_Target);
                changedItem = true;
            }
        }
  
        if (GUILayout.Button("X"))
        {
            m_Target.RemoveNode(data.NodeName);
            EditorUtility.SetDirty(m_Target);
            changedItem = true;
        }

        return changedItem;
    }
    
    public void SynAllChild()
    {
        if (m_Target.transform.childCount == 0) return;
        m_Target.UIContainerDict.Clear();
        AddToDict(m_Target.transform);
    }

    private void AddToDict(Transform trans)
    {
        if (trans.childCount == 0) return;

        int child = trans.childCount;

        for (int i = 0; i < child; i++)
        {
            Transform t = trans.GetChild(i);

            if (t.gameObject.name.Length > 1)
            {
                string namestr = t.gameObject.name.Substring(0,2);

                if (namestr == "m_")
                {
                    string type = GetDefaultType(t.gameObject);
                    if (type != null)
                    {
                        m_Target.AddNode(t.gameObject.name, t.gameObject,type,GetDefaultDomain().ToString());
                    }
                }
            }

            if (t.GetComponent<UIContainer>() == null)
            {
                AddToDict(t);
            }
        }
    }

    private string GetDefaultType(GameObject go)
    {
        if (go.GetComponent<UIContainer>())
        {
            return "UINodeMgr";
        }
        if(go.GetComponent<UGF_Button>())
        {
            return UIType.UGF_Button.ToString();
        }        
        if (go.GetComponent<UGF_Text>())
        {
            return UIType.UGF_Text.ToString();
        }
        if (go.GetComponent<UGF_Image>())
        {
            return UIType.UGF_Image.ToString();
        }
        if (go.GetComponent<TMP_InputField>())
        {
            return UIType.TMP_InputField.ToString();
        }
        return typeof(GameObject).ToString();
    }

    private DomainType GetDefaultDomain()
    {
        return DomainType.@private;
    }

    private void CopyUIContainer()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(UIContainerAutoCreate.GetDefineText(m_Target));
        builder.Append(UIContainerAutoCreate.GetBindText(m_Target,"UINode"));
        GUIUtility.systemCopyBuffer = builder.ToString();
    }
}