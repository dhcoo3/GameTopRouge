using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Component
{
    ///<summary>
    ///单位旋转控件，如果一个单位要通过游戏逻辑来进行旋转，就应该用它，不论是角色还是aoe还是bullet什么的
    ///</summary>
    public class UnitRotate:IReference
    {
        ///<summary>
        ///单位当前是否可以旋转角度
        ///</summary>
        private bool _canRotate = true;

        // 角度计算相关的临时变量
        private fix ANGLE_THRESHOLD = 0.01f;
        
        ///<summary>
        ///旋转的速度，1秒只能转这么多度（角度）
        ///每帧转动的角度上限是这个数字 * Time.fixedDeltaTime得来的。
        ///</summary>
        ///[Tooltip("旋转的速度，1秒只能转这么多度（角度），每帧转动的角度上限是这个数字*Time.fixedDeltaTime得来的。")]
        private fix _rotateSpeed = 720;

        private fix _targetDegree = 0.00f;  //目标转到多少度，因为旋转发生在围绕y轴旋转，所以只有y就足够了
        
        private IUnit _unit;

        public static UnitRotate Create(IUnit unit)
        {
            UnitRotate unitMove = ReferencePool.Acquire<UnitRotate>();
            unitMove._unit = unit;
            return unitMove;
        }
        
        public void LogicUpdate(fix deltaTime) 
        {
            if (!_canRotate || IsRotationComplete(deltaTime))
                return;

            fix currentRotation = NormalizeAngle(_unit.Behaviour.Rotation.eulerAngles.z);
            fix rotationStep = CalculateRotationStep(currentRotation, deltaTime);
            ApplyRotation(rotationStep);
        }
        
        /// <summary>
        /// 应用旋转到目标变换
        /// </summary>
        public void ApplyRotation(fix rotationStep)
        {
            // 使用四元数进行旋转，避免万向锁问题
            Quaternion deltaRotation = Quaternion.AngleAxis(rotationStep, Vector3.forward);
            _unit.Behaviour.Rotation *= deltaRotation;
        }
        
        /// <summary>
        /// 检查是否已完成旋转
        /// </summary>
        public bool IsRotationComplete(float deltaTime)
        {
            fix currentAngle = NormalizeAngle(_unit.Behaviour.Rotation.eulerAngles.z);
            fix angleDifference = fixMath.abs(NormalizeAngle(currentAngle - _targetDegree));
        
            // 使用最小容差，防止在高速旋转时过度敏感
            fix tolerance = fixMath.min(ANGLE_THRESHOLD, _rotateSpeed * deltaTime * 0.5f);
            return angleDifference <= tolerance;
        }
        
        /// <summary>
        /// 规范化角度到[-180, 180]范围
        /// </summary>
        private fix NormalizeAngle(fix angle)
        {
            angle %= 360f;
            if (angle > 180f)
                angle -= 360f;
            else if (angle < -180f)
                angle += 360f;
            return angle;
        }


        /// <summary>
        /// 计算本次帧的旋转步长
        /// </summary>
        private fix CalculateRotationStep(fix currentAngle, fix deltaTime)
        {
            // 计算两种可能的旋转路径
            fix directPath = _targetDegree - currentAngle;
            fix alternativePath = (_targetDegree > currentAngle) ? 
                _targetDegree - 360f - currentAngle : 
                _targetDegree + 360f - currentAngle;

            // 选择最短路径
            bool useAlternativePath = fixMath.abs(alternativePath) < fixMath.abs(directPath);
            fix shortestDistance = useAlternativePath ? alternativePath : directPath;

            // 计算旋转步长，确保不会旋转过度
            fix maxRotationStep = _rotateSpeed * deltaTime;
            fix rotationStep = fixMath.min(maxRotationStep, fixMath.abs(shortestDistance));
        
            // 应用旋转方向
            return rotationStep * fixMath.sign(shortestDistance);
        }
        
        ///<summary>
        ///旋转到指定角度
        ///<param name="degree">需要旋转到的角度</param>
        ///</summary>
        public void RotateTo(fix degree){
            _targetDegree = degree;
        }

        ///<summary>
        ///指定两个点，旋转到对应角度
        ///<param name="x">目标点x-起点x</param>
        ///<param name="z">目标点z-起点z</param>
        ///</summary>
        public void RotateTo(fix x, fix y){
            _targetDegree = fixMath.atan2(x, y) * 180.00f / fixMath.PI;   
        }

        ///<summary>
        ///旋转指定角度
        ///<param name="degree">需要旋转到的角度</param>
        ///</summary>
        public void RotateBy(fix degree){
            _targetDegree = _unit.Behaviour.Rotation.eulerAngles.z + degree;
        }

        ///<summary>
        ///禁止单位可以旋转的能力，这会终止当前正在进行的旋转
        ///终止当前的旋转看起来是一个side-effect，但是依照游戏规则设计来说，他只是“配套功能”所以严格的说并不是side-effect
        ///</summary>
        public void DisableRotate(){
            _canRotate = false;
            _targetDegree = _unit.Behaviour.Rotation.eulerAngles.z;
        }

        ///<summary>
        ///开启单位可以旋转的能力
        ///</summary>
        public void EnableRotate(){
            _canRotate = true;
        }

        public void Clear()
        {
            _unit = null;
        }
    }
}
