using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.AI
{  
    /// <summary>
    /// AI驱动指令
    /// </summary>
    public class AICommand:IReference
    {
        static int IdCount = 0;

        public int Id;
        
        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsFinished = false;
        
        public fix3 Move = fix3.zero;
        
        public void Clear()
        {
            IsFinished = false;
            Move = fix3.zero;
            Id = 0;
        }
        
        public static AICommand Null
        {
            get
            {
                AICommand aiCommand = ReferencePool.Acquire<AICommand>();
                aiCommand.Id = ++IdCount;
                return aiCommand;
            }
        }
        
        public static AICommand Finish
        {
            get
            {
                AICommand aiCommand = ReferencePool.Acquire<AICommand>();
                aiCommand.IsFinished = true;
                aiCommand.Id = ++IdCount;
                return aiCommand;
            }
        }

        public static AICommand CreateMove(fix3 move)
        {
            AICommand aiCommand = ReferencePool.Acquire<AICommand>();
            aiCommand.Move = move;
            aiCommand.IsFinished = false;
            aiCommand.Id = IdCount++;
            return aiCommand;
        }
    }
}