using System;
using AAAGame.ScriptsHotfix.UI.Module.FightMainChapter;
using AAAGame.ScriptsHotfix.UI.Module.FightShare;
using AAAGame.ScriptsHotfix.UI.Module.Login;
using cfg.UI;
using Cysharp.Threading.Tasks;
using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.UI.Core
{
    /// <summary>
    /// 管理 Luban UI 模块配置、模块控制器注册和模块入口开启条件。
    /// </summary>
    public class UIModuleManager : Singleton<UIModuleManager>
    {
        /// <summary>当前客户端使用的模块配置表。</summary>
        private TbGameModule _tbGameModule;

        /// <summary>已注册的模块控制器集合。</summary>
        private readonly GameFrameworkLinkedList<IController> _moduleControllers =
            new GameFrameworkLinkedList<IController>();

        /// <summary>已注册的模块数据模型集合。</summary>
        private readonly GameFrameworkLinkedList<IModel> _moduleModels =
            new GameFrameworkLinkedList<IModel>();

        /// <summary>已注册的模块视图集合。</summary>
        private readonly GameFrameworkLinkedList<IView> _moduleViews =
            new GameFrameworkLinkedList<IView>();

        /// <summary>等待模块表和模块实例初始化完成的异步结果。</summary>
        private UniTaskCompletionSource<bool> _initializationSource;

        /// <summary>标记模块控制器是否已经加入管理集合。</summary>
        private bool _controllersRegistered;

        /// <summary>标记模块协议和事件是否已经初始化。</summary>
        private bool _gameModulesInitialized;

        /// <summary>标记模块管理器是否完成初始化。</summary>
        private bool _initialized;

        /// <summary>
        /// 注册一个模块数据模型。
        /// </summary>
        /// <param name="moduleModel">要加入管理集合的数据模型。</param>
        public void Register(IModel moduleModel)
        {
            if (moduleModel != null)
            {
                _moduleModels.AddLast(moduleModel);
            }
        }

        /// <summary>
        /// 注册一个模块控制器。
        /// </summary>
        /// <param name="moduleController">要加入管理集合的控制器。</param>
        public void Register(IController moduleController)
        {
            if (moduleController != null)
            {
                _moduleControllers.AddLast(moduleController);
            }
        }

        /// <summary>
        /// 注册一个模块视图。
        /// </summary>
        /// <param name="moduleView">要加入管理集合的视图。</param>
        public void Register(IView moduleView)
        {
            if (moduleView != null)
            {
                _moduleViews.AddLast(moduleView);
            }
        }

        /// <summary>
        /// 加载模块表并注册模块控制器、协议及事件；并发调用共享同一初始化任务。
        /// </summary>
        /// <returns>模块管理器初始化完成的异步任务。</returns>
        public UniTask InitializeAsync()
        {
            if (_initialized)
            {
                return UniTask.CompletedTask;
            }

            if (_initializationSource != null)
            {
                return _initializationSource.Task;
            }

            var completion = new UniTaskCompletionSource<bool>();
            _initializationSource = completion;
            InitializeInternalAsync(completion).Forget();
            return completion.Task;
        }

        /// <summary>
        /// 执行一次完整的模块管理器初始化。
        /// </summary>
        /// <param name="completion">接收初始化结果的异步完成源。</param>
        private async UniTask InitializeInternalAsync(UniTaskCompletionSource<bool> completion)
        {
            try
            {
                _tbGameModule = await GF.LuBan.Get<TbGameModule>(cfg.Tables.ui_tbgamemodule);
                if (_tbGameModule == null)
                {
                    throw new InvalidOperationException("加载 UI 模块表失败：配置表返回空值。");
                }

                RegisterModuleControllers();
                InitGameModule();
                _initialized = true;
                _initializationSource = null;
                completion.TrySetResult(true);
            }
            catch (Exception exception)
            {
                _initializationSource = null;
                Log.Error($"初始化 UI 模块管理器失败：{exception}");
                completion.TrySetException(exception);
            }
        }

        /// <summary>
        /// 注册客户端当前维护的 UI 模块控制器。
        /// </summary>
        private void RegisterModuleControllers()
        {
            if (_controllersRegistered)
            {
                return;
            }

            Register(LoginController.Instance);
            Register(AAAGame.ScriptsHotfix.UI.Module.Dialogue.DialogueController.Instance);
            Register(AAAGame.ScriptsHotfix.UI.Module.Quest.QuestController.Instance);
            Register(FightShareController.Instance);
            Register(FightMainChapterController.Instance);
            _controllersRegistered = true;
        }

        /// <summary>
        /// 初始化所有已注册控制器的协议和事件。
        /// </summary>
        public void InitGameModule()
        {
            if (_gameModulesInitialized)
            {
                return;
            }

            RegisterModuleControllers();
            foreach (IController moduleController in _moduleControllers)
            {
                moduleController.RegisterProto();
                moduleController.RegisterEvent();
            }

            _gameModulesInitialized = true;
        }

        /// <summary>
        /// 通过 UIService 异步打开模块表单，并由模块解析流程检查开启条件。
        /// </summary>
        /// <param name="moduleType">要进入的模块类型。</param>
        /// <param name="parameters">传给模块表单的参数；所有权转交给 UIService。</param>
        /// <returns>模块表单打开完成后的逻辑；配置、资源或开启条件失败时抛出异常。</returns>
        public async UniTask<UIFormLogic> GoToModuleAsync(ModuleType moduleType, UIParams parameters = null)
        {
            return await UIService.Instance.OpenModuleAsync<UIFormLogic>(moduleType, parameters);
        }

        /// <summary>
        /// 判断模块入口表单是否已经打开或正在加载。
        /// </summary>
        /// <param name="moduleType">要查询的模块类型。</param>
        /// <returns>模块表单已打开或正在加载时返回 true。</returns>
        public bool IsOpenModule(ModuleType moduleType)
        {
            return UIService.Instance.IsOpen(moduleType);
        }

        /// <summary>
        /// 获取已加载模块表中的模块行；模块表尚未就绪或行不存在时返回 false。
        /// </summary>
        /// <param name="moduleType">要查找的模块类型。</param>
        /// <param name="module">查找到的模块行；失败时为 null。</param>
        /// <returns>模块表中存在对应行时返回 true。</returns>
        internal bool TryGetModule(ModuleType moduleType, out GameModule module)
        {
            module = _tbGameModule?.GetOrDefault(moduleType);
            return module != null;
        }

        /// <summary>
        /// 加载并验证模块定义及开启条件，供 UIService 解析模块资源路径。
        /// </summary>
        /// <param name="moduleType">要解析的模块类型。</param>
        /// <returns>模块表中对应的配置行。</returns>
        internal async UniTask<GameModule> ResolveModuleAsync(ModuleType moduleType)
        {
            await InitializeAsync();
            if (!TryGetModule(moduleType, out GameModule module))
            {
                throw new InvalidOperationException($"UI 模块表中不存在模块定义：{moduleType}。");
            }

            if (!CheckModuleOpenCondition(moduleType))
            {
                throw new InvalidOperationException($"模块入口不满足开启条件：{moduleType}。");
            }

            return module;
        }

        /// <summary>
        /// 检查指定模块当前是否满足开启条件。
        /// </summary>
        /// <param name="moduleType">要检查的模块类型。</param>
        /// <returns>模块允许打开时返回 true。</returns>
        public bool CheckModuleOpenCondition(ModuleType moduleType)
        {
            return true;
        }
    }
}
