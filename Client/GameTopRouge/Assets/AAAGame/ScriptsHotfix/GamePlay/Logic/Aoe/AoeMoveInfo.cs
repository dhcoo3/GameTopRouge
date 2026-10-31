using System.Numerics;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe
{
    public class AoeMoveInfo
    {
        ///<summary>
        ///此时此刻的移动方式
        ///</summary>
        public MoveType moveType;

        ///<summary>
        ///此时aoe移动的力量，在这个游戏里，y坐标依然无效，如果要做手雷一跳一跳的，请使用其他的component绑定到特效的gameobject上，而非aoe的
        ///</summary>
        public fix3 velocity;

        ///<summary>
        ///aoe的角度变成这个值
        ///</summary>
        public fix rotateToDegree;

        public AoeMoveInfo(MoveType moveType, fix3 velocity, fix rotateToDegree){
            this.moveType = moveType;
            this.velocity = velocity;
            this.rotateToDegree = rotateToDegree;
        }
    }
}