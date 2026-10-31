using System;
using System.Collections.Generic;
using cfg.UI;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.UI.Core
{
    /// <summary>
    /// 统一解析 UI 路由、合并单实例请求并管理表单生命周期入口。
    /// </summary>
    public sealed class UIService : Singleton<UIService>
    {
        /// <summary>模块入口使用的 UI 组名称。</summary>
        private const string ModuleUIGroupName = "Bottom";

        /// <summary>模块入口资源的加载优先级。</summary>
        private const int ModuleLoadPriority = 1;

        /// <summary>固定覆盖界面所属的 UI 组。</summary>
        private const string OverlayUIGroupName = "Middle";

        /// <summary>固定覆盖界面资源的加载优先级。</summary>
        private const int OverlayLoadPriority = 2;

        /// <summary>子界面沿用父级 UI 组时的加载优先级。</summary>
        private const int SubUIFormLoadPriority = 2;

        /// <summary>子界面至少比父面板高出的排序层级。</summary>
        private const int SubUIOrderOffset = 1;

        /// <summary>提示表单使用的 UI 组名称。</summary>
        private const string TipUIGroupName = "Bottom";

        /// <summary>提示表单资源的加载优先级。</summary>
        private const int TipLoadPriority = 1;

        /// <summary>正在打开的表单请求，按 Game Framework 序列号索引。</summary>
        private readonly Dictionary<int, PendingUIOpen> _pendingBySerial = new Dictionary<int, PendingUIOpen>();

        /// <summary>正在打开的表单请求，按传给 Game Framework 的参数对象索引。</summary>
        private readonly Dictionary<UIParams, PendingUIOpen> _pendingByParameters =
            new Dictionary<UIParams, PendingUIOpen>();

        /// <summary>参数到本次持有请求的映射，在参数实际归还对象池前同步移除。</summary>
        private readonly Dictionary<UIParams, PendingUIOpen> _ownedParameters =
            new Dictionary<UIParams, PendingUIOpen>();

        /// <summary>正在打开的单实例路由请求，按路由键索引。</summary>
        private readonly Dictionary<string, PendingUIOpen> _pendingByRoute = new Dictionary<string, PendingUIOpen>();

        /// <summary>已打开的单实例路由序列号，按路由键索引。</summary>
        private readonly Dictionary<string, int> _openedSerialByRoute = new Dictionary<string, int>();

        /// <summary>单实例路由键到已打开表单序列号的反向索引。</summary>
        private readonly Dictionary<int, string> _routeByOpenedSerial = new Dictionary<int, string>();

        /// <summary>已打开表单序列号到其 UI 参数对象的映射。</summary>
        private readonly Dictionary<int, UIParams> _parametersByOpenedSerial = new Dictionary<int, UIParams>();

        /// <summary>标记 UI 生命周期事件是否已订阅。</summary>
        private bool _eventsSubscribed;

        /// <summary>
        /// 按模块类型和 UI 名称异步打开覆盖界面；同一界面的并发请求共享结果。
        /// </summary>
        /// <typeparam name="TForm">目标表单逻辑类型。</typeparam>
        /// <param name="moduleType">覆盖界面所属的模块类型。</param>
        /// <param name="uiName">模块目录下的 UI 资源名称，不包含目录前缀。</param>
        /// <param name="parameters">传给表单的参数；所有权转交给服务。</param>
        /// <param name="uiGroupName">表单所属 UI 组；省略时使用 Middle。</param>
        /// <returns>加载并打开后的表单逻辑。</returns>
        public async UniTask<TForm> OpenAsync<TForm>(ModuleType moduleType, string uiName,
            UIParams parameters = null, string uiGroupName = OverlayUIGroupName)
            where TForm : UIFormLogic
        {
            UIParams ownedParameters = parameters ?? UIParams.Create();
            EnsureParametersAvailable(ownedParameters);
            if (string.IsNullOrWhiteSpace(uiName))
            {
                ReleaseParametersIfUnowned(ownedParameters);
                throw new ArgumentException("覆盖界面名称不能为空。", nameof(uiName));
            }

            string relativePath = $"{moduleType}/{uiName}";
            string assetName;
            try
            {
                assetName = UtilityBuiltin.AssetsPath.GetUIFormPath(relativePath);
            }
            catch
            {
                ReleaseParametersIfUnowned(ownedParameters);
                throw;
            }

            string routeKey = GetOverlayRouteKey(moduleType, uiName);
            UIFormLogic logic = await OpenFormAsync(assetName, uiGroupName, OverlayLoadPriority,
                false, ownedParameters, routeKey, null);
            return CastForm<TForm>(logic, relativePath);
        }

        /// <summary>
        /// 根据 Luban 模块表异步打开模块入口表单。
        /// </summary>
        /// <typeparam name="TForm">目标表单逻辑类型。</typeparam>
        /// <param name="moduleType">要打开的模块类型。</param>
        /// <param name="parameters">传给表单的参数；所有权转交给服务。</param>
        /// <param name="uiGroupName">表单所属 UI 组；省略时使用 Bottom。</param>
        /// <returns>加载并打开后的表单逻辑。</returns>
        public async UniTask<TForm> OpenModuleAsync<TForm>(ModuleType moduleType, UIParams parameters = null,
            string uiGroupName = ModuleUIGroupName)
            where TForm : UIFormLogic
        {
            UIParams ownedParameters = parameters ?? UIParams.Create();
            EnsureParametersAvailable(ownedParameters);
            GameModule module;
            try
            {
                module = await UIModuleManager.Instance.ResolveModuleAsync(moduleType);
            }
            catch
            {
                ReleaseParametersIfUnowned(ownedParameters);
                throw;
            }

            string relativePath = $"{module.Id}/{module.Entry}";
            string assetName;
            try
            {
                assetName = UtilityBuiltin.AssetsPath.GetUIFormPath(relativePath);
            }
            catch
            {
                ReleaseParametersIfUnowned(ownedParameters);
                throw;
            }

            string routeKey = GetModuleRouteKey(moduleType);
            UIFormLogic logic = await OpenFormAsync(assetName, uiGroupName, ModuleLoadPriority,
                false, ownedParameters, routeKey, null);
            return CastForm<TForm>(logic, relativePath);
        }

        /// <summary>
        /// 在父面板所在 UI 组中异步打开子表单，并让父面板负责关闭它。
        /// </summary>
        /// <typeparam name="TForm">目标表单逻辑类型。</typeparam>
        /// <param name="parent">负责管理子表单生命周期的父面板。</param>
        /// <param name="relativePath">相对于 UI 根目录的子表单资源路径。</param>
        /// <param name="parameters">传给子表单的参数；所有权转交给服务。</param>
        /// <param name="subUiOrder">子表单相对父面板的排序偏移；负数表示追加到现有子表单之后。</param>
        /// <returns>加载并打开后的子表单逻辑。</returns>
        public async UniTask<TForm> OpenSubAsync<TForm>(UGF_Panel parent, string relativePath,
            UIParams parameters = null, int subUiOrder = -1) where TForm : UIFormLogic
        {
            UIParams ownedParameters = parameters ?? UIParams.Create();
            EnsureParametersAvailable(ownedParameters);
            if (parent == null || parent.UIForm == null || parent.UIForm.UIGroup == null)
            {
                ReleaseParametersIfUnowned(ownedParameters);
                throw new ArgumentNullException(nameof(parent), "打开子界面时必须提供有效的父面板。");
            }

            string assetName;
            try
            {
                assetName = UtilityBuiltin.AssetsPath.GetUIFormPath(relativePath);
                int order = subUiOrder < 0 ? parent.SubUIFormCount : subUiOrder;
                ownedParameters.SortOrder = parent.Depth + order + SubUIOrderOffset;
                ownedParameters.IsSubUIForm = true;
            }
            catch
            {
                ReleaseParametersIfUnowned(ownedParameters);
                throw;
            }

            UIFormLogic logic = await OpenFormAsync(assetName, parent.UIForm.UIGroup.Name, SubUIFormLoadPriority, false,
                ownedParameters, null, parent);
            return CastForm<TForm>(logic, relativePath);
        }

        /// <summary>
        /// 异步打开一个允许多实例的提示表单。
        /// </summary>
        /// <typeparam name="TForm">目标表单逻辑类型。</typeparam>
        /// <param name="relativePath">相对于 UI 根目录的提示资源路径。</param>
        /// <param name="parameters">传给提示表单的参数；所有权转交给服务。</param>
        /// <returns>加载并打开后的提示表单逻辑。</returns>
        public async UniTask<TForm> OpenTipAsync<TForm>(string relativePath, UIParams parameters = null)
            where TForm : UIFormLogic
        {
            UIParams ownedParameters = parameters ?? UIParams.Create();
            EnsureParametersAvailable(ownedParameters);
            string assetName;
            try
            {
                assetName = UtilityBuiltin.AssetsPath.GetUIFormPath(relativePath);
            }
            catch
            {
                ReleaseParametersIfUnowned(ownedParameters);
                throw;
            }

            UIFormLogic logic = await OpenFormAsync(assetName, TipUIGroupName, TipLoadPriority,
                false, ownedParameters, null, null);
            return CastForm<TForm>(logic, relativePath);
        }

        /// <summary>
        /// 判断模块目录下的覆盖界面是否已打开或正在加载。
        /// </summary>
        /// <param name="moduleType">覆盖界面所属的模块类型。</param>
        /// <param name="uiName">模块目录下的 UI 资源名称，不包含目录前缀。</param>
        /// <returns>表单已打开或正在加载时返回 true。</returns>
        public bool IsOpen(ModuleType moduleType, string uiName)
        {
            if (string.IsNullOrWhiteSpace(uiName))
            {
                return false;
            }

            string relativePath = $"{moduleType}/{uiName}";
            string assetName = UtilityBuiltin.AssetsPath.GetUIFormPath(relativePath);
            return IsFormOpen(assetName, GetOverlayRouteKey(moduleType, uiName));
        }

        /// <summary>
        /// 判断模块入口表单是否已打开或正在加载。
        /// </summary>
        /// <param name="moduleType">要查询的模块类型。</param>
        /// <returns>表单已打开或正在加载时返回 true。</returns>
        public bool IsOpen(ModuleType moduleType)
        {
            if (!UIModuleManager.Instance.TryGetModule(moduleType, out GameModule module))
            {
                return false;
            }

            string relativePath = $"{module.Id}/{module.Entry}";
            string assetName = UtilityBuiltin.AssetsPath.GetUIFormPath(relativePath);
            return IsFormOpen(assetName, GetModuleRouteKey(moduleType));
        }

        /// <summary>
        /// 判断指定相对资源路径的表单是否已打开或正在加载。
        /// </summary>
        /// <param name="relativePath">相对于 UI 根目录的资源路径。</param>
        /// <returns>表单已打开或正在加载时返回 true。</returns>
        public bool IsOpen(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            string assetName = UtilityBuiltin.AssetsPath.GetUIFormPath(relativePath);
            return IsFormOpen(assetName, null);
        }

        /// <summary>
        /// 通过表单逻辑关闭界面，并保留表单自身的关闭生命周期处理。
        /// </summary>
        /// <param name="form">要关闭的表单逻辑。</param>
        public void Close(UIFormLogic form)
        {
            if (form?.UIForm == null || !GF.UI.HasUIForm(form.UIForm.SerialId))
            {
                return;
            }

            GF.UI.CloseUIForm(form.UIForm.SerialId);
        }

        /// <summary>
        /// 按实例策略检查已有表单后开始一次 Game Framework 打开请求。
        /// </summary>
        /// <param name="assetName">Game Framework 识别的完整资源名称。</param>
        /// <param name="uiGroupName">表单所属 UI 组。</param>
        /// <param name="priority">资源加载优先级。</param>
        /// <param name="pauseCoveredUIForm">打开时是否暂停底层表单。</param>
        /// <param name="parameters">由服务持有的 UI 参数。</param>
        /// <param name="routeKey">单实例路由键；多实例打开时为空。</param>
        /// <param name="parent">子表单所属父面板；普通表单时为空。</param>
        /// <returns>打开后的表单逻辑。</returns>
        private async UniTask<UIFormLogic> OpenFormAsync(string assetName, string uiGroupName, int priority,
            bool pauseCoveredUIForm, UIParams parameters, string routeKey, UGF_Panel parent)
        {
            EnsureParametersAvailable(parameters);
            EnsureEventsSubscribed();
            if (string.IsNullOrWhiteSpace(assetName) || string.IsNullOrWhiteSpace(uiGroupName))
            {
                parameters.Release();
                throw new InvalidOperationException(
                    $"UI 打开参数无效，资源：'{assetName ?? "<null>"}'，UI 组：'{uiGroupName ?? "<null>"}'。");
            }

            if (!string.IsNullOrEmpty(routeKey))
            {
                if (_pendingByRoute.TryGetValue(routeKey, out PendingUIOpen pending))
                {
                    parameters.Release();
                    return await pending.Completion.Task;
                }

                if (TryGetOpenedForm(routeKey, assetName, out UIFormLogic openedForm))
                {
                    parameters.Release();
                    return openedForm;
                }
            }

            if (GF.UI.IsLoadingUIForm(assetName) && !string.IsNullOrEmpty(routeKey))
            {
                parameters.Release();
                throw new InvalidOperationException(
                    $"UI 资源 '{assetName}' 正在由 UIService 之外的调用加载；请统一改用 UIService 打开。");
            }

            var request = new PendingUIOpen(assetName, routeKey, parameters);
            _ownedParameters.Add(parameters, request);
            parameters.ReleasingCallback = OnParametersReleasing;
            _pendingByParameters.Add(parameters, request);
            if (!string.IsNullOrEmpty(routeKey))
            {
                _pendingByRoute.Add(routeKey, request);
            }

            try
            {
                int serialId = GF.UI.OpenUIForm(assetName, uiGroupName, priority, pauseCoveredUIForm, parameters);
                if (serialId < 0)
                {
                    RemovePendingRequest(request);
                    parameters.Release();
                    throw new InvalidOperationException($"Game Framework 未能启动 UI 加载：'{assetName}'。");
                }

                request.SerialId = serialId;
                // 只登记仍属于本次请求的参数，避免 OnOpen 中关闭表单后的参数复用。
                if (_pendingByParameters.TryGetValue(parameters, out PendingUIOpen pending) && pending == request)
                {
                    _pendingBySerial.Add(serialId, request);
                }
            }
            catch
            {
                RemovePendingRequest(request);
                parameters.Release();
                throw;
            }

            // 失败和取消入口负责释放；等待结束后不能再次操作可能已复用的参数。
            UIFormLogic logic = await request.Completion.Task;

            if (parent != null && !parent.TryRegisterSubUIForm(logic.UIForm.SerialId))
            {
                Close(logic);
                throw new InvalidOperationException(
                    $"父面板已关闭，无法继续保留子表单：'{assetName}'。");
            }

            return logic;
        }

        /// <summary>
        /// 订阅 Game Framework 的表单打开和关闭事件。
        /// </summary>
        private void EnsureEventsSubscribed()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GF.Event.Subscribe(OpenUIFormFailureEventArgs.EventId, OnOpenUIFormFailure);
            GF.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIFormComplete);
            Application.quitting += OnApplicationQuitting;
            _eventsSubscribed = true;
        }

        /// <summary>
        /// 接收表单打开成功事件并将结果交给等待请求。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">表单打开成功事件参数。</param>
        private void OnOpenUIFormSuccess(object sender, GameEventArgs eventArgs)
        {
            var args = (OpenUIFormSuccessEventArgs)eventArgs;
            int serialId = args.UIForm.SerialId;
            if (!TryGetPendingRequest(serialId, args.UserData, out PendingUIOpen request))
            {
                return;
            }

            _pendingBySerial.Remove(serialId);
            _pendingByParameters.Remove(request.Parameters);
            if (args.UIForm?.Logic == null)
            {
                RemovePendingRoute(request);
                _ownedParameters.Remove(request.Parameters);
                request.Parameters.Release();
                request.Completion.TrySetException(new InvalidOperationException(
                    $"UI 打开成功事件没有表单逻辑：'{request.AssetName}'。"));
                return;
            }

            if (string.IsNullOrEmpty(request.RouteKey))
            {
                _parametersByOpenedSerial[serialId] = request.Parameters;
                request.Completion.TrySetResult(args.UIForm.Logic);
                return;
            }

            RemovePendingRoute(request);
            _openedSerialByRoute[request.RouteKey] = serialId;
            _routeByOpenedSerial[serialId] = request.RouteKey;
            _parametersByOpenedSerial[serialId] = request.Parameters;
            request.Completion.TrySetResult(args.UIForm.Logic);
        }

        /// <summary>
        /// 接收表单打开失败事件，释放参数并结束对应等待请求。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">表单打开失败事件参数。</param>
        private void OnOpenUIFormFailure(object sender, GameEventArgs eventArgs)
        {
            var args = (OpenUIFormFailureEventArgs)eventArgs;
            if (!TryGetPendingRequest(args.SerialId, args.UserData, out PendingUIOpen request))
            {
                return;
            }

            _pendingBySerial.Remove(args.SerialId);
            _pendingByParameters.Remove(request.Parameters);
            RemovePendingRoute(request);
            _ownedParameters.Remove(request.Parameters);
            request.Parameters.Release();
            var exception = new InvalidOperationException(
                $"打开 UI 失败，资源：'{args.UIFormAssetName}'，组：'{args.UIGroupName}'，原因：{args.ErrorMessage}");
            Log.Error(exception.ToString());
            request.Completion.TrySetException(exception);
        }

        /// <summary>
        /// 接收表单关闭完成事件并清理单实例路由缓存。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="eventArgs">表单关闭完成事件参数。</param>
        private void OnCloseUIFormComplete(object sender, GameEventArgs eventArgs)
        {
            var args = (CloseUIFormCompleteEventArgs)eventArgs;
            // 参数可能已在同一帧归还并被新表单复用；迟到事件只清理旧序列号。
            // 带关闭动画的面板则在实际 Release 时才解除参数所有权。
            _parametersByOpenedSerial.Remove(args.SerialId);

            if (!_routeByOpenedSerial.TryGetValue(args.SerialId, out string routeKey))
            {
                return;
            }

            _routeByOpenedSerial.Remove(args.SerialId);
            if (_openedSerialByRoute.TryGetValue(routeKey, out int openedSerial) && openedSerial == args.SerialId)
            {
                _openedSerialByRoute.Remove(routeKey);
            }

        }

        /// <summary>
        /// 应用退出时释放尚未完成加载的 UI 参数并结束等待请求。
        /// </summary>
        private void OnApplicationQuitting()
        {
            foreach (PendingUIOpen request in _pendingByParameters.Values)
            {
                _ownedParameters.Remove(request.Parameters);
                request.Parameters.Release();
                request.Completion.TrySetException(new OperationCanceledException(
                    $"应用退出时 UI 加载尚未完成：'{request.AssetName}'。"));
            }

            _ownedParameters.Clear();
            _pendingBySerial.Clear();
            _pendingByParameters.Clear();
            _pendingByRoute.Clear();
            _parametersByOpenedSerial.Clear();
            _openedSerialByRoute.Clear();
            _routeByOpenedSerial.Clear();
        }

        /// <summary>
        /// 查询单实例路由当前表单，必要时兼容服务启动前已打开的表单。
        /// </summary>
        /// <param name="routeKey">单实例路由键。</param>
        /// <param name="assetName">该路由对应的完整资源名称。</param>
        /// <param name="logic">查到的表单逻辑。</param>
        /// <returns>表单仍打开时返回 true。</returns>
        private bool TryGetOpenedForm(string routeKey, string assetName, out UIFormLogic logic)
        {
            logic = null;
            if (_openedSerialByRoute.TryGetValue(routeKey, out int serialId))
            {
                if (GF.UI.HasUIForm(serialId))
                {
                    logic = GF.UI.GetUIForm(serialId)?.Logic;
                    if (logic != null)
                    {
                        return true;
                    }
                }

                _openedSerialByRoute.Remove(routeKey);
                _routeByOpenedSerial.Remove(serialId);
            }

            UIForm existingForm = GF.UI.GetUIForm(assetName);
            if (existingForm?.Logic == null)
            {
                return false;
            }

            logic = existingForm.Logic;
            _openedSerialByRoute[routeKey] = existingForm.SerialId;
            _routeByOpenedSerial[existingForm.SerialId] = routeKey;
            return true;
        }

        /// <summary>
        /// 判断资源对应的表单是否已经打开或正在加载。
        /// </summary>
        /// <param name="assetName">Game Framework 识别的完整资源名称。</param>
        /// <param name="routeKey">单实例路由键；多实例查询时为空。</param>
        /// <returns>表单已打开或正在加载时返回 true。</returns>
        private bool IsFormOpen(string assetName, string routeKey)
        {
            if (!string.IsNullOrEmpty(routeKey) && _pendingByRoute.ContainsKey(routeKey))
            {
                return true;
            }

            return GF.UI.HasUIForm(assetName) || GF.UI.IsLoadingUIForm(assetName);
        }

        /// <summary>
        /// 按请求状态移除等待中的路由索引。
        /// </summary>
        /// <param name="request">要移除的打开请求。</param>
        private void RemovePendingRequest(PendingUIOpen request)
        {
            if (request.SerialId > 0)
            {
                _pendingBySerial.Remove(request.SerialId);
            }

            _pendingByParameters.Remove(request.Parameters);
            _ownedParameters.Remove(request.Parameters);
            RemovePendingRoute(request);
        }

        /// <summary>
        /// 参数归还对象池前同步解除所有权，阻止旧关闭事件清理复用后的参数。
        /// </summary>
        /// <param name="parameters">即将归还对象池的参数；此时仍属于当前借用周期。</param>
        private void OnParametersReleasing(UIParams parameters)
        {
            if (!_ownedParameters.TryGetValue(parameters, out PendingUIOpen request))
            {
                return;
            }

            _ownedParameters.Remove(parameters);
            if (_parametersByOpenedSerial.TryGetValue(request.SerialId, out UIParams openedParameters) &&
                ReferenceEquals(openedParameters, parameters))
            {
                _parametersByOpenedSerial.Remove(request.SerialId);
            }

            if (_pendingByParameters.TryGetValue(parameters, out PendingUIOpen pending) && pending == request)
            {
                RemovePendingRequest(request);
                request.Completion.TrySetException(new OperationCanceledException(
                    $"UI 打开完成前参数已释放：'{request.AssetName}'。"));
            }
        }

        /// <summary>
        /// 按序列号匹配打开请求，避免迟到事件通过已复用的参数匹配到新请求。
        /// </summary>
        /// <param name="serialId">事件对应的表单序列号。</param>
        /// <param name="userData">事件携带的打开参数，可能已被对象池复用。</param>
        /// <param name="request">匹配的请求；未匹配时为空。</param>
        /// <returns>事件仍属于当前等待请求时返回真。</returns>
        private bool TryGetPendingRequest(int serialId, object userData, out PendingUIOpen request)
        {
            if (_pendingBySerial.TryGetValue(serialId, out request))
            {
                return ReferenceEquals(request.Parameters, userData);
            }

            if (userData is UIParams parameters &&
                _pendingByParameters.TryGetValue(parameters, out request) &&
                (request.SerialId == 0 || request.SerialId == serialId))
            {
                return true;
            }

            request = null;
            return false;
        }

        /// <summary>
        /// 检查参数对象是否已转交给另一个打开请求或活动表单。
        /// </summary>
        /// <param name="parameters">准备转交所有权的参数对象。</param>
        internal void EnsureParametersAvailable(UIParams parameters)
        {
            if (_ownedParameters.ContainsKey(parameters))
            {
                throw new InvalidOperationException("同一个 UIParams 实例不能同时交给多个 UI 打开请求。");
            }
        }

        /// <summary>
        /// 仅在参数尚未被其他表单持有时归还对象池。
        /// </summary>
        /// <param name="parameters">打开流程准备释放的参数对象。</param>
        private void ReleaseParametersIfUnowned(UIParams parameters)
        {
            if (!_ownedParameters.ContainsKey(parameters))
            {
                parameters.Release();
            }
        }

        /// <summary>
        /// 移除仍指向指定请求的单实例路由索引。
        /// </summary>
        /// <param name="request">要从路由索引移除的打开请求。</param>
        private void RemovePendingRoute(PendingUIOpen request)
        {
            if (!string.IsNullOrEmpty(request.RouteKey) &&
                _pendingByRoute.TryGetValue(request.RouteKey, out PendingUIOpen current) && current == request)
            {
                _pendingByRoute.Remove(request.RouteKey);
            }
        }

        /// <summary>
        /// 验证打开结果是否符合调用方指定的表单类型。
        /// </summary>
        /// <typeparam name="TForm">调用方要求的表单逻辑类型。</typeparam>
        /// <param name="logic">实际打开的表单逻辑。</param>
        /// <param name="routeName">用于定位表单的路由或资源路径。</param>
        /// <returns>类型匹配后的表单逻辑。</returns>
        private TForm CastForm<TForm>(UIFormLogic logic, string routeName) where TForm : UIFormLogic
        {
            if (logic is TForm typedForm)
            {
                return typedForm;
            }

            string actualType = logic == null ? "<null>" : logic.GetType().FullName;
            throw new InvalidCastException(
                $"UI 路由 '{routeName}' 的逻辑类型为 '{actualType}'，无法转换为 '{typeof(TForm).FullName}'。");
        }

        /// <summary>
        /// 生成模块覆盖界面使用的稳定缓存键。
        /// </summary>
        /// <param name="moduleType">覆盖界面所属的模块类型。</param>
        /// <param name="uiName">模块目录下的 UI 资源名称。</param>
        /// <returns>覆盖界面的缓存键。</returns>
        private string GetOverlayRouteKey(ModuleType moduleType, string uiName)
        {
            return $"Overlay:{(int)moduleType}:{uiName}";
        }

        /// <summary>
        /// 生成模块入口使用的稳定缓存键。
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <returns>模块缓存键。</returns>
        private string GetModuleRouteKey(ModuleType moduleType)
        {
            return $"Module:{(int)moduleType}";
        }

        /// <summary>
        /// 保存一次表单打开请求的异步结果及其 UI 参数所有权。
        /// </summary>
        private sealed class PendingUIOpen
        {
            /// <summary>本次打开使用的完整资源名称。</summary>
            public string AssetName { get; }

            /// <summary>本次打开所属的单实例路由键。</summary>
            public string RouteKey { get; }

            /// <summary>打开请求持有的参数对象。</summary>
            public UIParams Parameters { get; }

            /// <summary>等待 Game Framework 打开结果的异步完成源。</summary>
            public UniTaskCompletionSource<UIFormLogic> Completion { get; } =
                new UniTaskCompletionSource<UIFormLogic>();

            /// <summary>Game Framework 分配给本次打开请求的序列号。</summary>
            public int SerialId { get; set; }

            /// <summary>
            /// 创建表单打开请求。
            /// </summary>
            /// <param name="assetName">本次打开使用的完整资源名称。</param>
            /// <param name="routeKey">本次打开所属的单实例路由键。</param>
            /// <param name="parameters">本次打开持有的参数。</param>
            public PendingUIOpen(string assetName, string routeKey, UIParams parameters)
            {
                AssetName = assetName;
                RouteKey = routeKey;
                Parameters = parameters;
            }
        }
    }
}
