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
        /// 资源条件检查
        /// </summary>
        /// <param name="npc">当前运行AI的对象</param>
        /// <param name="param">需要比对的资源条件</param>
        /// <returns>是否满足条件</returns>
        private static bool CheckAIResource(IUnit npc,UnitAI unitAI, cfg.AI.AICondition param)
        {
            if (param is cfg.AI.AIResource resource && npc is RoleUnit roleUnit)
            {
                switch (resource.PropertyType)
                {
                    case PropertyType.HP:
                        switch (resource.OperatorType)
                        {
                            case Operator.GT:
                                if (resource.Rate)
                                {
                                    fix rate = roleUnit.Data.Resource.hp / roleUnit.Data.Property.Hp * 100;
                                    return rate > resource.Val;
                                }
                                return roleUnit.Data.Resource.hp > resource.Val;
                            case Operator.LT:
                                if (resource.Rate)
                                {
                                    fix rate = roleUnit.Data.Resource.hp / roleUnit.Data.Property.Hp * 100;
                                    return rate < resource.Val;
                                }
                                return roleUnit.Data.Resource.hp < resource.Val;
                            case Operator.GE:
                                if (resource.Rate)
                                {
                                    fix rate = roleUnit.Data.Resource.hp / roleUnit.Data.Property.Hp * 100;
                                    return rate >= resource.Val;
                                }
                                return roleUnit.Data.Resource.hp >= resource.Val;
                            case Operator.LE:
                                if (resource.Rate)
                                {
                                    fix rate = roleUnit.Data.Resource.hp / roleUnit.Data.Property.Hp * 100;
                                    return rate <= resource.Val;
                                }
                                return roleUnit.Data.Resource.hp <= resource.Val;
                            case Operator.NE:
                                if (resource.Rate)
                                {
                                    fix rate = roleUnit.Data.Resource.hp / roleUnit.Data.Property.Hp * 100;
                                    return rate != resource.Val;
                                }
                                return roleUnit.Data.Resource.hp != resource.Val;
                            case Operator.EQ:
                                if (resource.Rate)
                                {
                                    fix rate = roleUnit.Data.Resource.hp / roleUnit.Data.Property.Hp * 100;
                                    return rate == resource.Val;
                                }
                                return roleUnit.Data.Resource.hp == resource.Val;
                        }
                        break;
                    case PropertyType.ATK:
                        
                        break;
                    case PropertyType.DEF:
                        
                        break;
                }
            }
            
            return false;
        }
    }
}