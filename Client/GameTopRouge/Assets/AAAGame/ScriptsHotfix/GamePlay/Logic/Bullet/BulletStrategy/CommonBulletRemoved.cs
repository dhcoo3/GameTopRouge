using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Core;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet.BulletStrategy
{
    public partial class BulletStrategyManager
    {
        ///<summary>
        ///onRemoved
        ///普通子结束，参数：
        ///[0]命中视觉特效
        ///</summary>
        private static void CommonBulletRemoved(IUnit bullet){
            /*BulletState bulletState = bullet.GetComponent<BulletState>();
            if (!bulletState) return;
            object[] onRemovedParams = bulletState.model.onRemovedParams;
            string sightEffect = onRemovedParams.Length > 0 ? (string)onRemovedParams[0] : "";
            if (sightEffect != ""){
                SceneVariants.CreateSightEffect(
                    sightEffect, 
                    bullet.transform.position, 
                    bullet.transform.rotation.eulerAngles.y
                );      
            }*/
        }
    }
}