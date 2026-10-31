using AAAGame.ScriptsHotfix.GamePlay.Render.Entity;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    public class MapGridEntity:EntityRender
    {
        private Vector3 _tmpVector3 = Vector3.zero;
        
        protected override void OnShow(object userData)
        {
            AssetInfo assetInfo = (AssetInfo)userData;
            MapGridCreate mapGridCreate = assetInfo.UserData as MapGridCreate;
            
            if (mapGridCreate == null)
            {
                return;
            }

            _tmpVector3.x = mapGridCreate.BirthPosition.x;
            _tmpVector3.y = mapGridCreate.BirthPosition.y;
            _tmpVector3.z = mapGridCreate.BirthPosition.z;
            
            transform.position = _tmpVector3;
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = (int)MapRenderLayer.Grid;
            base.OnShow(userData);
        }

        public override void LogicUpdate(fix deltaTime)
        {
            base.LogicUpdate(deltaTime);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
        }

        protected override void OnRecycle()
        {
            base.OnRecycle();
        }
    }
}