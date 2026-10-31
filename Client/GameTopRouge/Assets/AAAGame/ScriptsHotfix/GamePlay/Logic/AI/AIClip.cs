using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.AI.AIStrategy;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using cfg.AI;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.AI
{
    /// <summary>
    /// 一个AI片段，包含条件及行为,配置数据等，多单位共用
    /// </summary>
    public class AIClip:IReference
    {
        private AIActionDef _aiActionDef;
        
        public List<AiConditionData> conditions = new List<AiConditionData>();
        
        public List<AiActionData> aiAtions = new List<AiActionData>();

        public cfg.AI.SortType SortType => _aiActionDef.SortType;

        public static AIClip Create(AIActionDef actionDef)
        {
            AIClip aiClip = ReferencePool.Acquire<AIClip>();
            aiClip._aiActionDef = actionDef;
                
            foreach (cfg.AI.AICondition aiCondition in actionDef.Conditions)
            {
                AiStrategyManager.AICondition method = aiCondition switch
                {
                    cfg.AI.AIResource => AiStrategyManager.AIConditions["AIResource"],
                    cfg.AI.CheckPosition => AiStrategyManager.AIConditions["CheckPosition"],
                    cfg.AI.CheckEnemyInRange => AiStrategyManager.AIConditions["CheckEnemyInRange"],
                    cfg.AI.CheckCanUseSkill => AiStrategyManager.AIConditions["CheckCanUseSkill"],
                    _ => null
                };
                
                if (method != null)
                {
                    aiClip.conditions.Add(AiConditionData.Create(method, aiCondition));
                }
            }
            
            foreach (cfg.AI.AIAction aiAction in actionDef.Actions)
            {
                AiStrategyManager.AIAction method = aiAction switch
                {
                    cfg.AI.MoveTo => AiStrategyManager.AIActions["MoveTo"],
                    cfg.AI.WaitTime => AiStrategyManager.AIActions["WaitTime"],
                    cfg.AI.Follow => AiStrategyManager.AIActions["Follow"],
                    _ => null
                };

                if (method != null)
                {
                    aiClip.aiAtions.Add(AiActionData.Create(method, aiAction));
                }
            }

            return aiClip;
        }
        
        public void Clear()
        {
            aiAtions.Clear();
            conditions.Clear();
        }
    }
}