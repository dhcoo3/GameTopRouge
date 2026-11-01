using AAAGame.ScriptsHotfix.GamePlay.Render.Aoe;
using AAAGame.ScriptsHotfix.GamePlay.Render.Bullet;
using AAAGame.ScriptsHotfix.GamePlay.Render.Camera;
using AAAGame.ScriptsHotfix.GamePlay.Render.Effect;
using AAAGame.ScriptsHotfix.GamePlay.Render.Joystick;
using AAAGame.ScriptsHotfix.GamePlay.Render.Layer;
using AAAGame.ScriptsHotfix.GamePlay.Render.Map;
using AAAGame.ScriptsHotfix.GamePlay.Render.Role;
using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager
{
    public class GameRenderManager:Singleton<GameRenderManager>
    {
        private GameRender[] _gameRenders;
        
        /// <summary>
        /// 通用
        /// </summary>
        public void Register()
        {
            _gameRenders = new GameRender[]
            {
                ReferencePool.Acquire<MapRender>(),
                ReferencePool.Acquire<RoleRender>(),
                ReferencePool.Acquire<BulletRender>(),
                ReferencePool.Acquire<CameraRender>(),
                ReferencePool.Acquire<LayerRender>(),
                ReferencePool.Acquire<JoystickRender>(),
                ReferencePool.Acquire<EffectRender>(),
                ReferencePool.Acquire<AoeRender>()
            };
        }

        /// <summary>初始化当前已注册的表现层。</summary>
        public void Initialize()
        {
            GameRender[] renders = _gameRenders;
            if (renders == null)
            {
                return;
            }

            for (int i = 0; i < renders.Length; i++)
            {
                renders[i]?.Initialize();
            }
        }

        /// <summary>查找当前已注册的表现层；未注册时返回 null。</summary>
        /// <typeparam name="T">需要查找的表现层类型。</typeparam>
        /// <returns>匹配的表现层；当前未注册时返回 null。</returns>
        public T GetRender<T>() where T : GameRender
        {
            if (_gameRenders == null)
            {
                return null;
            }

            foreach (var data in _gameRenders)
            {
                if (data is T)
                {
                    return (T)data;
                }
            }

            return null;
        }
        
        /// <summary>刷新当前已注册的表现层。</summary>
        /// <param name="deltaTime">本次表现层更新的时间步长。</param>
        public void RenderUpdate(fix deltaTime)
        {
            GameRender[] renders = _gameRenders;
            if (renders == null)
            {
                return;
            }

            for (int i = 0; i < renders.Length; i++)
            {
                renders[i]?.RenderUpdate(deltaTime);
            }
        }

        /// <summary>释放全部表现层；重复调用不会访问已清理列表。</summary>
        public void Clear()
        {
            GameRender[] renders = _gameRenders;
            if (renders == null)
            {
                return;
            }

            _gameRenders = null;
            for (int i = 0; i < renders.Length; i++)
            {
                if (renders[i] != null)
                {
                    ReferencePool.Release(renders[i]);
                }
            }
        }
    }
}
