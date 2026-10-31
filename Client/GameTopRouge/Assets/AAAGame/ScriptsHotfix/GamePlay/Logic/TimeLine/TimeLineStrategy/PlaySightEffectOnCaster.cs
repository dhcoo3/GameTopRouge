using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using cfg.Skill;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine.TimeLineStrategy
{
    public partial class TimeLineStrategyManager
    {
        ///<summary>
        ///在timeline焦点角色身上播放一个视觉特效
        ///<param name="args">总共4个参数：
        ///[0]string：要播放特效的绑点
        ///[1]string：特效的文件名，位于Prafabs/下
        ///[2]string：特效的key，用于删除的
        ///[3]bool：是否循环播放特效（循环就要手动删除）
        ///</param>
        ///</summary>
        private static void PlaySightEffectOnCaster(TimelineObj tlo, TimelineNode timelineNode)
        {
            Log.Info("PlaySightEffectOnCaster");
            if (timelineNode.TimelineNodeDef is cfg.Skill.PlaySightEffectOnCaster playSightEffectOnCaster && 
                tlo.caster != null &&
                tlo.caster is RoleUnit roleUnit)
            {
                string bindPointKey = playSightEffectOnCaster.BindPointKey;
                string effectName = playSightEffectOnCaster.EffectName;
                string effectKey = playSightEffectOnCaster.BindPointKey != "" ? playSightEffectOnCaster.BindPointKey :string.Empty;
                bool loop = playSightEffectOnCaster.Loop;
                
                if (roleUnit.Behaviour is RoleBehaviour roleBehaviour)
                {
                    roleBehaviour.PlaySightEffect(bindPointKey, effectName, effectKey, loop);
                }
            }
        }
    }
}