using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Skill;
using AAAGame.ScriptsHotfix.GamePlay.Logic.TimeLine;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Component;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role
{
    /// <summary>
    /// 角色行为，移动、旋转、释放技能
    /// </summary>
    public class RoleBehaviour:IBehaviour
    {   
        private RoleUnit _roleUnit;
        public RoleData RoleData => _roleUnit.Data;
        private RoleBrian _roleBrian => (RoleBrian)_roleUnit.Brian;
        
        public fix3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        
        private UnitMove _unitMove;
        public UnitMove UnitMove => _unitMove;

        private UnitRotate _unitRotate;
        public UnitRotate UnitRotate => _unitRotate;
        
        private TimelineProxy _timelineProxy;
        
        private UnitProxy _unitProxy;
        
        public List<AnimPlayData> WaitPlayAnim  = new List<AnimPlayData>();

        private UnitAI _unitAI;

        public IUnit LookAtTarget;
        
        /// <summary>
        /// 上一帧的位置
        /// </summary>
        private fix3 _lastFramePosition { get; set; }

        public static RoleBehaviour CreateRoleBehaviour(RoleUnit roleUnit)
        {
            RoleBehaviour behaviour = ReferencePool.Acquire<RoleBehaviour>();
            behaviour._roleUnit = roleUnit;
            behaviour._unitMove = UnitMove.Create(roleUnit);
            behaviour._unitRotate = UnitRotate.Create(roleUnit);
            behaviour._timelineProxy = GameProxyManger.Instance.GetProxy<TimelineProxy>();
            behaviour._unitProxy =  GameProxyManger.Instance.GetProxy<UnitProxy>();
            behaviour.Rotation = Quaternion.identity;
            behaviour.Position = roleUnit.Data.InitPosition;
            behaviour._lastFramePosition = behaviour.Position;

            if (roleUnit.Data.AIId > 0)
            {
                behaviour._unitAI = UnitAI.Create(roleUnit,roleUnit.Data.AIId);
            }
            
            return behaviour;
        }
        
        public void Clear()
        {
            _roleUnit = null;
            Position = fix3.zero;
            Rotation = Quaternion.identity;
            _lastFramePosition = fix3.zero;
            ReferencePool.Release(_unitMove);
            _unitMove = null;
            ReferencePool.Release(_unitRotate);
            _unitRotate = null;
            if (_unitAI!=null)
            {
                ReferencePool.Release(_unitAI);
            }
            _unitAI = null;
            _timelineProxy = null;
            _unitProxy = null;
        }

        public void LogicUpdate(fix fixedDeltaTime)
        {
            _unitMove?.LogicUpdate(fixedDeltaTime);
            _unitRotate?.LogicUpdate(fixedDeltaTime);
            _unitAI?.LogicUpdate(fixedDeltaTime);
        }

        /// <summary>
        /// 保存本次逻辑步进后的移动位置，供下一帧开始前恢复。
        /// </summary>
        public void SaveFramePosition()
        {
            _lastFramePosition = Position;
        }

        ///<summary>
        ///释放一个技能，释放技能并不总是成功的，如果你一直发释放技能的命令，那失败率应该是骤增的
        ///<param name="id">要释放的技能的id</param>
        ///<return>是否释放成功</return>
        ///</summary>
        public bool UseSkill(int id){
            if (_roleBrian.RoleState.canUseSkill == false) 
            {
                return false; //不能用技能就不放了
            }
            
            SkillObj skillObj = GetSkillById(id);
            
            if (skillObj == null || skillObj.Cooldown > 0)
            {
                Debug.LogWarning("技能ID错误，无法释放");
                return false;
            }
            
            bool castSuccess = false;
            if (RoleData.Resource.Enough(skillObj.Model.condition))
            {
                TimelineObj timeline = new TimelineObj(
                    skillObj.Model.effect, _roleUnit, skillObj
                );
                
                for (int i = 0; i < RoleData.Buffs.Count; i++){
                    if (RoleData.Buffs[i].model.onCast != null)
                    {
                        timeline = RoleData.Buffs[i].model.onCast(RoleData.Buffs[i], skillObj, timeline);
                    }
                }
                
                if (timeline != null){
                    this.ModResource(-1 * skillObj.Model.cost);
                    _timelineProxy?.AddTimeline(timeline);
                    castSuccess = true;
                }
            }
            else
            {
                Debug.LogWarning("不满足释放技能条件");
            }
            
            skillObj.Cooldown = 0.1f;   //无论成功与否，都会进入gcd
            return castSuccess;
        }
        
        ///<summary>
        ///根据id获得角色学会的技能（skillObj），如果没有则返回null
        ///<param name="id">技能的id</param>
        ///<return>skillObj or null</return>
        ///</summary>
        public SkillObj GetSkillById(int id){
            for (int i = 0; i < RoleData.Skills.Count; i++ ){
                if (RoleData.Skills[i].Model.id == id){
                    return RoleData.Skills[i];
                }
            }
            return null;
        }
        
        ///<summary>
        ///增加角色的血量等资源，直接改变数字的，属于最后一步操作了
        ///<param name="value">要改变的量，负数为减少</param>
        ///</summary>
        public void ModResource(ChaResource value){
            RoleData.Resource += value;
            RoleData.Resource.hp = Mathf.Clamp(RoleData.Resource.hp, 0, RoleData.Property.Hp);
            RoleData.Resource.ammo = Mathf.Clamp(RoleData.Resource.ammo, 0, RoleData.Property.Ammo);
            RoleData.Resource.stamina = Mathf.Clamp(RoleData.Resource.stamina, 0, 100);
            if (RoleData.Resource.hp <= 0)
            {
                _roleBrian.RoleState.IsDeath = true;
                _unitProxy.RemoveRoleUnit(RoleData.RoleId);
            }
        }
    
        ///<summary>
        ///在角色身上放一个特效，其实是挂在一个gameObject而已
        ///<param name="bindPointKey">绑点名称，角色有Muzzle/Head/Body这3个，需要再加</param>
        ///<param name="effect">要播放的特效文件名，统一走Prefabs/下拿</param>
        ///<param name="effectKey">这个特效的key，要删除的时候就有用了</param>
        ///<param name="effect">要播放的特效</param>
        ///</summary>
        public void PlaySightEffect(string bindPointKey, string effect, string effectKey = "", bool loop = false){
            //bindPoints.AddBindGameObject(bindPointKey, "Prefabs/" + effect, effectKey, loop);
        }

        ///<summary>
        ///删除角色身上的一个特效
        ///<param name="bindPointKey">绑点名称，角色有Muzzle/Head/Body这3个，需要再加</param>
        ///<param name="effectKey">这个特效的key，要删除的时候就有用了</param>
        ///</summary>
        public void StopSightEffect(string bindPointKey, string effectKey){
            //bindPoints.RemoveBindGameObject(bindPointKey, effectKey);
        }

        public void AddAnim(cfg.Anim.Direction direction,cfg.Anim.Type type,fix speed)
        {
            WaitPlayAnim.Add(AnimPlayData.Create(direction,type,speed));
            //Log.Info("AddAnim {0} {1} {2} {3}",WaitPlayAnim.Count,direction, type,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }
        
        /// <summary>
        /// 强制定位
        /// </summary>
        /// <param name="position"></param>
        public void ApplyPosition(fix3 position){
            _unitMove.ApplyPosition(position);
            _lastFramePosition = position;
        }

        /// <summary>
        /// 回滚逻辑位置对齐上一帧，保证所有客户端同步下一帧开始相同。
        /// </summary>
        public void RollBackPosition()
        {
            _unitMove.ApplyPosition(_lastFramePosition);
        }
    }
}
