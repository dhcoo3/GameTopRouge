using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Player;
using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Role
{
    public class RoleHero:RoleData
    {
        public static RoleHero Create(PlayerData playerData,int id,cfg.Entity.Hero entityData)
        {
            RoleHero roleData = ReferencePool.Acquire<RoleHero>();
            roleData._roleType = GamePlayDefine.RoleType.Hero;
            roleData.RoleId = id;
            roleData.Name = entityData.Name;
            roleData.AssetPath = entityData.Asset;
            roleData.AnimId = entityData.AnimGroup;
            // 世界 XY 偏移；角色动画 Pivot 与脚底不重合时，在这里校准碰撞圆心。
            roleData.CollisionOffset = new fix3(0f, -0.4f, 0f);
            roleData.BaseProp = new ChaProperty(2, 100, 100, 100, 3, 0.2f, 0.25f, BulletDefine.MoveType.ground);
            roleData.Prop += roleData.BaseProp;
            roleData.Resource = new ChaResource(100, 100, 100);
            roleData.OperateId = playerData.Id;
            return roleData;
        }
    }
}