using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Dungeon;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Dungeon.Main;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Player;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Fight
{
    public class FightProxy:GameProxy
    {
        private FightLoadingProxy _fightLoadingProxy;
        private UnitProxy _unitProxy;
        private IDungeon _currentDungeon;
        private PlayerProxy _currentPlayer;
        private bool StartLogic;
        
        public override void Initialize()
        {
            _unitProxy = GetProxy<UnitProxy>();
            _fightLoadingProxy = GetProxy<FightLoadingProxy>();
            _currentPlayer = GetProxy<PlayerProxy>();
        }
        
        public void EnterFight(List<PlayerData> players,int characterId,cfg.Fight.DungeonType dungeonType)
        {
            _currentDungeon = dungeonType switch
            {
                cfg.Fight.DungeonType.Main => GetProxy<MainDungeonProxy>(),
                _ => null
            };

            if (_currentDungeon == null)
            {
                Log.Error("不存在副本类型");
                return;
            }

            _currentPlayer.IniPlayers(players);
            _currentDungeon.InitDungeon(characterId);
            _unitProxy.LoadingRole();
        }

        public void EndFight()
        {
            
        }

        public void ExitFight()
        {
            
        }
    }
}