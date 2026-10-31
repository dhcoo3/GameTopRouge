using System;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AAAGame.ScriptsHotfix.AssetUtility
{
    /// <summary>
    /// 资源缓存类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AssetCache<T> : IReference where T : Object
    {
        /// <summary>
        /// 资源引用集合
        /// </summary>
        Dictionary<int, GameObject> m_RefDict = new Dictionary<int, GameObject>(0);

        public int Count
        {
            get { return m_RefDict.Count; }
        }
    
        /// <summary>
        /// 资源
        /// </summary>
        public T Asset;
    
        /// <summary>
        /// 资源是否加载完成
        /// </summary>
        public bool LoadFinish;
    
        /// <summary>
        /// 资源名（路径）
        /// </summary>
        public string Name;
    
        /// <summary>
        /// 加载资源完成回调
        /// </summary>
        private GameFrameworkAction<AssetInfo> m_LoadAssetDoneCall;
    
        /// <summary>
        /// 注册的所有回调监听，加载完后后调用
        /// </summary>
        private Action<AssetCache<T>> m_LoadListeners;
    
        /// <summary>
        /// 释放时是否销毁
        /// </summary>
        private bool m_ReleaseAndDestroy = false;

        public static AssetCache<T> Create(bool releaseAndDestroy)
        {
            AssetCache<T> assetCache = ReferencePool.Acquire<AssetCache<T>>();
            assetCache.LoadFinish = false;
            assetCache.m_ReleaseAndDestroy = releaseAndDestroy;
            return assetCache;
        }

        /// <summary>
        /// 设置加载完成回调
        /// </summary>
        /// <param name="loadAssetCall"></param>
        public void SetLoadCall(GameFrameworkAction<AssetInfo> loadAssetCall)
        {
            m_LoadAssetDoneCall = loadAssetCall;
        }

        /// <summary>
        /// 注册加载监听
        /// </summary>
        /// <param name="action"></param>
        public void AddListener(Action<AssetCache<T>> action)
        {
            m_LoadListeners += action;        
        }

        /// <summary>
        /// 处理监听
        /// </summary>
        public void Dispose()
        {
            m_LoadListeners?.Invoke(this);
            m_LoadListeners = null;
        }

        /// <summary>
        /// 回收清理
        /// </summary>
        public void Clear()
        {
            //Debug.Log("AssetCache Clear:"+ Name + "  "+ m_RefDict.Count);
            m_RefDict.Clear();
            m_LoadListeners = null;
        
            if (m_ReleaseAndDestroy)
            {
                GameObject.Destroy(Asset);
            }
        
            if (LoadFinish)
            {
                GF.AssetBridge.UnloadAsset(Asset);
            }
            
            Asset = null;
        }

        public void BindAsset(int targetInstanceId,GameObject target = null)
        {
            //Debug.Log("AssetCache BindAsset:"+ Name + "  "+ targetInstanceId +  "  "+ m_RefDict.Count);
            m_RefDict.Add(targetInstanceId,target);
        }

        public void RemoveBindAsset(int targetInstanceId)
        {
            //Debug.Log("AssetCache RemoveBindAsset:"+Name + "  " + targetInstanceId+  "  "+ m_RefDict.Count);
            m_RefDict.Remove(targetInstanceId);
        }
    }
}