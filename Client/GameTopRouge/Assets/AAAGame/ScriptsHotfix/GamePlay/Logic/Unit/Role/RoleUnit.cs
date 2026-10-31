using AAAGame.ScriptsHotfix.GamePlay.Logic.Buff;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Damage;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Skill;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role
{
    public class RoleUnit:IUnit
    {
        #region interface
        public IBrian Brian => _brian;
        public IBehaviour Behaviour =>_behaviour;
        public MoveType MoveType => MoveType.ground;
        public fix BodyRadius => _data.Prop.BodyRadius;
        public fix3 CollisionOffset => _data.CollisionOffset;
        public bool SmoothMove => true;
        public bool IgnoreBorder => false;
        
        #endregion
        
        private RoleBrian _brian;
        
        private RoleBehaviour _behaviour;
        public RoleBehaviour RoleBehaviour => _behaviour;
        
        private RoleData _data;
        public RoleData Data => _data;
        public int RoleId => _data.RoleId;

        ///<summary>
        //角色最终的可操作性状态
        ///</summary>
        public RoleState RoleState;
        
        ///<summary>
        ///GameTimeline专享的ChaControlState
        ///</summary>
        public RoleState TimelineControlState = new RoleState(true,true,true);
       
        
        public bool HasEntity = false;

        public static RoleUnit Create(RoleData roleData)
        {
            RoleUnit roleUnit = ReferencePool.Acquire<RoleUnit>();
            roleUnit.RoleState = new RoleState(true,true,true);
            roleUnit.TimelineControlState = new RoleState(true,true,true);
            roleUnit._data = roleData;
            roleUnit._brian = RoleBrian.CreateRoleBrian(roleUnit);
            roleUnit._behaviour = RoleBehaviour.CreateRoleBehaviour(roleUnit);
            return roleUnit;
        }

        public void LogicUpdate(fix deltaTime)
        {
            if(!HasEntity) return;
            // 输入已在 InputProxy 中按帧应用；从上一逻辑帧的位置恢复后再模拟当前帧。
            _behaviour?.RollBackPosition();
            _brian?.LogicUpdate(deltaTime);
            _behaviour?.LogicUpdate(deltaTime);
            _behaviour?.SaveFramePosition();
        }

        public void Clear()
        {
            ReferencePool.Release(_data);
            ReferencePool.Release(_brian);
            ReferencePool.Release(_behaviour);
            _data = null;
            _brian = null;
            _behaviour = null;
        }

        public void SetOperator(InputObj inputObj)
        {
            if (inputObj.Key == KeyCode.None)
            {
                OrderMove(new fix3(inputObj.Horizontal,inputObj.Vertica,0));
            }
            else
            {
                if (inputObj.Key == KeyCode.K)
                {
                    UseSkill(100001);
                }
                else if (inputObj.Key == KeyCode.L)
                {
                    UseSkill(100004);
                }
            }

        }
        
        ///<summary>
        ///命令移动
        ///<param name="move">移动力</param>
        ///</summary>
        public void OrderMove(fix3 move){
            if(RoleState.IsDeath) return;
            _brian.MoveOrder.x = move.x * _data.MoveSpeed;
            _brian.MoveOrder.y = move.y * _data.MoveSpeed;
        }
        
        ///<summary>
        ///命令旋转到多少度
        ///<param name="degree">旋转目标</param>
        ///</summary>
        public void OrderRotateTo(fix degree){
            if(RoleState.IsDeath) return;
            _brian.RotateToOrder = degree;
        }
        
        ///<summary>
        ///学习某个技能
        ///<param name="skillModel">技能的模板</param>
        ///<param name="level">技能等级</param>
        ///</summary>
        public void LearnSkill(SkillModel skillModel, int level = 1)
        {
            if(RoleState.IsDeath) return;
            
            _data.Skills.Add(new SkillObj(skillModel, level));
            
            if (skillModel.buff != null)
            {
                for (int i = 0; i < skillModel.buff.Length; i++){
                    AddBuffInfo abi = skillModel.buff[i];
                    abi.permanent = true;
                    abi.duration = 10;
                    abi.durationSetTo = true;
                    AddBuff(abi);
                }
            }
        }

        public void UseSkill(int id)
        {
            if(RoleState.IsDeath) return;
            _behaviour.UseSkill(id);
        }

        public void AddBuff(AddBuffInfo buff)
        {
            if(RoleState.IsDeath) return;
            _brian.AddBuff(buff);
        }
        
        ///<summary>
        ///判断这个角色是否会被这个damageInfo所杀
        ///<param name="dInfo">要判断的damageInfo</param>
        ///<return>如果是true代表角色可能会被这次伤害所杀</return>
        ///</summary>
        public bool CanBeKilledByDamageInfo(DamageInfo damageInfo){
            if (RoleState.ImmuneTime > 0 || damageInfo.isHeal() == true)
            {
                return false;
            }
            
            int dValue = damageInfo.DamageValue(false);
            
            return dValue >= _data.Resource.hp;
        }

        public void ModResource(ChaResource value)
        {
            _behaviour.ModResource(value);
        }

        public bool IsDeath()
        {
            return RoleState.IsDeath;
        }
    }
}
