using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AAAGame.ScriptsHotfix.UI.Core;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

public enum UIModel
{
    /// <summary>
    /// 全屏适配
    /// </summary>
    FullScreen,

    /// <summary>
    /// 非全屏适配
    /// </summary>
    NotFullScreen
}

[RequireComponent(typeof(UIContainer))]
public class UGF_Panel : UIFormLogic
{
    private int _InstanceID = 0;

    private int m_InstanceId
    {
        get
        {
            if (_InstanceID == 0)
            {
                _InstanceID = GetInstanceID();
            }
            
            return _InstanceID;
        }
    }
    
    /// <summary>
    /// UI模式
    /// </summary>
    public UIModel UIModel = UIModel.FullScreen;

    /// <summary>
    /// 打开时，是否同步子节点所有的Text字体
    /// </summary>
    public bool SynTextFont;

    /// <summary>
    /// UI所处的层级
    /// </summary>
    public GameGlobalSetting.SortingLayers Layers = GameGlobalSetting.SortingLayers.UI;

    private string m_LayerName = "";
        
    /// <summary>
    /// UI相对于当前层级的深度,每个UI在当前层级间隔20
    /// </summary>
    public const int DepthFactor = 20;

    /// <summary>子界面相对父面板排序时使用的最小层级偏移。</summary>
    private const int SubUIOrderOffset = 1;

    /// <summary>
    /// 是否打开渐变效果
    /// </summary>
    public bool OpenFade = true;

    /// <summary>
    /// 渐变的过渡时间
    /// </summary>
    private const float FadeTime = 0.3f;

    /// <summary>
    /// 是否自动适配深度变化
    /// </summary>
    public bool AutoDepthChanged = true;

    /// <summary>
    /// 当前UICanvas
    /// </summary>
    private Canvas m_CachedCanvas;

    /// <summary>
    /// 当前UICanvasGroup
    /// </summary>
    private CanvasGroup m_CanvasGroup;

    /// <summary>
    /// 原深度
    /// </summary>
    public int OriginalDepth
    {
        get;
        private set;
    }

    public int Depth => m_CachedCanvas.sortingOrder;
    
    private int m_UIGroupDepth;
    
    private int m_DepthInUIGroup;

    /// <summary>
    /// 子节点所有的Canvas
    /// </summary>
    private Dictionary<string, int> m_CachedCanvasContainer;

    /// <summary>
    /// 子节点所有的渲染器
    /// </summary>
    private Dictionary<string, int> m_RendererDic;

    /// <summary>
    /// 子节点所有的SortingGroup
    /// </summary>
    private Dictionary<string, int> m_SortingGroupDic;


    /// <summary>
    /// 打开此UI时播放的音效
    /// </summary>
    [HideInInspector]
    public int OpenSoundId;

    /// <summary>
    /// 关闭此UI时播放的音效
    /// </summary>
    [HideInInspector]
    public int CloseSoundId;
   

    /// <summary>
    /// 子节点排序组件
    /// </summary>
    private ILayoutController[] m_LayoutControllers;

    public ILayoutController[] LayoutControllers
    {
        get
        {
            return m_LayoutControllers??= GetComponentsInChildren<ILayoutController>();
        }
    }
   
    /// <summary>
    /// 红点对象引用字典
    /// </summary>
    private Dictionary<int, int> m_RedDotDic;

    GraphicRaycaster m_GraphicRaycaster;     
    
    private UIContainer m_PanelUIContainer;

    public UIContainer PanelUIContainer
    {
        get
        {
            if (m_PanelUIContainer == null)
            {
                m_PanelUIContainer = GetComponentInParent<UIContainer>();
            }
            return m_PanelUIContainer;
        }
    }
    
    /// <summary>
    /// 获取界面是否可用。
    /// </summary>
    private bool m_UIAvailable;
    
    public bool AddCanvasGroup = true;

    public bool AddGraphicRaycaster = true;
    
    private AAAGameEventHelper m_EventHelper = new AAAGameEventHelper();

    public RectTransform URectTransform
    {
        get
        {
            return GetComponent<RectTransform>();
        }
    }

    public MonoBehaviour Mono
    {
        get
        {
            return GetComponent<MonoBehaviour>();
        }
    }
    
    public UIParams Params { get; private set; }

    /// <summary>当前由此面板管理的子表单数量。</summary>
    internal int SubUIFormCount => m_SubUIForms?.Count ?? 0;
    
    /// <summary>
    /// 子UI界面, 会随着父界面关闭而关闭
    /// </summary>
    IList<int> m_SubUIForms = null;
    
    protected void Awake()
    {
        #if UNITY_EDITOR
              CheckLocalization();
        #endif
    }

    /// <summary>
    /// 获取节点组件
    /// </summary>
    /// <typeparam name="T">获取节点的指定组件类型</typeparam>
    /// <param name="key">节点名</param>
    /// <returns>返回节点的指定组件类型</returns>
    public T Get<T>(string key)
    {
        return PanelUIContainer.Get<T>(key);
    }

    /// <summary>
    /// 关闭UI
    /// </summary>
    /// <param name="closeCall">由底层接口调用关闭回调</param>
    public void Close(GameFrameworkAction closeCall = null)
    {
        StopAllCoroutines();
        PlayCloseAnim();
        Close(OpenFade, closeCall);
    }

    /// <summary>
    /// 关闭UI
    /// </summary>
    public void Close(bool ignoreFade, GameFrameworkAction closeCall = null)
    {
        if (ignoreFade && isActiveAndEnabled)
        {
            StartCoroutine(CloseCo(FadeTime, closeCall));
        }
        else
        {
            if (closeCall != null)
            {
                closeCall();
            }
            else
            {
                GF.UI.CloseUIForm(UIForm);
            }
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="soundId"></param>
    private void PlayUISound(int soundId)
    {
        GF.AssetBridge?.PlayUISound(soundId);
    }
   
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        m_CachedCanvas = gameObject.GetOrAddComponent<Canvas>();
        m_CachedCanvas.overrideSorting = true;
        OriginalDepth = m_CachedCanvas.sortingOrder;
        
        if (AddCanvasGroup)
        {
            m_CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        }
     
        m_LayerName = Layers.ToString();
        m_CachedCanvas.sortingLayerName = m_LayerName;
        FindNeedRefreshDepthChild();
        
        URectTransform.anchoredPosition3D = Vector3.zero;
        
        if (UIModel == UIModel.FullScreen)
        {
            AdaptiveNotchFit();
        }
        
        if (AddGraphicRaycaster)
        {
            m_GraphicRaycaster = gameObject.GetOrAddComponent<GraphicRaycaster>();
        }

        UGF_Button[] btns = GetComponentsInChildren<UGF_Button>(true);
        foreach(UGF_Button btn in btns)
        {
            if (!btn.CustomPolygon)
            {                  
                if(btn.PolygonCollider2D)
                {
                    float width = btn.UIHelper.GetWidth();
                    float height = btn.UIHelper.GetHeight();
                    float widthValue = float.Parse((width * 0.5f).ToString(CultureInfo.InvariantCulture));
                    float heightValue = float.Parse((height * 0.5f).ToString(CultureInfo.InvariantCulture));
                    Vector2 vector1 = new Vector2(widthValue * -1, heightValue);
                    Vector2 vector2 = new Vector2(widthValue, heightValue);
                    Vector2 vector3 = new Vector2(widthValue, heightValue * -1);
                    Vector2 vector4 = new Vector2(widthValue * -1, heightValue * -1);
                    btn.PolygonCollider2D.points = new[] { vector1, vector2, vector3, vector4 };
                }                  
            }
        }
    }

    /// <summary>
    /// 接收 Game Framework 打开回调并初始化面板可用状态和排序。
    /// </summary>
    /// <param name="userData">由 UIService 或调用方传入的界面参数。</param>
    protected override void OnOpen(object userData)
    {           
        //Debug.Log("界面打开OnOpen：" + this.gameObject.name); 
        var cvs = GetComponent<Canvas>();
        
        RegisterUI();
        RegisterEvent();

        Params = userData as UIParams;
        m_UIAvailable = true;
        
        if (Params is { SortOrder: not null } && cvs != null)
        {
            cvs.sortingOrder = Params.SortOrder.Value;
        }
       
        PlayOpenAnim();
        base.OnOpen(userData);
    }

    /// <summary>
    /// 清理面板事件和子界面，并在关闭动画完成后释放界面参数。
    /// </summary>
    /// <param name="isShutdown">是否由 Game Framework 关闭流程整体关闭。</param>
    /// <param name="userData">关闭操作传入的自定义数据。</param>
    protected override void OnClose(bool isShutdown, object userData)
    {
        //Debug.Log("界面关闭OnClose：" + this.gameObject.name);  
        m_UIAvailable = false;
        RemoveAllSubscribe();
        ClearAllRedDot();
        ClearAllDynamicLoadImage();
        ClearAllEventTrigger();
        CloseAllSubUIForms();
        UIParams closingParameters = Params ?? userData as UIParams;

        void CompleteClose()
        {
            try
            {
                base.OnClose(isShutdown, userData);
            }
            finally
            {
                if (ReferenceEquals(Params, closingParameters))
                {
                    Params = null;
                }

                closingParameters?.Release();
            }
        }

        if (isShutdown)
        {
            CompleteClose();
            return;
        }

        Close(CompleteClose);
    }


    protected override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
    {
        m_UIGroupDepth = uiGroupDepth;
        m_DepthInUIGroup = depthInUIGroup;
        base.OnDepthChanged(m_UIGroupDepth, m_DepthInUIGroup);

        //面板
        int deltaDepth = UGuiGroupHelper.DepthFactor * m_UIGroupDepth + DepthFactor * m_DepthInUIGroup;
        RefreshChildDepth(deltaDepth);
    }

    protected override void InternalSetVisible(bool visible)
    {
        if (Available && visible)
        {
            GF.UI.RefocusUIForm(UIForm);
        }                    

        if (!visible)
        {
            if (m_UIAvailable)
            {
               // m_UIBackGrounpMask?.SetActive(false); 
                ClearAllRedDot();
                
                //Debug.Log("设置界面可见 InternalSetVisible：" + this.gameObject.name + " " + visible);
                Close(() =>
                {
                   gameObject.SetActive(false);
                });    
            }
        }
        else
        {
            if (m_GraphicRaycaster)
            {
                SetCanvasGroupAlpha(1);
            }
            //Debug.Log("设置界面可见 InternalSetVisible：" + this.gameObject.name + " " + visible);
            gameObject.SetActive(true);
           // m_UIBackGrounpMask?.SetActive(true); 
           
        }     
    }

    protected override void OnPause()
    {
        gameObject.SetActive(false);
        base.OnPause();
    }

    /// <summary>
    /// 渐进效果
    /// </summary>
    public void FadeIn()
    {
        if (gameObject.activeSelf&&m_CanvasGroup)
        {
            if (OpenFade)
            {
                SetCanvasGroupAlpha(0);
                StartCoroutine(m_CanvasGroup.FadeToAlpha(1f, FadeTime));
            }
            else
            {
                SetCanvasGroupAlpha(1);
            }
        }   
    }

    private IEnumerator CloseCo(float duration, GameFrameworkAction closeCall = null)
    {
        yield return m_CanvasGroup.FadeToAlpha(0f, duration);
        if (closeCall != null)
        {
            closeCall();
        }
        else
        {
            GF.UI.CloseUIForm(UIForm);
        }
    }

    /// <summary>
    /// 初始化需要更新层级的子节点对象
    /// </summary>
    private void FindNeedRefreshDepthChild()
    {
        if (AutoDepthChanged)
        {         
            m_CachedCanvasContainer ??= new Dictionary<string, int>();
            
            List<Canvas> canvas = new List<Canvas>();
            GetComponentsInChildren(true, canvas);
            foreach (var t in canvas)
            {
                if(t != m_CachedCanvas)
                {
                    m_CachedCanvasContainer.TryAdd(t.name,t.sortingOrder);
                }
            }
          
            //Renderer组件
            m_RendererDic ??= new Dictionary<string, int>();
            List<Renderer> renderers = new List<Renderer>();
            GetComponentsInChildren(true, renderers);
            foreach (var t in renderers)
            {
                m_RendererDic.TryAdd(t.name,t.sortingOrder);
            }
            
            //SortingGroup组件
            m_SortingGroupDic ??= new Dictionary<string, int>();
            List<SortingGroup> sortingGroups = new List<SortingGroup>();
            GetComponentsInChildren(true, sortingGroups);
            foreach (var t in sortingGroups)
            {
                m_SortingGroupDic.TryAdd(t.name,t.sortingOrder);
            }
        }
    }

    /// <summary>
    /// 刷新子节点的深度
    /// </summary>
    /// <param name="baseDepth">基础深度值，用于计算子节点的深度</param>
    private void RefreshChildDepth(int baseDepth)
    {
        m_CachedCanvas.sortingOrder = baseDepth;
        m_CachedCanvas.sortingLayerName = m_LayerName;

        if (AutoDepthChanged)
        {          
            //UI层级变化
            List<Canvas> canvas = new List<Canvas>();
            GetComponentsInChildren(true, canvas);
            foreach (var t in canvas)
            {
                if(t != m_CachedCanvas)
                {
                    var layer = 0;
                    if (m_CachedCanvasContainer.TryGetValue(t.name, out layer))
                    {
                        t.sortingOrder = layer + baseDepth;
                        t.sortingLayerName = m_LayerName;
                    }
                }
            }
          
            //Renderer组件
            m_RendererDic ??= new Dictionary<string, int>();
            List<Renderer> renderers = new List<Renderer>();
            GetComponentsInChildren(true, renderers);
            foreach (var t in renderers)
            {
                var layer = 0;
                if (m_RendererDic.TryGetValue(t.name, out layer))
                {
                    t.sortingOrder = layer + baseDepth;
                    t.sortingLayerName = m_LayerName;
                }
            }
            
            //SortingGroup组件
            m_SortingGroupDic ??= new Dictionary<string, int>();
            List<SortingGroup> sortingGroups = new List<SortingGroup>();
            GetComponentsInChildren(true, sortingGroups);
            foreach (var t in sortingGroups)
            {
                var layer = 0;
                if (m_SortingGroupDic.TryGetValue(t.name, out layer))
                {
                    t.sortingOrder = layer + baseDepth;
                    t.sortingLayerName = m_LayerName;
                }
            }
        }

        //m_CallBackListener.CallLua(CallBackType.UIDepthChanged,GetInstanceID());
    }
    
    /// <summary>
    /// 使用排序组参数
    /// </summary>
    /// <param name="sortingLayerName"></param>
    /// <param name="order"></param>
    public void SetSortingGroup(string sortingLayerName, int order)
    {
        m_LayerName = sortingLayerName;
        m_CachedCanvas.overrideSorting = true;
        RefreshChildDepth(order);
    }

    /// <summary>
    ///重新刷新排序组件，使其重绘
    /// </summary>
    public void RefreshLayout()
    {
        ILayoutController[] layoutControllers = GetComponentsInChildren<ILayoutController>();
        
        if(layoutControllers.Length > 0)
        {
            for (int i = layoutControllers.Length - 1; i >= 0; i--)
            {
                layoutControllers[i]?.SetLayoutVertical();
                layoutControllers[i]?.SetLayoutHorizontal();
            }
        } 
    }

    /// <summary>
    /// 添加红点
    /// </summary>
    /// <param name="target">UGF UI对象</param>
    /// <param name="pos">pos 1左上 2右上 3左下 4右下</param>
    /// <param name="x">x坐标</param>
    /// <param name="y">y坐标</param>
    /// <param name="callBack">回调</param>
    public void AddRedDot(GameObject target,int pos=2,float x=0,float y=0,GameFrameworkAction callBack = null)
    {
        if(target == null) return;
        
        int tHashCode = target.GetHashCode();
  

        m_RedDotDic ??= new Dictionary<int, int>();
        
        if (!m_RedDotDic.ContainsKey(tHashCode))
        {
            /*int instanceId = UIFramework.UISubjoin.LoadUISubjoin(UIFramework.AssetBridge.GetUIFormAsset("Global/UIRedDot"),"UISubjoinRedDot",
                redDot =>
                {
                    if (!Visible)
                    {
                        ClearAllRedDot();
                        return;
                    }
                    
                    UISubjoinRedDotLogic logic = (UISubjoinRedDotLogic)redDot;
                    
                    logic.Init(target.UTransform, pos, x, y,m_CachedCanvas.sortingOrder);
                    
                    if (callBack != null)
                    {
                        m_CallBackListener.CallLua(CallBackType.UIRedDot,tHashCode,logic);
                    }
                });
            
            m_RedDotDic.Add(tHashCode, instanceId);*/
        }
    }

    /// <summary>
    /// 移除红点
    /// </summary>
    /// <param name="target">UGF UI对象</param>
    public void RemoveRedDot(GameObject target)
    {
        if (target == null)
        {
            return;
        }
        
        if (m_RedDotDic == null)
        {
            return;
        }
        
        int id = target.GetHashCode();

        if (m_RedDotDic.ContainsKey(id))
        {
            //UIFramework.UISubjoin.RemoveUISubjoin(m_RedDotDic[id]);
            m_RedDotDic.Remove(id);
        }
    }

    /// <summary>
    /// 清除该面板上所有的红点
    /// </summary>
    public void ClearAllRedDot()
    {
        /*m_CallBackListener.RemoveLua(CallBackType.UIRedDot);
        
        if (m_RedDotDic == null)
        {
            return;
        }
        
        foreach(KeyValuePair<int,int> keyValuePair in m_RedDotDic)
        {               
            UIFramework.UISubjoin.RemoveUISubjoin(keyValuePair.Value);               
        }

        m_RedDotDic.Clear();*/
    }

    /// <summary>
    /// 清除所有的事件注册函数
    /// </summary>
    public void ClearAllEventTrigger()
    {          
        /*foreach (UGF_Button button in GetComponentsInChildren<UGF_Button>())
        {
            button.EventTrigger.Clean();
        }

        foreach (UGF_ItemCell itemCell in GetComponentsInChildren<UGF_ItemCell>())
        {
            itemCell.EventTrigger.Clean();
        }
        
        SetLuaBackgrounpOnClick(null);*/
    }

    /// <summary>
    /// 刘海屏适配
    /// </summary>
    private void AdaptiveNotchFit()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        float topOffseRatio = 0;
        topOffseRatio = GameGlobalSetting.OffsetHeight / GameGlobalSetting.DesignHeight;   
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = new Vector2(1, 1 - topOffseRatio);
        rectTransform.sizeDelta = Vector2.zero;
    }

    public void SetCanvasGroupAlpha(float val)
    {
        if(m_CanvasGroup == null) return;
        m_CanvasGroup.alpha = val;
        //Debug.LogFormat("SetCanvasGroupAlpha:{0} {1}",this.gameObject.name,val);
    }

    /// <summary>
    /// 关闭UI时，清空动态加载图片引用，否则会造成图集无法回收
    /// </summary>
    private void ClearAllDynamicLoadImage()
    {
        /*foreach (UGF_Image image in GetComponentsInChildren<UGF_Image>())
        {
            image.ClearDynamicLoadImage();
        }
        
        foreach (UGF_RawImage rawImage in GetComponentsInChildren<UGF_RawImage>())
        {
            rawImage.ClearDynamicLoadImage();
        }*/
    }

    /// <summary>
    /// 检测本地化文本是否正确使用
    /// </summary>
    private void CheckLocalization()
    {
        UGF_Text[] ugfTexts = GetComponentsInChildren<UGF_Text>(true);
        for (int i = 0; i < ugfTexts.Length; i++)
        {
            ugfTexts[i].CheckLocalization();
        }
    }
    
    /// <summary>
    /// 上锁后，不会被Release
    /// </summary>
    /// <param name="isLock">锁标记</param>
    public void SetLock(bool isLock)
    {
       GF.UI.SetUIFormInstanceLocked(this.UIForm,isLock);
    }
    
    /// <summary>
    /// 注册事件，让事件跟面板绑定，用于关闭面板时释放掉所有事件
    /// </summary>
    /// <param name="id"></param>
    /// <param name="handler"></param>
    protected void Subscribe(Int16 id,EventHandler<GameEvent> handler)
    {
        m_EventHelper.Subscribe(id,handler);
    }

    /// <summary>
    /// 派发事件
    /// </summary>
    /// <param name="id"></param>
    /// <param name="args"></param>
    protected void Fire(Int16 id,params object[] args)
    {
        m_EventHelper.Fire(id,args);
    }

    /// <summary>
    /// 移除事件
    /// </summary>
    /// <param name="id"></param>
    /// <param name="handler"></param>
    protected void Unsubscribe(int id, EventHandler<GameEvent> handler)
    {
        m_EventHelper.Unsubscribe(id,handler);
    }

    /// <summary>
    /// 移除所有绑定UI的事件
    /// </summary>
    protected void RemoveAllSubscribe()
    {
        m_EventHelper.RemoveAllSubscribe();
    }
    
    /// <summary>
    /// 初始化UI绑定,注意事机，在Base.OnInit
    /// </summary>
    protected virtual void RegisterUI()
    {
        
    }
    
    /// <summary>
    /// 初始化注册全部的事件,注意事机，在Base.OnInit
    /// </summary>
    protected virtual void RegisterEvent()
    {
       
    }
    
    /// <summary>
    /// 通过 UIService 异步打开子表单，并将排序参数传递给实际子表单。
    /// </summary>
    /// <typeparam name="TForm">目标子表单逻辑类型。</typeparam>
    /// <param name="relativePath">相对于 UI 根目录的子表单资源路径。</param>
    /// <param name="subUiOrder">子表单相对父面板的排序偏移；负数表示追加到现有子表单之后。</param>
    /// <param name="parameters">传给子表单的参数；所有权转交给 UIService。</param>
    /// <returns>加载并打开后的子表单逻辑。</returns>
    public UniTask<TForm> OpenSubUIFormAsync<TForm>(string relativePath, int subUiOrder = -1,
        UIParams parameters = null) where TForm : UIFormLogic
    {
        parameters ??= UIParams.Create();
        UIService.Instance.EnsureParametersAvailable(parameters);

        if (subUiOrder <= -1)
        {
            subUiOrder = SubUIFormCount;
        }

        parameters.SortOrder = Depth + subUiOrder + SubUIOrderOffset;
        parameters.IsSubUIForm = true;
        return UIService.Instance.OpenSubAsync<TForm>(this, relativePath, parameters, subUiOrder);
    }

    /// <summary>
    /// 在父面板仍处于打开状态时登记子表单，供父面板关闭时统一回收。
    /// </summary>
    /// <param name="serialId">Game Framework 分配给子表单的序列号。</param>
    /// <returns>父面板仍可管理子表单时返回 true。</returns>
    internal bool TryRegisterSubUIForm(int serialId)
    {
        if (!m_UIAvailable || UIForm == null || !GF.UI.HasUIForm(UIForm.SerialId))
        {
            return false;
        }

        if (m_SubUIForms == null)
        {
            m_SubUIForms = new List<int>(2);
        }

        m_SubUIForms.Add(serialId);
        return true;
    }
    
    /// <summary>
    /// 关闭子UI Form
    /// </summary>
    /// <param name="uiformId"></param>
    public void CloseSubUIForm(int uiformId)
    {
        if (!m_SubUIForms.Contains(uiformId)) return;
        m_SubUIForms.Remove(uiformId);
        if (GF.UI.HasUIForm(uiformId))
            GF.UI.CloseUIForm(uiformId);
    }
    
    /// <summary>
    /// 关闭全部子UI Form
    /// </summary>
    public void CloseAllSubUIForms()
    {
        if (m_SubUIForms != null)
        {
            for (int i = m_SubUIForms.Count - 1; i >= 0; i--)
            {
                CloseSubUIForm(m_SubUIForms[i]);
            }
        }
    }

    #region 面板动画

    private UGF_PanelAnimtion m_PanelAnimtion;
    private UGF_PanelAnimtion PanelAnimtion
    {
        get 
        { 
            if (m_PanelAnimtion == null) {

                m_PanelAnimtion = new UGF_PanelAnimtion();
            }

            return m_PanelAnimtion;
        }
    }


    /// <summary>
    /// 打开动画类型
    /// </summary>
    public PanelAnimtionType OpenAnim = PanelAnimtionType.Null;

    /// <summary>
    /// 关闭动画类型
    /// </summary>
    public PanelAnimtionType CloseAnim = PanelAnimtionType.Null;

    /// <summary>
    /// 若使用Animtor，使用的状态名
    /// </summary>
    public string OpenAnimStateName;

    /// <summary>
    /// 若使用Animtor，使用的状态名
    /// </summary>
    public string CloseAnimStateName;
  
    /// <summary>
    /// 播放打开动画
    /// </summary>
    private void PlayOpenAnim()
    {
        StopAllCoroutines();      
       
        PlayUISound(OpenSoundId);
        FadeIn();       
        
        PanelAnimtion.PlayAnim(this, OpenAnim, () => {
            //m_CallBackListener.CallLua(CallBackType.UIOpenAnimEnd,GetInstanceID());
            URectTransform.anchoredPosition3D = Vector3.zero;
        }, OpenAnimStateName);
    }

    /// <summary>
    /// 播放关闭动画
    /// </summary>
    private void PlayCloseAnim(GameFrameworkAction gameFrameworkAction = null)
    {
        if (!isActiveAndEnabled) return;
        PlayUISound(CloseSoundId);
        PanelAnimtion.PlayAnim(this, CloseAnim, () => {
            gameFrameworkAction?.Invoke();
        }, CloseAnimStateName);                   
    }

    /// <summary>
    /// 设置打开动画开始播放前的回调
    /// </summary>
    /// <param name="luaFunction"></param>
    public void SetOpenAnimStartCallBack(GameFrameworkAction callBack)
    {
        //m_CallBackListener.AddLua(CallBackType.UIOpenAnimStart,m_InstanceId,luaFunction);
    }

    /// <summary>
    /// 设置打开动画播放结束后的回调
    /// </summary>
    /// <param name="luaFunction"></param>
    public void SetOpenAnimEndCallBack(GameFrameworkAction callBack)
    {
        //m_CallBackListener.AddLua(CallBackType.UIOpenAnimEnd,m_InstanceId,luaFunction);
    }

    #endregion
    
}

