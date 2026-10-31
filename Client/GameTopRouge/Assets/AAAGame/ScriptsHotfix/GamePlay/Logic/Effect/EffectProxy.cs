using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Effect
{
    public class EffectProxy:GameProxy
    {
        private int _id = GamePlayDefine.CreateId.Effect;
        
        public override void Initialize()
        {
           
        }
        
        /// <summary>
        /// 在固定点播放一个特效,并在1秒后回收
        /// </summary>
        /// <param name="effectName">特效名</param>
        /// <param name="position">在哪个绑定点播放,默认为中心</param>
        public void PlayToPositionRecycle1(string effectName,fix3 position)
        {
            _id++;
            EffectData data = EffectData.Create(_id, effectName, position,1);
            Fire(GamePlayEvent.EPlayEffect, data);
        }
    }
}