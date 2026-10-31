using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Unit.Role;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Camera
{
    /// <summary>跟随本地玩家或显式指定的角色，更新战斗相机视野。</summary>
    public class CameraRender:GameRender
    {
        /// <summary>二维战斗相机在世界坐标中的固定深度。</summary>
        private const float FightCameraDepth = -1f;

        /// <summary>当前战斗主相机；场景切换销毁相机后重新获取。</summary>
        private UnityEngine.Camera _fightCamera;

        /// <summary>获取当前可用的主相机；相机尚未创建时返回空。</summary>
        public UnityEngine.Camera FightCamera
        {
            get
            {
                if (_fightCamera == null)
                {
                    _fightCamera = UnityEngine.Camera.main;
                }

                return _fightCamera;
            }
        }

        /// <summary>当前聚焦角色；没有收到聚焦事件时默认绑定本地玩家。</summary>
        private RoleUnit _focus;

        /// <summary>提供本地玩家角色的逻辑单位代理。</summary>
        private UnitProxy _unitProxy;

        /// <summary>每帧写入相机的世界坐标，复用以避免临时分配。</summary>
        private Vector3 _targetPosition = new Vector3(0, 0, FightCameraDepth);

        /// <summary>重置上一局的目标并订阅显式聚焦通知。</summary>
        public override void Initialize()
        {
            RemoveAllSubscribe();
            _focus = null;
            _unitProxy = GetProxy<UnitProxy>();
            _fightCamera = UnityEngine.Camera.main;
            Subscribe(GamePlayEvent.ECameraFocus,CameraFocus);
        }

        /// <summary>清理当前对局引用与事件订阅，避免对象池复用时跟随旧角色。</summary>
        public override void Clear()
        {
            _focus = null;
            _unitProxy = null;
            _fightCamera = null;
            _targetPosition = new Vector3(0, 0, FightCameraDepth);
            base.Clear();
        }

        /// <summary>接收业务层显式指定的聚焦角色。</summary>
        /// <param name="sender">发送聚焦通知的对象。</param>
        /// <param name="e">包含目标角色的聚焦事件。</param>
        private void CameraFocus(object sender, GameEvent e)
        {
            RoleUnit unit = e.GetParam1<RoleUnit>();
            if (unit == null)
            {
                Log.Warning("相机聚焦失败：目标角色为空。");
                return;
            }
            _focus = unit;
        }
     
        /// <summary>按角色最新位置更新视野，并补绑晚创建的玩家和相机。</summary>
        /// <param name="deltaTime">当前渲染帧的时间间隔，单位为秒。</param>
        public override void RenderUpdate(fix deltaTime)
        {
            UnityEngine.Camera fightCamera = FightCamera;
            if (fightCamera == null || !TryResolveFocus() || _focus.IsDeath())
            {
                return;
            }

            _targetPosition.x = _focus.Behaviour.Position.x;
            _targetPosition.y = _focus.Behaviour.Position.y;
            _targetPosition.z = FightCameraDepth;
            fightCamera.transform.position = _targetPosition;
            
            base.RenderUpdate(deltaTime);
        }

        /// <summary>保留显式聚焦目标，未绑定或角色已回收时查找本地玩家。</summary>
        /// <returns>当前存在行为数据有效的聚焦角色时返回真。</returns>
        private bool TryResolveFocus()
        {
            if (_focus != null && _focus.Behaviour != null)
            {
                return true;
            }

            _focus = null;
            if (_unitProxy == null ||
                !_unitProxy.PlayerRole.TryGetValue(GF.UserId, out RoleUnit localRole) ||
                localRole == null || localRole.Behaviour == null)
            {
                return false;
            }

            _focus = localRole;
            return true;
        }
    }
}
