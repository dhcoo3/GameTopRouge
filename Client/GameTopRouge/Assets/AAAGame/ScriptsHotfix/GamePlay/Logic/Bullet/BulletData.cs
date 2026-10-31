using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet.BulletStrategy;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet
{
    ///<summary>
    ///子弹的发射信息，专门有个系统会处理这个发射信息，然后往地图上放置出子弹的GameObject
    ///所有脚本中，需要创建一个子弹，也应该传递这个结构作为产生子弹的参数
    ///</summary>
    public class BulletData:IReference
    {
        public int BulletId;
        
        ///<summary>
        ///要发射的子弹
        ///</summary>
        public BulletModel model;

        ///<summary>
        ///要发射子弹的这个人的gameObject，这里就认角色（拥有ChaState的）
        ///当然可以是null发射的，但是写效果逻辑的时候得小心caster是null的情况
        ///</summary>
        public IUnit caster;

        ///<summary>
        ///发射的坐标，y轴是无效的
        ///</summary>
        public fix3 firePosition;
        
        ///<summary>
        ///发射的角度，单位：角度
        ///</summary>
        public fix fireDegree;

        ///<summary>
        ///子弹的初速度，单位：米/秒
        ///</summary>
        public fix speed;

        ///<summary>
        ///子弹的生命周期，单位：秒
        ///子弹应该是有个生命周期的，因为如果总是不命中，也不回收总不好
        ///当然更多的还是因为有些子弹射程非常短
        ///</summary>
        public fix duration;

        ///<summary>
        ///子弹在发射瞬间，可以捕捉一个GameObject作为目标，并且将这个目标传递给BulletTween，作为移动参数
        ///<param name="bullet">是当前的子弹GameObject，不建议公式中用到这个</param>
        ///<param name="targets">所有可以被选作目标的对象，这里是GameManager的逻辑决定的传递过来谁，比如这个游戏子弹只能捕捉角色作为对象，那就是只有角色的GameObject，当然如果需要，加入子弹也不麻烦</param>
        ///<return>在创建子弹的瞬间，根据这个函数获得一个GameObject作为followingTarget</return>
        ///</summary>
        public BulletStrategyManager.BulletTargettingFunction targetFunc;

        ///<summary>
        ///子弹的轨迹函数，传入一个时间点，返回出一个Vector3，作为这个时间点的速度和方向，这是个相对于正在飞行的方向的一个偏移（*speed的）
        ///正在飞行的方向按照z轴，来算，也就是说，当你只需要子弹匀速行动的时候，你可以让这个函数只做一件事情——return Vector3.forward。
        ///如果这个值是null，就会跟return Vector3.forward一样处理，性能还高一些。
        ///虽然是vector3，但是y坐标是无效的，只是为了统一单位
        ///比如手榴弹这种会一跳一跳的可不得y变化吗？是要变化，但是这个变化归我管，这是render的事情
        ///简单地说就是做一个跳跳的Component，update（而非fixedupdate）里面去管理跳吧
        ///<param name="t">子弹飞行了多久的时间点，单位秒。</param>
        ///<return>返回这一时间点上的速度和偏移，Vector3就是正常速度正常前进</return>
        ///</summary>
        public BulletStrategyManager.BulletTween tween = null;

        ///<summary>
        ///子弹的移动轨迹是否严格遵循发射出来的角度
        ///如果是true，则子弹每一帧Tween返回的角度是按照fireDegree来偏移的
        ///如果是false，则会根据子弹正在飞的角度(transform.rotation)来算下一帧的角度
        ///</summary>
        public bool useFireDegreeForever = false;

        ///<summary>
        ///子弹创建后多久是没有碰撞的，这样比如子母弹之类的，不会在创建后立即命中目标，但绝大多子弹还应该是0的
        ///单位：秒
        ///</summary>
        public fix canHitAfterCreated = 0;

        ///<summary>
        ///子弹的一些特殊逻辑使用的参数，可以在创建子的时候传递给子弹
        ///</summary>
        public Dictionary<string, object> param;
        
        ///<summary>
        ///还能命中几次
        ///</summary>
        public int hp = 0;
        
        ///<summary>
        ///子弹已经存在了多久了，单位：秒
        ///毕竟duration是可以被重设的，比如经过一个aoe，生命周期减半了
        ///</summary>
        public fix timeElapsed = 0;
        
        ///<summary>
        ///子弹命中纪录
        ///</summary>
        public List<BulletHitRecord> hitRecords = new List<BulletHitRecord>();
        
        ///<summary>
        ///子弹正在追踪的目标，不太建议使用这个，最好保持null
        ///</summary>
        public IUnit followingTarget = null;
        
        ///<summary>
        ///角色所处阵营，阵营不同就会对打
        ///</summary>
        public int side = 0;
        
        ///<summary>
        ///子弹发射时候，caster的属性，如果caster不存在，就会是一个ChaProperty.zero
        ///在一些设计中，比如wow的技能中，技能效果是跟发出时候的角色状态有关的，之后即使获得或者取消了buff，更换了装备，数值一样不会受到影响，所以得记录这个释放当时的值
        ///</summary>
        public ChaProperty propWhileCast = ChaProperty.zero;
        
        public static BulletData Create(
            BulletModel model, IUnit caster, fix3 firePos, fix degree, fix speed, fix duration,
            fix canHitAfterCreated,
            BulletStrategyManager.BulletTween tween = null, 
            BulletStrategyManager.BulletTargettingFunction targetFunc = null,
            bool useFireDegree = false,
            Dictionary<string, object> param = null
        ){
            BulletData bulletData = ReferencePool.Acquire<BulletData>();
            bulletData.model = model;
            bulletData.caster = caster;
            bulletData.firePosition = firePos;
            bulletData.fireDegree = degree;
            bulletData.speed = speed;
            bulletData.duration = duration;
            bulletData.tween = tween;
            bulletData.useFireDegreeForever = useFireDegree;
            bulletData.targetFunc = targetFunc;
            bulletData.param = param;
            bulletData.hp = model.hitTimes;
            return bulletData;
        }

        public void Clear()
        {
            ReferencePool.Release(this.model);
            this.model = null;
            this.caster = null;
            this.firePosition = fix3.zero;
            this.fireDegree = fix.Zero;
            this.speed = fix.Zero;
            this.duration = fix.Zero;
            this.tween = null;
            this.useFireDegreeForever = false;
            this.targetFunc = null;
            this.param = null;
            this.hp = 0;
            this.side = 0;
            this.timeElapsed = fix.Zero;
            BulletId = 0;
        }
    }
}