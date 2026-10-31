using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe.AoeStrategy;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe
{
    ///AoE发射器，创建aoe依赖的数据都在这里了
    public class AoeData:IReference
    {
        public int AoeId;
        
        ///<summary>
        ///要释放的aoe
        ///</summary>
        public AoeModel model;

        ///<summary>
        ///释放的中心坐标
        ///</summary>
        public fix3 position;

        ///<summary>
        ///释放aoe的角色的
        ///</summary>
        public IUnit caster;

        ///<summary>
        ///aoe的半径，单位：米
        ///目前这游戏的设计中，aoe只有圆形，所以只有一个半径，也不存在角度一说，如果需要可以扩展
        ///</summary>
        public fix radius;

        ///<summary>
        ///aoe存在的时间，单位：秒
        ///</summary>
        public fix duration;

        ///<summary>
        ///aoe已经存在过的时间，单位：秒
        ///</summary>
        public fix timeElapsed = 0;
        
        ///<summary>
        ///aoe的角度
        ///</summary>
        public fix degree;

        ///<summary>
        ///aoe移动轨迹函数
        ///</summary>
        public AoeStrategyManager.AoeTween tween;
        public object[] tweenParam = new object[0];

        ///<summary>
        ///aoe的传入参数，比如可以吸收次数之类的
        ///</summary>
        public Dictionary<string, object> param = new Dictionary<string, object>();

        ///<summary>
        ///aoe的轨迹运行了多少时间了，单位：秒
        ///<summary>
        public fix tweenRunnedTime = fix.Zero;
        
        ///<summary>
        ///是否被视作刚创建
        ///</summary>
        public bool justCreated = true;
        
        ///<summary>
        ///现在aoe范围内的所有角色单位
        ///</summary>
        public List<IUnit> characterInRange = new List<IUnit>();
        
        ///<summary>
        ///现在aoe范围内的所有子弹的gameobject
        ///</summary>
        public List<IUnit> bulletInRange = new List<IUnit>();
        
        ///<summary>
        ///创建时角色的属性
        ///</summary>
        public ChaProperty propWhileCreate;
        
        ///<summary>
        ///发射的角度，单位：角度
        ///</summary>
        public fix fireDegree;
        
        public static AoeData Create(
            AoeModel model, IUnit caster, fix3 position, fix radius, fix duration, fix degree, 
            AoeStrategyManager.AoeTween tween = null, object[] tweenParam = null, Dictionary<string, object> aoeParam = null
        ){
            AoeData aoeData = ReferencePool.Acquire<AoeData>();
            aoeData.model = model;
            aoeData.caster = caster;
            aoeData.position = position;
            aoeData.radius = radius;
            aoeData.duration = duration;
            aoeData.degree = degree;
            aoeData.tween = tween;
            
            if (caster is RoleUnit roleUnit)
            {
                aoeData.propWhileCreate = roleUnit.Data.Prop;
            }
            
            if (aoeParam != null) aoeData.param = aoeParam;
            if (tweenParam != null) aoeData.tweenParam = tweenParam;
            return aoeData;
        }

        public void Clear()
        {
            ReferencePool.Release(this.model);
            model = null;
            AoeId = 0;
            position = fix3.zero;
            caster = null;
            justCreated = true;
            radius = fix.Zero;
            duration = fix.Zero;
            timeElapsed = fix.Zero;
            degree = fix.Zero;
            tweenRunnedTime = fix.Zero;
            fireDegree = fix.Zero;
        }
    }
}