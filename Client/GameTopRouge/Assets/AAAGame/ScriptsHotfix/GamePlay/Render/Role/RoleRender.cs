using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Fight;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Player;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using GameFramework;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Role
{
    public class RoleRender:GameRender
    {
        private readonly Dictionary<int, RoleEntity> _roleViewDictionary = new Dictionary<int, RoleEntity>();
        
        private FightLoadingProxy _fightLoadingProxy;
        private UnitProxy _unitProxy;
        private PlayerProxy _currentPlayer;
        
        public override void Initialize()
        {
            _unitProxy = GetProxy<UnitProxy>();
            _fightLoadingProxy = GetProxy<FightLoadingProxy>();
            _currentPlayer = GetProxy<PlayerProxy>();
                
            Subscribe(GamePlayEvent.ERenderAllRole,RenderAllRole);
            Subscribe(GamePlayEvent.ERenderMonster,RenderMonster);
            Subscribe(GamePlayEvent.EStopRenderRole,StopRenderRole);
        }

        public override void Clear()
        {
          
        }

        public override void RenderUpdate(fix deltaTime)
        {
            if(_roleViewDictionary.Count == 0) return;

            foreach (var (key,data) in _roleViewDictionary)
            {
                data.LogicUpdate(deltaTime);
            }
            
            base.RenderUpdate(deltaTime);
        }

        private void RenderAllRole(object sender, GameEvent e)
        {
            foreach (var roleUnit in _unitProxy.Heros.Values)
            {
                Log.Info("RenderAllRole {0}",roleUnit.RoleId);
                GF.AssetBridge.LoadEntity(roleUnit.Data.AssetPath,
                    typeof(RoleEntity),"Hero",
                    GamePlayDefine.LoadPriority.Role,
                    LoadRoleFinish,
                    roleUnit);
            }
        }

        private void LoadRoleFinish(AssetInfo info)
        {
         
            RoleUnit roleUnit = info.UserData as RoleUnit;
            if (roleUnit == null)
            {
                Log.Error("roleUnit is null");
                return;
            }
            
            UnityGameFramework.Runtime.Entity roleEntity = info.Asset as UnityGameFramework.Runtime.Entity;
            if (roleEntity == null || roleEntity.Logic == null)
            {
                Log.Error("RoleEntity.Logic is null");
                return;
            }
            //Log.Info("LoadRoleFinish {0}",roleUnit.RoleId);
            _roleViewDictionary.Add(roleUnit.RoleId, roleEntity.Logic as RoleEntity);
            _fightLoadingProxy.SetRoleProgress(_roleViewDictionary.Count/(float)_currentPlayer.Players.Count);
        }

        private void RenderMonster(object sender, GameEvent e)
        {
            RoleUnit roleUnit = e.GetParam1<RoleUnit>();
            
            GF.AssetBridge.LoadEntity(roleUnit.Data.AssetPath,
                typeof(RoleEntity),"Monster",
                GamePlayDefine.LoadPriority.Role,
                LoadRoleFinish,
                roleUnit);
        }

        private void StopRenderRole(object sender, GameEvent e)
        {
            int id = e.GetParam1<int>();

            if (_roleViewDictionary.TryGetValue(id, out RoleEntity roleEntity))
            {
                GF.AssetBridge.HideEntity(roleEntity.Entity);
                _roleViewDictionary.Remove(id);
            }
        }
    }
}