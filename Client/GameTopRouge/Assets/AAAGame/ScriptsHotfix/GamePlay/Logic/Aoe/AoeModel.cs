using AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe.AoeStrategy;
using cfg.Skill;
using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe
{
    public class AoeModel:IReference
    {
        public int id;

        ///<summary>
        ///aoe的视觉特效，如果是空字符串，就不会添加视觉特效
        ///这里需要的是在Prefabs/下的路径，因为任何东西都可以是视觉特效
        ///</summary>
        public string prefab;

        ///<summary>
        ///aoe是否碰撞到阻挡就摧毁了（removed），如果不是，移动就是smooth的，如果移动的话……
        ///</summary>
        public bool removeOnObstacle;

        ///<summmary>
        ///aoe的tag
        ///</summary>
        public string[] tags;

        ///<summary>
        ///aoe每一跳的时间，单位：秒
        ///如果这个时间小于等于0，或者没有onTick，则不会执行aoe的onTick事件
        ///</summary>
        public fix tickTime;

        ///<summary>
        ///aoe创建时的事件
        ///</summary>
        public AoeStrategyManager.AoeOnCreate onCreate;
        public AoeEvent onCreateParams;

        ///<summary>
        ///aoe每一跳的事件，如果没有，就不会发生每一跳
        ///</summary>
        public AoeStrategyManager.AoeOnTick onTick;
        public AoeEvent onTickParams;

        ///<summary>
        ///aoe结束时的事件
        ///</summary>
        public AoeStrategyManager.AoeOnRemoved onRemoved;
        public AoeEvent onRemovedParams;

        ///<summary>
        ///有角色进入aoe时的事件，onCreate时候位于aoe范围内的人不会触发这个，但是在onCreate里面会已经存在
        ///</summary>
        public AoeStrategyManager.AoeOnCharacterEnter onChaEnter;
        public AoeEvent onChaEnterParams;

        ///<summary>
        ///有角色离开aoe结束时的事件
        ///</summary>
        public AoeStrategyManager.AoeOnCharacterLeave onChaLeave;
        public AoeEvent onChaLeaveParams;

        ///<summary>
        ///有子弹进入aoe时的事件，onCreate时候位于aoe范围内的子弹不会触发这个，但是在onCreate里面会已经存在
        ///</summary>
        public AoeStrategyManager.AoeOnBulletEnter onBulletEnter;
        public AoeEvent onBulletEnterParams;

        ///<summary>
        ///有子弹离开aoe时的事件
        ///</summary>
        public AoeStrategyManager.AoeOnBulletLeave onBulletLeave;
        public AoeEvent onBulletLeaveParams;

        public static AoeModel Create(
            int id, string prefab, string[] tags, fix tickTime, bool removeOnObstacle,
            AoeEvent onCreate,
            AoeEvent onRemoved, 
            AoeEvent onTick,
            AoeEvent onChaEnter,
            AoeEvent onChaLeave, 
            AoeEvent onBulletEnter, 
            AoeEvent onBulletLeave
        ){
            AoeModel aoeModel = ReferencePool.Acquire<AoeModel>();
            aoeModel.id = id;
            aoeModel.prefab = prefab;
            aoeModel.tags = tags;
            aoeModel.tickTime = tickTime;
            aoeModel.removeOnObstacle = removeOnObstacle;

            if (onCreate != null)
            {
                aoeModel.onCreate = onCreate switch
                {
                    cfg.Skill.CreateAoeSightEffect => AoeStrategyManager.onCreateFunc["CreateAoeSightEffect"],
                    cfg.Skill.DoDamageToCreate => AoeStrategyManager.onCreateFunc["DoDamageToCreate"],
                    _ => null
                };
                aoeModel.onCreateParams = onCreate;
            }

            if (onRemoved != null)
            {
                aoeModel.onRemoved = onRemoved switch
                {
                    _ => null
                };
                aoeModel.onRemovedParams = onRemoved;
            }

            if (onTick != null)
            {
                aoeModel.onTick = onTick switch
                {
                    _ => null
                };
                aoeModel.onTickParams = onTick;
            }

            if (onChaEnter != null)
            {
                aoeModel.onChaEnter = onChaEnter switch
                {
                    cfg.Skill.DoDamageToEnterCha => AoeStrategyManager.onChaEnterFunc["DoDamageToEnterCha"],
                    _ => null
                };
                aoeModel.onChaEnterParams = onChaEnter;
            }

            if (onChaLeave != null)
            {
                aoeModel.onChaLeave = onChaLeave switch
                {
                    _ => null
                };
                aoeModel.onChaLeaveParams = onChaLeave;
            }

            if (onBulletEnter != null)
            {
                aoeModel.onBulletEnter = onBulletEnter switch
                {
                    _ => null
                };
                aoeModel.onBulletEnterParams = onBulletEnter;
            }

            if (onBulletLeave != null)
            {
                aoeModel.onBulletLeave = onBulletLeave switch
                {
                    _ => null
                };
                aoeModel.onBulletLeaveParams = onBulletLeave;
            }

            return aoeModel;
        }

        public void Clear()
        {
            id = 0;
            prefab = string.Empty;
            removeOnObstacle = false;
            tickTime = fix.Zero;
            onCreate = null;
            onTick = null;
            onChaEnter = null;
            onChaLeave = null;
            onBulletEnter = null;
            onBulletLeave = null;
            onCreateParams = null;
            onTickParams = null;
            onChaLeaveParams = null;
            onBulletLeaveParams = null;
            tags = null;
        }
    }
}