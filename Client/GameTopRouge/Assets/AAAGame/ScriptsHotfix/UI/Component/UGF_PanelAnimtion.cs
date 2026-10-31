using System.Collections;
using GameFramework;
using UnityEngine;

public enum PanelAnimtionType
{
    /// <summary>
    /// 无动画
    /// </summary>
    Null = 0,

    /// <summary>
    /// 使用Animotr动画
    /// </summary>
    Animtor = 1,

    /// <summary>
    /// 从左滑动进入
    /// </summary>
    LeftIn = 2,

    /// <summary>
    /// 从右滑动出去
    /// </summary>
    RightOut = 3,

    /// <summary>
    /// 从右滑动进入
    /// </summary>
    RightIn = 4,

    /// <summary>
    /// 从左滑动出去
    /// </summary>
    LeftOut = 5,

    /// <summary>
    /// 放大
    /// </summary>
    Magnify = 6,

    /// <summary>
    /// 缩小
    /// </summary>
    Lessen = 7
}


/// <summary>
/// 面板动画播放器
/// </summary>
public class UGF_PanelAnimtion
{ 
    public void PlayAnim(UGF_Panel ugf_Panel, PanelAnimtionType panelAnimtionType, GameFrameworkAction gameFramework = null,string animtorStateName = null)
    {
        switch (panelAnimtionType)
        {
            case PanelAnimtionType.Animtor: { 
                    UseAnimtor(ugf_Panel, animtorStateName, gameFramework);
                    break;
                }
            case PanelAnimtionType.LeftIn:
                {
                    LeftIn(ugf_Panel, gameFramework);
                    break;
                }
            case PanelAnimtionType.RightOut:
                {
                    RightOut(ugf_Panel, gameFramework);
                    break;
                }
            case PanelAnimtionType.RightIn:
                {
                    RightIn(ugf_Panel, gameFramework);
                    break;
                }
            case PanelAnimtionType.LeftOut:
                {
                    LeftOut(ugf_Panel, gameFramework);
                    break;
                }
            case PanelAnimtionType.Magnify:
                {
                    MagnifyIn(ugf_Panel, gameFramework);
                    break;
                }
            case PanelAnimtionType.Lessen:
                {
                    LessenOut(ugf_Panel, gameFramework);
                    break;
                }
            default:
                {
                    ugf_Panel.URectTransform.anchoredPosition3D = Vector3.zero;
                    gameFramework?.Invoke();
                    break;
                }
        }
    }

    public void UseAnimtor(UGF_Panel ugf_Panel, string animtorStateName, GameFrameworkAction gameFramework = null)
    {
        /*ugf_Panel.OpenUIAnimtion(animtorStateName, 1, true, null, () => {
            gameFramework?.Invoke();
        });*/
    }

    public void LeftIn(UGF_Panel ugf_Panel,GameFrameworkAction gameFramework = null)
    {
        Vector3 vector3 = new Vector3(-1500, 0, 0);
        ugf_Panel.URectTransform.anchoredPosition = vector3;

        ugf_Panel.StartCoroutine(SmoothValue(vector3.x, 0,0.2f, val=> {     
            vector3.x = val;
            ugf_Panel.URectTransform.anchoredPosition = vector3;
        }, gameFramework));
    }

    public void RightOut(UGF_Panel ugf_Panel, GameFrameworkAction gameFramework = null)
    {
        Vector3 vector3 = ugf_Panel.URectTransform.anchoredPosition; 

        ugf_Panel.StartCoroutine(SmoothValue(0, 1500, 0.2f, val => {
            vector3.x = val;
            ugf_Panel.URectTransform.anchoredPosition = vector3;
        }, gameFramework));
    }

    public void RightIn(UGF_Panel ugf_Panel, GameFrameworkAction gameFramework = null)
    {
        Vector3 vector3 = new Vector3(1500, 0, 0);
        ugf_Panel.URectTransform.anchoredPosition = vector3;

        ugf_Panel.StartCoroutine(SmoothValue(vector3.x, 0, 0.2f, val => {
            vector3.x = val;
            ugf_Panel.URectTransform.anchoredPosition = vector3;
        }, gameFramework));
    }

    public void LeftOut(UGF_Panel ugf_Panel, GameFrameworkAction gameFramework = null)
    {
        Vector3 vector3 = ugf_Panel.URectTransform.anchoredPosition;

        ugf_Panel.StartCoroutine(SmoothValue(0, -1500, 0.2f, val => {
            vector3.x = val;
            ugf_Panel.URectTransform.anchoredPosition = vector3;
        }, gameFramework));
    }

    public void MagnifyIn(UGF_Panel ugf_Panel, GameFrameworkAction gameFramework = null)
    {
        Vector3 vector3 = Vector3.zero;
        ugf_Panel.URectTransform.anchoredPosition3D = Vector3.zero;

        ugf_Panel.StartCoroutine(SmoothValue(0,1.1f, 0.2f, val => {
            vector3.x = vector3.y = vector3.z = val;
            ugf_Panel.transform.localScale = vector3;
        }, ()=> {
            ugf_Panel.StartCoroutine(SmoothValue(1.1f, 1f, 0.1f, val =>
            {
                vector3.x = vector3.y = vector3.z = val;
                ugf_Panel.transform.localScale = vector3;
            }, gameFramework));
        }));
    }

    public void LessenOut(UGF_Panel ugf_Panel, GameFrameworkAction gameFramework = null)
    {
        Vector3 vector3 = ugf_Panel.transform.localScale;

        ugf_Panel.StartCoroutine(SmoothValue(1, 0, 0.2f, val => {
            vector3.x = vector3.y = vector3.z = val;
            ugf_Panel.transform.localScale = vector3;
        }, gameFramework));
    }

    public IEnumerator SmoothValue(float startVal,float endVal, float duration,GameFrameworkAction<float> updateAction, GameFrameworkAction endAction =null)
    {
        float time = 0f;
       
        while (time < duration)
        {
            time += Time.deltaTime;
            float value = Mathf.Lerp(startVal, endVal, time / duration);
            updateAction?.Invoke(value);
            yield return new WaitForEndOfFrame();
        }

        endAction?.Invoke();
    }
}
