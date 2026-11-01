# UI 预制与面板逻辑规范

适用于客户端 UI Prefab、面板脚本及其控件绑定。以 `LoginPanel.prefab` 和 `LoginPanel.cs` 为结构参考，结合当前 `UGF_Panel`、`UIContainer` 实现制定。组件访问限制以本规范为准，参考代码中的历史写法不作为例外。视觉和文案要求同时遵循 [UI 美术规范](UI美术规范.md) 与 [客户端代码规范](客户端代码规范.md)。

## 资源与层级

- 面板预制体放在 `Client/GameTopRouge/Assets/AAAGame/Prefabs/UI/<模块>/`；逻辑放在 `ScriptsHotfix/UI/Module/<模块>/`。根节点、Prefab 与面板类使用同一语义名称，例如 `LoginPanel`。
- 根节点必须有 `RectTransform`、对应的 `UGF_Panel` 派生脚本和 `UIContainer`；同一面板根节点只保留一份面板逻辑和一份容器。
- 背景、装饰、内容区和交互区按职责分层。LoginPanel 的 `Bg`、`LoginCard`、输入区和按钮区可作为组织参考，不要求复制固定层级路径。
- 默认布局、锚点、尺寸、图片、字体、组件引用及初始状态在 Prefab 中完成并保存。面板代码只处理数据和交互带来的状态变化，不在打开时重建静态界面。
- 在 Unity 编辑器中制作并保存资源；不得为单次交付新增临时 Editor 资源生成脚本。运行时框架负责添加的 Canvas、CanvasGroup、GraphicRaycaster 等按 `UGF_Panel` 配置管理，不因编辑态缺少这些组件判定资源错误。

## 组件约束

| 用途 | 组件与约束 |
| --- | --- |
| 按钮 | 使用 `UGF_Button`，通过 `SetClickListener` 绑定点击；同一操作节点不叠加普通 Button 和 UGF_Button |
| 图片 | 默认使用项目 `UGF_Image`；如 TMP 输入框模板等已有组合需要普通 Image，保留其必要引用，不机械替换 |
| 文本 | 使用 TextMeshPro 系列组件；所有 TMP 文本（含输入内容、占位符、按钮标签及隐藏节点）统一使用 `Assets/AAAGame/Font/Common/思源黑体 SDF.asset` 及兼容材质，不新建旧版 UI Text |
| 输入框 | 使用 `TMP_InputField`，在 Prefab 配齐 Text Viewport、Text Component、Placeholder 等引用；密码框配置正确的内容类型 |
| 布局与裁剪 | 使用 RectTransform、布局组件和必要的 Mask/RectMask2D；同一尺寸避免被多套布局规则同时驱动 |
| 射线 | 装饰和纯展示文字默认关闭 Raycast Target；交互背景与模态遮罩按实际点击、阻挡需求开启 |

必须无 Missing Script、无丢失字体/材质/图片和必要组件引用。禁用状态仍需可读，隐藏控件也必须完成有效绑定。动态业务文字不得烘焙进贴图；参考资源里已有的图片标签不作为新增业务文案的实现模板。

## UIContainer 是面板访问控件的唯一入口

**面板逻辑只能使用自身 UIContainer 中已登记的控件。** 所有需要读取、赋值、显隐、改变交互状态或绑定事件的目标，必须先注册，再在 `RegisterUI()` 中通过 `PanelUIContainer.Get<T>(key)` 取得并缓存。`UGF_Panel.Get<T>(key)` 是同一容器的转发入口，但新增代码统一使用前者便于检查。

- **所有 UIContainer 条目引用的实际 GameObject 节点名必须以 `m_` 开头**，包括文字、布局节点、模板根节点以及运行时克隆对象。只给 key 或字段添加前缀不算满足规则；同一对象注册多个组件时，其实际节点名同样必须满足此前缀要求。
- 容器 key、`NodeName` 与脚本字段名保持一致，统一使用 `m_` 加语义名称，例如 `m_GuestLoginButton`、`m_StartButtonText`。同一容器内 key 唯一，不依赖节点路径或兄弟顺序。
- `NodeObj` 指向实际挂载目标组件的对象；`NodeType` 与字段、`Get<T>` 类型一致；`Domain` 默认 `private`。不登记无效对象，不用宽泛类型隐藏真实依赖。
- `UIContainer.Get<T>` 内部使用 `GetComponent<T>`，因此登记对象本身必须具有所需组件，不能仅指向其父节点。缺少 key 会抛异常，对象缺失或组件类型不符也会导致绑定失败；交付前必须检查，不能靠业务中的空值判断掩盖配置错误。
- 禁止在面板里用 `transform.Find`、`GameObject.Find`、`FindObjectOfType`、`GetComponent*`、`GetComponentsInChildren*`、`GetChild`、层级遍历或运行时 AddComponent 获取/补建控件；禁止用额外 Inspector 字段或外部单例绕过容器注入控件。
- 已取得容器控件不代表可以借它发现其它控件。例如读取 `TMP_InputField.placeholder`、`textComponent` 后修改文字，或从按钮查找标签，都属于绕过绑定。目标文字必须作为独立条目注册。
- 可以修改已绑定组件的自身属性，例如 `.text`、`.interactable`、`.color`，以及通过其 `.gameObject.SetActive(...)` 控制该对象。需要调整 RectTransform 时直接登记 RectTransform，不再临时取组件。
- 静态装饰且从不被逻辑访问的对象无需登记。列表项、复用子视图由自己的容器和组件负责内部绑定，父面板只调用已登记子视图的明确展示接口，不穿透访问内部节点；动态实例沿用项目现有列表/资源机制，其内部控件同样不得临时查找。

### LoginPanel 绑定基线与待对齐项

当前容器登记了以下四项：

| key / 字段 | 类型 |
| --- | --- |
| `m_AccountInputField` | `TMP_InputField` |
| `m_PasswordInputField` | `TMP_InputField` |
| `m_StartButton` | `UGF_Button` |
| `m_GuestLoginButton` | `UGF_Button` |

当前 `LoginPanel.OnOpen()` 通过 `m_AccountInputField.placeholder` 修改占位文案，并通过 `m_StartButton.GetComponentInChildren<TMP_Text>(true)` 查找按钮文字。这两处不满足本规范。后续调整该面板时，应在 Prefab 容器中增加 `m_AccountPlaceholderText`、`m_StartButtonText`，绑定实际文本节点，再直接赋值。本次文档定义规则，不代表这两处代码已完成迁移。

```csharp
#region Auto Create
/// <summary>账号输入框的占位文案。</summary>
private TMP_Text m_AccountPlaceholderText;
/// <summary>账号登录按钮的文字。</summary>
private TMP_Text m_StartButtonText;
#endregion

protected override void RegisterUI()
{
    #region Auto Bind
    m_AccountPlaceholderText = PanelUIContainer.Get<TMP_Text>("m_AccountPlaceholderText");
    m_StartButtonText = PanelUIContainer.Get<TMP_Text>("m_StartButtonText");
    #endregion
}

// 在 OnOpen 调用 base.OnOpen(userData) 完成绑定后刷新。
private void RefreshAccountLoginText()
{
    m_AccountPlaceholderText.text = LoginConst.Text.AccountLoginUnavailable;
    m_StartButtonText.text = LoginConst.Text.AccountLoginUnavailable;
}
```

示例仅展示新增绑定，实际迁移时保留原有四项。新增文字条目的 NodeObj 必须分别指向占位符和按钮标签的实际文本对象。

## 面板逻辑与生命周期

| 位置 | 责任 |
| --- | --- |
| `Auto Create` | 与容器条目对应的控件字段，不放业务状态 |
| `RegisterUI()` / `Auto Bind` | 从容器取得全部控件，不做网络请求和业务编排 |
| `RegisterEvent()` / `Auto Event` | 绑定按钮回调、订阅模块语义事件 |
| `OnOpen()` | 先调用 `base.OnOpen(userData)`，再初始化本次打开状态、取消源和显示内容 |
| 刷新方法 | 将 Model 或事件数据映射到已绑定控件，处理加载、完成、失败、空数据和禁用状态 |
| `OnClose()` | 取消并释放本面板任务，释放本面板持有的资源和输入锁，保留基类关闭调用 |

- 当前 `UGF_Panel.OnOpen()` 每次打开都会调用 `RegisterUI()` 和 `RegisterEvent()`；不要再次手动调用这两个方法，也不要只在 OnInit 订阅需要每次打开恢复的事件。
- 使用基类 `Subscribe` 登记的事件由基类 OnClose 的 `RemoveAllSubscribe()` 清理。自行订阅的外部事件必须自行成对解除，不能假设基类会清理任意来源的监听。
- 同一按钮只在一个位置绑定，禁止同时配置 Inspector 业务回调和代码回调造成重复执行。使用普通事件 AddListener 时必须明确移除机制，重开面板不得累积监听。
- 按钮回调可直接调用所属模块 Controller 的公开用例入口；Panel 不实现认证、协议收发、持久化和玩法规则。Controller 更新 Model 后发布语义事件，Panel 根据状态刷新。跨模块展示及 Gameplay 交互遵循客户端代码规范中的门面和 Bridge 边界。
- 异步任务采用每次打开独立的取消生命周期；关闭、重开时取消并释放旧 CancellationTokenSource。关闭后到达的结果不得更新旧面板。调用 `.Forget()` 的流程必须有明确异常处理归属，不能静默丢失异常。
- 文案统一直接通过 `GF.Localization.GetText(key)` 取得，不再单独编写 Text/Localize 转发方法、委托或属性；模块常量只保存本地化 key，不在 Panel 中直接写面向玩家的中文。注册 key 与本地化 key 分别管理，不混用。

## 制作与验收

1. 在 Unity 中完成 Prefab 的层级、布局、组件、字体和初始状态，登记所有需要逻辑访问的对象。
2. 同步容器字段、类型和 key，生成或更新 Auto Create、Auto Bind、Auto Event；检查生成结果符合当前 RegisterUI/RegisterEvent 生命周期，不能直接照搬旧工具模板的 OnInit 写法。
3. 编写面板展示及事件回调，通过 Controller 发起业务；扫描并移除查找节点、二次获取子控件和容器之外的控件引用。
4. 检查所有容器项对象有效、类型正确、字段能够绑定，TMP 字体及材质一致；保存 Prefab，不只修改场景实例。
5. 实际打开、关闭、重开面板；检查按钮只响应一次、订阅正确恢复、异步任务关闭后停止影响界面。验证加载、禁用、失败、长文案和目标分辨率下的显示与点击。

评审必须同时核对 Prefab 与脚本：任何由面板操作但未登记到 UIContainer 的控件，均不通过验收。
