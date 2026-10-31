using AAAGame.ScriptsHotfix.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine.TimeLineStrategy;
using cfg.Skill;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine
{
    ///<summary>
    ///Timeline每一个节点上要发生的事情
    ///</summary>
    public class TimelineNode
    {
        ///<summary>
        ///Timeline运行多久之后发生，单位：秒
        ///</summary>
        public readonly fix TimeElapsed;

        ///<summary>
        ///要执行的脚本函数
        ///</summary>
        public TimeLineStrategyManager.TimelineEvent DoEvent;
        
        public TimeLineDefParam TimelineNodeDef;

        public TimelineNode(TimeLineDefParam param){
            this.TimeElapsed = MathUtils.Convert(param.Time);

            this.DoEvent = param switch
            {
                cfg.Skill.SetCasterControlState => TimeLineStrategyManager.functions["SetCasterControlState"],
                cfg.Skill.CasterPlayAnim => TimeLineStrategyManager.functions["CasterPlayAnim"],
                cfg.Skill.PlaySightEffectOnCaster => TimeLineStrategyManager.functions["PlaySightEffectOnCaster"],
                cfg.Skill.FireBullet => TimeLineStrategyManager.functions["FireBullet"],
                cfg.Skill.UseAoeToPoint => TimeLineStrategyManager.functions["UseAoeToPoint"],
                _ => null
            };

            this.TimelineNodeDef = param;
        }
    }
}