using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using GameFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Entity
{
    /// <summary>
    /// GameObject + Component
    /// </summary>
    public abstract class EntityRender:EntityLogic
    {
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
        }

        protected override void OnRecycle()
        {
            base.OnRecycle();
        }

        public virtual void LogicUpdate(fix deltaTime)
        {
            
        }
    }
}