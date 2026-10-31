using System;
using AAAGame.ScriptsHotfix.UI.Core;
using GameFramework;
using UnityEngine;
using UnityEngine.UI;

public class UISubjoinUIBgLogic:MonoBehaviour
{
    private GameObject m_GameObject;
    
    
    private UGF_Button m_Button;

    /// <summary>
    /// 文本组件
    /// </summary>
    private UGF_Text m_HintText;

    /// <summary>
    /// 遮罩透明度
    /// </summary>
    private float m_MaxMaskAlpha = 0;

    public void Create(int id)
    {
        /*m_GameObject = gameObject;
        m_Button = GetComponent<UGF_Button>();
        m_HintText = GetComponentInChildren<UGF_Text>();
        m_HintText.SetText("");
        base.Create(id);*/
    }


    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="targetUI">遮罩所适用的UI</param>
    /// <param name="sortingOrder">目标UI的层级</param>
    /// <param name="layerName">目标UI的层名字</param>
    /// <param name="maskAlpha">遮罩的透明度</param>
    public void Init(Transform targetUI, int sortingOrder,string layerName,int maskAlpha = 220)
    {
        m_MaxMaskAlpha = maskAlpha;
        
        int index = 0;

        Transform targetUIParent = targetUI.parent;
        for (int i = 0; i < targetUIParent.childCount; i++)
        {
            if (targetUI == targetUIParent.GetChild(i))
            {
                index = i;
                break;
            }
        }

        m_GameObject.transform.SetParent(targetUIParent, false);
        m_GameObject.transform.localPosition = Vector3.zero;
        m_GameObject.transform.SetSiblingIndex(index);
        m_GameObject.transform.localScale = Vector3.one;

        UpdateLayer(sortingOrder, layerName);

        float imgRatio = GameGlobalSetting.DesignHeight / GameGlobalSetting.DesignWidth;
        float curScreenRatio = (float)Screen.height /Screen.width;
        double newRatio = 1;
        if(curScreenRatio > imgRatio)
        {
            newRatio = Math.Round(curScreenRatio/imgRatio + 0.01,2);
        }
        else
        {
            newRatio = Math.Round(imgRatio/curScreenRatio + 0.01,2);
        }
        
        m_Button = m_GameObject.GetOrAddComponent<UGF_Button>();
        m_Button.UIHelper.SetSize(GameGlobalSetting.DesignWidth, GameGlobalSetting.DesignHeight);
        m_Button.UIHelper.SetScale((float)newRatio,(float)newRatio,1);
       
        m_GameObject.GetOrAddComponent<GraphicRaycaster>();
        m_GameObject.layer = targetUI.gameObject.layer;
    }

    /// <summary>
    /// 显示遮罩
    /// </summary>
    /// <param name="useFade">是否使用渐进</param>
    public void Show(bool useFade)
    {
        /*if (useFade)
        {
            m_Button.ButtonImage.SetColor(0, 0, 0, 0f);
            SetActive(true);
            m_Button.ButtonImage.CrossFadeAlpha((float)m_MaxMaskAlpha/255, 0.3f, false);
        }
        else
        {
            m_Button.ButtonImage.SetColor(0, 0, 0, (float)m_MaxMaskAlpha/255);
            SetActive(true);
        }*/
    }

    /// <summary>
    /// 更新深度
    /// </summary>
    /// <param name="sortingOrder"></param>
    /// <param name="layerName"></param>
    public void UpdateLayer(int sortingOrder,string layerName)
    {
        Canvas cachedCanvas = m_GameObject.GetOrAddComponent<Canvas>();
        cachedCanvas.overrideSorting = true;
        cachedCanvas.sortingOrder = sortingOrder - 9;
        cachedCanvas.sortingLayerName = layerName;
    }
    
    public void InitText(string hintTextId ,bool isOpenHindtext = false,float hindTextPosX = 0,float hindTextPosY = 0,float hindTextPosZ = 0)
    {
        if(isOpenHindtext)
        {
            m_HintText.SetLocalizationText(hintTextId);
            m_HintText.UIHelper.SetLocalPosition(hindTextPosX,hindTextPosY,hindTextPosZ);    
        }
        else
        {
            m_HintText.SetText("");
        }
    }

    public void SetBackgrounpOnClick(GameFrameworkAction gameframeworkAciton)
    {
        m_Button.SetClickListener(gameframeworkAciton);
    }
}
