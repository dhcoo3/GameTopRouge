using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Render.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Aoe
{
    public class AoeRender:GameRender
    {
        private readonly Dictionary<int, AoeEntity> _aoeEntities = new Dictionary<int, AoeEntity>();
        
        public override void Initialize()
        {
            Subscribe(GamePlayEvent.ERenderAoe,RenderAoe);
            Subscribe(GamePlayEvent.EStopRenderAoe,StopRenderAoe);
        }
        
        /// <summary>
        /// 渲染子弹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenderAoe(object sender, GameEvent e)
        {
            AoeUnit aoeUnit = e.GetParam1<AoeUnit>();
            if (aoeUnit == null)
            {
                Log.Warning("aoeUnit unit is null");
                return;
            }

            string path = GF.AssetBridge.GetAoe(aoeUnit.Data.model.prefab);
            GF.AssetBridge.LoadEntity(path,typeof(AoeEntity),"Aoe",
                GamePlayDefine.LoadPriority.Aoe,LoadAoeFinish,aoeUnit);
        }
        
        private void LoadAoeFinish(AssetInfo info)
        {
            AoeUnit aoeUnit = info.UserData as AoeUnit;
            if (aoeUnit == null)
            {
                Log.Error("aoeUnit is null");
                return;
            }
            
            UnityGameFramework.Runtime.Entity entity = info.Asset as UnityGameFramework.Runtime.Entity;
            if (entity == null || entity.Logic == null)
            {
                Log.Error("entity.Logic is null");
                return;
            }
  
            _aoeEntities.Add(aoeUnit.Data.AoeId, entity.Logic as AoeEntity);
        }

        private void StopRenderAoe(object sender, GameEvent e)
        {
            int id = e.GetParam1<int>();

            if (_aoeEntities.TryGetValue(id, out AoeEntity bulletEntity))
            {
                GF.AssetBridge.HideEntity(bulletEntity.Entity);
                _aoeEntities.Remove(id);
            }
        }
        
        public override void RenderUpdate(fix deltaTime)
        {
            if(_aoeEntities.Count == 0) return;

            foreach (var (key,data) in _aoeEntities)
            {
                data.LogicUpdate(deltaTime);
            }
            
            base.RenderUpdate(deltaTime);
        }
    }
}