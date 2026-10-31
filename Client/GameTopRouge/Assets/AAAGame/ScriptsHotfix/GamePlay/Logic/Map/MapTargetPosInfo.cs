

using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Map
{
    ///<summary>
    ///目标地点的信息
    ///</summary>
    public struct MapTargetPosInfo{
        ///<summary>
        ///是否会碰到阻碍
        ///</summary>
        public bool obstacle;

        ///<summary>
        ///建议移动到的位置
        ///</summary>
        public fix3 suggestPos;

        public MapTargetPosInfo(bool obstacle, fix3 suggestPos){
            this.obstacle = obstacle;
            this.suggestPos = suggestPos;
        }
    }
}