using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet.BulletStrategy;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine.TimeLineStrategy
{
    public partial class TimeLineStrategyManager
    {
        public delegate void TimelineEvent(TimelineObj timeline,TimelineNode timelineNode);
        
        public static Dictionary<string, TimelineEvent> functions = new Dictionary<string, TimelineEvent>()
        {
            {"FireBullet", FireBullet},
            {"SetCasterControlState", SetCasterControlState},
            {"CasterPlayAnim", CasterPlayAnim},
            {"PlaySightEffectOnCaster", PlaySightEffectOnCaster},
            {"UseAoeToPoint",UseAoeToPoint}
        };
    }
}