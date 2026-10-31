using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Aoe
{
    /// <summary>
    /// Aoe逻辑
    /// </summary>
    public class AoeBrian:IBrian
    {
        private AoeUnit _aoeUnit;
        public fix FaceDegree { get; }
        public fix MoveDegree { get; }

        private UnitProxy _unitProxy;

        private List<IUnit> _enterCha = new List<IUnit>();
        
        private List<IUnit> _leaveCha = new List<IUnit>();
        
        private List<IUnit> _toRemove = new List<IUnit>();
        
        private List<IUnit> _enterBullet = new List<IUnit>();
        
        private List<IUnit> _leaveBullet = new List<IUnit>();
        
        public static AoeBrian CreateBrian(AoeUnit aoeUnit)
        {
            AoeBrian aoeBrian = ReferencePool.Acquire<AoeBrian>();
            aoeBrian._aoeUnit = aoeUnit;
            aoeBrian._unitProxy = GameProxyManger.Instance.GetProxy<UnitProxy>();
            return aoeBrian;
        }
        
        public void Clear()
        {
            _aoeUnit = null;
            _enterCha.Clear();
            _leaveCha.Clear();
            _toRemove.Clear();
            _enterBullet.Clear();
            _leaveBullet.Clear();
            _unitProxy = null;
        }
        
        public void LogicUpdate(fix deltaTime)
        {
            if(_aoeUnit == null) return;
           
           //首先是aoe的移动
            if (_aoeUnit.Data.duration > 0 && _aoeUnit.Data.tween != null){
                AoeMoveInfo aoeMoveInfo = _aoeUnit.Data.tween(_aoeUnit, _aoeUnit.Data.tweenRunnedTime);
                _aoeUnit.Data.tweenRunnedTime += deltaTime;
                SetMoveAndRotate(aoeMoveInfo);
            }
            else
            {
                DefaultMoveAndRotate();
            }
            
            if (_aoeUnit.Data.justCreated){
                //刚创建的，走onCreate
                _aoeUnit.Data.justCreated = false;

                FirstMoveAndRotate();
                FirstCheckAllRoleInRange();
                
                //执行OnCreate
                if (_aoeUnit.Data.model.onCreate != null){
                    _aoeUnit.Data.model.onCreate(_aoeUnit,_aoeUnit.Data.characterInRange,_aoeUnit.Data.bulletInRange);
                }
            }else{
                //已经创建完成的
                //先抓角色离开事件
                _leaveCha.Clear();
                _toRemove.Clear();
                
                CheckAllRoleLeavesRange(ref _leaveCha,ref _toRemove);
                
                for (int m = 0; m < _toRemove.Count; m++){
                    _aoeUnit.Data.characterInRange.Remove(_toRemove[m]);
                }
                if (_aoeUnit.Data.model.onChaLeave != null){
                    _aoeUnit.Data.model.onChaLeave(_aoeUnit, _leaveCha);
                }

                //再看进入的角色
                _enterCha.Clear();
                CheckAllRoleInRange(ref _enterCha);
                
                if (_aoeUnit.Data.model.onChaEnter != null){
                    _aoeUnit.Data.model.onChaEnter(_aoeUnit, _enterCha);
                }
                
                for (int m = 0; m < _enterCha.Count; m++){
                    if (_enterCha[m] != null && _enterCha[m].IsDeath() == false){
                        _aoeUnit.Data.characterInRange.Add(_enterCha[m]);
                    }
                }

                //子弹离开
                _leaveBullet.Clear();
                _toRemove.Clear();
                CheckAllBulletLeavesRange(ref _leaveBullet, ref _toRemove);
               
                for (int m = 0; m < _toRemove.Count; m++){
                    _aoeUnit.Data.bulletInRange.Remove(_toRemove[m]);
                }
                if (_aoeUnit.Data.model.onBulletLeave != null){
                    _aoeUnit.Data.model.onBulletLeave(_aoeUnit, _leaveBullet);
                }
                
                _toRemove.Clear();

                //子弹进入
                _enterBullet.Clear();
                CheckAllBulletInRange(ref _enterBullet);
                
                if (_aoeUnit.Data.model.onBulletEnter != null){
                    _aoeUnit.Data.model.onBulletEnter(_aoeUnit, _enterBullet);
                }
                for (int m = 0; m < _enterBullet.Count; m++){
                    if (_enterBullet[m] != null){
                        _aoeUnit.Data.bulletInRange.Add(_enterBullet[m]);
                    }
                }
            }
            
            //然后是aoe的duration
            _aoeUnit.Data.duration -= deltaTime;
            _aoeUnit.Data.timeElapsed += deltaTime;
            if (_aoeUnit.Data.duration <= 0 || _aoeUnit.AoeBehaviour.HitObstacle() == true){
                if (_aoeUnit.Data.model.onRemoved != null){
                    _aoeUnit.Data.model.onRemoved(_aoeUnit);
                }
                Destroy();
            }else{
                 //最后是onTick，remove
                if (
                    _aoeUnit.Data.model.tickTime > 0 && _aoeUnit.Data.model.onTick != null &&
                    fixMath.roundToInt(_aoeUnit.Data.duration * 1000) % fixMath.roundToInt(_aoeUnit.Data.model.tickTime * 1000) == 0
                ){
                    _aoeUnit.Data.model.onTick(_aoeUnit);
                }
            }
        }

        public void FirstMoveAndRotate()
        {
            _aoeUnit.AoeBehaviour.ApplyRotation(_aoeUnit.Data.fireDegree);
            _aoeUnit.AoeBehaviour.ApplyPosition(_aoeUnit.Data.position);
        }

        public void DefaultMoveAndRotate()
        {
            _aoeUnit.AoeBehaviour.RotateTo(_aoeUnit.Data.fireDegree);
            _aoeUnit.AoeBehaviour.ApplyPosition(_aoeUnit.Data.position);
        }

        ///<summary>
        ///设置移动和旋转的信息，用于执行
        ///</summary>
        public void SetMoveAndRotate(AoeMoveInfo aoeMoveInfo){
            if (aoeMoveInfo != null){
                /*unitMove.moveType = aoeMoveInfo.moveType;
                unitMove.bodyRadius = this.radius;
                _velo = aoeMoveInfo.velocity / Time.fixedDeltaTime;
                _aoeBehaviour.MoveBy(_velo);
                _aoeBehaviour.RotateTo(aoeMoveInfo.rotateToDegree);*/
            }
        }

        /// <summary>
        /// 目标是否在Aoe范围内
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool InRange(IUnit target)
        {
            if (target == null || target.Behaviour == null) return false;
            if (target.IsDeath()) return false;
            
            /*return Utils.InRange(
                _aoeUnit.AoeBehaviour.Position.x, _aoeUnit.AoeBehaviour.Position.z,
                target.Behaviour.Position.x, target.Behaviour.Position.z,
                _aoeUnit.Data.radius
            );*/
            
            return Utils.IsTargetInSectorArea2D(MathUtils.Fix3ToFix2(_aoeUnit.AoeBehaviour.Position), 
                _aoeUnit.AoeBehaviour.Rotation.eulerAngles.z,
                MathUtils.Fix3ToFix2(target.Behaviour.Position),
                _aoeUnit.Data.radius,
                _aoeUnit.Data.degree);
        }
        
        /// <summary>
        /// 生成时，获取范围内的角色对象
        /// </summary>
        void FirstCheckAllRoleInRange()
        {
            foreach (var (id,target) in _unitProxy.Units)
            {
                if (target.IsDeath()) continue;
                
                if (InRange(target))
                {
                    if (target is BulletUnit)
                    {
                        _aoeUnit.Data.bulletInRange.Add(target);
                    }
                    else if (target is RoleUnit)
                    {
                        _aoeUnit.Data.characterInRange.Add(target);
                    }
                }
            }
        }

        /// <summary>
        /// 检测是否有角色离开Aoe
        /// </summary>
        /// <param name="leaveCha"></param>
        /// <param name="toRemove"></param>
        void CheckAllRoleLeavesRange(ref List<IUnit> leaveCha,ref List<IUnit> toRemove)
        {
            for (int m = 0; m < _aoeUnit.Data.characterInRange.Count; m++){
                if (_aoeUnit.Data.characterInRange[m] != null){
                    if (InRange(_aoeUnit.Data.characterInRange[m]) == false)
                    {
                        leaveCha.Add(_aoeUnit.Data.characterInRange[m]);
                        toRemove.Add(_aoeUnit.Data.characterInRange[m]);
                    }
                }else{
                    toRemove.Add(_aoeUnit.Data.characterInRange[m]);
                }
            }
        }

        /// <summary>
        /// 检测是否有角色进入范围
        /// </summary>
        void CheckAllRoleInRange(ref List<IUnit> enterCha)
        {
            foreach (var (id,target) in _unitProxy.Units)
            {
                if (target.IsDeath()) continue;
                
                if (_aoeUnit.Data.characterInRange.IndexOf(target) < 0 && InRange(target))
                {
                    if (target is RoleUnit)
                    {
                        enterCha.Add(target);
                    }
                }
            }
        }

        /// <summary>
        /// 检测是否有子弹离开范围
        /// </summary>
        /// <param name="leaveBullet"></param>
        /// <param name="toRemove"></param>
        void CheckAllBulletLeavesRange(ref List<IUnit> leaveBullet, ref List<IUnit> toRemove)
        {
            for (int m = 0; m < _aoeUnit.Data.bulletInRange.Count; m++){
                if (_aoeUnit.Data.bulletInRange[m] != null)
                {
                    if (InRange(_aoeUnit.Data.bulletInRange[m]) == false)
                    {
                        leaveBullet.Add(_aoeUnit.Data.bulletInRange[m]);
                        toRemove.Add(_aoeUnit.Data.bulletInRange[m]);
                    }
                }else{
                    toRemove.Add(_aoeUnit.Data.bulletInRange[m]);
                }
                        
            }
        }

        /// <summary>
        /// 检测是否有子弹进入范围
        /// </summary>
        /// <param name="enterBullet"></param>
        void CheckAllBulletInRange(ref List<IUnit> enterBullet)
        {
            foreach (var (id,target) in _unitProxy.Bullets)
            {
                if (_aoeUnit.Data.bulletInRange.IndexOf(target) < 0 && InRange(target))
                {
                    enterBullet.Add(target);
                }
            }
        }

        void Destroy()
        {
            _unitProxy.RemoveRoleUnit(_aoeUnit.Data.AoeId);
        }
    }
}