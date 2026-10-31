using System.Collections.Generic;
using AAAGame.ScriptsHotfix.UI.Core;
using GameFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;


public enum ButtonType
{
    /// <summary>
    /// 点击,点击响应EventOnClick
    /// </summary>
    Click,

    /// <summary>
    /// 有间隔的，点击后在间隔时间ClickInterva内无法再次点击, 点击响应EventOnClick
    /// </summary>
    Interva,

    /// <summary>
    /// 持续长按一次响应，按下后直到达到长按响应时间LongPressOnTime,响应一次 EventOnLongPress，否则，不做处理
    /// </summary>
    LongPressOn,

    /// <summary>
    /// 点击与长按一次响应，按下抬起时，若没有达到长按响应时间LongPressOnTime，响应一次EventOnClick，否则，响应一次EventOnLongPress
    /// </summary>
    ClickAndLongPressOn,

    /// <summary>
    /// 持续长按循环响应，按下后每次达到间隔时间LongPressOnTime时，响应一次 EventOnLongPress
    /// 持续长按 并以一定的间隔时间响应，直到放手 每次向应 EventOnLongPress
    /// </summary>
    LongPressOnLoop,
    
    /// <summary>
    /// 点击\长按\循环检测，按下抬起时，若没有达到长按响应时间LongPressOnTime，响应一次EventOnClick
    /// 否则开始循环响应EventOnLongPress
    ///</summary>
    ClickAndLongPressOnLoop,
}

[RequireComponent(typeof(UGF_Image))]
public class UGF_Button : Button
{
    /// <summary>
    /// 按钮类型
    /// </summary>
    [HideInInspector]
    public ButtonType ButtonType = ButtonType.Click;   

    /// <summary>
    /// 按钮图片
    /// </summary>
    private UGF_Image m_ButtonImage;
    public UGF_Image ButtonImage
    {
        get
        {
            if(m_ButtonImage == null)
            {
                m_ButtonImage = GetComponent<UGF_Image>();
            }

            return m_ButtonImage;
        }
    }

    /// <summary>
    /// 点击间隔，类型为Interva时生效
    /// </summary>
    [HideInInspector]
    public float ClickInterva;

    /// <summary>
    /// 是否置灰
    /// </summary>
    [HideInInspector]
    public bool IsGray = true;
    
    /// <summary>
    /// 上一次点击时间
    /// </summary>
    protected float m_ClickTime;

    /// <summary>
    /// 点击计时器
    /// </summary>
    protected bool m_ClickTimerStart;

    /// <summary>
    /// 长按触发时间,类型为LongPressOn或ClickAndLongPressOn时生效
    /// </summary>
    [HideInInspector]
    public float LongPressOnTime;

    /// <summary>
    /// 按下时间
    /// </summary>
    protected float m_OnDownTime;

    /// <summary>
    /// 抬起时间
    /// </summary>
    protected float m_OnUpTime;

    /// <summary>
    /// 点击音效
    /// </summary>
    [HideInInspector]
    public int SoundId;    
 
    [HideInInspector]
    public List<GameObject> StateObjList = new List<GameObject>(4);

    /// <summary>
    /// MultiState类型按钮，当前处于的状态索引
    /// </summary>
    private int m_MultiStateIndex;
    
    /// <summary>
    /// 是否启动自定义点击区域
    /// </summary>
    [HideInInspector]
    public bool CustomPolygon;

    /// <summary>
    /// 用于自定义点击区域，在子节点，添加GameObject,并添加m_PolygonCollider2D组件
    /// </summary>
    private PolygonCollider2D m_PolygonCollider2D;

    /// <summary>
    /// 自定义占击区域
    /// </summary>
    public PolygonCollider2D PolygonCollider2D
    {
        get
        {
            if(m_PolygonCollider2D == null)
            {
                m_PolygonCollider2D = GetComponentInChildren<PolygonCollider2D>();
            }

            return m_PolygonCollider2D;
        }
    }

    /// <summary>
    /// 按钮文字组件
    /// </summary>
    [HideInInspector]
    public TextMeshProUGUI ButtonText;

    private bool m_IsGuideUI;

    /// <summary>
    /// 是否为引导UI
    /// </summary>
    public bool IsGuideUI
    {
        get { return m_IsGuideUI; }
        set
        {
            Debug.Log("标记Button为引导UI:" + this.name,this.gameObject);
            m_IsGuideUI = value;
        }
    }

    UGF_UIHelper _mUgfUIHelper;

    public UGF_UIHelper UIHelper
    {
        get
        {
            if (_mUgfUIHelper == null)
            {
                _mUgfUIHelper = UGF_UIHelper.Create(this.gameObject);
            }
            return _mUgfUIHelper;
        }
    }
    
    private void Update()
    {       
        //点击间隔计算
        if (ButtonType == ButtonType.Interva)
        {
            CheckIntervaTrigger();                
        }

        //长按时间计算
        if(LongPressOnTime > 0)
        {
            CheckLongPressTrigger();               
        }
    }

    protected override void OnDestroy()
    {
        if (_mUgfUIHelper != null)
        {
            ReferencePool.Release(_mUgfUIHelper);
            _mUgfUIHelper = null;
        }
        
        base.OnDestroy();
    }

    /// <summary>
    /// 设置点击间隔
    /// </summary>
    /// <param name="clickInterva">时间</param>
    public void SetClickInterval(float clickInterva)
    {
        ClickInterva = clickInterva;
    }

    /// <summary>
    /// 播放点击音效
    /// </summary>
    private void PlayUISound()
    {
        //UIFramework.AssetBridge.PlayUISound(SoundId);
    }       

    /// <summary>
    /// 模拟触发按钮点击事件调用
    /// </summary>
    public void TriggerButtonClickEvent()
    {        
       // EventTrigger.CallLua(CallBackType.EventOnClick, this);
       // EventTrigger.CallLua(CallBackType.EventOnUp, this);
    }    

    /// <summary>
    /// 设置按钮事件响应类型
    /// </summary>
    /// <param name="type"></param>
    public void SetButtonType(int type)
    {
        ButtonType = (ButtonType)type;
    }

    /// <summary>
    /// 设置长按响应时间
    /// </summary>
    /// <param name="time"></param>
    public void SetLongPressOnTime(float time)
    {
        LongPressOnTime = time;
    }     

    /// <summary>
    /// 设置一个样式显示
    /// </summary>
    /// <param name="index"></param>
    public void SetButtonStyle(int index)
    {
        if (index < StateObjList.Count)
        {
            for (int i = 0; i < StateObjList.Count; i++)
            {
                if (i == index)
                {
                    StateObjList[i].SetActive(true);
                    m_MultiStateIndex = i;
                }
                else
                {
                    StateObjList[i].SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 获取样式索引
    /// </summary>
    /// <returns></returns>
    public int GetState()
    {
        return m_MultiStateIndex;
    }

    /// <summary>
    /// 获取样式对象
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject GetStateObj(int index)
    {
        return StateObjList[index];
    }
   
    /// <summary>
    /// 所有图片材质球 图片加载有特殊材质球勿用
    /// </summary>
    /// <param name="isGray"></param>
    public void SetAllChildImgGray(bool isGray)
    {
        /*UGF_Image[] imageList = GetComponentsInChildren<UGF_Image>();
        if (isGray)
        {
            foreach (var t in imageList)
            {
                t.SetMaterial(UIFramework.AssetBridge.GetMaterialsAsset(AssetBridgeComponent.GrayMaterial));
            }
        }
        else
        {
            foreach (var t in imageList)
            {
                t.RestoreMaterial();
            }
        }*/
    }

    /// <summary>
    /// 是否在列表中进行拖动
    /// </summary>
    /// <returns></returns>
    protected bool IsDragList()
    {
        /*
        UGF_ScrollView[] _ScrollView = gameObject.GetComponentsInParent<UGF_ScrollView>();
        foreach (var t in _ScrollView)
        {
            if (t.m_IsDrag)
            {
                return true;
            }
        }
        */

        return false;
    }
    
    #region EventTrigger 事件响应

    private bool m_OpenEventTrigger;
    
    private GameFrameworkAction m_OnClickAction;
    private GameFrameworkAction m_OnDownAction;
    private GameFrameworkAction m_OnUpAction;
    private GameFrameworkAction m_OnLongPressAction;
    private GameFrameworkAction m_OnMoveAction;
    private GameFrameworkAction m_OnEnterAction;
    private GameFrameworkAction m_OnExitAction;
    
    private GameFrameworkAction m_OnClickGuideAction;
    private GameFrameworkAction m_OnDownGuideAction;
    private GameFrameworkAction m_OnUpGuideAction;
    private GameFrameworkAction m_OnLongPressGuideAction;
    private GameFrameworkAction m_OnMoveGuideAction;
    private GameFrameworkAction m_OnEnterGuideAction;
    private GameFrameworkAction m_OnExitGuideAction;
    
    /// <summary>
    /// 是否可点击
    /// </summary>
    /// <param name="bl"></param>
    public void SetEventTrigger(bool bl)
    {
        m_OpenEventTrigger = bl;
    }

    /// <summary>
    /// 点击注册
    /// </summary>
    /// <param name="action">回调</param>
    /// <param name="isGuide"></param>
    public void SetClickListener(GameFrameworkAction action,bool isGuide = false)
    {
        if (ButtonType == ButtonType.Click || ButtonType == ButtonType.Interva || ButtonType == ButtonType.ClickAndLongPressOn)
        {
            if (isGuide)
            {
                m_OnClickGuideAction = action;
            }
            else
            {
                m_OnClickAction = action;
            }
        }
    }
  
    /// <summary>
    /// 按下注册
    /// </summary>
    /// <param name="action"></param>
    /// <param name="isGuide">是否为引导注入</param>
    public void SetDownListener(GameFrameworkAction action,bool isGuide = false)
    {
        if (isGuide)
        {
            m_OnDownGuideAction = action;
        }
        else
        {
            m_OnDownAction = action;
        }
    }
    
    /// <summary>
    /// 抬起注册
    /// </summary>
    /// <param name="action"></param>
    /// <param name="isGuide">是否为引导注入</param>
    public void SetUpListener(GameFrameworkAction action,bool isGuide = false)
    {
        if (isGuide)
        {
            m_OnUpGuideAction = action;
        }
        else
        {
            m_OnUpAction = action;
        }
    }

    /// <summary>
    /// 长按注册
    /// </summary>
    /// <param name="action"></param>
    /// <param name="isGuide">是否为引导注入</param>
    public void SetLongPressListener(GameFrameworkAction action,bool isGuide = false)
    {
        if (isGuide)
        {
            m_OnLongPressGuideAction = action;
        }
        else
        {
            m_OnLongPressAction = action;
        }
    }

    /// <summary>
    /// 手指移动注册
    /// </summary>
    /// <param name="action"></param>
    /// <param name="isGuide">是否为引导注入</param>
    public void SetMoveListener(GameFrameworkAction action,bool isGuide = false)
    {
        if (isGuide)
        {
            m_OnMoveGuideAction = action;
        }
        else
        {
            m_OnMoveAction = action;
        }
    }

    /// <summary>
    /// 进入按钮区域注册
    /// </summary>
    /// <param name="action"></param>
    /// <param name="isGuide">是否为引导注入</param>
    public void SetEnterListener(GameFrameworkAction action,bool isGuide = false)
    {
        if (isGuide)
        {
            m_OnEnterGuideAction = action;
        }
        else
        {
            m_OnEnterAction = action;
        }
    }

    /// <summary>
    /// 离开按钮区域注册
    /// </summary>
    /// <param name="action"></param>
    /// <param name="isGuide">是否为引导注入</param>
    public void SetExitListener(GameFrameworkAction action,bool isGuide = false)
    {
        if (isGuide)
        {
            m_OnExitGuideAction = action;
        }
        else
        {
            m_OnExitAction = action;
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (ButtonType == ButtonType.Click|| ButtonType == ButtonType.Interva)
        {
            if (IsInClickArea())
            {
                if (ButtonType == ButtonType.Interva)
                {
                    if (m_ClickTimerStart)
                    {
                        return;
                    }

                    m_ClickTime = Time.time;
                    m_ClickTimerStart = true;
                    SetEventTrigger(false);
                }

                if (GameGlobalSetting.IsGuideIng)
                {
                    if (IsGuideUI)
                    {
                        if (m_OnClickGuideAction != null)
                        {
                            m_OnClickGuideAction?.Invoke();
                        }
                        else
                        {
                            m_OnClickAction?.Invoke(); 
                        }
                    }
                }
                else
                {
                    m_OnClickAction?.Invoke();
                }
            }
        }
        
        base.OnPointerClick(eventData);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (LongPressOnTime > 0)
        {              
            m_OnDownTime = Time.time;
        }
        
        PlayUISound();
        
        if (GameGlobalSetting.IsGuideIng)
        {
            if (IsGuideUI)
            {
                if (m_OnDownGuideAction != null)
                {
                    m_OnDownGuideAction?.Invoke();
                }
                else
                {
                    m_OnDownAction?.Invoke(); 
                }
            }
        }
        else
        {
            m_OnDownAction?.Invoke();
        }
        
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (IsDragList())
        {
            m_OnUpTime = 0;
            m_OnDownTime = 0;
            base.OnPointerUp(eventData);
            return;
        }
        
        if(ButtonType == ButtonType.ClickAndLongPressOn||ButtonType ==  ButtonType.ClickAndLongPressOnLoop)
        {
            if (IsInClickArea())
            {
                if (m_OnDownTime > 0 && LongPressOnTime > 0)
                {
                    if (m_OnUpTime - m_OnDownTime < LongPressOnTime && IsGuideUI)
                    {
                        if (GameGlobalSetting.IsGuideIng)
                        {
                            if (IsGuideUI)
                            {
                                if (m_OnClickGuideAction != null)
                                {
                                    m_OnClickGuideAction?.Invoke();
                                }
                                else
                                {
                                    m_OnClickAction?.Invoke(); 
                                }
                            }
                        }
                        else
                        {
                            m_OnClickAction?.Invoke();
                        }
                    }
                }
            }
        }

        if (GameGlobalSetting.IsGuideIng)
        {
            if (IsGuideUI)
            {
                if (m_OnUpGuideAction != null)
                {
                    m_OnUpGuideAction?.Invoke();
                }
                else
                {
                    m_OnUpAction?.Invoke(); 
                }
            }
        }
        else
        {
            m_OnUpAction?.Invoke();
        }

        m_OnUpTime = 0;
        m_OnDownTime = 0;
        
        base.OnPointerUp(eventData);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (GameGlobalSetting.IsGuideIng)
        {
            if (IsGuideUI)
            {
                if (m_OnEnterGuideAction != null)
                {
                    m_OnEnterGuideAction?.Invoke();
                }
                else
                {
                    m_OnEnterGuideAction?.Invoke(); 
                }
            }
        }
        else
        {
            m_OnEnterAction?.Invoke();
        }
  
        base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (GameGlobalSetting.IsGuideIng)
        {
            if (IsGuideUI)
            {
                if (m_OnExitGuideAction != null)
                {
                    m_OnExitGuideAction?.Invoke();
                }
                else
                {
                    m_OnExitAction?.Invoke(); 
                }
            }
        }
        else
        {
            m_OnExitAction?.Invoke();
        }
     
        base.OnPointerExit(eventData);
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (GameGlobalSetting.IsGuideIng)
        {
            if (IsGuideUI)
            {
                if (m_OnMoveGuideAction != null)
                {
                    m_OnMoveGuideAction?.Invoke();
                }
                else
                {
                    m_OnMoveAction?.Invoke(); 
                }
            }
        }
        else
        {
            m_OnMoveAction?.Invoke();
        }
        
        base.OnMove(eventData);
    }

    /// <summary>
    /// 是否在点击区域内，要定义点击区域时，添加PolygonCollider2D
    /// </summary>
    /// <returns></returns>
    private bool IsInClickArea()
    {
        if (m_PolygonCollider2D != null)
        {            
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {                   
                foreach(Touch touch in Input.touches)
                {
                    if (Camera.main != null)
                    {
                        Vector3 clickPosition = Camera.main.ScreenToWorldPoint(touch.position);

                        if (m_PolygonCollider2D.OverlapPoint(clickPosition))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
            else
            {
                if (Camera.main != null)
                {
                    Vector3 clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                    if (m_PolygonCollider2D.OverlapPoint(clickPosition))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检测间隔时间触发事件
    /// </summary>
    private void CheckIntervaTrigger()
    {
        if (m_ClickTime > 0 && ClickInterva >= 0 && m_ClickTimerStart)
        {
            if (Time.time - m_ClickTime > ClickInterva)
            {
                SetEventTrigger(true);
                ButtonImage.RestoreMaterial();
                m_ClickTime = 0;
                m_ClickTimerStart = false;
            }
        }
    }
    
    /// <summary>
    /// 检测触发长按事件
    /// </summary>
    private void CheckLongPressTrigger()
    {
        if (ButtonType == ButtonType.ClickAndLongPressOn||ButtonType == ButtonType.ClickAndLongPressOnLoop)
        {
            //已经抬手，如果触发了点击，则不继续判定长按
            if (m_OnUpTime > 0)
            {
                if (m_OnUpTime - m_OnDownTime < LongPressOnTime)
                {               
                    return;
                }
            }
        }

        if (m_OnDownTime > 0 && LongPressOnTime > 0)
        {
            if (IsDragList())
            {
                return;
            }
            
            if ((Time.time - m_OnDownTime) > LongPressOnTime)
            {                
                //UGF_ScrollView[] _ScrollView = gameObject.GetComponentsInParent<UGF_ScrollView>();
                /*foreach (var t in _ScrollView)
                {
                    if (t.m_IsDrag)
                    {
                        m_OnDownTime = 0;
                        return;
                    }
                }*/

                
                if (ButtonType == ButtonType.LongPressOnLoop 
                    ||ButtonType == ButtonType.ClickAndLongPressOnLoop)
                {
                    m_OnDownTime = Time.time;
                }
                else
                {
                    m_OnDownTime = 0;
                }
               
               if (GameGlobalSetting.IsGuideIng)
               {
                   if (IsGuideUI)
                   {
                       if (m_OnLongPressGuideAction != null)
                       {
                           m_OnLongPressGuideAction?.Invoke();
                       }
                       else
                       {
                           m_OnLongPressAction?.Invoke(); 
                       }
                   }
               }
               else
               {
                   m_OnLongPressAction?.Invoke();
               }
            }
        }
    }
    #endregion
}

