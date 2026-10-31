using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet.BulletStrategy;
using cfg;
using cfg.Skill;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet
{
    ///<summary>
    ///子弹的模板，也是策划填表的东西，当然游戏过程中所有的子弹模板，未必都得由策划填表，也可以运行的脚本逻辑产生
    ///值得注意的是，这些信息只是构成“一个子弹”，也就是描述了这个子弹是怎样的，因此有很多数据并不属于这个结构
    ///比如子弹的飞行速度、轨迹等，这些数据其实都是子弹的发射环境决定的，同一个导弹，可能被不同的人、地形、其他任何东西发射出来
    ///这些子弹的性质是一样的，就是被填表的这些内容，但是他们可能轨迹之类都不同。
    ///</summary>
    public class BulletModel:IReference
    {
       public int id;

        ///<summary>
        ///子弹需要用的prefab，默认是Resources/Prefabs/Bullet/下的，所以这个string需要省略前半部分
        ///比如是BlueRocket0，就会创建自Resources/Prefabs/Bullet/BlueRocket0这个prefab
        ///</summary>
        public string prefab;

        ///<summary>
        ///子弹的碰撞半径，单位：米。这个游戏里子弹在逻辑世界都是圆形的，当然是这个游戏设定如此，实际策划的需求未必只能是圆形。
        ///</summary>
        public fix radius;

        ///<summary>
        ///子弹可以碰触的次数，每次碰到合理目标-1，到0的时候子弹就结束了。
        ///</summary>
        public int hitTimes;

        ///<summary>
        ///子弹碰触同一个目标的延迟，单位：秒，最小值是Time.fixedDeltaTime（每帧发生一次）
        ///</summary>
        public fix sameTargetDelay;

        ///<summary>
        ///子弹被创建的事件
        ///<param name="bullet">要创建的子弹</param>
        ///</summary>
        public BulletStrategyManager.BulletOnCreate onCreate;

        public BulletEventParam onCreateParam;

        ///<summary>
        ///子弹命中目标时候发生的事情
        ///<param name="bullet">发生碰撞的子弹，应该是个bulletObj，但是在unity的逻辑下，他就是个GameObject，具体数据从GameObject拿了</param>
        ///<param name="target">被击中的角色</param>
        ///</summary>
        public BulletStrategyManager.BulletOnHit onHit;

        ///<summary>
        ///OnHit的参数
        ///</summary>
        public BulletEventParam onHitParams;

        ///<summary>
        ///子弹生命周期结束时候发生的事情
        ///<param name="bullet">发生碰撞的子弹，应该是个bulletObj，但是在unity的逻辑下，他就是个GameObject，具体数据从GameObject拿了</param>
        ///</summary>
        public BulletStrategyManager.BulletOnRemoved onRemoved;

        ///<summary>
        ///OnRemoved的参数
        ///</summary>
        public BulletEventParam onRemovedParams;

        ///<summary>
        ///子弹的移动方式，一般来说都是飞行的，也有部分是手雷这种属于走路的
        ///</summary>
        public BulletDefine.MoveType moveType;

        ///<summary>
        ///子弹的是否碰到障碍物就爆炸，不会的话会沿着障碍物移动
        ///</summary>
        public bool removeOnObstacle;

        ///<summary>
        ///子弹是否会命中敌人
        ///</summary>
        public bool hitFoe;

        ///<summary>
        ///子弹是否会命中盟军
        ///</summary>
        public bool hitAlly;

        public static BulletModel Create(
            int id, string prefab, 
            BulletEventParam onCreate,
            BulletEventParam onHit,
            BulletEventParam onRemoved,
            BulletDefine.MoveType moveType = BulletDefine.MoveType.fly, bool removeOnObstacle = true,
            fix radius = default, int hitTimes = 1, fix sameTargetDelay = default,
            bool hitFoe = true, bool hitAlly = false
        ){
            BulletModel bulletModel = ReferencePool.Acquire<BulletModel>();
            
            bulletModel.id = id;
            bulletModel.prefab = prefab;
            
            bulletModel.onCreate = onCreate switch
            {
                RecordBullet => BulletStrategyManager.onCreateFunc["RecordBullet"],
                _ => null
            };
            
            bulletModel.onRemoved = onRemoved switch
            {
                CommonBulletRemoved => BulletStrategyManager.onRemovedFunc["CommonBulletRemoved"],
                _ => null
            };
            
            bulletModel.onHit = onHit switch
            {
                CommonBulletHit => BulletStrategyManager.onHitFunc["CommonBulletHit"],
                _ => null
            };

            bulletModel.onCreateParam = onCreate;
            bulletModel.onHitParams = onHit;
            bulletModel.onRemovedParams = onRemoved;
            
            bulletModel.radius = radius;
            bulletModel.hitTimes = hitTimes;
            bulletModel.sameTargetDelay = sameTargetDelay;
            bulletModel.moveType = moveType;
            bulletModel.removeOnObstacle = removeOnObstacle;
            bulletModel.hitAlly = hitAlly;
            bulletModel.hitFoe = hitFoe;
            return bulletModel;
        }

        public void Clear()
        {
            this.id = 0;
            this.prefab = string.Empty;
            this.onCreate = null;
            this.onRemoved = null;
            this.onHit = null;
            this.onCreateParam = null;
            this.onHitParams = null;
            this.onRemovedParams = null;
            this.radius = 0;
            this.hitTimes = 0;
            this.sameTargetDelay = 0;
            this.moveType = BulletDefine.MoveType.fly;
            this.removeOnObstacle = false;
            this.hitAlly = false;
            this.hitFoe = false;
        }
    }
}