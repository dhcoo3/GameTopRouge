using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using GameFramework;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Bullet
{
    public class BulletRender:GameRender
    {
        private readonly Dictionary<int, BulletEntity> _bulletEntities = new Dictionary<int, BulletEntity>();
        
        public override void Initialize()
        {
            Subscribe(GamePlayEvent.ERenderBullet,RenderBullet);
            Subscribe(GamePlayEvent.EStopRenderBullet,StopRenderBullet);
        }
        
        public override void RenderUpdate(fix deltaTime)
        {
            if(_bulletEntities.Count == 0) return;

            foreach (var (key,data) in _bulletEntities)
            {
                data.LogicUpdate(deltaTime);
            }
            
            base.RenderUpdate(deltaTime);
        }
        
        /// <summary>
        /// 渲染子弹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenderBullet(object sender, GameEvent e)
        {
            BulletUnit bulletUnit = e.GetParam1<BulletUnit>();

            string path = GF.AssetBridge.GetBullet(bulletUnit.Data.model.prefab);
            GF.AssetBridge.LoadEntity(path,typeof(BulletEntity),"Bullet",
                GamePlayDefine.LoadPriority.Bullet,LoadBulletFinish,bulletUnit);
        }
        
        private void LoadBulletFinish(AssetInfo info)
        {
            BulletUnit bulletUnit = info.UserData as BulletUnit;
            if (bulletUnit == null)
            {
                Log.Error("bulletUnit is null");
                return;
            }
            
            UnityGameFramework.Runtime.Entity entity = info.Asset as UnityGameFramework.Runtime.Entity;
            if (entity == null || entity.Logic == null)
            {
                Log.Error("entity.Logic is null");
                return;
            }
  
            _bulletEntities.Add(bulletUnit.Data.BulletId, entity.Logic as BulletEntity);
        }

        private void StopRenderBullet(object sender, GameEvent e)
        {
            int id = e.GetParam1<int>();

            if (_bulletEntities.TryGetValue(id, out BulletEntity bulletEntity))
            {
                GF.AssetBridge.HideEntity(bulletEntity.Entity);
                _bulletEntities.Remove(id);
            }
        }
    }
}