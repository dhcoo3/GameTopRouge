using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Effect
{
    public class EffectData:IReference
    {
        private int _id;

        public int Id => _id;
        
        private string _effectDataEffectName;

        /// <summary>
        /// 特效名字，也是加载路径
        /// </summary>
        public string EffectName => _effectDataEffectName;
        
        private fix3 _effectDataCreatePosition;
        /// <summary>
        /// 特效生成位置
        /// </summary>
        public fix3 CreatePosition => _effectDataCreatePosition;

        private fix _effectDataRecycleTime;

        /// <summary>
        /// 多少秒后自动回收
        /// </summary>
        public fix RecycleTime => _effectDataRecycleTime;

        public static EffectData Create(int id,string effectName,fix3 position,fix recycleTime)
        {
            EffectData effectData = ReferencePool.Acquire<EffectData>();
            effectData._id = id;
            effectData._effectDataEffectName = effectName;
            effectData._effectDataCreatePosition = position;
            effectData._effectDataRecycleTime = recycleTime;
            return effectData;
        }
        
        public void Clear()
        {
            _effectDataEffectName = string.Empty;
            _effectDataCreatePosition = fix3.zero;
            _effectDataRecycleTime = 0;
        }
    }
}