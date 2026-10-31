using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core
{
    public interface IUnit:IReference
    {
        IBehaviour Behaviour { get; }
        
        IBrian Brian { get; }
        
        /// <summary>
        /// 单位的移动类型，根据游戏设计不同，这个值也可以不同
        /// </summary>
        MoveType MoveType  { get; }
    
        /// <summary>
        /// 单位的移动体型碰撞圆形的半径，单位：米
        /// </summary>
        fix BodyRadius  { get; }

        /// <summary>碰撞圆心相对逻辑位置的世界坐标偏移。</summary>
        fix3 CollisionOffset { get; }

        /// <summary>
        /// 当单位移动被地图阻挡的时候，是选择一个更好的落脚点（true）还是直接停止移动（false），如果直接停止移动，那么停下的时候访问hitObstacle的时候就是true，否则hitObstacle永远是false
        /// </summary>
        bool SmoothMove  { get; }

        /// <summary>
        /// 是否会忽略关卡外围，即飞行（只有飞行允许）到地图外的地方全部视作可过
        /// </summary>
        public bool IgnoreBorder  { get; }

        bool IsDeath();
        
        void LogicUpdate(fix deltaTime);
    }
}
