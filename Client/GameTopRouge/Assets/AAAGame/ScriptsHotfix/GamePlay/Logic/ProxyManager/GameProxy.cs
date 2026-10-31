using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using GameFramework;
using GameFramework.Event;
using Unity.Burst;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager
{
    public abstract class GameProxy:IReference
    {
        public abstract void Initialize();

        private readonly AAAGameEventHelper _aaaGameEventHelper = ReferencePool.Acquire<AAAGameEventHelper>();
      
        public virtual void Clear()
        {
            RemoveAllSubscribe();
        }

        [BurstCompile]
        public virtual void LogicUpdate(fix deltaTime)
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
        
        protected void FireNow(Int16 id, params object[] args)
        {
            _aaaGameEventHelper.FireNow(id, args);
        }

        protected void Unsubscribe(Int16 id, EventHandler<GameEvent> handler)
        {
            _aaaGameEventHelper.Unsubscribe(id,handler);
        }

        protected void RemoveAllSubscribe()
        {
            _aaaGameEventHelper.RemoveAllSubscribe();
        }

        protected T GetProxy<T>() where T : GameProxy
        {
            return GameProxyManger.Instance.GetProxy<T>();
        }
    }
}