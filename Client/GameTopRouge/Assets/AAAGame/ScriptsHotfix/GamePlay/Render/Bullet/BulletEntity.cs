using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Render.Entity;
using AAAGame.ScriptsHotfix.GamePlay.Render.Map;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Bullet
{
    public class BulletEntity:EntityRender
    {
        public BulletUnit BulletUnit { get; private set; }
        
        private Vector3 _tmpVector3 = Vector3.zero;
        
        public Vector3 Position
        {
            get
            {
                return transform.position;
            }
            set
            {
                transform.position = value;
            }
        }
        
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
        }

        protected override void OnShow(object userData)
        {
            AssetInfo assetInfo = (AssetInfo)userData;
            BulletUnit = assetInfo.UserData as BulletUnit;
           
            if (BulletUnit == null)
            {
                Log.Error("RoleData is invalid.");
                return;
            }
            
            _tmpVector3.x =  BulletUnit.Behaviour.Position.x;
            _tmpVector3.y =  BulletUnit.Behaviour.Position.y;
            _tmpVector3.z =  BulletUnit.Behaviour.Position.z;
            
            Position = _tmpVector3;
            transform.rotation = BulletUnit.Behaviour.Rotation;
            
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = (int)MapRenderLayer.Bullet;
            }
            
            BulletUnit.HasEntity = true;
            base.OnShow(userData);
        }

        public override void LogicUpdate(fix deltaTime)
        {
            if (BulletUnit == null) return;
            //将逻辑层坐标同步到渲染层
            _tmpVector3.x =  BulletUnit.Behaviour.Position.x;
            _tmpVector3.y =  BulletUnit.Behaviour.Position.y;
            _tmpVector3.z =  BulletUnit.Behaviour.Position.z;
            
            transform.position = _tmpVector3;
            transform.rotation = BulletUnit.Behaviour.Rotation;
            base.LogicUpdate(deltaTime);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            BulletUnit = null;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnRecycle()
        {
            BulletUnit = null;
            base.OnRecycle();
        }
    }
}
