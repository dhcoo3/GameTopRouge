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
        /// 是否处于指定坐标
        /// </summary>
        /// <param name="npc">当前运行AI的对象</param>
        /// <param name="param">需要比对的坐标</param>
        /// <returns>是否满足条件</returns>
        private static bool CheckPosition(IUnit npc, UnitAI unitAI,cfg.AI.AICondition param)
        {
            if (param is CheckPosition checkPosition && npc is RoleUnit roleUnit)
            {
                switch (checkPosition.OperatorType)
                {
                    case Operator.GT:
                        break;
                    case Operator.LT:
                        break;
                    case Operator.GE:
                        break;
                    case Operator.LE:
                        break;
                    case Operator.NE:
                        if (!Vector3Approximately(roleUnit.Behaviour.Position.x,
                                roleUnit.Behaviour.Position.y,
                                roleUnit.Behaviour.Position.z,
                                checkPosition.Target.X,
                                checkPosition.Target.Y,
                                checkPosition.Target.Z,0.01f))
                        {
                            return true;
                        }
                        break;
                    case Operator.EQ:
                        if (Vector3Approximately(roleUnit.Behaviour.Position.x,
                                roleUnit.Behaviour.Position.y,
                                roleUnit.Behaviour.Position.z,
                                checkPosition.Target.X,
                                checkPosition.Target.Y,
                                checkPosition.Target.Z,0.01f))
                        {
                            return true;
                        }
                        break;
                }
            }
            
            return false;
        }
        
        public static bool Vector3Approximately(fix aX,fix aY,fix az,fix bX,fix bY,fix bZ, fix precision)
        {
            return fixMath.abs(aX - bX) < precision &&
                   fixMath.abs(aY - bY) < precision &&
                   fixMath.abs(az - bZ) < precision;
        }
    }
}