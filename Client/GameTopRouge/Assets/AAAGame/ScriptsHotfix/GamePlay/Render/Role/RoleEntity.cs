using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Role;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using AAAGame.ScriptsHotfix.GamePlay.Render.Entity;
using AAAGame.ScriptsHotfix.GamePlay.Render.Map;
using cfg.Anim;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;
#if UNITY_EDITOR
using Handles = UnityEditor.Handles;
using SceneView = UnityEditor.SceneView;
using UnityEngine.Rendering;
#endif

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Role
{
    public class RoleEntity:EntityRender
    {
        private RoleUnit _roleUnit;
        
        private RoleBehaviour _roleBehaviour;
        
        private RoleData _roleData;
        
        private UnitAnim _unitAnim;

        private SpriteRenderer _spriteRenderer;

        private Vector3 _tmpVector3 = Vector3.zero;

#if UNITY_EDITOR
        private static readonly Color CollisionAreaColor = new Color(1f, 0.35f, 0.05f, 0.95f);
        private static readonly Color CollisionCenterColor = new Color(1f, 0.9f, 0.1f, 1f);

        [SerializeField, Tooltip("在 Scene 视图显示角色实际使用的圆形碰撞区域。")]
        private bool _drawCollisionGizmos = true;

        /// <summary>是否在 Scene 视图显示角色碰撞区域。</summary>
        public bool DrawCollisionGizmos
        {
            get => _drawCollisionGizmos;
            set
            {
                if (_drawCollisionGizmos == value) return;
                _drawCollisionGizmos = value;
                SceneView.RepaintAll();
            }
        }
#endif

        public UnitAnim RoleUnitAnim
        {
            get
            {
                return _unitAnim ??= gameObject.GetOrAddComponent<UnitAnim>();
            }
        }
       
        public VarVector3 Position
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
            _roleUnit = assetInfo.UserData as RoleUnit;
           
            if (_roleUnit == null)
            {
                Log.Error("RoleData is invalid.");
                return;
            }
            
            _roleBehaviour = _roleUnit.Behaviour as RoleBehaviour;
            _roleData = _roleUnit.Data;
            
            _tmpVector3.x =  _roleBehaviour.Position.x;
            _tmpVector3.y = _roleBehaviour.Position.y;
            _tmpVector3.z = _roleBehaviour.Position.z;
            
            Position = _tmpVector3;
            transform.rotation = _roleUnit.Behaviour.Rotation;

            InitAnimation(_roleData.AnimId);
            
            RoleUnitAnim.RoleBehaviour = _roleBehaviour;
          
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = MapSorting.WorldObjectOrder(GetCollisionCenter().y);
            }
            
            _roleUnit.HasEntity = true;
            
            base.OnShow(userData);
        }

        public override void LogicUpdate(fix deltaTime)
        {
            //将逻辑层坐标同步到渲染层
            if(_roleUnit == null) return;
            
            _tmpVector3.x =  _roleUnit.Behaviour.Position.x;
            _tmpVector3.y =  _roleUnit.Behaviour.Position.y;
            _tmpVector3.z =  _roleUnit.Behaviour.Position.z;
            
            //更新显示位置
            transform.position = _tmpVector3;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = MapSorting.WorldObjectOrder(GetCollisionCenter().y);
            }
            
            //更新显示旋转,  注释PS：只更新逻辑层方向，显示的方向 由动画决定
            //transform.rotation = _roleBehaviour.Rotation;
            
            RoleUnitAnim.LogicUpdate(deltaTime);
            
            GamePlayToUIBridge.Instance.UpdatePos(_roleUnit.Data.RoleId, transform.position);
            
            base.LogicUpdate(deltaTime);
        }

        /// <summary>角色地图碰撞圆心的世界坐标。</summary>
        private Vector3 GetCollisionCenter()
        {
            if (_roleUnit == null || _roleBehaviour == null) return transform.position;

            fix3 position = _roleBehaviour.Position + _roleUnit.CollisionOffset;
            return new Vector3((float)position.x, (float)position.y, (float)position.z);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            _roleUnit = null;
            _spriteRenderer = null;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnRecycle()
        {
            _roleUnit = null;
            _spriteRenderer = null;
            base.OnRecycle();
        }

#if UNITY_EDITOR
        /// <summary>在 Scene 视图绘制移动逻辑使用的碰撞圆及其圆心。</summary>
        private void OnDrawGizmos()
        {
            if (!_drawCollisionGizmos || _roleUnit == null) return;

            float radius = (float)_roleUnit.BodyRadius;
            if (radius <= 0f) return;

            Vector3 center = GetCollisionCenter();
            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.Always;

            try
            {
                Handles.color = CollisionAreaColor;
                Handles.DrawWireDisc(center, Vector3.forward, radius);

                Handles.color = CollisionCenterColor;
                Handles.DrawSolidDisc(center, Vector3.forward, Mathf.Max(radius * 0.08f, 0.025f));
            }
            finally
            {
                Handles.color = previousColor;
                Handles.zTest = previousZTest;
            }
        }
#endif

        /// <summary>
        /// 初始化动画组
        /// </summary>
        /// <param name="animGroup">动画组</param>
        private async void InitAnimation(int animGroup)
        {
           if (animGroup <= 0) return;
            
            Dictionary<cfg.Anim.Type, AnimInfo> animDic = new Dictionary<cfg.Anim.Type, AnimInfo>();
            TbAnimGroup tbAnimGroup = await GF.LuBan.Get<TbAnimGroup>(cfg.Tables.anim_tbanimgroup);
            TbAnimDef tbAnimDef = await GF.LuBan.Get<TbAnimDef>(cfg.Tables.anim_tbanimdef);
            
            AnimGroup data = tbAnimGroup.GetOrDefault(animGroup);
            if (data == null)
            {
                Log.Warning("AnimGroup is invalid. {0}",animGroup);
                return;
            }

            for (int i = 0; i < data.AnimList.Count; i++)
            {
                AnimDef anim = tbAnimDef.GetOrDefault(data.AnimList[i]);
                
                if (anim == null)
                {
                    Log.Warning("AnimDef is invalid. {0}",data.AnimList[i]);
                    continue;
                }
                
                animDic.TryAdd(anim.Type, new AnimInfo(anim.Type,anim.Priority));

                if (animDic.TryGetValue(anim.Type, out AnimInfo animInfo))
                {
                    animInfo.allAnims.TryAdd(anim.Direction,new List<SingleAnimInfo>());

                    if (animInfo.allAnims.TryGetValue(anim.Direction, out List<SingleAnimInfo> allAnimInfo))
                    {
                        for (int j = 0; j < anim.Sub.Length; j++)
                        {
                            SingleAnimInfo singleAnimInfo = new SingleAnimInfo(anim.Sub[j].AnimName,
                                j + 1,
                                MathUtils.Convert(anim.Sub[j].Duration),
                                 anim.Direction);
                            allAnimInfo.Add(singleAnimInfo);
                        }
                    }
                }
            }

            RoleUnitAnim.animInfo = animDic;
        }
    }
}
