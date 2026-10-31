using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Entity;
using GameFramework.Event;
using GameFramework.Resource;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;
using UnityGameFramework.Runtime;
using Object = UnityEngine.Object;
using ShowEntityFailureEventArgs = UnityGameFramework.Runtime.ShowEntityFailureEventArgs;
using ShowEntitySuccessEventArgs = UnityGameFramework.Runtime.ShowEntitySuccessEventArgs;

namespace AAAGame.ScriptsHotfix.AssetUtility
{
    /// <summary>
    /// 资源扩展接口
    /// </summary>
    public partial class AssetBridgeComponent: GameFrameworkComponent
    {
        private bool m_Init = false;
    
        /// <summary>
        /// 游戏中使用的主字体
        /// </summary>
        private AssetCache<Font> m_MainFont;

        /// <summary>
        /// 动态生成 Image Sprite的缓存器
        /// </summary>
        private AssetCacheHelper<Sprite> m_SpriteCacheHelper = new AssetCacheHelper<Sprite>();

        /// <summary>
        /// 加载 材质球的缓存器
        /// </summary>
        private AssetCacheHelper<Material> m_MaterialCacheHelper = new AssetCacheHelper<Material>();
    
        /// <summary>
        /// 图集的缓存器
        /// </summary>
        private AssetCacheHelper<SpriteAtlas> m_SpriteAtlasCacheHelper = new AssetCacheHelper<SpriteAtlas>();

        private Dictionary<int, GameFrameworkAction<Object>> m_DownloadImageDic = new Dictionary<int, GameFrameworkAction<Object>>();
    

        private float m_LastReleaseTime = 0;
    
        private static LoadAssetCallbacks m_LoadAssetCallbacks = null;
        
        /// <summary>
        /// 加载资源计数
        /// </summary>
        private int m_LoadId = 0;
    
        /// <summary>
        /// 音效管理器
        /// </summary>
        private SoundComponent m_SoundMediator;
        private SoundComponent SoundMediator
        {
            get
            {
                return m_SoundMediator ??= GF.Sound; 
            }
        }
    
        /// <summary>
        /// 自动释放间隔
        /// </summary>
        private float AutoReleaseInterval
        {
            get
            {
                return 5;
            }
        }

        protected override void Awake()
        {
            RegisterLoadAssetFunc();
            base.Awake();
        }

        /// <summary>
        /// 注册所有的加载回调
        /// </summary>
        public void RegisterLoadAssetFunc()
        {
            if (!m_Init)
            {
                m_Init = true;
                m_LoadAssetCallbacks = new LoadAssetCallbacks(OnLoadAssetSuccess, OnLoadAssetFailure);
                GF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
                GF.Event.Subscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
                //GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUISuccess);
                //GF.Event.Subscribe(OpenUIFormFailureEventArgs.EventId, OnOpenUIFailure);
            }        
        }
    
        private void Update()
        {
            if (m_LastReleaseTime > 0)
            {
                if (Time.time - m_LastReleaseTime >= AutoReleaseInterval)
                {
                    Release();
                    m_LastReleaseTime = Time.time;
                }
            }
            else
            {
                if (Time.time > AutoReleaseInterval)
                {
                    Release();
                    m_LastReleaseTime = Time.time;
                }
            }
        }
    
        public void Shutdown()
        {
            if (m_MainFont != null)
            {
                m_MainFont.Clear();        
                m_MainFont = null;
            }
      
            ClearAssetCachePool();
            GF.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            GF.Event.Unsubscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
            m_Init = false;
        }
    
        private void Release()
        {
            m_SpriteCacheHelper.Release();
            m_MaterialCacheHelper.Release();
            m_SpriteAtlasCacheHelper.Release();
        }
    
        /// <summary>
        /// 清除资源缓存，建议切换场景调用
        /// </summary>
        public void ClearAssetCachePool()
        {
            m_LastReleaseTime = Time.time;
            m_SpriteCacheHelper.Clear();
            m_MaterialCacheHelper.Clear();
            m_SpriteAtlasCacheHelper.Clear();
        }

        public void LoadAsset(string assetName,GameFrameworkAction<AssetInfo> callBack,Type type = null)
        {
            if (GF.Base.EditorResourceMode)
            {
#if UNITY_EDITOR
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(assetName,type);

                if (obj == null)
                {
                    Log.Error("资源不存在:{0}", assetName);
                }
                else
                {
                    AssetInfo assetInfo = AssetInfo.Create(++m_LoadId,assetName,callBack,type);
                    GF.Resource.LoadAsset(assetName, assetInfo.AssetType,m_LoadAssetCallbacks, assetInfo);
                }
#endif
            }
            else
            {
                if (GF.Resource.AssetCount > 0)
                {
                    AssetInfo assetInfo = AssetInfo.Create(++m_LoadId, assetName,callBack);
                    GF.Resource.LoadAsset(assetName, assetInfo.AssetType,m_LoadAssetCallbacks, assetInfo);
                }               
            }
        }
    
        /// <summary>
        /// 加载资源成功
        /// </summary>
        /// <param name="AssetName"></param>
        /// <param name="Asset"></param>
        /// <param name="duration"></param>
        /// <param name="userData"></param>
        private static void OnLoadAssetSuccess(string AssetName, object Asset, float duration, object userData)
        {
            AssetInfo loadInfo = (AssetInfo)userData;
       
            if (loadInfo == null)
            {
                throw new GameFrameworkException("Load Asset info is invalid.");
            }
       
            loadInfo.Asset = Asset;
            loadInfo.CSharpcCallBack(loadInfo);
            ReferencePool.Release(loadInfo);
        }

        /// <summary>
        /// 加载资源失败
        /// </summary>
        /// <param name="configAssetName"></param>
        /// <param name="status"></param>
        /// <param name="errorMessage"></param>
        /// <param name="userData"></param>
        private static void OnLoadAssetFailure(string configAssetName, LoadResourceStatus status, string errorMessage, object userData)
        {
            Log.Error("LoadAssetFailure:{0} {1} {2}", configAssetName, status, errorMessage);
        }

        /// <summary>
        /// 卸载一个资源
        /// </summary>
        /// <param name="asset"></param>
        public void UnloadAsset(object asset)
        {           
            GF.Resource.UnloadAsset(asset);
        }
    
        public int LoadEntity(string assetName, Type logicType,string group, int priority,GameFrameworkAction<AssetInfo> CSharpcCallBack,object userData = null)
        {
            if (m_LoadAssetCallbacks == null)
            {
                RegisterLoadAssetFunc();
            }

            if (GF.Base.EditorResourceMode)
            {
#if UNITY_EDITOR
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetName);

                if (obj == null)
                {
                    Log.Error("资源不存在:{0}", assetName);
                    return -1;
                }
                else
                {
                    int id = ++m_LoadId;
                    AssetInfo assetInfo = AssetInfo.Create(id, assetName,CSharpcCallBack,logicType,userData);
                    GF.Entity.ShowEntity(id,logicType, assetName, group, priority, assetInfo);
                    return id;
                }
#endif
            }
            else
            {
                int id = ++m_LoadId;
                AssetInfo assetInfo = AssetInfo.Create(id, assetName, CSharpcCallBack, logicType, userData);
                GF.Entity.ShowEntity(id,logicType, assetName, group, priority, assetInfo);
                return id;
            }
        }
    
    
        /// <summary>
        /// 加载实体成功
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
            ShowEntitySuccessEventArgs ne = (ShowEntitySuccessEventArgs)e;

            AssetInfo loadInfo = (AssetInfo)ne.UserData;
            if (loadInfo != null)
            {          
                loadInfo.Asset = ne.Entity;
                loadInfo.CSharpcCallBack(loadInfo);
            }

            ReferencePool.Release(loadInfo);
        }

        /// <summary>
        /// 加载实体失败
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            ShowEntityFailureEventArgs ne = (ShowEntityFailureEventArgs)e;
            Log.Warning("Show entity failure with error message '{0}'.", ne.ErrorMessage);
        }

        public void HideEntity(Entity entity)
        {
            GF.Entity.HideEntity(entity);
        }
    
        /// <summary>
        /// 加载纹理转换成Sprite到Image
        /// </summary>
        /// <param name="assetPath">资源名</param>
        /// <param name="border">九宫格数据</param>
        /// <param name="callBack">回调</param>
        /// <param name="releaseAndDestroy">释放资源时，同时销毁</param>
        public void LoadSprite(int instanceId,GameObject target,string assetPath, Action<AssetCache<Sprite>> callBack,bool releaseAndDestroy = true)
        {
            AssetCache<Sprite> assetCache = m_SpriteCacheHelper.GetAsset(instanceId,assetPath,target);

            if (assetCache == null)
            {
                GameFrameworkAction<AssetInfo> loadCall = ((assetObj) =>
                {
                    Sprite sprite = assetObj.Asset as Sprite;
                    if (sprite == null)
                    {
                        return;
                    }
                    m_SpriteCacheHelper.SetLoadAsset(assetPath, sprite);
                });
            
                ++m_LoadId;
                assetCache = m_SpriteCacheHelper.AddAsset(instanceId,target,assetPath, loadCall,releaseAndDestroy);
                assetCache.AddListener(callBack);
                LoadAsset(assetPath, loadCall,typeof(Sprite));
            }
            else
            {
                if (assetCache.LoadFinish)
                {
                    callBack?.Invoke(assetCache);
                }
                else
                {
                    assetCache.AddListener(callBack);
                }
            }
        }

        public void RemoveSprite(int instanceId,string assetPath)
        {
            m_SpriteCacheHelper.RemoveAsset(instanceId,assetPath);
        }
    
        /// <summary>
        /// 下载图片纹理转换成Sprite到Image
        /// </summary>
        /// <param name="assetPath">资源名</param>
        /// <param name="border">九宫格数据</param>
        /// <param name="callBack">回调</param>
        public void DownloadTextureToImageAsync(int instanceId,GameObject target,string assetPath, Action<AssetCache<Sprite>> callBack)
        {
            AssetCache<Sprite> assetCache = m_SpriteCacheHelper.GetAsset(instanceId,assetPath,target);

            if (assetCache == null)
            {
                GameFrameworkAction<AssetInfo> loadCall = ((assetObj) =>
                {
                    if(assetObj != null)
                    {
                        Texture2D tex = assetObj.Asset as Texture2D;
            
                        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), 
                            new Vector2(0.5f, 0.5f),
                            100,
                            0,
                            SpriteMeshType.Tight);
     
                        m_SpriteCacheHelper.SetLoadAsset(assetPath, sprite);
                    }
                });
            
                ++m_LoadId;
                assetCache = m_SpriteCacheHelper.AddAsset(instanceId,target,assetPath, loadCall,true);
                assetCache.AddListener(callBack);
                DownloadImage(instanceId,assetPath, loadCall);
            }
            else
            {
                if (assetCache.LoadFinish)
                {
                    callBack?.Invoke(assetCache);
                }
                else
                {
                    assetCache.AddListener(callBack);
                }
            }
        }
    
        /// <summary>
        /// 下载图片
        /// </summary>
        /// <param name="id">下载图片的对象</param>
        /// <param name="url">图片地址</param>
        /// <param name="callback">加载回调</param>
        public void DownloadImage(int id,string url,GameFrameworkAction<AssetInfo> callback)
        {
            /*//Debug.Log("DownloadImage:"+url);
        CancelDownloadImage(id);
        m_DownloadImageDic.Add(id,callback);
        GF.Base.StartCoroutine(DownloadImageAsyn(id,url));*/
        }
        
        /// <summary>
        /// 异步下载图片
        /// </summary>
        /// <param name="id">加载的UI组件对象ID</param>
        /// <param name="url">下载地址</param>
        private IEnumerator DownloadImageAsyn(int id,string url)
        {
            var request = UnityWebRequestTexture.GetTexture(url);
            request.timeout = 5;
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("下载图片失败: " + request.error);
                yield break;
            }
        
            // 将下载的纹理设置为主纹理
            Object texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            if (texture == null)
            {
                Debug.Log("下载资源格式不正确");
                yield break;
            }

            if (m_DownloadImageDic.ContainsKey(id))
            {
                m_DownloadImageDic[id].Invoke(texture);
            }
        }

        public void CancelDownloadImage(int id)
        {
            if (m_DownloadImageDic.ContainsKey(id))
            {
                m_DownloadImageDic.Remove(id);
            }
        }

        /// <summary>
        /// 加载并生成一个新的材质
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="callBack"></param>
        public void LoadNewMaterial(int instanceId,GameObject target,string assetPath, Action<AssetCache<Material>> callBack)
        {
            AssetCache<Material> assetCache = m_MaterialCacheHelper.GetAsset(instanceId,assetPath,target);

            if (assetCache == null)
            {
                GameFrameworkAction<AssetInfo> loadCall = ((assetObj) =>
                {
                    if (assetObj == null)
                    {
                        Debug.LogError("加载资源失败 LoadNewMaterial:"+assetPath);
                        return;
                    }
                
                    Material material = new Material(assetObj.Asset as Material);
                    m_MaterialCacheHelper.SetLoadAsset(assetPath, material);
                });
            
                ++m_LoadId;
                assetCache = m_MaterialCacheHelper.AddAsset(instanceId,target,assetPath, loadCall,true);
                assetCache.AddListener(callBack);
                LoadAsset(assetPath, loadCall);
            }
            else
            {
                if (assetCache.LoadFinish)
                {
                    callBack?.Invoke(assetCache);
                }
                else
                {
                    assetCache.AddListener(callBack);
                }
            }
        }
    
        public void RemoveNewMaterial(int instanceId,string assetPath)
        {
            m_MaterialCacheHelper.RemoveAsset(instanceId,assetPath);
        }
    
        /// <summary>
        /// 动态加载图集
        /// </summary>
        /// <param name="assetPath">图集资源地址</param>
        /// <param name="callBack">回调</param>
        public void LoadAtlas(int instanceId,GameObject target,string assetPath, Action<AssetCache<SpriteAtlas>> callBack, bool isLocalized = false)
        {
            AssetCache<SpriteAtlas> assetCache = m_SpriteAtlasCacheHelper.GetAsset(instanceId,assetPath,target);

            if (assetCache == null)
            {
                GameFrameworkAction<AssetInfo> loadCall = ((assetObj) =>
                {
                    if (assetObj == null)
                    {
                        Debug.LogError("加载资源失败 LoadAtlas:"+assetPath);
                        return;
                    }
                
                    SpriteAtlas spriteAtlas = assetObj.Asset as SpriteAtlas;
                    m_SpriteAtlasCacheHelper.SetLoadAsset(assetPath, spriteAtlas);
                });
            
                ++m_LoadId;
                assetCache = m_SpriteAtlasCacheHelper.AddAsset(instanceId,target,assetPath, loadCall,false);
                assetCache.AddListener(callBack);
                LoadAsset(assetPath, loadCall);
            }
            else
            {
                if (assetCache.LoadFinish)
                {
                    callBack?.Invoke(assetCache);
                }
                else
                {
                    assetCache.AddListener(callBack);
                }
            }
        }
 
        public void RemoveSpriteAtlasRef(int instanceId,string assetPath)
        {
            m_SpriteAtlasCacheHelper.RemoveAsset(instanceId,assetPath);
        }
   
  
        /// <summary>
        /// 关闭UI
        /// </summary>
        /// <param name="serialId"></param>
        public void CloseUIForm(int serialId)
        {
            if(GF.UI == null) return;
            GF.UI.CloseUIForm(serialId);
        }

        /// <summary>
        /// 加载字体
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="gameFrameworkAction"></param>
        public void LoadFont(string assetName,GameFrameworkAction<Font> gameFrameworkAction)
        {
            ++m_LoadId;
            string path = GetFontAsset(assetName);
            LoadAsset(path, (assetInfo) => {    
                if (assetInfo == null)
                {
                    Debug.LogError("加载资源失败 LoadFont:"+assetName);
                    return;
                }
                gameFrameworkAction?.Invoke(assetInfo.Asset as Font);
            },typeof(Font));
        }
    
        /// <summary>
        /// 加载主字体
        /// </summary>
        /// <param name="assetName">字体名字</param>
        public void LoadMainFont(string assetName)
        {
            string path = GetFontAsset(assetName);
            ++m_LoadId;
            LoadFont(path, (font) => {
                m_MainFont = AssetCache<Font>.Create(false);
                m_MainFont.Asset = font;
                m_MainFont.Name = path;
            });
        }

        /// <summary>
        /// 主字体
        /// </summary>
        /// <returns></returns>
        public Font GetMainFont()
        {
            return m_MainFont?.Asset;
        }

        /// <summary>
        /// 加载本地化文件
        /// </summary>
        /// <param name="assetPath">本地化文件路径</param>
        public void LoadLocalizationFile(string assetPath)
        {
            /*AssetsFactory.Instance?.LoadAssetAsync(assetPath, (UnityEngine.Object assetObj) => {
            if (assetObj == null)
            {
                Debug.LogError("加载资源失败 LoadLocalizationFile:"+assetPath);
                return;
            }
           
            TextAsset textAsset = assetObj as TextAsset;
            ReadLocalizationFile(textAsset.text);
            AssetsFactory.Instance?.UnloadAsset(assetPath,true);
        });*/
        }

        /// <summary>
        /// 本地化文件解析
        /// </summary>
        /// <param name="content">文本内容</param>
        private void ReadLocalizationFile(string content)
        {
            /*//1000行数据，PC执行速度为5ms
        string[] list = content.Split("\r\n");
        string[] list2;
        for(int i= 0; i < list.Length; i++)
        {
            list2 = list[i].Split("|");
            UIFramework.Localization.AddRowData(list2[0], list2[1]);
        }*/
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="id"></param>
        public void PlayUISound(int id)
        {     
            if(id > 0)
            {
                //SoundMediator.PlaySound("", false);
            }     
        }
    }
}
