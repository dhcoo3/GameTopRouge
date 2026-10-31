using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core
{
    public interface IBehaviour:IReference
    {
        fix3 Position { get; set; }
        Quaternion Rotation { get; set; }
        void LogicUpdate(fix deltaTime);
    }
}