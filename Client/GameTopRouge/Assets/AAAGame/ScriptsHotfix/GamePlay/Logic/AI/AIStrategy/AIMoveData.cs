using System.Collections.Generic;
using Unity.Mathematics;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.AI.AIStrategy
{
    public class AIMoveData
    {
        public List<int2> Path = new List<int2>();
        public int CurrentPathIndex;
    }
    
    public class AIWaitData
    {
        public float TimeElapsed;
    }
}