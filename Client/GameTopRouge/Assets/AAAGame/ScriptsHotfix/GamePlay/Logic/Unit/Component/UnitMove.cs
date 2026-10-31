using AAAGame.ScriptsHotfix.GamePlay.Logic.Map;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Component
{
    ///<summary>
    ///单位移动控件，所有需要移动的单位都应该添加这个来控制它的移动，不论是角色，aoe还是子弹，但是地形不能用这个，因为地形是terrain不是unit
    ///这里负责的是每一帧往一个方向移动，至于往什么方向移动，这应该是其他控件的事情，比如角色是由操作来决定的，子弹则是轨迹决定的
    ///在这游戏里，角色只有x,z方向移动，依赖于地形的y移动如果有，也不归这个逻辑来管理，而是由视觉元素（比如小土坡）自身决定的
    ///</summary>
    public class UnitMove:IReference
    {
        //是否有权移动
        private bool _canMove = true;
        public bool HitObstacle => _hitObstacle;
        
        private bool _hitObstacle = false;
        
        //要移动的方向的力，单位：米/秒。
        private fix3 _velocity = fix3.zero;
        
        private MapProxy _mapProxy;
        
        private IUnit _unit;

        private fix3 _tmpVector3 = fix3.zero;
        public static UnitMove Create(IUnit unit)
        {
            UnitMove unitMove = ReferencePool.Acquire<UnitMove>();
            unitMove._mapProxy = GameProxyManger.Instance.GetProxy<MapProxy>();
            unitMove._unit = unit;
            return unitMove;
        }

        public void LogicUpdate(fix fixedDeltaTime) {
            if (_canMove == false || _velocity == fix3.zero) return;

            fix3 collisionOffset = _unit.CollisionOffset;
            fix3 collisionPosition = _unit.Behaviour.Position + collisionOffset;
            _tmpVector3.x = _velocity.x * fixedDeltaTime + collisionPosition.x;
            _tmpVector3.y = _velocity.y * fixedDeltaTime + collisionPosition.y;
            _tmpVector3.z = _velocity.z * fixedDeltaTime + collisionPosition.z;
            
            MapTargetPosInfo mapTargetPosInfo = _mapProxy.MapInfo.FixTargetPosition(
                collisionPosition,
                _unit.BodyRadius, 
                _tmpVector3, 
                _unit.MoveType, (_unit.IgnoreBorder&& _unit.MoveType == MoveType.fly)
            );
            
            if (_unit.SmoothMove == false && mapTargetPosInfo.obstacle){
                _hitObstacle = true;
                _canMove = false;
            }

            _unit.Behaviour.Position = mapTargetPosInfo.suggestPos - collisionOffset;
            _velocity = fix3.zero;
            _tmpVector3 = fix3.zero;
        }

        private void StopMoving(){
            _velocity = fix3.zero;
        }

        ///<summary>
        ///当前的移动方向
        ///</summary>
        public fix3 GetMoveDirection(){
            return _velocity;
        }

        ///<summary>
        ///移动向某个方向，距离也决定了速度，距离单位是米，1秒内移动的量
        ///<param name="moveForce">移动方向和力，单位：米/秒</param>
        ///</summary>
        public void MoveBy(fix3 moveForce){
            if (_canMove == false) return;
            this._velocity = moveForce;
        }
        
        /// <summary>
        /// 应用旋转到坐标
        /// </summary>
        public void ApplyPosition(fix3 position)
        {
            _unit.Behaviour.Position = position;
        }

        ///<summary>
        ///禁止角色可以移动能力，会停止当前的移动
        ///</summary>
        public void DisableMove(){
            StopMoving();
            _canMove = false;
        }

        ///<summary>
        ///开启角色可以移动的能力
        ///</summary>
        public void EnableRotate(){
            _canMove = true;
        }

        public void Clear()
        {
            _unit = null;
        }
    }   
}



