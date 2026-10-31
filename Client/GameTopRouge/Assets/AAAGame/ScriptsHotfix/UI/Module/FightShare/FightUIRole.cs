using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.UI.Module.FightCommon
{
    public class FightUIRole:IReference
    {
        public RoleUnit RoleUnit;
        public int Id => RoleUnit.RoleId;
        
        public FightUIRoleHud Hud;

        public static FightUIRole Create(RoleUnit roleUnit)
        {
            FightUIRole role = ReferencePool.Acquire<FightUIRole>();
            role.RoleUnit = roleUnit;
            return role;
        }
        
        public void Clear()
        {
        
        }
    }
}