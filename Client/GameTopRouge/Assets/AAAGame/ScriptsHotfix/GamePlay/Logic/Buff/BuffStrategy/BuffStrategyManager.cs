using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Damage;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Skill;
using AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Buff.BuffStrategy
{
    public class BuffStrategyManager
    {
        public delegate void BuffOnOccur(BuffObj buff, int modifyStack);
        
        public delegate void BuffOnRemoved(BuffObj buff);
        
        public delegate void BuffOnTick(BuffObj buff);
        
        public delegate void BuffOnHit(BuffObj buff, ref DamageInfo damageInfo, IUnit target);
        
        public delegate void BuffOnBeHurt(BuffObj buff, ref DamageInfo damageInfo, IUnit attacker);
        
        public delegate void BuffOnKill(BuffObj buff, DamageInfo damageInfo, IUnit target);
        
        public delegate void BuffOnBeKilled(BuffObj buff, DamageInfo damageInfo, IUnit attacker);
        
        public delegate TimelineObj BuffOnCast(BuffObj buff, SkillObj skill, TimelineObj timeline);
        
        public static Dictionary<string, BuffOnOccur> onOccurFunc = new Dictionary<string, BuffOnOccur>(){
            
        };
        public static Dictionary<string, BuffOnRemoved> onRemovedFunc = new Dictionary<string, BuffOnRemoved>(){
           
        };
        public static Dictionary<string, BuffOnTick> onTickFunc = new Dictionary<string, BuffOnTick>(){
          
        };
        public static Dictionary<string, BuffOnCast> onCastFunc = new Dictionary<string, BuffOnCast>(){
            {"ReloadAmmo", ReloadAmmo},
        };
        public static Dictionary<string, BuffOnHit> onHitFunc = new Dictionary<string, BuffOnHit>(){
            
        };
        public static Dictionary<string, BuffOnBeHurt> beHurtFunc = new Dictionary<string, BuffOnBeHurt>(){
          
        };
        public static Dictionary<string, BuffOnKill> onKillFunc = new Dictionary<string, BuffOnKill>(){
            
        };
        public static Dictionary<string, BuffOnBeKilled> beKilledFunc = new Dictionary<string, BuffOnBeKilled>(){
          
        };
        
        public static Dictionary<string, BuffModel> data = new Dictionary<string, BuffModel>(){
           
        };
        
        ///<summary>
        ///onCast
        ///如果子弹不够放技能，就会填装子弹
        ///no params
        ///</summary>
        private static TimelineObj ReloadAmmo(BuffObj buff, SkillObj skill, TimelineObj timeline){
            if (buff.carrier is RoleUnit roleUnit)
            {
                if (roleUnit.Data.Resource.Enough(skill.Model.cost))
                {
                    return timeline;
                }
            }
            
            Debug.LogWarning("弹药不足");
            
            /*ChaState cs = buff.carrier.GetComponent<ChaState>();
            return (cs.resource.Enough(skill.model.cost) == true) ? timeline : 
                new TimelineObj(DesingerTables.Timeline.data["skill_reload"], buff.carrier, new object[0]);*/
            return null;
        }
    }
}