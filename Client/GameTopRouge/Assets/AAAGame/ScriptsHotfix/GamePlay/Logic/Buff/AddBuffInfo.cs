using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Buff
{
    ///<summary>
    ///用于添加一条buff的信息
    ///</summary>
    public class AddBuffInfo:IReference
    {
        ///<summary>
        ///buff的负责人是谁，可以是null
        ///</summary>
        public IUnit caster;

        ///<summary>
        ///buff要添加给谁，这个必须有
        ///</summary>
        public IUnit target;

        ///<summary>
        ///buff的model，这里当然可以从数据里拿，也可以是逻辑脚本现生成的
        ///</summary>
        public BuffModel buffModel;

        ///<summary>
        ///要添加的层数，负数则为减少
        ///</summary>
        public int addStack;

        ///<summary>
        ///关于时间，是改变还是设置为, true代表设置为，false代表改变
        ///</summary>
        public bool durationSetTo;

        ///<summary>
        ///是否是一个永久的buff，即便=true，时间设置也是有意义的，因为时间如果被减少到0以下，即使是永久的也会被删除
        ///</summary>
        public bool permanent;

        ///<summary>
        ///时间值，设置为这个值，或者加上这个值，单位：秒
        ///</summary>
        public fix duration;

        ///<summary>
        ///buff的一些参数，这些参数是逻辑使用的，比如wow中牧师的盾还能吸收多少伤害，就可以记录在buffParam里面
        ///</summary>
        public Dictionary<string, object> buffParam;

        public static AddBuffInfo Create(
            BuffModel model, IUnit caster, IUnit target,
            int stack, fix duration, bool durationSetTo = true,
            bool permanent = false,
            Dictionary<string, object> buffParam = null
        ){
            AddBuffInfo addBuffInfo = ReferencePool.Acquire<AddBuffInfo>();
            
            addBuffInfo.buffModel = model;
            addBuffInfo.caster = caster;
            addBuffInfo.target = target;
            addBuffInfo.addStack = stack;
            addBuffInfo.duration = duration;
            addBuffInfo.durationSetTo = durationSetTo;
            addBuffInfo.buffParam = buffParam;
            addBuffInfo.permanent = permanent;

            return addBuffInfo;
        }
        
        public void Clear()
        {
            this.buffModel = null;
            this.caster = null;
            this.target = null;
            this.addStack = 0;
            this.duration = fix.Zero;
            this.durationSetTo = false;
            this.buffParam = null;
            this.permanent = false;
        }
    }
}