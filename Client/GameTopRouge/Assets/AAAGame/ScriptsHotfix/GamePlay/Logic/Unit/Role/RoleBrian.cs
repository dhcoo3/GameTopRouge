using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Buff;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role
{
    public class RoleBrian:IBrian
    {
        private RoleUnit _roleUnit;
        
        public RoleState RoleState
        {
            get => _roleUnit.RoleState + _roleUnit.TimelineControlState;
        }
        
        //来自操作或者ai的移动请求信息
        public fix3 MoveOrder = fix3.zero;
        
        ///<summary>
        ///角色主动期望的移动方向
        ///</summary>
        public fix MoveDegree{
            get{
                return _wishToMoveDegree;
            }
        }
        
        private fix _wishToMoveDegree = 0.00f;
        
        ///<summary>
        ///角色主动期望的面向
        ///</summary>
        public fix FaceDegree{
            get{
                return _wishToFaceDegree;
            }
        }
        
        private fix _wishToFaceDegree = 0.00f;
        
        //来自操作或者ai的旋转角度请求
        public fix RotateToOrder;
        
        //来自强制发生的位移信息，通常是技能效果等导致的，比如翻滚、被推开等
        private List<MovePreorder> forceMove = new List<MovePreorder>();
        
        //来自强制执行的旋转角度
        private List<fix> forceRotate = new List<fix>();
        
        //收到的来自各方的播放动画的请求
        private List<string> animOrder = new List<string>();
        
        
        public static RoleBrian CreateRoleBrian(RoleUnit roleUnit)
        {
            RoleBrian roleBrian = ReferencePool.Acquire<RoleBrian>();
            roleBrian._roleUnit = roleUnit;
            return roleBrian;
        }
        
        public void Clear()
        {
            _roleUnit = null;
            MoveOrder = fix3.zero;
            RotateToOrder = 0;
            _wishToMoveDegree = 0;
            _wishToFaceDegree = 0;
            forceMove.Clear();
            forceRotate.Clear();
            animOrder.Clear();
        }

        public void LogicUpdate(fix deltaTime)
        {
            if (RoleState.IsDeath)
            {
                
                _wishToFaceDegree = _roleUnit.RoleBehaviour.Rotation.eulerAngles.y * 180.00f / fixMath.PI;
                _wishToMoveDegree = _wishToFaceDegree;
                return;
            }
            
            //如果角色没死，做这些事情：

            //无敌时间减少
            if (RoleState.IsImmune)
            {
                RoleState.ImmuneTime -= deltaTime;
            }
            
            //技能冷却时间
            for (int i = 0; i < _roleUnit.Data.Skills.Count; i++){
                if (_roleUnit.Data.Skills[i].Cooldown > 0){
                    _roleUnit.Data.Skills[i].Cooldown -= deltaTime;
                }
            }
            
            //对身上的buff进行管理
            List<BuffObj> toRemove = new List<BuffObj>();
            for (int i = 0; i < _roleUnit.Data.Buffs.Count; i++)
            {
                BuffObj buffObj = _roleUnit.Data.Buffs[i];
                if (buffObj.permanent == false)
                {
                    buffObj.duration -= deltaTime;
                }
                
                buffObj.timeElapsed += deltaTime;

                if (buffObj.model.tickTime > 0 && buffObj.model.onTick != null){
                    //float取模不精准，所以用x1000后的整数来
                    if (fixMath.roundToInt(buffObj.timeElapsed * 1000) % fixMath.roundToInt(buffObj.model.tickTime * 1000) == 0){
                        buffObj.model.onTick(buffObj);
                        buffObj.ticked += 1;
                    }
                }

                //只要duration <= 0，不管是否是permanent都移除掉
                if (buffObj.duration <= 0 ||buffObj.stack <= 0){
                    if (buffObj.model.onRemoved != null){
                        buffObj.model.onRemoved(buffObj);
                    }
                    toRemove.Add(buffObj);
                }
            }
               
            if (toRemove.Count > 0)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    _roleUnit.Data.Buffs.Remove(toRemove[i]);
                }
                AttrRecheck();
            }
            
            bool tryRun = RoleState.canMove && MoveOrder != fix3.zero;

            bool wishToMove = MoveOrder != fix3.zero;
            if (wishToMove)
            {
                _wishToMoveDegree = fixMath.atan2(MoveOrder.x, MoveOrder.y) * 180 / fixMath.PI;
            }
               
            CalcMove(deltaTime);
            CalcRotation();
            CalcAnimation(tryRun);
            //MoveOrder = fix3.zero;
        }
        
        ///<summary>
        ///重新计算所有属性，并且获得一个最终属性
        ////其实这个应该走脚本函数返回，抛给脚本函数多个ChaProperty，由脚本函数运作他们的运算关系，并返回结果
        ///</summary>
        private void AttrRecheck(){
            _roleUnit.RoleState.Origin();
            _roleUnit.Data.Prop.Zero();

            ChaProperty[] buffProp = _roleUnit.Data.BuffProp;
            List<BuffObj> buffs = _roleUnit.Data.Buffs;
            for (var i = 0; i < buffProp.Length; i++) buffProp[i].Zero();
            for (int i = 0; i < buffs.Count; i++){
                for (int j = 0; j < fixMath.min(buffProp.Length, buffs[i].model.propMod.Length); j++){
                    buffProp[j] += buffs[i].model.propMod[j] * buffs[i].stack;
                }
                _roleUnit.RoleState += buffs[i].model.stateMod;
            }
            
            _roleUnit.Data.Prop = (_roleUnit.Data.BaseProp + _roleUnit.Data.EquipmentProp + buffProp[0]) * buffProp[1];
            //_roleUnit.RoleBehaviour.UnitMove.bodyRadius =_roleUnit.Data._prop.bodyRadius;
        }

        private void CalcMove(fix timePassed)
        {
            if (RoleState.canMove == false)
            {
                MoveOrder = fix3.zero;
            }
            
            int fmIndex = 0;
            
            while (fmIndex < forceMove.Count){
                MoveOrder += forceMove[fmIndex].VeloInTime(timePassed);
                if (forceMove[fmIndex].duration <= 0)
                {
                    forceMove.RemoveAt(fmIndex);
                }else{
                    fmIndex++;
                }
            }
            
            _roleUnit.RoleBehaviour.UnitMove.MoveBy(MoveOrder);
            //forceMove.Clear();
        }
        
        private void CalcRotation()
        {
            bool wishToMove = MoveOrder != fix3.zero;
         
            if(wishToMove)
            {
                //如果移动了，则根据移动方向，计算面向角度
                fix angle = fixMath.deg(fixMath.atan2(MoveOrder.y, MoveOrder.x));
              
                if (angle < 0)
                {
                    angle += 360;
                }
                
                Utils.FormatDirection(ref angle);
                RotateToOrder = angle;
            }
            
            if (!wishToMove)
            {
                _wishToMoveDegree = _wishToFaceDegree;
            }
            
            _wishToFaceDegree = RotateToOrder;
            
            //然后是旋转信息
            if (RoleState.canRotate == false)
            {
                RotateToOrder = _wishToFaceDegree;
            }
            
            for (int i = 0; i < forceRotate.Count; i++)
            {
                //这里全是增量，而不是设定为，所以可以直接加
                RotateToOrder += forceRotate[i];
            }
            
            _roleUnit.RoleBehaviour.UnitRotate.RotateTo(RotateToOrder);
            
            forceRotate.Clear();
        }
        
        //再是动画处理
        private void CalcAnimation(bool tryRun)
        {           
            cfg.Anim.Direction direction = Utils.GetEightDirection(_wishToFaceDegree);
            
            if (tryRun == false)
            {
                _roleUnit.RoleBehaviour.AddAnim(direction,cfg.Anim.Type.Wait,_roleUnit.Data.ActionSpeed);
            }
            else
            {
                _roleUnit.RoleBehaviour.AddAnim(direction,cfg.Anim.Type.Run,_roleUnit.Data.ActionSpeed);
            }
        }
        
        ///<summary>
        ///为角色添加buff，当然，删除也是走这个的
        ///</summary>
        public void AddBuff(AddBuffInfo buff){
            List<IUnit> bCaster = new List<IUnit>();
            
            if (buff.caster != null)
            {
                bCaster.Add(buff.caster);
            }
            
            List<BuffObj> hasOnes = _roleUnit.Data.GetBuffById(buff.buffModel.id, bCaster);
            int modStack = (int)fixMath.min(buff.addStack, buff.buffModel.maxStack);
            bool toRemove = false;
            BuffObj toAddBuff = null;
        
            if (hasOnes.Count > 0){
                //已经存在
                hasOnes[0].buffParam = new Dictionary<string, object>();
                if (buff.buffParam != null){
                    foreach (KeyValuePair<string, object> kv in buff.buffParam)
                    {
                        hasOnes[0].buffParam[kv.Key] = kv.Value;
                    };
                }
            
                hasOnes[0].duration = (buff.durationSetTo == true) ? buff.duration : (buff.duration + hasOnes[0].duration);
                int afterAdd = hasOnes[0].stack + modStack;
                modStack = afterAdd >= hasOnes[0].model.maxStack ? 
                    (hasOnes[0].model.maxStack - hasOnes[0].stack) : 
                    (afterAdd <= 0 ? (0 - hasOnes[0].stack) : modStack);
                hasOnes[0].stack += modStack;
                hasOnes[0].permanent = buff.permanent;
                toAddBuff = hasOnes[0];
                toRemove = hasOnes[0].stack <= 0;
            }
            else
            {
                //新建
                toAddBuff = BuffObj.Create(
                    buff.buffModel,
                    buff.caster,
                    _roleUnit,
                    buff.duration,
                    buff.addStack,
                    buff.permanent,
                    buff.buffParam
                );
                _roleUnit.Data.Buffs.Add(toAddBuff);
                _roleUnit.Data.Buffs.Sort((a, b)=>{
                    return a.model.priority.CompareTo(b.model.priority);
                });
            }
            if (toRemove == false && buff.buffModel.onOccur != null){
                buff.buffModel.onOccur(toAddBuff, modStack);
            }
            AttrRecheck();
        }
    }
}
