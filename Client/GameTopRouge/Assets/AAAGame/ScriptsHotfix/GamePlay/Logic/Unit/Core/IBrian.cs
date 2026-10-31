using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core
{
    public interface IBrian:IReference
    {
        fix FaceDegree { get; }
        
        fix MoveDegree { get;}
        
        void LogicUpdate(fix deltaTime);
    }
}