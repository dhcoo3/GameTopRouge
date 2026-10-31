using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using GameFramework;
using GameFramework.Event;
using Unity.Burst;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager
{
    /// <summary>可池化的玩法渲染器基类，管理渲染更新和本局事件订阅。</summary>
    public abstract class GameRender : IReference
    {
        public abstract void Initialize();
        
        /// <summary>随渲染器实例长期持有的事件助手，对局结束时只清空订阅。</summary>
        private readonly AAAGameEventHelper _aaaGameEventHelper = ReferencePool.Acquire<AAAGameEventHelper>();

        private readonly Dictionary<int, List<EventHandler<GameEventArgs>>> _events =
            new Dictionary<int, List<EventHandler<GameEventArgs>>>();

        /// <summary>移除本局事件订阅；助手仍随可池化渲染器持有，不能单独归还。</summary>
        public virtual void Clear()
        {
            RemoveAllSubscribe();
        }
      
        public virtual void RenderUpdate(fix deltaTime)
        {

        }
      
        protected void Subscribe(Int16 id, EventHandler<GameEvent> handler)
        {
            _aaaGameEventHelper.Subscribe(id,handler);
        }

        protected void Fire(Int16 id, params object[] args)
        {
            _aaaGameEventHelper.Fire(id, args);
        }

        protected void Unsubscribe(Int16 id, EventHandler<GameEvent> handler)
        {
            _aaaGameEventHelper.Unsubscribe(id,handler);
        }

        protected void RemoveAllSubscribe()
        {
            _aaaGameEventHelper.RemoveAllSubscribe();
        }

        protected T GetRender<T>() where T : GameRender
        {
            return GameRenderManager.Instance.GetRender<T>();
        }
        
        protected T GetProxy<T>() where T : GameProxy
        {
            return GameProxyManger.Instance.GetProxy<T>();
        }
    }
}
