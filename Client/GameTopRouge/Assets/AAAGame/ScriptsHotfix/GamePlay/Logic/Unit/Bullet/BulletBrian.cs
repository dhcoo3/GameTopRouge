using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Bullet
{
    public class BulletBrian:IBrian
    {
        private BulletUnit _bulletUnit;
        private BulletData _bulletData => _bulletUnit.Data;
        private BulletBehaviour _bulletBehaviour => (BulletBehaviour)_bulletUnit.Behaviour;
        
        public fix FaceDegree { get; }
        public fix MoveDegree { get; }
        
        ///<summary>
        ///本帧的移动
        ///</summary>
        private fix3 moveForce = fix3.zero;

        ///<summary>
        ///本帧的移动信息
        ///</summary>
        public fix3 velocity{
            get{return moveForce;}
        }
        
        private UnitProxy _unitProxy;
        
        public static BulletBrian CreateBrian(BulletUnit bulletUnit)
        {
            BulletBrian bulletBrian = ReferencePool.Acquire<BulletBrian>();
            bulletBrian._bulletUnit = bulletUnit;
            bulletBrian._unitProxy = GameProxyManger.Instance.GetProxy<UnitProxy>();
            return bulletBrian;
        }
        
        public void Clear()
        {
            
        }
        
        public void LogicUpdate(fix deltaTime)
        {
            if (!HasEnemy()) return;

            fix timePassed = deltaTime;
            if (_bulletData.hp <= 0)
            {
                return;
            }

            //如果是刚创建的，那么就要处理刚创建的事情
            if (_bulletData.timeElapsed <= 0 && _bulletData.model.onCreate != null){
                _bulletData.model.onCreate(_bulletUnit);
            }

            //处理子弹命中纪录信息
            int hIndex = 0;
            while (hIndex < _bulletData.hitRecords.Count){
                _bulletData.hitRecords[hIndex].timeToCanHit -= timePassed;
                if (_bulletData.hitRecords[hIndex].timeToCanHit <= 0 || _bulletData.hitRecords[hIndex].target == null){
                    //理论上应该支持可以鞭尸，所以即使target dead了也得留着……
                    _bulletData.hitRecords.RemoveAt(hIndex);
                }else{
                    hIndex += 1;
                }
            }
       
            //处理子弹的移动信息
            CalcMove(
                _bulletData.tween == null ? fix3.right : _bulletData.tween(_bulletData.timeElapsed, _bulletUnit, _bulletData.followingTarget)
            );

            CalcCollision(deltaTime);

            ///生命周期的结算
            _bulletData.duration -= timePassed;
            _bulletData.timeElapsed += timePassed;
            if (_bulletData.duration <= 0 || _bulletBehaviour.HitObstacle()){
                if (_bulletData.model.onRemoved != null){
                    _bulletData.model.onRemoved(_bulletUnit);
                }
                
                Destroy();
            }
        }

        public bool HasEnemy()
        {
            return _bulletUnit.HasEntity;
        }
   
        public void CalcMove(fix3 mf)
        {
            this.moveForce = mf;

            moveForce.z = fix.Zero;
            moveForce *= _bulletData.speed;
            
            fix moveDeg;
            
            if (_bulletData.useFireDegreeForever || _bulletData.timeElapsed <= fix.Zero)
            {
                moveDeg = _bulletData.fireDegree;
            }
            else
            {
                moveDeg = _bulletUnit.Behaviour.Rotation.eulerAngles.z;
              
                // 改为绕Z轴的角度计算：fixMath.atan2(y, x)
                moveDeg += fixMath.atan2(moveForce.y, moveForce.x) * 180 / fix.Pi;

                fix mR = moveDeg * fix.Pi / 180;
                fix mLen = fixMath.sqrt(Mathf.Pow(moveForce.x, 2) + Mathf.Pow(moveForce.y, 2)); // 计算XY平面的长度

                // 改为绕Z轴的向量计算：Cos对应X，Sin对应Y
                moveForce.x = fixMath.cos(mR) * mLen;
                moveForce.y = fixMath.sin(mR) * mLen;
                moveForce.z = fix.Zero; // 确保Z轴为0
            }

            _bulletBehaviour.MoveBy(moveForce);

            if (_bulletData.useFireDegreeForever || _bulletData.timeElapsed <= fix.Zero)
            {
                _bulletBehaviour.ApplyRotation(moveDeg);
            }
            
            _bulletBehaviour.RotateTo(moveDeg);
        }
        
        private void CalcCollision(fix timePassed)
        {
            //处理子弹的碰撞信息，如果子弹可以碰撞，才会执行碰撞逻辑
            if (_bulletData.canHitAfterCreated > 0) 
            {
                _bulletData.canHitAfterCreated -= timePassed;  
            }
            else
            {
                fix bRadius = _bulletData.model.radius;
                int bSide = -1;
                
                if (_bulletData.caster is RoleUnit roleUnit){
                    bSide = roleUnit.Data.Side;
                }

                foreach (var (id,unit) in _unitProxy.Monsters)
                {
                    if (!CanHit(unit)) continue;

                    if (unit is RoleUnit roleUnit2)
                    {
                        if(unit.RoleState.IsDeath || unit.RoleState.IsImmune)
                        {
                            continue;
                        }

                        if (_bulletData.model.hitAlly == false && bSide == unit.Data.Side)
                        {
                            continue;
                        }

                        if (_bulletData.model.hitFoe == false && bSide != unit.Data.Side)
                        {
                            continue;
                        }
                    
                        fix cRadius = roleUnit2.Data.Property.HitRadius;
                        fix3 dis = _bulletUnit.Behaviour.Position - unit.Behaviour.Position;
                    
                        if (Mathf.Pow(dis.x, 2) + Mathf.Pow(dis.y, 2) <= Mathf.Pow(bRadius + cRadius, 2)){
                            //命中了
                            _bulletData.hp -= 1;

                            if ( _bulletData.model.onHit != null){
                                _bulletData.model.onHit(_bulletUnit,roleUnit2);
                            }
                        
                            if (_bulletData.hp > 0){
                                AddHitRecord(roleUnit2);
                            }
                            else
                            {
                                Destroy();
                            }
                        }
                    }
                }
            }
        }
        
        ///<summary>
        ///判断子弹是否还能击中某个GameObject
        ///<param name="target">目标gameObject</param>
        ///</summary>
        public bool CanHit(IUnit target){
            if (_bulletData.canHitAfterCreated > 0)
            {
                return false;
            }
            
            for (int i = 0; i < _bulletData.hitRecords.Count; i++){
                if (_bulletData.hitRecords[i].target == target){
                    return false;
                }
            }

            return true;
        }
        
        ///<summary>
        ///添加命中纪录
        ///<param name="target">目标GameObject</param>
        ///</summary>
        public void AddHitRecord(IUnit target){
            _bulletData.hitRecords.Add(new BulletHitRecord(
                target,
                _bulletData.model.sameTargetDelay
            ));
        }

        public void Destroy()
        {
            _unitProxy.RemoveRoleUnit(_bulletData.BulletId);
        }
    }
}