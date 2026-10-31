using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Map;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Component;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using cfg.AI;
using Unity.Mathematics;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.AI.AIStrategy
{
    public partial class AiStrategyManager
    {
        private static AICommand AIMoveTo(IUnit npc, UnitAI unitAI,float deltaTime,AIClip aiClip, cfg.AI.AIAction aiAction)
        {
            if (aiAction is MoveTo moveTo && npc is RoleUnit npcUnit)
            {
                MapProxy mapProxy = GameProxyManger.Instance.GetProxy<MapProxy>();

                //如果有缓存的移动路径
                if (unitAI.AIMoveData.Path.Count > 0)
                {
                    List<int2> movePath = unitAI.AIMoveData.Path;
                    int curIndex = unitAI.AIMoveData.CurrentPathIndex;
                    fix3 collisionPosition = npc.Behaviour.Position + npc.CollisionOffset;
                    
                    int2 curGrid = mapProxy.MapInfo.GetGridPosByMeter(collisionPosition.x, collisionPosition.y);
                    int2 targetGrid = movePath[curIndex];
                    
                    //到达下一个目标点
                    if (curGrid.x == targetGrid.x && curGrid.y == targetGrid.y)
                    {
                        unitAI.AIMoveData.CurrentPathIndex++;
                        
                        //已经走完全路径
                        if (unitAI.AIMoveData.CurrentPathIndex >= movePath.Count)
                        {
                            unitAI.AIMoveData.Path.Clear();
                            unitAI.AIMoveData.CurrentPathIndex = 0;
                            mapProxy.RemovePathRequest(npcUnit.RoleId);
                            return AICommand.Finish;
                        }
                     
                        //切换到下一个目标点
                        curIndex = unitAI.AIMoveData.CurrentPathIndex;
                        fix3 move = mapProxy.MapInfo.GetWorldPosByGrid(movePath[curIndex]) - collisionPosition;
                        return AICommand.CreateMove(fixMath.normalize(move));
                    }
                    else
                    {
                        //没有到达，则继续走
                        fix3 move = mapProxy.MapInfo.GetWorldPosByGrid(movePath[curIndex]) - collisionPosition;
                        return AICommand.CreateMove(fixMath.normalize(move));
                    }
                }
               
                //使用的是多线程Job,如果获取到路径
                List<int2> path = mapProxy.GetPath(npcUnit.RoleId);
                if (path!=null && path.Count > 0)
                {
                    unitAI.AIMoveData.Path = path;
                    unitAI.AIMoveData.CurrentPathIndex = 0;
                    fix3 collisionPosition = npc.Behaviour.Position + npc.CollisionOffset;
                    fix3 move = mapProxy.MapInfo.GetWorldPosByGrid(path[unitAI.AIMoveData.CurrentPathIndex]) - collisionPosition;
                    return AICommand.CreateMove(fixMath.normalize(move));
                }
                
                //找一条路径,使用的是多线程Job。所以路径在下一次获取时才有数据
                fix3 currentCollisionPosition = npc.Behaviour.Position + npc.CollisionOffset;
                int2 start = mapProxy.MapInfo.GetGridPosByMeter(currentCollisionPosition.x, currentCollisionPosition.y);
                int2 end = mapProxy.MapInfo.GetGridPosByMeter(MathUtils.Convert(moveTo.Target.X),MathUtils.Convert(moveTo.Target.Y));
                mapProxy.AddPathRequest(start,end,npcUnit.RoleId);
            }
            
            return AICommand.Null;
        }
    }
}
