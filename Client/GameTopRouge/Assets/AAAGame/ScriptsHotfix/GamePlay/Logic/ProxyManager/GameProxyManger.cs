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

        public void Initialize()
        {
            for(int i = 0;i< _proxyList.Length;i++)
            {
                _proxyList[i].Initialize();
            }
        }

        public T GetProxy<T>() where T : GameProxy
        {
            foreach (var data in _proxyList)
            {
                if (data is T)
                {
                    return (T)data;
                }
            }

            return null;
        }
     
        public void LogicUpdate(fix deltaTime)
        {
            for(int i = 0;i< _proxyList.Length;i++)
            {
                _proxyList[i].LogicUpdate(deltaTime);
            }
        }

        public void Clear()
        {
            for(int i = 0;i< _proxyList.Length;i++)
            {
                ReferencePool.Release(_proxyList[i]);
            }
            
            _proxyList = null;
        }
    }
}