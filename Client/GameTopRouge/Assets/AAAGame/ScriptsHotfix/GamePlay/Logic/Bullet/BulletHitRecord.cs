using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet
{
    public class BulletHitRecord
    {
        ///<summary>
        ///角色的GameObject
        ///</summary>
        public IUnit target;

        ///<summary>
        ///多久之后还能再次命中，单位秒
        ///</summary>
        public fix timeToCanHit;

        public BulletHitRecord(IUnit character, fix timeToCanHit){
            this.target = character;
            this.timeToCanHit = timeToCanHit;
        }
    }
}