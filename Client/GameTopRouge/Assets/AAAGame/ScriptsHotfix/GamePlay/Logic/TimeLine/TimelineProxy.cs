using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine
{
    public class TimelineProxy:GameProxy
    {
        private List<TimelineObj> timelines = new List<TimelineObj>();
        
        public override void Initialize()
        {
            
        }

        public override void LogicUpdate(fix deltaTime)
        {
            if (this.timelines.Count <= 0) return;

            int idx = 0;
            while (idx < this.timelines.Count)
            {
                TimelineObj timelineObj = timelines[idx];
                fix wasTimeElapsed =timelineObj.timeElapsed;
                timelineObj.timeElapsed += deltaTime * timelineObj.timeScale;

                //判断有没有返回点
                if (
                    timelineObj.model.chargeGoBack.atDuration < timelineObj.timeElapsed &&
                    timelineObj.model.chargeGoBack.atDuration >= wasTimeElapsed
                ){
                    if (timelines[idx].caster is RoleUnit roleUnit){
                        if (roleUnit.RoleState.charging == true){
                            timelines[idx].timeElapsed = timelines[idx].model.chargeGoBack.gotoDuration;
                            continue;
                        }
                    }
                }
                
                //执行时间点内的事情
                for (int i = 0; i < timelineObj.model.nodes.Length; i++)
                {
                    fix nodeTimeElapsed = timelineObj.model.nodes[i].TimeElapsed;
                    
                    bool time1 = nodeTimeElapsed < timelineObj.timeElapsed;
                    bool time2 = nodeTimeElapsed >= wasTimeElapsed;
                    
                    if (time1 && time2)
                    {
                        timelineObj.model.nodes[i].DoEvent(
                            timelineObj, 
                            timelineObj.model.nodes[i]
                        );
                    }
                }

                //判断timeline是否终结
                if (timelineObj.model.duration <= timelineObj.timeElapsed){
                    timelines.RemoveAt(idx);
                }else{
                    idx++;
                }
            }
        }
    
        ///<summary>
        ///添加一个timeline
        ///<param name="timelineModel">要添加的timeline</param>
        ///</summary>
        public void AddTimeline(TimelineObj timeline){
            this.timelines.Add(timeline);
        }

        public bool CasterHasTimeline(IUnit caster){
            for (var i = 0; i < timelines.Count; i++)
            {
                if (timelines[i].caster == caster)
                {
                    return true;
                }
            }
            return false;
        }
    }
}