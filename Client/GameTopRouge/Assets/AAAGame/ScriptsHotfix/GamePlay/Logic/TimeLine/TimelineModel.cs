using System.Collections.Generic;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine
{
    public class TimelineModel
    {
        public readonly int id;

        ///<summary>
        ///Timeline运行多久之后发生，单位：秒
        ///</summary>
        public readonly TimelineNode[] nodes;

        ///<summary>
        ///Timeline一共多长时间（到时间了就丢掉了），单位秒
        ///</summary>
        public readonly fix duration;

        ///<summary>
        ///如果有caster，并且caster处于蓄力状态，则可能会经历跳转点
        ///</summary>
        public readonly TimeLineGoTo chargeGoBack;

        public TimelineModel(int id, TimelineNode[] nodes, fix duration, TimeLineGoTo chargeGoBack){
            this.id = id;
            this.nodes = nodes;
            this.duration = duration;
            this.chargeGoBack = chargeGoBack;
        }
    }
}