using AAAGame.ScriptsHotfix.GamePlay.Logic.Buff;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine;
using AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine.TimeLineStrategy;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Skill
{
    public class SkillModel:IReference
    {
        ///<summary>
        ///技能的id
        ///</summary>
        public int id;

        ///<summary>
        ///技能使用的条件，这个游戏中只有资源需求，比如hp、ammo之类的
        ///</summary>
        public ChaResource condition;

        ///<summary>
        ///技能的消耗，成功之后会扣除这些资源
        ///</summary>
        public ChaResource cost;

        ///<summary>
        ///技能的效果，必然是一个timeline
        ///</summary>
        public TimelineModel effect;

        ///<summary>
        ///学会技能的时候，同时获得的buff
        ///</summary>
        public AddBuffInfo[] buff;

        public SkillModel(int id, ChaResource cost, ChaResource condition, TimelineModel timelineModel, AddBuffInfo[] buff){
            this.id = id;
            this.cost = cost;
            this.condition = condition;
            this.effect = timelineModel; //SceneVariants.desingerTables.timeline.data[effectTimeline];
            this.buff = buff;
        }

        public void Clear()
        {
            
        }
    }
}