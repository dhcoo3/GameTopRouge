using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Fight;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Frame;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Player;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using Fantasy;

namespace AAAGame.ScriptsHotfix.GamePlay
{
    public class GamePlayFacade:Singleton<GamePlayFacade>
    {
        private FixRandom _fixRandom;
        private float _logicTime;
        public int CallRandomTimes { get; private set; }

        public bool IsRoomMode;
        /// <summary>当前房间对局的服务端固定初始化描述。</summary>
        private FrameMatchBootstrapConfig _roomBootstrap;

        /// <summary>玩法系统是否已注册更新与代理。</summary>
        private bool _isRunning;
        
        /// <summary>
        /// 玩法总入口
        /// </summary>
        /// <param name="isRoom">是否为房间模式</param>
        /// <param name="players">参与的玩家数据，包含装备、属性等</param>
        /// <param name="characterId">要参与的关卡ID</param>
        /// <param name="dungeonType">玩法类型</param>
        public void Run(bool isRoom,List<PlayerData> players,int characterId,cfg.Fight.DungeonType dungeonType,
            FrameMatchBootstrapConfig roomBootstrap = null)
        {
            if (_isRunning)
            {
                End();
            }

            IsRoomMode = isRoom;
            _roomBootstrap = roomBootstrap;
            _time = 0;
            
            SetRandomSeed();
            
            GF.UpdateActions.Add(Update);
            _isRunning = true;
            
            GameProxyManger.Instance.Register(dungeonType);
            GameRenderManager.Instance.Register();
            
            GameProxyManger.Instance.Initialize();

            if (isRoom && roomBootstrap != null)
            {
                FrameProxy frameProxy = GameProxyManger.Instance.GetProxy<FrameProxy>();
                frameProxy.BeginFrameSynchronization(roomBootstrap.MatchId, roomBootstrap.CurrentFrameId, true);
            }
            GameRenderManager.Instance.Initialize();

            FightProxy fightProxy = GameProxyManger.Instance.GetProxy<FightProxy>();
            fightProxy.EnterFight(players, characterId, dungeonType);
        }

        public void End()
        {
            if (!_isRunning)
            {
                return;
            }

            GameProxyManger.Instance.Clear();
            GameRenderManager.Instance.Clear();
            
            GF.UpdateActions.Remove(Update);
            _isRunning = false;
        }

        /// <summary>单机模式累计的固定逻辑时间，单位为秒。</summary>
        private float _time;

        /// <summary>
        /// 推进单机固定逻辑或由 FrameProxy 驱动房间权威逻辑，并始终刷新表现层。
        /// </summary>
        /// <param name="deltaTime">本次渲染更新经过的秒数。</param>
        public void Update(float deltaTime)
        {
            if (IsRoomMode)
            {
                FrameProxy frameProxy = GameProxyManger.Instance.GetProxy<FrameProxy>();
                if (frameProxy != null)
                {
                    frameProxy.Advance(deltaTime, ExecuteRoomLogicFrame);
                }

                GameRenderManager.Instance.RenderUpdate(deltaTime);
                return;
            }

            _time += deltaTime;

            while (_time >= FrameCommandDefine.LogicFrameRate)
            {
                GameProxyManger.Instance.LogicUpdate(FrameCommandDefine.LogicFrameRate);
                _time -= FrameCommandDefine.LogicFrameRate;
            }

            // 每次 Update 都更新渲染
            GameRenderManager.Instance.RenderUpdate(deltaTime);
        }
        
        /// <summary>
        /// 执行由 FrameProxy 确认可以消费的一次房间确定性逻辑步。
        /// </summary>
        /// <param name="deltaTime">固定逻辑步长，单位为秒。</param>
        private void ExecuteRoomLogicFrame(fix deltaTime)
        {
            GameProxyManger.Instance.LogicUpdate(deltaTime);
        }

        /// <summary>
        /// 使用房间固定种子或单机随机种子初始化玩法随机源。
        /// </summary>
        public void SetRandomSeed()
        {
            int seed = 0;
            
            if (IsRoomMode)
            {
                seed = _roomBootstrap?.RandomSeed ?? 0;
            }
            else
            {
                seed = UnityEngine.Random.Range(10000, int.MaxValue);
            }
                
            if (_fixRandom == null)
            {
                _fixRandom = new FixRandom(seed);
            }
            else
            {
                _fixRandom.Reinitialise(seed);
            }
        }

        /// <summary>当前玩法唯一允许用于确定性规则的随机源。</summary>
        public FixRandom Random
        {
            get
            {
                CallRandomTimes++;
                //Debug.Log(" =====================frameIndex: " +  _frameIndex + " CallRandomTimes: " + CallRandomTimes);
                return _fixRandom;
            }
        }

        /// <summary>
        /// 网络层收到服务端对局描述后调用。相同对局仅请求缺失帧；新对局重新创建确定性世界。
        /// </summary>
        public void ApplyServerMatchBootstrap(G2C_FrameMatchBootstrap message)
        {
            FrameMatchBootstrapConfig bootstrap = FrameMatchBootstrapConfig.Create(message);
            if (IsRoomMode && _roomBootstrap != null && _roomBootstrap.MatchId == bootstrap.MatchId)
            {
                _roomBootstrap = bootstrap;
                FrameProxy currentFrameProxy = GameProxyManger.Instance.GetProxy<FrameProxy>();
                currentFrameProxy?.BeginFrameSynchronization(bootstrap.MatchId, bootstrap.CurrentFrameId, false);
                return;
            }

            End();
            Run(true, bootstrap.CreatePlayers(), bootstrap.DungeonId,
                (cfg.Fight.DungeonType)bootstrap.DungeonType, bootstrap);
        }

    }

    /// <summary>脱离协议对象保存的对局初始化数据，供异步加载完成后使用。</summary>
    public sealed class FrameMatchBootstrapConfig
    {
        /// <summary>服务端分配的固定对局标识。</summary>
        public int MatchId { get; private set; }

        /// <summary>本局确定性随机种子。</summary>
        public int RandomSeed { get; private set; }

        /// <summary>本局副本配置标识。</summary>
        public int DungeonId { get; private set; }

        /// <summary>本局副本类型枚举值。</summary>
        public int DungeonType { get; private set; }

        /// <summary>收到描述时服务端已封存的最新帧号。</summary>
        public int CurrentFrameId { get; private set; }

        /// <summary>按服务端稳定顺序保存的玩家初始化配置。</summary>
        private readonly List<FrameMatchPlayerConfig> _players = new List<FrameMatchPlayerConfig>();

        /// <summary>
        /// 从网络协议复制一份不受消息对象池影响的对局描述。
        /// </summary>
        /// <param name="message">服务端下发的对局初始化协议。</param>
        /// <returns>供玩法层长期持有的不可变对局配置。</returns>
        public static FrameMatchBootstrapConfig Create(G2C_FrameMatchBootstrap message)
        {
            var config = new FrameMatchBootstrapConfig
            {
                MatchId = message.MatchId,
                RandomSeed = message.RandomSeed,
                DungeonId = message.DungeonId,
                DungeonType = message.DungeonType,
                CurrentFrameId = message.CurrentFrameId,
            };

            foreach (FrameMatchPlayer player in message.Players)
            {
                config._players.Add(new FrameMatchPlayerConfig(player.PlayerId, player.Position, player.HeroIds));
            }

            config._players.Sort((left, right) => left.PlayerId.CompareTo(right.PlayerId));
            return config;
        }

        /// <summary>
        /// 将服务端固定玩家描述转换为玩法初始化使用的玩家数据。
        /// </summary>
        /// <returns>按稳定玩家标识排序的玩法玩家列表。</returns>
        public List<PlayerData> CreatePlayers()
        {
            var players = new List<PlayerData>(_players.Count);
            foreach (FrameMatchPlayerConfig config in _players)
            {
                PlayerData player = PlayerData.Create(config.PlayerId, $"Player{config.PlayerId}");
                player.Pos = config.Position;
                player.HeroIds = new List<int>(config.HeroIds);
                players.Add(player);
            }

            return players;
        }
    }

    /// <summary>单个玩家脱离协议对象保存的确定性初始化数据。</summary>
    internal sealed class FrameMatchPlayerConfig
    {
        /// <summary>认证后的玩家标识。</summary>
        public int PlayerId { get; }

        /// <summary>服务器分配的出生位置标识。</summary>
        public int Position { get; }

        /// <summary>本局携带的英雄配置标识。</summary>
        public IReadOnlyList<int> HeroIds { get; }

        /// <summary>
        /// 创建一名玩家的固定初始化描述。
        /// </summary>
        /// <param name="playerId">认证后的玩家标识。</param>
        /// <param name="position">服务器分配的出生位置标识。</param>
        /// <param name="heroIds">本局携带的英雄配置标识。</param>
        public FrameMatchPlayerConfig(int playerId, int position, IReadOnlyList<int> heroIds)
        {
            PlayerId = playerId;
            Position = position;
            HeroIds = new List<int>(heroIds);
        }
    }
}
