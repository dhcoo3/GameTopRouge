using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.AI;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Damage;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Dungeon.Main;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Effect;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Fight;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Frame;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Map;
using AAAGame.ScriptsHotfix.GamePlay.Logic.MonsterSpawner;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Player;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Skill;
using AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using cfg.Fight;
using GameFramework;
using Unity.Burst;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager
{
    public class GameProxyManger:Singleton<GameProxyManger>
    {
        private GameProxy[] _proxyList;
        
        /// <summary>
        /// 通用
        /// </summary>
        public void Register(DungeonType dungeonType)
        {
            //副本放在更新顺序最后
            GameProxy dungeonProxy = GetDungeonProxy(dungeonType);
            
            _proxyList = new[]
            {  
                ReferencePool.Acquire<FrameProxy>(),
                ReferencePool.Acquire<InputProxy>(),
                ReferencePool.Acquire<FightProxy>(),
                ReferencePool.Acquire<FightLoadingProxy>(),
                ReferencePool.Acquire<UnitProxy>(),
                ReferencePool.Acquire<PlayerProxy>(),
                ReferencePool.Acquire<MapProxy>(),
                ReferencePool.Acquire<TimelineProxy>(),
                ReferencePool.Acquire<MonsterSpawnerProxy>(),
                ReferencePool.Acquire<DamageProxy>(),
                ReferencePool.Acquire<SkillProxy>(),
                ReferencePool.Acquire<EffectProxy>(),
                ReferencePool.Acquire<AIProxy>(),
                dungeonProxy
            };
        }

        /// <summary>
        /// 各类副本
        /// </summary>
        /// <param name="dungeonType"></param>
        public GameProxy GetDungeonProxy(DungeonType dungeonType)
        {
            switch (dungeonType)
            {
                case DungeonType.Main:
                    return ReferencePool.Acquire<MainDungeonProxy>();
            }

            return null;
        }

        /// <summary>初始化当前已注册的玩法代理。</summary>
        public void Initialize()
        {
            GameProxy[] proxies = _proxyList;
            if (proxies == null)
            {
                return;
            }

            for (int i = 0; i < proxies.Length; i++)
            {
                proxies[i]?.Initialize();
            }
        }

        /// <summary>查找当前已注册的玩法代理；系统结束或代理不存在时返回 null。</summary>
        /// <typeparam name="T">需要查找的代理类型。</typeparam>
        /// <returns>匹配的代理；当前未注册时返回 null。</returns>
        public T GetProxy<T>() where T : GameProxy
        {
            GameProxy[] proxies = _proxyList;
            if (proxies == null)
            {
                return null;
            }

            foreach (var data in proxies)
            {
                if (data is T)
                {
                    return (T)data;
                }
            }

            return null;
        }

        /// <summary>推进所有当前注册且有效的玩法代理。</summary>
        /// <param name="deltaTime">本次确定性逻辑更新的固定时间步长。</param>
        public void LogicUpdate(fix deltaTime)
        {
            GameProxy[] proxies = _proxyList;
            if (proxies == null)
            {
                return;
            }

            for (int i = 0; i < proxies.Length; i++)
            {
                proxies[i]?.LogicUpdate(deltaTime);
            }
        }

        /// <summary>释放全部玩法代理；重复调用不会访问已清理列表。</summary>
        public void Clear()
        {
            GameProxy[] proxies = _proxyList;
            if (proxies == null)
            {
                return;
            }

            _proxyList = null;
            for (int i = 0; i < proxies.Length; i++)
            {
                if (proxies[i] != null)
                {
                    ReferencePool.Release(proxies[i]);
                }
            }
        }
    }
}
