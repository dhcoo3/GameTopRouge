using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Buff;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Damage
{
    ///<summary>
    ///游戏中任何一次伤害、治疗等逻辑，都会产生一条damageInfo，由此开始正常的伤害流程，而不是直接改写hp
    ///值得一提的是，在类似“攻击时产生额外一次伤害”这种效果中，额外一次伤害也应该是一个damageInfo。
    ///</summary>
    public class DamageInfo:IReference
    {
        ///<summary>
        ///造成伤害的攻击者，当然可以是null的
        ///</summary>
        public IUnit attacker;

        ///<summary>
        ///造成攻击伤害的受击者，这个必须有
        ///</summary>
        public IUnit defender;

        ///<summary>
        ///这次伤害的类型Tag，这个会被用于buff相关的逻辑，是一个极其重要的信息
        ///这里是策划根据游戏设计来定义的，比如游戏中可能存在"frozen" "fire"之类的伤害类型，还会存在"directDamage" "period" "reflect"之类的类型伤害
        ///根据这些伤害类型，逻辑处理可能会有所不同，典型的比如"reflect"，来自反伤的，那本身一个buff的作用就是受到伤害的时候反弹伤害，如果双方都有这个buff
        ///并且这个buff没有判断damageInfo.tags里面有reflect，则可能造成“短路”，最终有一下有一方就秒了。
        ///</summary>
        public DamageDefine.DamageInfoTag[] tags;

        ///<summary>
        ///伤害值，其实伤害值是多元的，通常游戏都会有多个属性伤害，所以会用一个struct，否则就会是一个int
        ///尽管起名叫Damage，但实际上治疗也是这个，只是负数叫做治疗量，这个起名看似不严谨，对于游戏（这个特殊的业务）而言却又是严谨的
        ///</summary>
        public DamageVal DamageVal;

        ///<summary>
        ///是否暴击，这是游戏设计了有暴击的可能性存在。
        ///这里记录一个总暴击率，随着buff的不断改写，最后这个暴击率会得到一个0-1的值，代表0%-100%。
        ///最终处理的时候，会根据这个值来进行抉择，可以理解为，当这个值超过1的时候，buff就可以认为这次攻击暴击了。
        ///</summary>
        public fix criticalRate;

        /// <summary>本次伤害是否已经完成暴击判定；同一伤害只消耗一次随机数。</summary>
        private bool _criticalResolved;

        /// <summary>本次伤害缓存的暴击结果，在引用池回收时重置。</summary>
        private bool _isCritical;

        ///<summary>
        ///是否命中，是否命中与是否暴击并不直接相关，都是单独的算法
        ///作为一个射击游戏，子弹命中敌人是一种技巧，所以在这里设计命中了还会miss是愚蠢的
        ///因此这里的hitRate始终是2，就是必定命中的，之所以把这个属性放着，也是为了说明问题，而不是这个属性真的对这个游戏有用。
        ///要不要这个属性还是取决于游戏设计，比如当前游戏，本不该有这个属性。
        ///</summary>
        public fix hitRate = 2.00f;

        ///<summary>
        ///伤害的角度，作为伤害打向角色的入射角度，比如子弹，就是它当前的飞行角度
        ///</summary>
        public fix degree;

        ///<summary>
        ///伤害过后，给角色添加的buff
        ///</summary>
        public List<AddBuffInfo> addBuffs = new List<AddBuffInfo>();

        /// <summary>
        /// 创建一次独立伤害，并接管伤害数值对象的回收；数值对象不能同时交给其他伤害。
        /// </summary>
        /// <param name="attacker">攻击来源，环境伤害时允许为空。</param>
        /// <param name="defender">伤害或治疗的接收单位。</param>
        /// <param name="damageVal">本次伤害独占的数值对象，不允许为空。</param>
        /// <param name="damageDegree">伤害入射角度，单位为度。</param>
        /// <param name="baseCriticalRate">暴击率，零及以下不暴击，一及以上必定暴击。</param>
        /// <param name="tags">伤害类型标签，允许为空，并复制后保存。</param>
        /// <returns>由伤害流程持有、结算结束后回收的伤害信息。</returns>
        public static DamageInfo Create(IUnit attacker, IUnit defender, DamageVal damageVal, fix damageDegree, fix baseCriticalRate, DamageDefine.DamageInfoTag[] tags)
        {
            if (damageVal == null) throw new ArgumentNullException(nameof(damageVal));

            DamageInfo damageInfo = ReferencePool.Acquire<DamageInfo>();
            damageInfo.attacker = attacker;
            damageInfo.defender = defender;
            damageInfo.DamageVal = damageVal;
            damageInfo.criticalRate = baseCriticalRate;
            damageInfo.degree = damageDegree;
            damageInfo.tags = tags == null
                ? Array.Empty<DamageDefine.DamageInfoTag>()
                : (DamageDefine.DamageInfoTag[])tags.Clone();
            return damageInfo;
        }

        /// <summary>
        /// 首次结算时判定暴击并缓存，后续伤害查询和击杀判断复用同一结果。
        /// </summary>
        /// <returns>本次伤害是否暴击。</returns>
        internal bool ResolveCritical()
        {
            if (!_criticalResolved)
            {
                fix roll = GamePlayFacade.Instance.Random.Random01();
                _isCritical = criticalRate >= fix.One || criticalRate > fix.Zero && roll < criticalRate;
                _criticalResolved = true;
            }

            return _isCritical;
        }

        /// <summary>计算伤害或治疗值；重复查询复用暴击结果，不再消耗随机数。</summary>
        /// <param name="asHeal">是否只统计治疗分量。</param>
        /// <returns>取整后的伤害正值或治疗负值。</returns>
        public int DamageValue(bool asHeal){
            return DamageDefine.CommonScripts.DamageValue(this, asHeal);
        }

        ///<summary>
        ///根据tag判断，这是否是一次治疗，那些tag算是治疗，当然是策划定义了才算数的
        ///</summary>
        public bool isHeal(){
            for (int i = 0; i < this.tags.Length; i++){
                if (tags[i] == DamageDefine.DamageInfoTag.directHeal || tags[i] == DamageDefine.DamageInfoTag.periodHeal){
                    return true;
                }
            }
            return false;
        }

        ///<summary>
        ///根据tag决定是否要播放受伤动作，当然你还可以是根据类型决定不同的受伤动作
        ///</summary>
        public bool requireDoHurt(){
            for (int i = 0; i < this.tags.Length; i++){
                if (tags[i] == DamageDefine.DamageInfoTag.directDamage){
                    return true;
                }
            }
            return false;
        }

        /// <summary>登记结算后执行的 Buff 请求，只持有引用，不接管请求及模板的回收。</summary>
        /// <param name="buffInfo">包含实际目标的 Buff 添加请求；空请求会被忽略。</param>
        public void AddBuffToCha(AddBuffInfo buffInfo){
            if (buffInfo != null) addBuffs.Add(buffInfo);
        }

        /// <summary>归还独占数值并清空所有事件引用，避免对象池复用残留。</summary>
        public void Clear()
        {
            if (DamageVal != null) ReferencePool.Release(DamageVal);
            DamageVal = null;
            attacker = null;
            defender = null;
            tags = null;
            criticalRate = fix.Zero;
            hitRate = fix.Two;
            degree = fix.Zero;
            _criticalResolved = false;
            _isCritical = false;
            addBuffs.Clear();
        }
    }
}
