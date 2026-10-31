using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Effect;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using cfg.Skill;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Aoe.AoeStrategy
{
    public partial class AoeStrategyManager
    {
        
        ///<summary>
        ///aoe创建时的事件
        ///<param name="aoe">被创建出来的aoe的对象</param>
        ///</summary>
        public delegate void AoeOnCreate(AoeUnit aoe,List<IUnit> cha,List<IUnit> bullet);

        ///<summary>
        ///aoe移除时候的事件
        ///<param name="aoe">被创建出来的aoe的对象</param>
        ///</summary>
        public delegate void AoeOnRemoved(IUnit aoe);

        ///<summary>
        ///aoe每一跳的事件
        ///<param name="aoe">被创建出来的aoe的对象</param>
        ///</summary>
        public delegate void AoeOnTick(IUnit aoe);

        ///<summary>
        ///当有角色进入aoe范围的时候触发
        ///<param name="aoe">被创建出来的aoe</param>
        ///<param name="cha">进入aoe范围的那些角色，他们现在还不在aoeState的角色列表里</param>
        ///</summary>
        public delegate void AoeOnCharacterEnter(AoeUnit aoe, List<IUnit> cha);

        ///<summary>
        ///当有角色离开aoe范围的时候
        ///<param name="aoe">离开aoe的gameObject</param>
        ///<param name="cha">离开aoe范围的那些角色，他们现在已经不在aoeState的角色列表里</param>
        ///</summary>
        public delegate void AoeOnCharacterLeave(IUnit aoe, List<IUnit> cha);

        ///<summary>
        ///当有子弹进入aoe范围的时候
        ///<param name="aoe">被创建出来的aoe的gameObject</param>
        ///<param name="bullet">离开aoe范围的那些子弹，他们现在已经不在aoeState的子弹列表里</param>
        ///</summary>
        public delegate void AoeOnBulletEnter(IUnit aoe, List<IUnit> bullet);

        ///<summary>
        ///当有子弹离开aoe范围的时候
        ///<param name="aoe">离开的aoe的gameObject</param>
        ///<param name="bullet">离开aoe范围的那些子弹，他们现在已经不在aoeState的子弹列表里</param>
        ///</summary>
        public delegate void AoeOnBulletLeave(IUnit aoe, List<IUnit> bullet);

        ///<summary>
        ///aoe的移动轨迹函数
        ///<param name="aoe">要执行的aoeObj</param>
        ///<param name="t">这个tween在aoe中运行了多久了，单位：秒</param>
        ///<return>aoe在这时候的移动信息</param>
        public delegate AoeMoveInfo AoeTween(IUnit aoe, fix t); 
        
        
        public static Dictionary<string, AoeOnCreate> onCreateFunc = new Dictionary<string, AoeOnCreate>(){
            {"CreateAoeSightEffect",CreateAoeSightEffect},
            {"DoDamageToCreate",DoDamageToCreate}
        };


        public static Dictionary<string, AoeOnRemoved> onRemovedFunc = new Dictionary<string, AoeOnRemoved>(){
          
        };
        
        public static Dictionary<string, AoeOnTick> onTickFunc = new Dictionary<string, AoeOnTick>(){
          
        };
        
        public static Dictionary<string, AoeOnCharacterEnter> onChaEnterFunc = new Dictionary<string, AoeOnCharacterEnter>(){
            {"DoDamageToEnterCha",DoDamageToEnterCha}
        };
        
        public static Dictionary<string, AoeOnCharacterLeave> onChaLeaveFunc = new Dictionary<string, AoeOnCharacterLeave>(){
            
        };
        
        public static Dictionary<string, AoeOnBulletEnter> onBulletEnterFunc = new Dictionary<string, AoeOnBulletEnter>(){
        };
        
        public static Dictionary<string, AoeOnBulletLeave> onBulletLeaveFunc = new Dictionary<string, AoeOnBulletLeave>(){
            
        };
        
        public static Dictionary<string, AoeTween> aoeTweenFunc = new Dictionary<string, AoeTween>(){
        };
         
            
        static void CreateAoeSightEffect(AoeUnit aoe,List<IUnit> characters,List<IUnit> bullets)
        {
            if (aoe.Data.model.onCreateParams is CreateAoeSightEffect createAoeSightEffect)
            {
                if (string.IsNullOrEmpty(createAoeSightEffect.SightEffect))
                {
                    return;
                }
                
                GameProxyManger.Instance.GetProxy<EffectProxy>().PlayToPositionRecycle1(
                    GF.AssetBridge.GetEffect(createAoeSightEffect.SightEffect),aoe.Behaviour.Position);
            }
        }
        
    }
}