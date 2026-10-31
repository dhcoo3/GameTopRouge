using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Role
{
    public class RoleMonster:RoleData
    {
        public static RoleMonster Create(int id,cfg.Entity.Monster cfg)
        {
            RoleMonster roleData = ReferencePool.Acquire<RoleMonster>();
            roleData._roleType = GamePlayDefine.RoleType.Monster;
            roleData.Side = 1;
            roleData.RoleId = id;
            roleData.Name = cfg.Name;
            roleData.AssetPath = cfg.Asset;
            
            roleData.BaseProp = new ChaProperty(2, 
                100, 
                100,
                100,
                3, 
                0.25f, 
                0.25f,
                BulletDefine.MoveType.ground,
                cfg.SearchRange);
            
            roleData.Prop += roleData.BaseProp;
            
            roleData.Resource = new ChaResource(100, 100, 100);
            roleData.AIId = cfg.AI;
            roleData.AnimId = cfg.AnimGroup;
            return roleData;
        }
    }
}