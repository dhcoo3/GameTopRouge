using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Component;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using UnityEngine.UIElements;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.AI.AIStrategy
{
    public partial class AiStrategyManager
    {
        /// <summary>
        /// 范围内是否有敌人
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="unitAI"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private static bool CheckEnemyInRange(IUnit npc, UnitAI unitAI,cfg.AI.AICondition param)
        {
            if (param is cfg.AI.CheckEnemyInRange checkEnemyInRange 
                && npc is RoleUnit { Behaviour: RoleBehaviour npcBehaviour } npcUnit)
            {
                UnitProxy unitProxy = GameProxyManger.Instance.GetProxy<UnitProxy>();

                foreach (var role in unitProxy.RoleUnits.Values)
                {
                    if (role != npcUnit 
                        && role is RoleUnit checkUnit
                        && checkUnit.Data.Side != npcUnit.Data.Side)
                    {
                        if (fixMath.distance(npcUnit.Behaviour.Position, checkUnit.Behaviour.Position) <= npcUnit.Data.Property.SearchRange)
                        {
                            npcBehaviour.LookAtTarget = checkUnit;
                            return true;
                        }
                    }
                }
                
                npcBehaviour.LookAtTarget = null;
            }
          
            return false;
        }
    }
}