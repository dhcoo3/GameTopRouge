using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using AAAGame.ScriptsHotfix.UI.Module.FightCommon;
using AAAGame.ScriptsHotfix.UI.Module.FightShare;
using GameFramework;
using UnityEngine;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.Bridge
{
    /// <summary>
    /// 战斗层向UI层通信
    /// </summary>
    public class GamePlayToUIBridge:Singleton<GamePlayToUIBridge>
    {
        private readonly AAAGameEventHelper _eventHelper = ReferencePool.Acquire<AAAGameEventHelper>();
      
        public void CreateRole(RoleUnit roleUnit)
        {
            _eventHelper.Fire(FightShareConst.Event.ECreateRole,roleUnit);
        }
        
        public void UpdatePos(int id,Vector3 pos)
        {
            _eventHelper.Fire(FightShareConst.Event.EUpdatePos,id,pos);
        }
        
        public void UpdateHp(RoleUnit roleUnit)
        {
            _eventHelper.Fire(FightShareConst.Event.EUpdateHp,roleUnit);
        }
        
        public void RoleDie(RoleUnit roleUnit)
        {
            _eventHelper.Fire(FightShareConst.Event.ERoleDie,roleUnit);
        }
        
        public void PopUpNumberOnCharacter(RoleUnit roleUnit,int dmageVal,bool isHeal)
        {
            _eventHelper.Fire(FightShareConst.Event.EPopUpNumber,roleUnit.Behaviour.Position,dmageVal,isHeal);
        }
    }
}