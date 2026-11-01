using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Fight;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Map;
using AAAGame.ScriptsHotfix.GamePlay.Logic.ProxyManager;
using AAAGame.ScriptsHotfix.Bridge;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using System;
using System.Threading;
using cfg.Map;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Map
{
    /// <summary>加载地图场景、网格和固定地图物件的渲染代理。</summary>
    public class MapRender : GameRender
    {
        /// <summary>当前地图各网格位置的配置快照。</summary>
        private GridInfo[,] _grid;
        /// <summary>负责实例化地图固定物件和 NPC 的加载器。</summary>
        private readonly MapPlacementLoader _placements = new MapPlacementLoader();
        /// <summary>当前地图的静态摆放配置。</summary>
        private MapData _placementMap;
        /// <summary>当前地图任务 UI 和进度请求的生命周期。</summary>
        private CancellationTokenSource _questLifetime;
        /// <summary>使地图切换前异步会话的迟到回调失效。</summary>
        private int _mapSessionGeneration;
        /// <summary>地图切换期间持有的玩家输入锁。</summary>
        private IDisposable _mapTransitionInputLock;
        /// <summary>负责更新地图加载界面进度的渲染代理。</summary>
        private FightLoadingProxy _fightLoadingProxy;
        /// <summary>异步加载中的地图场景操作。</summary>
        private AsyncOperation _sceneLoader;
        /// <summary>轮询地图场景加载状态的计时器。</summary>
        private GameTimer _loadMapTimer;
        /// <summary>读取当前地图逻辑信息的代理。</summary>
        private MapProxy _mapProxy;
        /// <summary>当前地图网格总数。</summary>
        private float _gridMaxCount;
        /// <summary>已经完成实例化的地图网格数。</summary>
        private int _gridCreateCount;

#if UNITY_EDITOR
        /// <summary>是否在编辑器中显示当前地图网格辅助线。</summary>
        private bool _drawGridGizmos = true;
        /// <summary>编辑器地图网格辅助线绘制组件。</summary>
        private MapGridGizmoDrawer _gridGizmoDrawer;

        /// <summary>切换编辑器中的地图网格辅助线显示状态。</summary>
        public bool DrawGridGizmos
        {
            get => _drawGridGizmos;
            set
            {
                if (_drawGridGizmos == value) return;
                _drawGridGizmos = value;
                RefreshGridGizmos();
            }
        }
#endif
        
        /// <summary>初始化地图加载代理并订阅创建地图事件。</summary>
        public override void Initialize()
        {
            _fightLoadingProxy = GetProxy<FightLoadingProxy>();
            _mapProxy = GetProxy<MapProxy>();
            Subscribe(GamePlayEvent.ECreateMapRender,CreateMapView);
#if UNITY_EDITOR
            RefreshGridGizmos();
#endif
        }

        /// <summary>离开地图时取消任务会话、释放场景加载计时器和网格数据。</summary>
        public override void Clear()
        {
            StopQuestSession();
            _placements.Clear();
            _placementMap = null;
#if UNITY_EDITOR
            ClearGridGizmos();
            _drawGridGizmos = false;
#endif
            _loadMapTimer?.Dispose();
            _loadMapTimer = null;
            _sceneLoader = null;
            _grid = null;
            _mapProxy = null;
            _fightLoadingProxy = null;
            base.Clear();
        }

        /// <summary>响应地图创建事件并加载场景和地图摆放物件。</summary>
        /// <param name="sender">触发当前游戏事件的对象。</param>
        /// <param name="e">包含目标地图配置的创建事件。</param>
        public void CreateMapView(object sender, GameEvent e)
        {
            MapData mapData = e.GetParam1<MapData>();
            StopQuestSession();
            _mapTransitionInputLock = GameProxyManger.Instance.GetProxy<InputProxy>()?.AcquirePlayerInputLock();
            _placements.Clear();
            _loadMapTimer?.Dispose();
            _loadMapTimer = null;
            _placementMap = mapData;
            _gridCreateCount = 0;

#if UNITY_EDITOR
            RefreshGridGizmos();
#endif

            if (mapData.Random)
            {
                CreateMapGameObjects();
                return;
            }
            
#if UNITY_EDITOR
            LoadSceneParameters mode = new LoadSceneParameters(LoadSceneMode.Additive);
            _sceneLoader = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(mapData.Name, mode);
            _sceneLoader.allowSceneActivation = false;
#else
            //assetsFactory?????????????????????????????????
            //_loadSceneBundleFailed = !param;
            //if (_loadSceneBundleFailed)
            //{
            //    UpdateLoadingProgress();
            //    return;
            //}
            //_sceneLoader = SceneManager.LoadSceneAsync(_mapData.SceneName, LoadSceneMode.Additive);
#endif
            // 使用正间隔轮询，避免场景等待激活时连续占用主线程。
            _loadMapTimer = GameTimer.CreateLoop(-1, 0.016f, UpdateLoadSceneProgress);
            _loadMapTimer.Start();
        }

#if UNITY_EDITOR
        /// <summary>根据地图配置刷新编辑器网格辅助线。</summary>
        private void RefreshGridGizmos()
        {
            MapInfo mapInfo = _mapProxy?.MapInfo;
            if (!_drawGridGizmos || mapInfo?.grid == null)
            {
                ClearGridGizmos();
                return;
            }

            if (_gridGizmoDrawer == null)
            {
                GameObject gizmoObject = new GameObject("Map Grid Gizmos");
                _gridGizmoDrawer = gizmoObject.AddComponent<MapGridGizmoDrawer>();
            }

            _gridGizmoDrawer.SetMapInfo(mapInfo);
        }

        /// <summary>销毁当前地图的编辑器网格辅助线。</summary>
        private void ClearGridGizmos()
        {
            if (_gridGizmoDrawer == null) return;

            GameObject gizmoObject = _gridGizmoDrawer.gameObject;
            _gridGizmoDrawer.SetMapInfo(null);
            _gridGizmoDrawer = null;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gizmoObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gizmoObject);
            }
        }
#endif
        
        /// <summary>轮询场景加载进度，并在场景就绪后加载地图摆放物件。</summary>
        /// <param name="dt">本次轮询间隔秒数。</param>
        private void UpdateLoadSceneProgress(float dt)
        {
            if (_sceneLoader == null)
            {
                return;
            }

            if (_sceneLoader.isDone)
            {
                _loadMapTimer.Dispose();
                _placements.LoadAsync(_placementMap).Forget(Debug.LogException);
                StartQuestSession().Forget(Debug.LogException);
                _fightLoadingProxy.SetSceneProgress(1);
            }
            else
            {
                _fightLoadingProxy.SetSceneProgress(_sceneLoader.progress);
                
                if (_sceneLoader.progress >= 0.88f)
                {
                    _sceneLoader.allowSceneActivation = true;
                }
            }
        }
        
        /// <summary>按当前地图启动任务状态加载和任务界面生命周期。</summary>
        private async UniTask StartQuestSession()
        {
            if (_placementMap == null)
            {
                ReleaseMapTransitionInputLock();
                return;
            }
            int generation = _mapSessionGeneration;
            CancellationTokenSource lifetime = new CancellationTokenSource();
            _questLifetime = lifetime;
            try
            {
                await GamePlayToUIBridge.Instance.OnMapEnteredAsync(_placementMap.Id, lifetime.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                lifetime.Dispose();
                if (generation == _mapSessionGeneration)
                {
                    if (ReferenceEquals(_questLifetime, lifetime)) _questLifetime = null;
                    ReleaseMapTransitionInputLock();
                }
            }
        }

        /// <summary>结束当前地图的任务生命周期并关闭任务界面。</summary>
        private void StopQuestSession()
        {
            _mapSessionGeneration++;
            if (_questLifetime != null)
            {
                _questLifetime.Cancel();
                _questLifetime.Dispose();
                _questLifetime = null;
            }
            GamePlayToUIBridge.Instance.CloseMapUI();
            ReleaseMapTransitionInputLock();
        }

        /// <summary>释放地图切换期间占用的输入锁。</summary>
        private void ReleaseMapTransitionInputLock()
        {
            _mapTransitionInputLock?.Dispose();
            _mapTransitionInputLock = null;
        }

        /// <summary>根据当前地图尺寸创建固定物件，并按配置创建全部网格。</summary>
        private void CreateMapGameObjects()
        {
            int width = _mapProxy.MapInfo.MapWidth();
            int height = _mapProxy.MapInfo.MapHeight();
            _gridMaxCount = width * height;
            _grid = new GridInfo[width, height];
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    RenderGrid(_mapProxy.MapInfo.grid[i, j], i, j, new fix3(i, j, 0), 0);
                }
            }
        }

        /// <summary>将单个逻辑网格配置转换为地图实体创建请求。</summary>
        /// <param name="gridInfo">当前网格的配置。</param>
        /// <param name="row">当前网格的行坐标。</param>
        /// <param name="col">当前网格的列坐标。</param>
        /// <param name="position">地图中的世界位置。</param>
        /// <param name="rotation">地图中的旋转角度。</param>
        private void RenderGrid(GridInfo gridInfo, int row, int col, fix3 position, fix rotation)
        {
            MapGridCreate mapGridCreate = new MapGridCreate(gridInfo, row,col,position,rotation);
            string path = GF.AssetBridge.GetTerrain(gridInfo.prefabPath);
            GF.AssetBridge.LoadEntity(path,typeof(MapGridEntity),"Map",
                GamePlayDefine.LoadPriority.Aoe,LoadGridFinish,mapGridCreate);
        }
        
        /// <summary>处理单个地图网格实体加载完成事件。</summary>
        /// <param name="info">包含加载实体和网格请求数据的资源信息。</param>
        private void LoadGridFinish(AssetInfo info)
        {
            MapGridCreate mapGridCreate = info.UserData as MapGridCreate;
            if (mapGridCreate == null)
            {
                Log.Warning("Map grid create error.");
                return;
            }
            
            UnityGameFramework.Runtime.Entity entity = info.Asset as UnityGameFramework.Runtime.Entity;
            if (entity == null || entity.Logic == null)
            {
                Log.Error("entity.Logic is null");
                return;
            }
            
            _grid[mapGridCreate.Row,mapGridCreate.Col] = mapGridCreate.GridInfo;
            _gridCreateCount++;
            if (_gridCreateCount == _gridMaxCount)
            {
                _placements.LoadAsync(_placementMap).Forget(Debug.LogException);
                StartQuestSession().Forget(Debug.LogException);
            }
            _fightLoadingProxy.SetSceneProgress(_gridCreateCount/_gridMaxCount);
        }

    }
}
