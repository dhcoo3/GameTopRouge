using System.Collections;
using System.Text;
using GameFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class UGF_Image : Image
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
    
    private Material Material { get; set; }
    
    /// <summary>
    /// 是否为动态加载图片
    /// </summary>
    [HideInInspector]
    public bool DynamicLoad;
    
    /// <summary>
    /// 动态加载路径,不填则在代码中处理
    /// </summary>
    [HideInInspector]
    public string DynamicLoadPath = "";
    
    /// <summary>
    /// 动态加载后的透明度设置
    /// </summary>
    [HideInInspector]
    public float DynamicLoadAlphaVal = 1;
    
    /// <summary>
    /// 缓存原材质
    /// </summary>
    private Material m_CacheOriginalMaterial;

    /// <summary>
    /// 缓存原图片名
    /// </summary>
    private StringBuilder m_CacheTextureAssetName;
    
    /// <summary>
    /// 缓存原材质名
    /// </summary>
    private StringBuilder m_CacheMaterialAssetName;

    private WaitForEndOfFrame m_WaitForEndOfFrame;
    
    /// <summary>
    /// 透明度颜色，用于做透明度渐变使用,避免频繁new color
    /// </summary>
    private Color m_AlphaColor = Color.white;

    /// <summary>
    /// 缓存图集名
    /// </summary>
    private StringBuilder m_SpriteAtlasAssetName;

    /// <summary>
    /// 忽略自动化绑定图集，见BuildPrefabAtlas
    /// </summary>
    [HideInInspector] public bool IgnoreAutoAtlas = false;

    UGF_UIHelper m_UgfUIHelper;

    public UGF_UIHelper UIHelper
    {
        get
        {
            if (m_UgfUIHelper == null)
            {
                m_UgfUIHelper = UGF_UIHelper.Create(this.gameObject);
            }
            return m_UgfUIHelper;
        }
    }
    
    protected override void Awake()
    {
        StartDynamicLoadImage();
        
        #if UNITY_EDITOR
        CheckImageSize();
        #endif
   
        base.Awake();
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 检查图片尺寸，当尺寸宽高大于512*512时，使用动态加载，不要直接绑定，并勾选NeedLoad,加快UI面板的加载速度
    /// </summary>
    private void CheckImageSize()
    {
        if (!Application.isPlaying && sprite != null)
        {
            if (sprite.rect is { height: >= 512, width: >= 512 }
                && DynamicLoad == false)
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                if (!path.Contains("Assets/Res")) return;
                Debug.LogWarning("请优化该图片,清除图片引用,从代码动态加载,并勾选NeedLoad。Name="+gameObject.name+" "+path,gameObject);
            }
        }
    }
#endif
    protected override void OnEnable()
    {
        StartDynamicLoadImage();
        base.OnEnable();
    }
    
    protected override void OnDisable()
    {
        CancelDownloadImage();
        base.OnDisable();
    }

    protected override void Start()
    {
        if (material)
        {
            Material = material;           
        }

        base.Start();        
    }

    protected override void OnDestroy()
    {
        material = null;
        sprite = null;
        CancelInvoke();
        CancelDownloadImage();
        ReleaseBindAsset();
        base.OnDestroy();
    }    
    

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        switch (type)
        {
            case Type.Filled when m_SlicedClipMode && (fillMethod == FillMethod.Horizontal || fillMethod == FillMethod.Vertical) && hasBorder:
                GenerateSlicedSprite(toFill);
                break;
            default:
                base.OnPopulateMesh(toFill);
                break;
        }
    }

    public string GetCacheTextureAssetName()
    {
        if (m_CacheTextureAssetName == null) return "";
        return m_CacheTextureAssetName.ToString();
    }

    /// <summary>
    /// 设置图片，通过路径来加载AB包的图片
    /// 使用场景：当图片为单张图（不在图集中）,且需要此图做单独的材质处理时使用此接口，Sprite.Create生成的为独立个体
    /// PS:如果无需使用九宫格、拉伸、填充等操作，为减少开销可使用UGF_RawImage加载图片,不需要调用Sprite.Create
    /// </summary>
    /// <param name="assetPath">资源路径</param>
    /// <param name="callback">回调</param>
    /// <param name="loadIsSynNativeSize">加载后是否自适应大小</param>
    /// <param name="releaseAndDestroy">释放资源时，同时销毁</param>
    public void SetImageCs(string assetPath, GameFrameworkAction callback = null,bool loadIsSynNativeSize = false,bool releaseAndDestroy = true)
    {
        SetAlpha(0);
        
        if (assetPath != "")
        {
            m_CacheTextureAssetName ??= new StringBuilder(0);
            m_CacheTextureAssetName.Clear();
            m_CacheTextureAssetName.Append(assetPath);
            
            GF.AssetBridge?.LoadSprite(m_InstanceId,this.gameObject,assetPath, assetCache => {
                if (this && gameObject)
                {
                    if (!m_CacheTextureAssetName.Equals(assetCache.Name))
                    {
                        SetAlpha(DynamicLoadAlphaVal);
                        return;
                    }

                    sprite = assetCache.Asset;
                    SetAlpha(DynamicLoadAlphaVal);
                    
                    if (loadIsSynNativeSize)
                    {
                        SetNativeSize();
                    }
                }

                callback?.Invoke();
            },releaseAndDestroy);
        }
        else
        {
            sprite = null;
        }
    }
    
    /// <summary>
    /// 设置图片，通过路径来加载AB包的图片
    /// 使用场景：当图片为单张图（不在图集中）,且需要此图做单独的材质处理时使用此接口，Sprite.Create生成的为独立个体
    /// PS:如果无需使用九宫格、拉伸、填充等操作，为减少开销可使用UGF_RawImage加载图片,不需要调用Sprite.Create
    /// </summary>
    /// <param name="atlasAssetPath">图集路径</param>
    /// <param name="assetName">图集内图处名</param>
    /// <param name="callback">加载回调</param>
    /// <param name="loadIsSynNativeSize">使用图片大小</param>
    public void SetImageCs(string atlasAssetPath, string assetName,GameFrameworkAction callback = null,bool loadIsSynNativeSize = false)
    {
        SetAlpha(0);
        
        if (atlasAssetPath != "")
        {
            m_SpriteAtlasAssetName ??= new StringBuilder(0);
            m_SpriteAtlasAssetName.Clear();
            m_SpriteAtlasAssetName.Append(atlasAssetPath);
            
            m_CacheTextureAssetName ??= new StringBuilder(0);
            m_CacheTextureAssetName.Clear();
            m_CacheTextureAssetName.Append(assetName);
            
            GF.AssetBridge.LoadAsset(atlasAssetPath, assetObj =>
            {
                if (this && gameObject)
                {
                    SpriteAtlas atlas = assetObj.Asset as SpriteAtlas;
                    this.sprite = atlas.GetSprite(assetName);
                    SetAlpha(DynamicLoadAlphaVal);
                    if (loadIsSynNativeSize)
                    {
                        SetNativeSize();
                    }
                }                 

                callback?.Invoke();
            },typeof(SpriteAtlas));
        }
        else
        {
            sprite = null;
        }
    }

    /// <summary>
    /// 下载图片到本地
    /// </summary>
    /// <param name="assetPath">url</param>
    /// <param name="callback">回调</param>
    /// <param name="loadIsSynNativeSize">使用图片尺寸</param>
    public void DownloadImage(string assetPath,GameFrameworkAction callback = null,bool loadIsSynNativeSize = false)
    {
        SetAlpha(0);
        
        if (assetPath != "")
        {
            m_CacheTextureAssetName ??= new StringBuilder(0);
            m_CacheTextureAssetName.Clear();
            m_CacheTextureAssetName.Append(assetPath);
            
            
            GF.AssetBridge?.DownloadTextureToImageAsync(m_InstanceId,this.gameObject,assetPath, assetCache => {
                if (this && gameObject)
                {
                    if (!m_CacheTextureAssetName.Equals(assetCache.Name))
                    {
                        SetAlpha(DynamicLoadAlphaVal);
                        return;
                    }

                    sprite = assetCache.Asset;
                    SetAlpha(DynamicLoadAlphaVal);
                    
                    if (loadIsSynNativeSize)
                    {
                        SetNativeSize();
                    }
                }
                callback?.Invoke();
            });
        }
        else
        {
            sprite = null;
        }
    }

    /// <summary>
    /// 中断下载图片
    /// </summary>
    public void CancelDownloadImage()
    {
        GF.AssetBridge?.CancelDownloadImage(GetInstanceID());
    }
    
    /// <summary>
    /// 设置图片加载完成后的回调
    /// </summary>
    /// <param name="luaFunction"></param>
    public void SetImageLoadCallBack(GameFrameworkAction callBack)
    {
        //m_CallBackListener.AddLua(CallBackType.LoadImageCall,m_InstanceId,luaFunction);
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    public void SetColor(float r, float g, float b, float a)
    {
        color = new Color(r, g, b, a);
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    /// <param name="color"></param>
    public void SetColor(Color color)
    {
        this.color = color;
    }

    /// <summary>
    /// 获取颜色的代码
    /// </summary>
    /// <returns></returns>
    public string GetColorString()
    {
        return GetColorString(color);
    }

    /// <summary>
    /// 获取颜色的代码
    /// </summary>
    /// <returns></returns>
    public string GetColorString(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }


    /// <summary>
    /// 设置颜色
    /// </summary>
    /// <param name="colorStr"></param>
    public void SetColor(string colorStr)
    {
        ColorUtility.TryParseHtmlString(colorStr, out var nowColor);
        color = nowColor;
    }

    /// <summary>
    /// 重置图片大小到原资源Size
    /// </summary>
    public void SetImageNativeSize()
    {
        SetNativeSize();
    }

    /// <summary>
    /// 设置图片是否可被射线检测
    /// </summary>
    /// <param name="isRaycastTarget"></param>
    public void SetRaycastTarget(bool isRaycastTarget)
    {
        raycastTarget = isRaycastTarget;
    }

    public override Material GetModifiedMaterial(Material baseMaterial)
    {
        Material cModifiedMat  = base.GetModifiedMaterial(baseMaterial);
        Material = cModifiedMat;
        return cModifiedMat;
    }

    /// <summary>
    /// 设置图片上的Material的某个属性的值
    /// </summary>
    /// <param name="name"></param>
    /// <param name="val"></param>
    public void SetMaterialParameterFloat(string name, float val)
    {            
       Material.SetFloat(name, val);                        
    }

    /// <summary>
    /// 设置图片的Material
    /// </summary>
    /// <param name="materialsAsset">加载的材质名字</param>
    /// <param name="callBack">加载回调</param>
    /// <param name="restoreTime">使用当前加载的材质显示多少时间后，还原为原材质</param>
    /// <param name="isLocalized">判定本地化加载</param>
    public void SetMaterial(string materialsAsset,GameFrameworkAction callBack = null, float restoreTime = -1)
    {
        //缓存原材质
        if (m_CacheOriginalMaterial == null && material != null)
        {
            m_CacheOriginalMaterial = material;               
        }

        m_CacheMaterialAssetName ??= new StringBuilder(50);

        if (string.IsNullOrEmpty(materialsAsset))
        {
            material = null;
            m_CacheMaterialAssetName.Clear();
            return;
        }
        
        m_CacheMaterialAssetName.Clear();
        m_CacheMaterialAssetName.Append(materialsAsset);
        
        GF.AssetBridge?.LoadNewMaterial(m_InstanceId,this.gameObject,materialsAsset, assetCache => 
        {
            if (!m_CacheMaterialAssetName.Equals(assetCache.Name))
            {
                return;
            }
            
            material = assetCache.Asset;   

            if (restoreTime > -1)
            {
                Invoke("RestoreMaterial", restoreTime);
            }
            
            callBack.Invoke();
        });
    }

    /// <summary>
    /// 还原初始材质
    /// </summary>
    public void RestoreMaterial()
    {
        m_CacheOriginalMaterial ??= material;
        m_CacheMaterialAssetName?.Clear();
        material = m_CacheOriginalMaterial;
    }        
    
    /// <summary>
    /// 通过CanvasGroup设置透明，会包含影响到子节点
    /// </summary> 
    /// <param name="endValue"></param>
    public void SetCanvasGroupAlpha(float endValue)
    {
        CanvasGroup group = this.gameObject.GetOrAddComponent<CanvasGroup>();             
        group.alpha = endValue;
    }

    /// <summary>
    /// 设置Fill裁剪,只适用于Filled模式
    /// </summary>
    /// <param name="value"></param>
    public void SetFillAmount(float value)
    {
        StopAllCoroutines();
        if (type == Type.Filled)
        {
            fillAmount = value;
        }
    }
    
    /// <summary>
    /// 设置Fill裁剪,只适用于Filled模式，使用缓动效果
    /// </summary>
    /// <param name="toVal"></param>
    /// <param name="duration"></param>
    /// <param name="updateCall"></param>
    /// <param name="endCall"></param>
    public void DoFillAmount(float toVal,float duration, GameFrameworkAction<float> updateCall, GameFrameworkAction endCall=null)
    {
        if(!gameObject.activeInHierarchy) return;
        m_WaitForEndOfFrame ??= new WaitForEndOfFrame();
        StopAllCoroutines();
        StartCoroutine(SmoothAmountValue(toVal,duration,updateCall,endCall));
    }
    
    private IEnumerator SmoothAmountValue(float value, float duration,GameFrameworkAction<float> updateCall, GameFrameworkAction endCall=null)
    {
        float time = 0f;
        float originalValue = fillAmount;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            fillAmount = Mathf.Lerp(originalValue, value, time / duration);
            updateCall.Invoke(fillAmount);
            yield return m_WaitForEndOfFrame;
        }

        fillAmount = value;
        endCall?.Invoke();
    }
    
    /// <summary>
    /// 设置透明度
    /// </summary>
    /// <param name="alpha">透明度</param>
    public void SetAlpha(float alpha)
    {
        if (this.sprite == null)
        {
            alpha = 0;
        }
        
        color = new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// Alpha渐变
    /// </summary>
    /// <param name="alpha"></param>
    /// <param name="duration"></param>
    /// <param name="ignoreTimeScale"></param>
    public override void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
    {
        if(!gameObject.activeInHierarchy) return;
        m_WaitForEndOfFrame ??= new WaitForEndOfFrame();
        StopAllCoroutines();
        m_AlphaColor = color;
        StartCoroutine(SmoothAlphaValue(m_AlphaColor.a, alpha, duration));
    }

    public IEnumerator SmoothAlphaValue(float oldVal,float value, float duration)
    {
        float time = 0f;
        float originalValue = oldVal;
        while (time < duration)
        {
            time += Time.deltaTime;
            m_AlphaColor.a = Mathf.Lerp(originalValue, value, time / duration);
            color = m_AlphaColor;
            yield return m_WaitForEndOfFrame;
        }

        color = new Color(color.r, color.g, color.b, value);
    }

    /// <summary>
    /// 激活UI时，检测动态加载
    /// </summary>
    private void StartDynamicLoadImage()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        
        if (!DynamicLoad)
        {
            return;
        }
            
        if (string.IsNullOrEmpty(DynamicLoadPath))
        {
            if (m_CacheTextureAssetName == null)
            {
                SetAlpha(0);
            }
          
            return;
        }

        if (!DynamicLoadPath.Contains("Assets/Res"))
        {
            return;
        }
      
        if (m_CacheTextureAssetName != null 
            && m_CacheTextureAssetName.Equals(DynamicLoadPath))
        {
            return;
        }
        
        SetAlpha(0);
        SetImageCs(DynamicLoadPath);
    }
    
    /// <summary>
    /// 关闭UI时，清空动态加载图片引用，否则会造成图集无法回收
    /// </summary>
    public void ClearDynamicLoadImage()
    {
        if (!DynamicLoad)
        {
            return;
        }
        
        SetAlpha(0);
        m_CacheTextureAssetName?.Clear();
        this.sprite = null;
    }
    
    /// <summary>
    /// 释放该组件加载的资源对象
    /// </summary>
    private void ReleaseBindAsset()
    {
        if (GF.AssetBridge != null)
        {
            if(m_CacheTextureAssetName != null && m_CacheTextureAssetName.Length != 0)
            {
                GF.AssetBridge.RemoveSprite(m_InstanceId, m_CacheTextureAssetName.ToString());
            }
        
            if(m_CacheMaterialAssetName != null && m_CacheMaterialAssetName.Length != 0)
            {
                GF.AssetBridge.RemoveNewMaterial(m_InstanceId, m_CacheMaterialAssetName.ToString());
            }

            if (m_SpriteAtlasAssetName != null && m_SpriteAtlasAssetName.Length != 0)
            {
                GF.AssetBridge.RemoveSpriteAtlasRef(m_InstanceId, m_SpriteAtlasAssetName.ToString());
            }
        }
    }

    #region Filled模块支持九宫格效果

    [HideInInspector]
    public bool m_SlicedClipMode = false;
   
    private Vector2[] s_VertScratch = new Vector2[4];
    private Vector2[] s_UVScratch = new Vector2[4];

    private void GenerateSlicedSprite(VertexHelper toFill)
    {
        var activeSprite = overrideSprite ?? sprite;

        Vector4 outer, inner, padding, border;

        if (activeSprite != null)
        {
            outer = UnityEngine.Sprites.DataUtility.GetOuterUV(activeSprite);
            inner = UnityEngine.Sprites.DataUtility.GetInnerUV(activeSprite);
            padding = UnityEngine.Sprites.DataUtility.GetPadding(activeSprite);
            border = activeSprite.border;
        }
        else
        {
            outer = Vector4.zero;
            inner = Vector4.zero;
            padding = Vector4.zero;
            border = Vector4.zero;
        }
        Rect rect = GetPixelAdjustedRect();
        Vector4 adjustedBorders = GetAdjustedBorders(border / pixelsPerUnit, rect);
        padding = padding / pixelsPerUnit;

        s_VertScratch[0] = new Vector2(padding.x, padding.y);
        s_VertScratch[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);

        s_VertScratch[1].x = adjustedBorders.x;
        s_VertScratch[1].y = adjustedBorders.y;

        s_VertScratch[2].x = rect.width - adjustedBorders.z;
        s_VertScratch[2].y = rect.height - adjustedBorders.w;

        for (int i = 0; i < 4; ++i)
        {
            s_VertScratch[i].x += rect.x;
            s_VertScratch[i].y += rect.y;
        }

        s_UVScratch[0] = new Vector2(outer.x, outer.y);
        s_UVScratch[1] = new Vector2(inner.x, inner.y);
        s_UVScratch[2] = new Vector2(inner.z, inner.w);
        s_UVScratch[3] = new Vector2(outer.z, outer.w);

        float xLength = s_VertScratch[3].x - s_VertScratch[0].x;
        float yLength = s_VertScratch[3].y - s_VertScratch[0].y;
        float len1XRatio = (s_VertScratch[1].x - s_VertScratch[0].x) / xLength;
        float len1YRatio = (s_VertScratch[1].y - s_VertScratch[0].y) / yLength;
        float len2XRatio = (s_VertScratch[2].x - s_VertScratch[1].x) / xLength;
        float len2YRatio = (s_VertScratch[2].y - s_VertScratch[1].y) / yLength;
        float len3XRatio = (s_VertScratch[3].x - s_VertScratch[2].x) / xLength;
        float len3YRatio = (s_VertScratch[3].y - s_VertScratch[2].y) / yLength;
        int xLen = 3, yLen = 3;
        if (fillMethod == FillMethod.Horizontal)
        {
            if (fillAmount >= (len1XRatio + len2XRatio))
            {
                float ratio = 1 - (fillAmount - (len1XRatio + len2XRatio)) / len3XRatio;
                s_VertScratch[3].x = s_VertScratch[3].x - (s_VertScratch[3].x - s_VertScratch[2].x) * ratio;
                s_UVScratch[3].x = s_UVScratch[3].x - (s_UVScratch[3].x - s_UVScratch[2].x) * ratio;
            }
            else if (fillAmount >= len1XRatio)
            {
                xLen = 2;
                float ratio = 1 - (fillAmount - len1XRatio) / len2XRatio;
                s_VertScratch[2].x = s_VertScratch[2].x - (s_VertScratch[2].x - s_VertScratch[1].x) * ratio;
            }
            else
            {
                xLen = 1;
                float ratio = 1 - fillAmount / len1XRatio;
                s_VertScratch[1].x = s_VertScratch[1].x - (s_VertScratch[1].x - s_VertScratch[0].x) * ratio;
                s_UVScratch[1].x = s_UVScratch[1].x - (s_UVScratch[1].x - s_UVScratch[0].x) * ratio;
            }
        }
        else if (fillMethod == FillMethod.Vertical)
        {
            if (fillAmount >= (len1YRatio + len2YRatio))
            {
                float ratio = 1 - (fillAmount - (len1YRatio + len2YRatio)) / len3YRatio;
                s_VertScratch[3].y = s_VertScratch[3].y - (s_VertScratch[3].y - s_VertScratch[2].y) * ratio;
                s_UVScratch[3].y = s_UVScratch[3].y - (s_UVScratch[3].y - s_UVScratch[2].y) * ratio;
            }
            else if (fillAmount >= len1YRatio)
            {
                yLen = 2;
                float ratio = 1 - (fillAmount - len1YRatio) / len2YRatio;
                s_VertScratch[2].y = s_VertScratch[2].y - (s_VertScratch[2].y - s_VertScratch[1].y) * ratio;
            }
            else
            {
                yLen = 1;
                float ratio = 1 - fillAmount / len1YRatio;
                s_VertScratch[1].y = s_VertScratch[1].y - (s_VertScratch[1].y - s_VertScratch[0].y) * ratio;
                s_UVScratch[1].y = s_UVScratch[1].y - (s_UVScratch[1].y - s_UVScratch[0].y) * ratio;
            }
        }

        toFill.Clear();

        for (int x = 0; x < xLen; ++x)
        {
            int x2 = x + 1;

            for (int y = 0; y < yLen; ++y)
            {
                if (!fillCenter && x == 1 && y == 1)
                    continue;

                int y2 = y + 1;


                AddQuad(toFill,
                    new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
                    new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
                    color,
                    new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
                    new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
            }
        }
    }

    static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax)
    {
        int startIndex = vertexHelper.currentVertCount;

        vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
        vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
        vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
        vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect)
    {
        Rect originalRect = rectTransform.rect;

        for (int axis = 0; axis <= 1; axis++)
        {
            float borderScaleRatio;

            if (originalRect.size[axis] != 0)
            {
                borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                border[axis] *= borderScaleRatio;
                border[axis + 2] *= borderScaleRatio;
            }

            float combinedBorders = border[axis] + border[axis + 2];
            if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
            {
                borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                border[axis] *= borderScaleRatio;
                border[axis + 2] *= borderScaleRatio;
            }
        }
        return border;
    }
    #endregion
}

