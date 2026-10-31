using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using GameFramework;
using GameFramework.Event;

namespace UnityEngine.UI
{
    public class AAAGameEventHelper:IReference
    {
        public class EventData:IReference
        {
            public int Id;
            public EventHandler<GameEventArgs> Listener;
            public List<EventHandler<GameEvent>> Handlers = new List<EventHandler<GameEvent>>();
            public void Clear()
            {
                Id = 0;
                Listener = null;
                Handlers.Clear();
            }
        }
        
        private readonly Dictionary<int, EventData> _events =new Dictionary<int,EventData>();

        public bool Init = false;
        
        /// <summary>
        /// 注册事件，让事件跟面板绑定，用于关闭面板时释放掉所有事件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="handler"></param>
        public void Subscribe(int id, EventHandler<GameEvent> handler)
        {
            EventData eventData;
            
            if (!_events.TryGetValue(id, out eventData))
            {
                eventData = ReferencePool.Acquire<EventData>();
                _events.Add(id, eventData);
            }
            
            eventData.Handlers.Add(handler);
            
            if (eventData.Listener == null)
            {
                eventData.Listener = (sender, args) =>
                {
                    GameEvent gameEvent = (GameEvent)args;

                    if (_events.TryGetValue(gameEvent.Id, out EventData _eventData))
                    {
                        for (int i = 0; i < _eventData.Handlers.Count; i++)
                        {
                            _eventData.Handlers[i].Invoke(sender,gameEvent);
                        }
                    }
                };
            }

            if (!GF.Event.Check((short)id, eventData.Listener))
            {
                GF.Event.Subscribe((short)id, eventData.Listener);
            }
        }

        /// <summary>
        /// 派发事件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="args"></param>
        public void Fire(short id, params object[] args)
        {
            GF.Event.Fire(this, GameEvent.Create(id, args));
        }
        
        /// <summary>
        /// 派发事件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="args"></param>
        public void FireNow(short id, params object[] args)
        {
            GF.Event.FireNow(this, GameEvent.Create(id, args));
        }

        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="handler"></param>
        public void Unsubscribe(int id, EventHandler<GameEvent> handler)
        {
            if (_events.TryGetValue(id, out EventData _eventData))
            {
                for (var i = _eventData.Handlers.Count - 1; i >= 0; i--)
                {
                    if (_eventData.Handlers[i] != handler) continue;
                    _eventData.Handlers.RemoveAt(i);
                    break;
                }

                if (_eventData.Handlers.Count != 0)
                {
                    return;
                }
                
                if (GF.Event.Check((short)id, _eventData.Listener))
                {
                    return;
                }
                
                GF.Event.Subscribe((short)id, _eventData.Listener);
                
                ReferencePool.Release(_eventData);
                _events.Remove(id);
            }
        }

        /// <summary>
        /// 移除所有绑定UI的事件
        /// </summary>
        public void RemoveAllSubscribe()
        {
            foreach (var (gamePlayEvent, eEventData) in _events)
            {
                GF.Event.Unsubscribe((short)gamePlayEvent, eEventData.Listener);
                ReferencePool.Release(eEventData);
            }

            _events.Clear();
        }

        public void Clear()
        {
            RemoveAllSubscribe();
        }
    }
}