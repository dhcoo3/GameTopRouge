using AAAGame.ScriptsHotfix.GamePlay.Logic.AI.AIStrategy;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.AI
{
    /// <summary>
    /// AI行为方法及参数
    /// </summary>
    public class AiActionData:IReference
    {
        public AiStrategyManager.AIAction method;
        public cfg.AI.AIAction parameter;

        public static AiActionData Create(AiStrategyManager.AIAction method, cfg.AI.AIAction parameter)
        {
            AiActionData aiActionData =ReferencePool.Acquire<AiActionData>();
            aiActionData.method = method;
            aiActionData.parameter = parameter;
            return aiActionData;
        }
        
        public void Clear()
        {
            method = null;
            parameter = null;
        }
    }
}