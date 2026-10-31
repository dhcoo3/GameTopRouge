using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Damage
{
    ///<summary>
    ///游戏中伤害值的struct，这游戏的伤害类型包括子弹伤害（治疗）、爆破伤害（治疗）、精神伤害（治疗）3种，这两种的概念更像是类似物理伤害、金木水火土属性伤害等等这种元素伤害的概念
    ///但是游戏的逻辑可能会依赖于这个伤害做一些文章，比如“受到子弹伤害减少90%”之类的
    ///</summary>
    public class DamageVal:IReference
    {
        public int bullet;
        public int explosion;
        public int mental;

        public static DamageVal Create(int bullet, int explosion = 0, int mental = 0){
            DamageVal damageVal = ReferencePool.Acquire<DamageVal>();
            damageVal.bullet = bullet;
            damageVal.explosion = explosion;
            damageVal.mental = mental;
            return damageVal;
        }

        ///<summary>
        ///统计规则，在这个游戏里伤害和治疗不能共存在一个结果里，作为抵消用
        ///<param name="asHeal">是否当做治疗来统计</name>
        ///</summary>
        public fix Overall(bool asHeal = false){
            return (asHeal == false) ? 
                (fixMath.max(0, bullet) + fixMath.max(0, explosion) + fixMath.max(0, mental)):
                (fixMath.min(0, bullet) + fixMath.min(0, explosion) + fixMath.min(0, mental));
        }

        public static DamageVal operator +(DamageVal a, DamageVal b)
        {
            return DamageVal.Create(a.bullet + b.bullet, 
                a.explosion + b.explosion, 
                a.mental + b.mental);
        }
        public static DamageVal operator *(DamageVal a, fix b){
            return DamageVal.Create(
                fixMath.roundToInt(a.bullet * b),
                fixMath.roundToInt(a.explosion * b),
                fixMath.roundToInt(a.mental * b)
            );
        }

        public void Clear()
        {
            bullet = 0;
            explosion = 0;
            mental = 0;
        }
    }
}