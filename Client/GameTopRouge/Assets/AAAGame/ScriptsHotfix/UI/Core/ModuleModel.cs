using System;
using GameFramework;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.UI.Core
{
    public class ModuleModel<T> where T : class, IModel,new()
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                    return _instance;
                }
            }
        }

        protected ModuleModel()
        {
            // 防止外部实例化
            if (_instance != null)
            {
                throw new System.Exception($"Only one instance of {typeof(T)} is allowed.");
            }
        }
        
        private readonly AAAGameEventHelper _aaaGameEventHelper = ReferencePool.Acquire<AAAGameEventHelper>();
        
        public virtual void Clear()
        {
            RemoveAllSubscribe();
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
    }
}