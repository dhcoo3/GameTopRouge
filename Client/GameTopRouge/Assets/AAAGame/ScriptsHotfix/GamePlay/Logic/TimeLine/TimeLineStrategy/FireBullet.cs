using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Skill;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using cfg.Skill;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine.TimeLineStrategy
{
    public partial class TimeLineStrategyManager
    {
        ///<summary>
        ///在Caster的某个绑点(Muzzle/Head/Body)上发射一个子弹出来
        ///<param name="args">总共3个参数：
        ///[0]BulletLauncher：子弹发射信息，其中caster和position是需要获得后该写的，degree则需要加上角色的转向
        ///[1]string：角色身上绑点位置，默认Muzzle
        ///</param>
        ///</summary>
        private static void FireBullet(TimelineObj tlo, TimelineNode timelineNode){
            Log.Info("FireBullet");
            if (timelineNode.TimelineNodeDef is cfg.Skill.FireBullet fireBullet)
            {
                if (tlo.caster != null && tlo.caster is RoleUnit roleUnit)
                {
                    //UnitBindManager ubm = tlo.caster.GetComponent<UnitBindManager>();
                    //if (!ubm) return;

                    SkillProxy skillProxy = GameProxyManger.Instance.GetProxy<SkillProxy>();
                    BulletData bData = skillProxy.GetBulletData(fireBullet.BulletDataId);
                    //UnitBindPoint ubp = ubm.GetBindPointByKey(args.Length > 1 ? (string)args[1] : "Muzzle");
                    //if (!ubp) return;

                    bData.caster = tlo.caster;                    
                    bData.fireDegree = tlo.caster.Brian.FaceDegree +  bData.fireDegree;                  
                    bData.firePosition = new fix3(tlo.caster.Behaviour.Position.x, tlo.caster.Behaviour.Position.y, tlo.caster.Behaviour.Position.z);
                    Log.Info("FireBullet {0} {1}",bData.fireDegree,tlo.caster.Behaviour.Position);
                    bData.propWhileCast = roleUnit.Data.Property;
                
                    GameProxyManger.Instance.GetProxy<UnitProxy>().CreateBulletUnit(bData);
                }
            }
        }
    }
}