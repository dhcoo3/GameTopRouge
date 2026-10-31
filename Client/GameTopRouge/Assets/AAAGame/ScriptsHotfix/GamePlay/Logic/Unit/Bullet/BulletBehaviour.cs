using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Component;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Bullet
{
    public class BulletBehaviour:IBehaviour
    {
        private BulletUnit _bulletUnit;
        public fix3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        
        private UnitMove _unitMove;
        public UnitMove UnitMove => _unitMove;

        private UnitRotate _unitRotate;
        public UnitRotate UnitRotate => _unitRotate;
        
        public static BulletBehaviour CreateBehaviour(BulletUnit bulletUnit)
        {
            BulletBehaviour behaviour = ReferencePool.Acquire<BulletBehaviour>();
            behaviour._bulletUnit = bulletUnit;
            behaviour._unitMove = UnitMove.Create(bulletUnit);
            behaviour._unitRotate = UnitRotate.Create(bulletUnit);
            behaviour.Rotation = Quaternion.identity;
            behaviour.Position = bulletUnit.Data.firePosition;
            return behaviour;
        }
        
        public void Clear()
        {
            Position = fix3.zero;
            Rotation = Quaternion.identity;
        }
        
        public void LogicUpdate(fix deltaTime)
        {
            _unitMove?.LogicUpdate(deltaTime);
            _unitRotate?.LogicUpdate(deltaTime);
        }
        
        ///<summary>
        ///移动向某个方向，距离也决定了速度，距离单位是米，1秒内移动的量
        ///<param name="moveForce">移动方向和力，单位：米/秒</param>
        ///</summary>
        public void MoveBy(fix3 moveForce){
            _unitMove.MoveBy(moveForce);
        }
        
        //<summary>
        ///旋转到指定角度
        ///<param name="degree">需要旋转到的角度</param>
        ///</summary>
        public void RotateTo(fix degree){
            _unitRotate.RotateTo(degree);
        }
        
        //<summary>
        ///旋转到指定角度
        ///<param name="degree">需要旋转到的角度</param>
        ///</summary>
        public void ApplyRotation(fix degree){
            _unitRotate.ApplyRotation(degree);
        }
        
        //<summary>
        ///旋转到指定角度
        ///<param name="degree">需要旋转到的角度</param>
        ///</summary>
        public void ApplyPosition(fix3 position){
            _unitMove.ApplyPosition(position);
        }

        /// <summary>
        /// 是否碰到障碍
        /// </summary>
        /// <returns></returns>
        public bool HitObstacle()
        {
            return _unitMove.HitObstacle;
        }
    }
}