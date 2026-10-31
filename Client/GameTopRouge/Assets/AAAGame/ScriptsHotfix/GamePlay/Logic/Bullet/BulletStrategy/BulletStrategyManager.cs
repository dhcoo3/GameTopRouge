using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet.BulletStrategy
{
    public partial class BulletStrategyManager
    {
        ///<summary>
        ///子弹被创建的事件
        ///</summary>
        public delegate void BulletOnCreate(IUnit bullet);

        ///<summary>
        ///子弹命中目标的时候触发的事件
        ///<param name="bullet">发生碰撞的子弹，应该是个bulletObj，但是在unity的逻辑下，他就是个GameObject，具体数据从GameObject拿了</param>
        ///<param name="target">被击中的角色</param>
        ///<summary>
        public delegate void BulletOnHit(IUnit bullet, IUnit target);

        ///<summary>
        ///子弹在生命周期消耗殆尽之后发生的事件，生命周期消耗殆尽是因为BulletState.duration<=0，或者是因为移动撞到了阻挡。
        ///<param name="bullet">发生碰撞的子弹，应该是个bulletObj，但是在unity的逻辑下，他就是个GameObject，具体数据从GameObject拿了</param>
        ///</summary>
        public delegate void BulletOnRemoved(IUnit bullet);

        ///<summary>
        ///子弹的轨迹函数，传入一个时间点，返回出一个Vector3，作为这个时间点的速度和方向，这是个相对于正在飞行的方向的一个偏移（*speed的）
        ///正在飞行的方向按照z轴，来算，也就是说，当你只需要子弹匀速行动的时候，你可以让这个函数只做一件事情——return Vector3.forward。
        ///<param name="t">子弹飞行了多久的时间点，单位秒。</param>
        ///<param name="bullet">是当前的子弹GameObject，不建议公式中用到这个</param
        ///<param name="following">是正在跟踪的对象的GameObject，除非要做“跟踪弹”不然不建议使用</param>
        ///<return>返回这一时间点上的速度和偏移，Vector3就是正常速度正常前进</return>
        ///</summary>
        public delegate fix3 BulletTween(fix t, IUnit bullet, IUnit target);

        ///<summary>
        ///子弹在发射瞬间，可以捕捉一个GameObject作为目标，并且将这个目标传递给BulletTween，作为移动参数
        ///<param name="bullet">是当前的子弹GameObject，不建议公式中用到这个</param>
        ///<param name="targets">所有可以被选作目标的对象，这里是GameManager的逻辑决定的传递过来谁，比如这个游戏子弹只能捕捉角色作为对象，那就是只有角色的GameObject，当然如果需要，加入子弹也不麻烦</param>
        ///<return>在创建子弹的瞬间，根据这个函数获得一个GameObject作为followingTarget</return>
        ///</summary>
        public delegate IUnit BulletTargettingFunction(IUnit bullet, IUnit[] targets);
        
        public static Dictionary<string, BulletOnCreate> onCreateFunc = new Dictionary<string, BulletOnCreate>(){
            {"RecordBullet", RecordBullet},
        };
        public static Dictionary<string, BulletOnHit> onHitFunc = new Dictionary<string, BulletOnHit>(){
            {"CommonBulletHit", CommonBulletHit},
        };
        public static Dictionary<string, BulletOnRemoved> onRemovedFunc = new Dictionary<string, BulletOnRemoved>(){
            {"CommonBulletRemoved", CommonBulletRemoved},
        };
    }
}