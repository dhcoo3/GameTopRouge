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

        public void Initialize()
        {
            for(int i=0;i<_gameRenders.Length;i++)
            {
                _gameRenders[i].Initialize();
            }
        }

        public T GetRender<T>() where T : GameRender
        {
            foreach (var data in _gameRenders)
            {
                if (data is T)
                {
                    return (T)data;
                }
            }

            return null;
        }
        
        public void RenderUpdate(fix deltaTime)
        {
            for(int i=0;i<_gameRenders.Length;i++)
            {
                _gameRenders[i].RenderUpdate(deltaTime);
            }
        }

        public void Clear()
        {
            for(int i=0;i<_gameRenders.Length;i++)
            {
                ReferencePool.Release(_gameRenders[i]);
            }

            _gameRenders = null;
        }
    }
}