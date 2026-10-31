using AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Skill;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine.TimeLineStrategy
{
    public partial class TimeLineStrategyManager
    {
        private static void UseAoeToPoint(TimelineObj tlo, TimelineNode timelineNode)
        {
            if (timelineNode.TimelineNodeDef is cfg.Skill.UseAoeToPoint useAoeToPoint)
            {
               int id = useAoeToPoint.SkillAoeId;
               SkillProxy skillProxy = GameProxyManger.Instance.GetProxy<SkillProxy>();
               AoeData aoeData = skillProxy.GetAoeData(id,tlo.caster);

               if (aoeData == null)
               {
                   return;
               }

               if (aoeData.tween == null)
               {
                   //默认使用释放者的朝向和位置。如果有其它移动需求，需要写Tween
                   aoeData.fireDegree = tlo.caster.Brian.FaceDegree +  aoeData.fireDegree;
                   aoeData.position = new fix3(tlo.caster.Behaviour.Position.x, tlo.caster.Behaviour.Position.y, tlo.caster.Behaviour.Position.z); ;
               }
               
               GameProxyManger.Instance.GetProxy<UnitProxy>().CreateAoeUnit(aoeData);
            }
        }
    }
}