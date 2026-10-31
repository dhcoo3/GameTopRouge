using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput;
using AAAGame.ScriptsHotfix.GamePlay.Render.RenderManager;
using GameFramework;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.GamePlay.Render.Joystick
{
    /// <summary>
    /// 管理战斗中的固定虚拟摇杆、指针移动输入和编辑器键盘测试输入。
    /// </summary>
    public class JoystickRender : GameRender
    {
        /// <summary>
        /// 摇杆移动方向的归一化死区比例，范围为 0 到 1。
        /// </summary>
        private const float MovementDeadZone = 0.01f;

        /// <summary>
        /// 摇杆中心与屏幕安全区域左、下边缘的距离，单位为 Canvas 坐标。
        /// </summary>
        private const float JoystickEdgePadding = 150f;

        /// <summary>
        /// 表示当前没有由摇杆接管的触摸手指。
        /// </summary>
        private const int NoActiveTouch = -1;

        /// <summary>
        /// 战斗摇杆预制体的资源路径。
        /// </summary>
        private const string AssetPath = "Assets/AAAGame/Prefabs/UI/Joystick/Joystick.prefab";

        /// <summary>
        /// 当前创建的摇杆预制体实例；清理渲染器时销毁。
        /// </summary>
        private GameObject _joystickInstance;

        /// <summary>
        /// 摇杆根节点，用于根据屏幕安全区域定位整体摇杆。
        /// </summary>
        private RectTransform _joystickRoot;

        /// <summary>
        /// 摇杆底座区域，用于检测新触摸或鼠标按下是否可被接管。
        /// </summary>
        private RectTransform _baseArea;

        /// <summary>
        /// 可移动的摇杆帽节点，释放指针后回到底座中心。
        /// </summary>
        private RectTransform _joystickHandle;

        /// <summary>
        /// 摇杆所在画布，用于确定屏幕坐标转换所需的事件相机。
        /// </summary>
        private Canvas _joystickCanvas;

        /// <summary>
        /// 接收摇杆移动和编辑器技能测试输入的逻辑层代理。
        /// </summary>
        private InputProxy _inputProxy;

        /// <summary>
        /// 指示渲染器是否已初始化并订阅战斗加载事件。
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// 指示摇杆预制体是否正在加载，用于避免重复请求。
        /// </summary>
        private bool _loadRequested;

        /// <summary>
        /// 指示是否已发送移动输入，用于在停止时只发送一次零输入。
        /// </summary>
        private bool _movementInputActive;

        /// <summary>
        /// 指示鼠标左键是否已从摇杆底座区域开始并被摇杆接管。
        /// </summary>
        private bool _mouseCaptured;

        /// <summary>
        /// 当前由摇杆接管的触摸手指编号；无接管手指时为 <see cref="NoActiveTouch"/>。
        /// </summary>
        private int _activeTouchId = NoActiveTouch;

        /// <summary>
        /// 摇杆资源加载请求的代数，用于使清理前发出的异步回调失效。
        /// </summary>
        private int _loadGeneration;

        /// <summary>
        /// 上次定位摇杆时使用的屏幕宽度，单位为像素；零表示尚未缓存。
        /// </summary>
        private int _lastScreenWidth;

        /// <summary>
        /// 上次定位摇杆时使用的屏幕高度，单位为像素；零表示尚未缓存。
        /// </summary>
        private int _lastScreenHeight;

        /// <summary>
        /// 上次定位摇杆时使用的屏幕安全区域，单位为屏幕像素。
        /// </summary>
        private Rect _lastSafeArea;

        /// <summary>
        /// 初始化摇杆渲染器并订阅战斗加载事件；重复初始化会被忽略。
        /// </summary>
        public override void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _inputProxy = GetProxy<InputProxy>();
            if (_inputProxy == null)
            {
                Log.Error("Joystick cannot send movement because InputProxy is unavailable.");
            }

            ResetInputState();
            _loadRequested = false;
            _initialized = true;
            Subscribe(GamePlayEvent.EFightLoadingFinish, FightLoadingFinish);
        }

        /// <summary>
        /// 清理摇杆实例、异步加载回调、输入状态和战斗事件订阅。
        /// </summary>
        public override void Clear()
        {
            _initialized = false;
            _loadGeneration++;
            _loadRequested = false;
            Unsubscribe(GamePlayEvent.EFightLoadingFinish, FightLoadingFinish);
            DestroyJoystick();
            ResetInputState();
            _inputProxy = null;
            base.Clear();
        }

        /// <summary>
        /// 请求加载战斗摇杆预制体；渲染器未初始化、已加载或正在加载时不重复请求。
        /// </summary>
        private void LoadJoystick()
        {
            if (!_initialized || _loadRequested || _joystickInstance != null)
            {
                return;
            }

            if (GF.AssetBridge == null)
            {
                Log.Error("Cannot load joystick because the asset bridge is unavailable.");
                return;
            }

            _loadRequested = true;
            int generation = ++_loadGeneration;
            GF.AssetBridge.LoadAsset(AssetPath, assetInfo => LoadCall(assetInfo, generation), typeof(GameObject));
        }

        /// <summary>
        /// 接收摇杆预制体加载结果，并忽略清理后返回的过期回调。
        /// </summary>
        /// <param name="assetInfo">资源加载回调提供的摇杆预制体信息。</param>
        /// <param name="generation">本次资源请求的序号，用于识别过期回调。</param>
        private void LoadCall(AssetInfo assetInfo, int generation)
        {
            if (!_initialized || generation != _loadGeneration)
            {
                return;
            }

            _loadRequested = false;
            GameObject prefab = assetInfo == null ? null : assetInfo.Asset as GameObject;
            if (prefab == null)
            {
                Log.Error("Joystick prefab asset is unavailable: {0}.", AssetPath);
                return;
            }

            if (GF.UIRoot == null)
            {
                Log.Error("Cannot create joystick because the UI root is unavailable.");
                return;
            }

            CreateJoystick(prefab);
        }

        /// <summary>
        /// 实例化摇杆视图并缓存底座、摇杆帽和画布引用。
        /// </summary>
        /// <param name="prefab">已加载的摇杆预制体，必须包含 Base 和 Handle 子节点。</param>
        private void CreateJoystick(GameObject prefab)
        {
            _joystickInstance = GameObject.Instantiate(prefab, GF.UIRoot, false);
            _joystickRoot = _joystickInstance.GetComponent<RectTransform>();
            if (_joystickRoot == null)
            {
                Log.Error("Joystick prefab root must have a RectTransform.");
                DestroyJoystick();
                return;
            }

            _baseArea = _joystickRoot.Find("Base") as RectTransform;
            _joystickHandle = _joystickRoot.Find("Handle") as RectTransform;
            if (_baseArea == null || _joystickHandle == null)
            {
                Log.Error("Joystick prefab must contain Base and Handle RectTransforms.");
                DestroyJoystick();
                return;
            }

            _joystickCanvas = _joystickRoot.GetComponentInParent<Canvas>();
            _joystickHandle.anchoredPosition = Vector2.zero;
            UpdateSafeAreaPosition();
        }

        /// <summary>
        /// 战斗资源加载完成时显示摇杆。
        /// </summary>
        /// <param name="sender">触发事件的对象。</param>
        /// <param name="e">战斗加载完成事件参数。</param>
        private void FightLoadingFinish(object sender, GameEvent e)
        {
            LoadJoystick();
        }

        /// <summary>
        /// 每个渲染帧更新安全区、移动输入和编辑器技能键。
        /// </summary>
        /// <param name="deltaTime">当前渲染帧的时间间隔。</param>
        public override void RenderUpdate(fix deltaTime)
        {
            UpdateSafeAreaPosition();
            ApplyMovementInput(GetMovementInput());
            HandleEditorSkillInput();
            base.RenderUpdate(deltaTime);
        }

        /// <summary>
        /// 读取摇杆触摸或鼠标方向；未接管指针时在编辑器读取原始轴输入。
        /// </summary>
        /// <returns>当前帧的移动方向；无输入时返回零向量。</returns>
        private Vector2 GetMovementInput()
        {
            if (UpdatePointerInput(out Vector2 pointerInput))
            {
                return pointerInput;
            }

            if (!Application.isEditor)
            {
                return Vector2.zero;
            }

            // 使用原始轴值，避免 Unity 轴平滑导致帧同步输入延迟。
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        /// <summary>
        /// 在编辑器中读取原有技能测试按键。
        /// </summary>
        private void HandleEditorSkillInput()
        {
            if (!Application.isEditor || _inputProxy == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                _inputProxy.Input(KeyCode.K);
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                _inputProxy.Input(KeyCode.L);
            }
        }

        /// <summary>
        /// 采样摇杆触摸或鼠标，当前指针释放前不允许其他指针抢占。
        /// </summary>
        /// <param name="movementInput">输出的单位长度移动方向。</param>
        /// <returns>当前是否有触摸或鼠标由摇杆接管。</returns>
        private bool UpdatePointerInput(out Vector2 movementInput)
        {
            movementInput = Vector2.zero;
            if (_joystickInstance == null || _baseArea == null || _joystickHandle == null)
            {
                return false;
            }

            if (UpdateCapturedPointer(out movementInput))
            {
                return true;
            }

            UnityEngine.Camera eventCamera = GetEventCamera();
            return TryCaptureTouch(eventCamera, out movementInput) || TryCaptureMouse(eventCamera, out movementInput);
        }

        /// <summary>
        /// 跟踪当前已接管的手指或鼠标，并在对应指针释放时复位。
        /// </summary>
        /// <param name="movementInput">输出的单位长度移动方向。</param>
        /// <returns>当前指针仍被摇杆接管时返回真。</returns>
        private bool UpdateCapturedPointer(out Vector2 movementInput)
        {
            movementInput = Vector2.zero;
            if (_activeTouchId != NoActiveTouch)
            {
                return UpdateCapturedTouch(out movementInput);
            }

            if (!_mouseCaptured)
            {
                return false;
            }

            if (!Input.GetMouseButton(0))
            {
                ReleasePointer();
                return false;
            }

            UpdateHandlePosition(Input.mousePosition, out movementInput);
            return true;
        }

        /// <summary>
        /// 按已接管的手指编号更新摇杆，触摸结束或丢失时复位。
        /// </summary>
        /// <param name="movementInput">输出的单位长度移动方向。</param>
        /// <returns>已接管手指仍有效时返回真。</returns>
        private bool UpdateCapturedTouch(out Vector2 movementInput)
        {
            movementInput = Vector2.zero;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId != _activeTouchId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    ReleasePointer();
                    return false;
                }

                UpdateHandlePosition(touch.position, out movementInput);
                return true;
            }

            ReleasePointer();
            return false;
        }

        /// <summary>
        /// 接管从摇杆底座区域开始的单指触摸。
        /// </summary>
        /// <param name="eventCamera">当前画布用于屏幕坐标转换的相机，可为空。</param>
        /// <param name="movementInput">输出的单位长度移动方向。</param>
        /// <returns>接管了底座区域内新触摸时返回真。</returns>
        private bool TryCaptureTouch(UnityEngine.Camera eventCamera, out Vector2 movementInput)
        {
            movementInput = Vector2.zero;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began ||
                    !RectTransformUtility.RectangleContainsScreenPoint(_baseArea, touch.position, eventCamera))
                {
                    continue;
                }

                _activeTouchId = touch.fingerId;
                UpdateHandlePosition(touch.position, out movementInput);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 接管从摇杆底座区域开始的鼠标左键操作。
        /// </summary>
        /// <param name="eventCamera">当前画布用于屏幕坐标转换的相机，可为空。</param>
        /// <param name="movementInput">输出的单位长度移动方向。</param>
        /// <returns>鼠标在底座区域按下时返回真。</returns>
        private bool TryCaptureMouse(UnityEngine.Camera eventCamera, out Vector2 movementInput)
        {
            movementInput = Vector2.zero;
            if (Input.touchCount > 0 || !Input.GetMouseButtonDown(0) ||
                !RectTransformUtility.RectangleContainsScreenPoint(_baseArea, Input.mousePosition, eventCamera))
            {
                return false;
            }

            _mouseCaptured = true;
            UpdateHandlePosition(Input.mousePosition, out movementInput);
            return true;
        }

        /// <summary>
        /// 将屏幕坐标换算为底座偏移，限制摇杆帽行程并输出单位长度方向。
        /// </summary>
        /// <param name="screenPosition">当前触摸或鼠标的屏幕坐标。</param>
        /// <param name="movementInput">输出的单位长度移动方向；死区内为零向量。</param>
        private void UpdateHandlePosition(Vector2 screenPosition, out Vector2 movementInput)
        {
            movementInput = Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _baseArea, screenPosition, GetEventCamera(), out Vector2 localPosition))
            {
                return;
            }

            float travelRadius = GetHandleTravelRadius();
            if (travelRadius <= 0f)
            {
                _joystickHandle.anchoredPosition = Vector2.zero;
                return;
            }

            Vector2 clampedPosition = Vector2.ClampMagnitude(localPosition, travelRadius);
            _joystickHandle.anchoredPosition = clampedPosition;

            Vector2 normalizedOffset = clampedPosition / travelRadius;
            if (normalizedOffset.sqrMagnitude > MovementDeadZone * MovementDeadZone)
            {
                // 摇杆只决定方向；超过死区后始终按满速移动，力度不随拖动距离变化。
                movementInput = normalizedOffset.normalized;
            }
        }

        /// <summary>
        /// 根据底座和摇杆帽尺寸计算摇杆帽中心允许的最大移动距离。
        /// </summary>
        /// <returns>底座半径扣除摇杆帽半径后的行程，单位为 Canvas 坐标。</returns>
        private float GetHandleTravelRadius()
        {
            float baseRadius = Mathf.Min(_baseArea.rect.width, _baseArea.rect.height) * 0.5f;
            float handleRadius = Mathf.Max(_joystickHandle.rect.width, _joystickHandle.rect.height) * 0.5f;
            return Mathf.Max(0f, baseRadius - handleRadius);
        }

        /// <summary>
        /// 将移动方向传给逻辑层，并在停止时只补发一次零输入。
        /// </summary>
        /// <param name="movementInput">当前帧的水平和垂直移动方向。</param>
        private void ApplyMovementInput(Vector2 movementInput)
        {
            if (_inputProxy == null)
            {
                _movementInputActive = false;
                return;
            }

            if (movementInput.sqrMagnitude <= MovementDeadZone * MovementDeadZone)
            {
                if (_movementInputActive)
                {
                    _inputProxy.Input(0f, 0f);
                    _movementInputActive = false;
                }

                return;
            }

            _inputProxy.Input(movementInput.x, movementInput.y);
            _movementInputActive = true;
        }

        /// <summary>
        /// 结束当前摇杆触摸或鼠标操作并将摇杆帽复位。
        /// </summary>
        private void ReleasePointer()
        {
            _activeTouchId = NoActiveTouch;
            _mouseCaptured = false;
            if (_joystickHandle != null)
            {
                _joystickHandle.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// 按屏幕安全区域更新摇杆锚点，并在安全区域或窗口尺寸变化时跳过重复写入。
        /// </summary>
        private void UpdateSafeAreaPosition()
        {
            if (_joystickRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = GetScreenSafeArea();
            if (_lastScreenWidth == Screen.width && _lastScreenHeight == Screen.height && _lastSafeArea == safeArea)
            {
                return;
            }

            Vector2 safeAreaAnchor = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            _joystickRoot.anchorMin = safeAreaAnchor;
            _joystickRoot.anchorMax = safeAreaAnchor;
            _joystickRoot.pivot = new Vector2(0.5f, 0.5f);
            _joystickRoot.anchoredPosition = new Vector2(JoystickEdgePadding, JoystickEdgePadding);

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastSafeArea = safeArea;
        }

        /// <summary>
        /// 获取设备安全区域；平台未提供有效区域时使用整个屏幕。
        /// </summary>
        /// <returns>有效的屏幕安全区域，单位为屏幕像素。</returns>
        private Rect GetScreenSafeArea()
        {
            Rect safeArea = Screen.safeArea;
            if (safeArea.width > 0f && safeArea.height > 0f)
            {
                return safeArea;
            }

            return new Rect(0f, 0f, Screen.width, Screen.height);
        }

        /// <summary>
        /// 返回适用于当前 UI 画布的屏幕坐标转换相机。
        /// </summary>
        /// <returns>屏幕空间覆盖画布返回空值，其他画布返回其世界相机。</returns>
        private UnityEngine.Camera GetEventCamera()
        {
            if (_joystickCanvas == null || _joystickCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return _joystickCanvas.worldCamera;
        }

        /// <summary>
        /// 释放摇杆实例并清空预制体及安全区缓存引用。
        /// </summary>
        private void DestroyJoystick()
        {
            if (_joystickInstance != null)
            {
                Object.Destroy(_joystickInstance);
            }

            _joystickInstance = null;
            _joystickRoot = null;
            _baseArea = null;
            _joystickHandle = null;
            _joystickCanvas = null;
            _lastScreenWidth = 0;
            _lastScreenHeight = 0;
            _lastSafeArea = new Rect();
        }

        /// <summary>
        /// 将当前移动输入与指针接管状态恢复到未操作状态。
        /// </summary>
        private void ResetInputState()
        {
            _movementInputActive = false;
            _mouseCaptured = false;
            _activeTouchId = NoActiveTouch;
        }
    }
}
