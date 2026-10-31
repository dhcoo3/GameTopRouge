using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Component;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using cfg.AI;
using cfg.Skill;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.AI.AIStrategy
{
    public partial class AiStrategyManager
    {
        /// <summary>
        /// 自己是否使用任意一个技能
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="unitAI"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private static bool CheckCanUseSkill(IUnit npc, UnitAI unitAI,cfg.AI.AICondition param)
        {
            if (param is cfg.AI.CheckCanUseSkill checkCanUseSkill 
                && npc is RoleUnit roleUnit)
            {
                return false;
            }
            
            return false;
        }
    }
}