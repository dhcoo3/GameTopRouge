using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Effect;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using AAAGame.ScriptsHotfix.GamePlay.Render.Role;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Effect
{
    public class EffectRender:GameRender
    {
        private readonly Dictionary<int, EffectEntity> _effectViewDictionary = new Dictionary<int, EffectEntity>();
        public override void Initialize()
        {
            Subscribe(GamePlayEvent.EPlayEffect,PlayEffect);
        }

        private void PlayEffect(object sender, GameEvent e)
        {
            EffectData effectData = e.GetParam1<EffectData>();
            if (effectData == null)
            {
                Debug.LogWarning("effectData is null");
                return;
            }

            if (string.IsNullOrEmpty(effectData.EffectName))
            {
                Debug.LogWarning("effect name is null");
                return;
            }
            
            GF.AssetBridge.LoadEntity(effectData.EffectName,
                typeof(EffectEntity),"Effect",
                GamePlayDefine.LoadPriority.Effect,
                LoadEffectFinish,
                effectData);
        }
        
        private void LoadEffectFinish(AssetInfo info)
        {
            EffectData effectData = info.UserData as EffectData;
            if (effectData == null)
            {
                Log.Error("effectData is null");
                return;
            }
            
            UnityGameFramework.Runtime.Entity entity = info.Asset as UnityGameFramework.Runtime.Entity;
            if (entity == null || entity.Logic == null)
            {
                Log.Error("Entity.Logic is null");
                return;
            }
            
            Log.Info("LoadEffectFinish {0}",effectData.Id);
            _effectViewDictionary.Add(effectData.Id, entity.Logic as EffectEntity);
            ReferencePool.Release(effectData);
        }
        
        public void StopRenderRole(int id)
        {
            if (_effectViewDictionary.TryGetValue(id, out EffectEntity entity))
            {
                GF.AssetBridge.HideEntity(entity.Entity);
                _effectViewDictionary.Remove(id);
            }
        }
    }
}