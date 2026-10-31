using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Bullet
{
    public class BulletUnit:IUnit
    {
        #region interface
        public IBrian Brian => _brian;
        public IBehaviour Behaviour =>_behaviour;
        public MoveType MoveType => MoveType.ground;
        public fix BodyRadius => 0.25f;
        public fix3 CollisionOffset => fix3.zero;
        public bool SmoothMove => false;
        public bool IgnoreBorder => false;
        
        #endregion
        
        BulletBrian _brian;
        
        BulletBehaviour _behaviour;
        
        private BulletData _data;
        public BulletData Data => _data;
        
        public bool HasEntity = false;
        
        public static BulletUnit Create(BulletData bulletData)
        {
            BulletUnit bullet = ReferencePool.Acquire<BulletUnit>();
            bullet._data = bulletData;
            bullet._brian = BulletBrian.CreateBrian(bullet);
            bullet._behaviour = BulletBehaviour.CreateBehaviour(bullet);
            return bullet;
        }

        public void Clear()
        {
            HasEntity = false;
            ReferencePool.Release(_behaviour);
            ReferencePool.Release(_brian);
            ReferencePool.Release(_data);
            _behaviour = null;
            _brian = null;
            _data = null;
        }

        public bool IsDeath()
        {
            return false;
        }

        public void LogicUpdate(fix fixedDeltaTime)
        {
            if(!HasEntity) return;
            _brian?.LogicUpdate(fixedDeltaTime);
            _behaviour?.LogicUpdate(fixedDeltaTime);
        }
    }
}
