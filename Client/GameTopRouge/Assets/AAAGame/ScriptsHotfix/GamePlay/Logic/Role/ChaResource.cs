using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Role
{
    ///<summary>
    ///角色的资源类属性，比如hp，mp等都属于这个
    ///</summary>
    public class ChaResource
    {
        ///<summary>
        ///当前生命
        ///</summary>
        public int hp;

        ///<summary>
        ///当前弹药量，在这游戏里就相当于mp了
        ///</summary>
        public int ammo;

        ///<summary>
        ///当前耐力，耐力是一个百分比消耗，实时恢复的概念，所以上限按规则就是100了，这里是现有多少
        ///</summary>
        public int stamina;

        public ChaResource(int hp, int ammo = 0, int stamina = 0){
            this.hp = hp;
            this.ammo = ammo;
            this.stamina = stamina;
        }

        ///<summary>
        ///是否足够
        ///</summary>
        public bool Enough(ChaResource requirement){
            return (
                this.hp >= requirement.hp &&
                this.ammo >= requirement.ammo &&
                this.stamina >= requirement.stamina
            );
        }

        public static ChaResource operator +(ChaResource a, ChaResource b){
            return new ChaResource(
                a.hp + b.hp,
                a.ammo + b.ammo,
                a.stamina + b.stamina
            );
        }
        public static ChaResource operator *(ChaResource a, fix b){
            return new ChaResource(
                fixMath.floorToInt(a.hp * b),
                fixMath.floorToInt(a.ammo * b),
                fixMath.floorToInt(a.stamina * b)
            );
        }
        public static ChaResource operator *(fix a, ChaResource b){
            return new ChaResource(
                fixMath.floorToInt(b.hp * a),
                fixMath.floorToInt(b.ammo * a),
                fixMath.floorToInt(b.stamina * a)
            );
        }
        public static ChaResource operator -(ChaResource a, ChaResource b){
            return a + b * (-1);
        }

        public static ChaResource Null = new ChaResource(0);
    }
}