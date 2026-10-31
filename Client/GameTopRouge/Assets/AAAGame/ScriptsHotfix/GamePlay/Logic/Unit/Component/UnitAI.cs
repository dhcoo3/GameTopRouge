using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.AI;
using AAAGame.ScriptsHotfix.GamePlay.Logic.AI.AIStrategy;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using cfg.AI;
using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Component
{
    public class UnitAI:IReference
    {
        private IUnit _unit;
        
        public readonly AIMoveData AIMoveData = new AIMoveData();
        public readonly AIWaitData AIWaitData = new AIWaitData();
        
        /// <summary>
        /// AI脚本数组，错误的理解就是“行为树”
        /// </summary>
        private List<AIClip> _aiClips = new List<AIClip>();
        
        /// <summary>
        /// 要执行的命令组，角色行为依赖于这个，而非AI
        /// </summary>
        private List<AICommand> _todoCommand = new List<AICommand>();
        
        /// <summary>
        /// 执行完成的AI
        /// </summary>
        private List<AIClip> _finishClips = new List<AIClip>();
        
        private AIProxy _aiProxy;
        
        public static UnitAI Create(IUnit unit,int aiId)
        {
            UnitAI unitMove = ReferencePool.Acquire<UnitAI>();
            unitMove._unit = unit;
            unitMove._aiProxy = GameProxyManger.Instance.GetProxy<AIProxy>();
            unitMove._aiClips = unitMove._aiProxy.GetAIClips(aiId);
            return unitMove;
        }
        
        public void Clear()
        {
            _unit = null;
            _todoCommand.Clear();
            _aiClips.Clear();
        }

        public void LogicUpdate(fix deltaTime)
        {
            RunAI(deltaTime);

            for (int i = 0; i < _todoCommand.Count; i++)
            {
                ParseCommand(_todoCommand[i]);
                ReferencePool.Release(_todoCommand[i]);
            }
            
            _todoCommand.Clear();

            CheckSort();
        }
        
        /// <summary>
        /// 可以运行AI的时候，用这个运行AI
        /// 好吧，简单地说就是“执行AI"
        /// <param name="game">哪个运行中的游戏要我执行AI？</param>
        /// </summary>
        public void RunAI(fix deltaTime)
        {
            //必须For i，因为顺序是十分重要的，foreach在一些设备会被优化而牺牲顺序
            for (int i = 0; i < _aiClips.Count; i++)
            {
                //判断条件，如果满足才能运行，有一条不满足就再见了
                bool run = true;
                
                foreach (var condition in _aiClips[i].conditions)
                {
                    if (!condition.method.Invoke(_unit,this,condition.parameter))
                    {
                        run = false;
                        break;
                    }
                }

                if (!run)
                {
                    continue;
                }
                
                bool finished = true;
                
                foreach (var aiAction in _aiClips[i].aiAtions)
                {
                    AICommand aiCommand = aiAction.method.Invoke(_unit, this, deltaTime,_aiClips[i], aiAction.parameter);
                    
                    _todoCommand.Add(aiCommand);
                    
                    if (!aiCommand.IsFinished)
                    {
                        finished = false;
                    }
                }
                
                if (finished)
                {
                    _finishClips.Add(_aiClips[i]);
                }
                
                //【注意】只有第一个符合条件的aiClip才会执行，执行完毕，这次ai就算完事儿了，后续是不检查的
                //所以AIClip的list顺序是严谨的
                return;
            }            
        }

        /// <summary>
        /// 解析处理指令
        /// </summary>
        /// <param name="command"></param>
        public void ParseCommand(AICommand command)
        {
            if (command.Move != fix3.zero)
            {
                if (_unit is RoleUnit roleUnit)
                {
                    roleUnit.OrderMove(command.Move);
                }
            }
        }

        public void CheckSort()
        {
            if (_finishClips.Count == 0)
            {
                return;                
            }

            for (int i = 0; i < _finishClips.Count; i++)
            {
                AIClip finishClip = _finishClips[i];
                
                if (finishClip.SortType == SortType.ToLast)
                {
                    ResortAIList((clip1, clip2) =>
                    {
                        if (finishClip == clip1)
                        {
                            return 1;
                        }
                        
                        if (finishClip == clip2)
                        {
                            return -1;
                        }
                        
                        return 0;
                    });
                }
            }
            
            _finishClips.Clear();
        }

        /// <summary>
        /// 重新排列AIClip
        /// 这样一来，npc的行为队列就会发生变化，原本某些行为甚至会变得不会在做了，因为在列表下方了
        /// 这是行为树难以做到（但是强行要做有何不可，策划辛苦点就行了）的
        /// </summary>
        /// <param name="comparison"></param>
        public void ResortAIList(Comparison<AIClip> comparison) => _aiClips.Sort(comparison);

    }
}