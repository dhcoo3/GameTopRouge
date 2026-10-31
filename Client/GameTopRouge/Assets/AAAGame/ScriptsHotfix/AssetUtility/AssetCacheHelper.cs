using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AAAGame.ScriptsHotfix.AssetUtility
{
    /// <summary>
    /// 资源缓存器
    /// </summary>
    public class AssetCacheHelper<T> where T : Object
    {
        /// <summary>
        /// 资源缓存对象字典
        /// </summary>
        Dictionary<string, AssetCache<T>> m_AssetCacheDic = new Dictionary<string, AssetCache<T>>(10);
    
        /// <summary>
        /// 资源绑定对象集合
        /// </summary>
        Dictionary<int, AssetCache<T>> m_AssetBindDic = new Dictionary<int, AssetCache<T>>(10);


        private List<AssetCache<T>> m_WaitReleaseAssetList = new List<AssetCache<T>>();
     
        /// <summary>
        /// 增加一个资源到缓存池中
        /// </summary>
        /// <param name="assetName">资源名</param>
        /// <param name="action"></param>
        /// <param name="releaseAndDestroy">释放资源时，同时销毁</param>
        public AssetCache<T> AddAsset(int targetInstanceId,GameObject bindTraget,string assetName,GameFrameworkAction<AssetInfo> loadAssetCall,bool releaseAndDestroy)
        {       
            if (string.IsNullOrEmpty(assetName)) return null;
        
            if (!m_AssetCacheDic.ContainsKey(assetName))
            {
                AssetCache<T> assetCache = AssetCache<T>.Create(releaseAndDestroy);
                assetCache.Name = assetName;
                assetCache.SetLoadCall(loadAssetCall);
                m_AssetCacheDic.Add(assetName, assetCache);
                TryAdd(assetCache, targetInstanceId,bindTraget);
                return assetCache;
            }

            return null;
        }

        /// <summary>
        /// 获取缓存资源
        /// </summary>
        /// <param name="assetName">资源名</param>
        /// <returns></returns>
        public AssetCache<T> GetAsset(int targetInstanceId,string assetName,GameObject bindTraget)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            if (m_AssetCacheDic.TryGetValue(assetName, out var assetCache))
            {
                TryAdd(assetCache, targetInstanceId,bindTraget);
            };
        
            return assetCache;
        }

        private void TryAdd(AssetCache<T> bindAssetCache,int targetInstanceId,GameObject bindTraget)
        {
            if (m_AssetBindDic.TryGetValue(targetInstanceId,out var assetCache))
            {
                assetCache.RemoveBindAsset(targetInstanceId);
                m_AssetBindDic.Remove(targetInstanceId);
            }
    
            bindAssetCache.BindAsset(targetInstanceId, bindTraget);
            m_AssetBindDic.Add(targetInstanceId, bindAssetCache);
        }

        /// <summary>
        /// 设置加载资源到资源缓存对象
        /// </summary>
        /// <param name="assetCacheKey"></param>
        /// <param name="asset"></param>
        public void SetLoadAsset(string assetCacheKey, T asset)
        {
            if(string.IsNullOrEmpty(assetCacheKey)) return;
        
            if(m_AssetCacheDic.TryGetValue(assetCacheKey, out var assetCache))
            {
                assetCache.Asset = asset;
                assetCache.LoadFinish = true;
                assetCache.Dispose();
            }
        }

        /// <summary>
        /// 移除资源计数
        /// </summary>
        /// <param name="assetName"></param>
        public int RemoveAsset(int targetInstanceId,string assetName)
        {
            if(string.IsNullOrEmpty(assetName)) return 0 ;
        
            if (m_AssetBindDic.TryGetValue(targetInstanceId,out var assetBind))
            {
                assetBind.RemoveBindAsset(targetInstanceId);
                m_AssetBindDic.Remove(targetInstanceId);
                return assetBind.Count;
            }

            return 0;
        }

        public void Clear()
        {
            //Debug.Log("AssetCacheHelper clear");
            foreach(KeyValuePair<string,AssetCache<T>> keyValuePair in m_AssetCacheDic)
            {
                ReferencePool.Release(keyValuePair.Value);
            }

            m_AssetCacheDic.Clear();
            m_AssetBindDic.Clear();
            m_WaitReleaseAssetList.Clear();
        }

        /// <summary>
        /// 释放不在用的资源
        /// </summary>
        public void Release()
        {
            foreach (var data in m_AssetCacheDic)
            {
                if (data.Value.Count == 0)
                {
                    ReferencePool.Release(data.Value);
                    m_WaitReleaseAssetList.Add(data.Value);
                }
            }

            foreach (var assetCache in m_WaitReleaseAssetList)
            {
                //Debug.Log("AssetCacheHelper Release " + assetCache.Name);
                m_AssetCacheDic.Remove(assetCache.Name);
            }
        
            m_WaitReleaseAssetList.Clear();
        }
    
        /// <summary>
        /// 释放不在用的资源
        /// </summary>
        public void Release(string name)
        {
            if (m_AssetCacheDic.TryGetValue(name, out var assetCache))
            {
                ReferencePool.Release(assetCache);
                m_AssetCacheDic.Remove(name);
            }
        }
    
        /// <summary>
        /// 移除AssetCache
        /// </summary>
        /// <param name="assetName"></param>
        public void RemoveAssetCache(string assetName)
        {
            m_AssetCacheDic.Remove(assetName);
        }

        /// <summary>
        /// 获取AssetCache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AssetCache<T> GetAssetCache(int id)
        {
            return m_AssetBindDic.TryGetValue(id, out var assetCache) ? assetCache : null;
        }
    }
}
