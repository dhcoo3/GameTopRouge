using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine.TimeLineStrategy
{
    public partial class TimeLineStrategyManager
    {
        ///<summary>
        ///timelien的焦点角色播放某个动作，是否是跳转到那个动作一直播放还是会回到站立，这取决于animator里面做的，我也无能为力
        ///<param name="args">总共3个参数：
        ///[0]string：是要播放的动画
        ///[1]bool：是否要取得动画的方向，如果不要就直接用预设的值了
        ///[2]bool：是否启用当前正在进行的面向和移动角度，如果false或者缺省了，就代表启用timelineObj中储存的（开始时的）
        ///</param>
        ///</summary>
        private static void CasterPlayAnim(TimelineObj tlo, TimelineNode timelineNode)
        {
            if (timelineNode.TimelineNodeDef is cfg.Skill.CasterPlayAnim casterPlayAnim && 
                tlo.caster != null &&
                tlo.caster is RoleUnit roleUnit)
            {
                bool useCurrentDeg = casterPlayAnim.UseCurrentDeg;
               
                fix faceDeg = useCurrentDeg ? roleUnit.Brian.FaceDegree : (fix)tlo.GetValue("faceDegree");
             
                cfg.Anim.Direction direction = Utils.GetEightDirection(faceDeg);
                
                if (roleUnit.Behaviour is RoleBehaviour roleBehaviour)
                {
                    roleBehaviour.AddAnim(direction,casterPlayAnim.AnimType,roleUnit.Data.ActionSpeed); 
                }
            }
        }
    }
}