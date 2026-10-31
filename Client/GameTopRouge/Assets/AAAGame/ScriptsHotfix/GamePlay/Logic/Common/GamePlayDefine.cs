namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Common
{
    public class GamePlayDefine
    {
        public class CreateId
        {
            public const int Unit = 100000;
            public const int Effect = 200000;
        }
        
        public class LoadPriority
        {
            public const int Scene = 1;
            public const int Role = 2;
            public const int UI = 3;
            public const int Bullet = 4;
            public const int Effect = 5;
            public const int Aoe = 6;
        }
        
        public enum RoleType
        {
            Null = 0,
            Hero = 1,
            Monster = 2
        }
    }
}