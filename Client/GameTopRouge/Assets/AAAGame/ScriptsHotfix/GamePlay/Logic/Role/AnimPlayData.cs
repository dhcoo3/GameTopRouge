using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Role
{
    public class AnimPlayData:IReference
    {
        public cfg.Anim.Direction Direction;
        public cfg.Anim.Type AnimType;
        public fix TimeScale = 1.0f;

        public static AnimPlayData Create(cfg.Anim.Direction direction, cfg.Anim.Type animType,fix timeScale)
        {
            AnimPlayData animPlayData = ReferencePool.Acquire<AnimPlayData>();
            animPlayData.Direction = direction;
            animPlayData.AnimType = animType;
            animPlayData.TimeScale = timeScale;;
            return animPlayData;
        }
        
        public void Clear()
        {
            Direction = cfg.Anim.Direction.Null;
            AnimType = cfg.Anim.Type.Null;
            TimeScale = 1.0f;
        }
    }
}