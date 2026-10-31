using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using cfg.Skill;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine.TimeLineStrategy
{
    public partial class TimeLineStrategyManager
    {
        ///<summary>
        ///设置timeline的焦点角色的ChaControlState
        ///<param name="args">总共3个参数：
        ///[0]bool：可否移动，如果得不到参数，就保持原值。
        ///[1]bool：可否转身，如果得不到参数，就保持原值。
        ///[2]bool：可否释放技能，如果得不到参数，就保持原值。
        ///</param>
        ///</summary>
        private static void SetCasterControlState(TimelineObj tlo, TimelineNode timelineNode)
        {
            if (timelineNode.TimelineNodeDef is cfg.Skill.SetCasterControlState setCasterControlState 
                &&tlo.caster != null
                && tlo.caster is RoleUnit roleUnit)
            {
                roleUnit.TimelineControlState.canMove = setCasterControlState.CanMove;
                roleUnit.TimelineControlState.canRotate = setCasterControlState.CanRotate;
                roleUnit.TimelineControlState.canUseSkill =setCasterControlState.CanUseSkill;
            }
        }
    }
}