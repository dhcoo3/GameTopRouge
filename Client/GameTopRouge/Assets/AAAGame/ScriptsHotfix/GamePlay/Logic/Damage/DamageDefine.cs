namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Damage
{
    /// <summary>伤害类型及全局伤害计算规则。</summary>
    public class DamageDefine
    {
        ///<summary>
        ///这里的函数都是程序暴露给策划的脚本，这些脚本是游戏中一些“规则级”的，比如升级经验等，都是流程中一些关键的函数
        ///
        ///</summary>
        public class CommonScripts{
            /// <summary>暴击伤害倍率，沿用当前玩法的 1.8 倍规则。</summary>
            private static readonly fix CriticalDamageMultiplier = (fix)1.80f;

            /// <summary>根据伤害分量和本事件缓存的暴击结果计算结算数值。</summary>
            /// <param name="damageInfo">已完成攻击及受击修正的伤害信息。</param>
            /// <param name="asHeal">是否只统计治疗分量。</param>
            /// <returns>向上取整的伤害正值或治疗负值。</returns>
            public static int DamageValue(DamageInfo damageInfo, bool asHeal = false)
            {
                fix multiplier = damageInfo.ResolveCritical() ? CriticalDamageMultiplier : fix.One;
                return fixMath.ceilToInt(damageInfo.DamageVal.Overall(asHeal) * multiplier);
            }
        }
        
       

        ///<summary>
        ///伤害类型的Tag元素，因为DamageInfo的逻辑需要的严谨性远高于其他的元素，所以伤害类型应该是枚举数组的
        ///这个伤害类型不应该是类似 火伤害、水伤害、毒伤害之类的，如果是这种元素伤害，那么应该是在damage做文章，即damange不是int而是一个struct或者array或者dictionary，然后DamageValue函数里面去改最终值算法
        ///这里的伤害类型，指的还是比如直接伤害、反弹伤害、dot伤害等等，一些在逻辑处理流程会有不同待遇的东西，比如dot伤害可能不会触发一些效果等，当然这最终还是取决于策划设计的规则。
        ///</summary>
        public enum DamageInfoTag{
            directDamage = 0,   //直接伤害
            periodDamage = 1,   //间歇性伤害
            reflectDamage = 2,  //反噬伤害
            directHeal = 10,    //直接治疗
            periodHeal = 11,    //间歇性治疗
            monkeyDamage = 9999    //这个类型的伤害在目前这个demo中没有意义，只是告诉你可以随意扩展，仅仅比string严肃些。
        }
    }
}
