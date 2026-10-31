# 对话系统使用说明

## 地图 NPC 接入

1200004 地图现有 NPC `1500000`（玛拉）配置 `dialogueId=3`，复用原位置 `(20.75, -5.15)` 和半径 `1`。C1-M00 的村口巡境记号调查完成前，玛拉交互入口保持关闭；调查完成后靠近她出现 `[E] 交谈`，按 E 播放首次询问。剧情使用 NPC 表中的名称和头像，结束或 Esc 取消后恢复本地输入。实体隐藏或场景退出会取消会话，不暂停联机权威帧。独立对话演示继续使用剧情 `1`，不与玛拉剧情混用。

NPC 表 `dialogueId=0` 表示不提供交谈入口，非零必须引用剧情表。M00 巡境记号调查播放旁白剧情 `2`，玛拉使用剧情 `3`；地图 NPC 摆放位置不变，也不新增 NPC。

## C1-M00 灰钟村开场

首次进入 `1200004` 时，运行时叠加单页旁白，播报哨站签到记录与队长搜寻命令。确认后解除移动输入锁并显示任务目标“寻找阿尔文留下的巡境记号”。玩家需在村口按 E 调查记号；调查旁白确认阿尔文进入村庄且没有返回标记，然后任务目标切换为“向玛拉询问阿尔文的下落”。按 J 打开本次运行内的任务记录，可回看命令与线索。此阶段不揭露镇印、回名礼或亡者真相。

M00 的运行时线索和首次开场状态只保留到退出游戏，不写入剧情进度存档；当前版本不接入任务奖励或正式任务系统。标记调查前玛拉的交互被锁定，调查完成后开放。地图上的记号是场景运行时生成的交互点。

## 运行演示

打开 `Client/GameTopRouge/Assets/AAAGame/Scene/Dialogue/DialogueDemo.unity`，进入 Play 后点击“开始交谈”。演示不需要登录或启动服务器。

- 点击对话框、空格或回车：补全当前页，再次操作翻页或继续。
- 上下键移动选项焦点，回车或点击确认。选项出现的同一帧不接受确认。
- “历史”查看本次会话已经显示的文字与选择，鼠标滚轮滚动；打开历史时暂停打字。
- Esc 优先关闭历史，再次按下取消对话。
- 选择“询问入口”会设置 `KnowsEntrance`，再次交谈使用不同开场。“重置剧情标记”恢复首次开场。

当前头像为统一像素占位图。正式 NPC 头像通过 NPC 表的 `portraitAsset` 配置；资源缺失时保留占位图。

## Excel 配置

配置源位于 `Config/DataTables/Datas/Dialogue`，沿用 Luban 的前四行结构头，业务数据从第五行开始。不要修改生成的 C# 或 `.bytes`。

| 文件 | 用途 |
| --- | --- |
| `Dialogue.xlsx` | 剧情编号、入口节点和策划备注 |
| `../Map/#Npc.xlsx` | 统一 NPC 配置，名称本地化键和对话头像路径 |
| `#DialogueNode.xlsx` | 节点与条件、标记和跳转 |
| `#DialogueOption.xlsx` | 选项所属节点、排序、文案与跳转 |

`Dialogue.xlsx` 在 `__tables__.xlsx` 显式注册为 `Dialogue.TbDialogue`，记录类型为 `Dialogue.DialogueDefinition`，避免 C# 中命名空间与 `Dialogue` 类型同名。节点表、选项表和 NPC 表使用项目现有的 `#` 文件自动发现规则，无需重复注册。没有独立的说话者表。

| 节点类型 | 使用字段 |
| --- | --- |
| `Line` | `npcId`、`textKey`、`nextNodeId` |
| `Choice` | `npcId`、`textKey`，在选项表配置 1–4 个选项 |
| `Condition` | `flagKey`、`flagValue`、`trueNodeId`、`falseNodeId` |
| `SetFlag` | `flagKey`、`flagValue`、`nextNodeId` |
| `End` | 结束当前会话，无后续节点 |

所有节点填写唯一 `id` 和所属 `dialogueId`。未使用的数值字段填 0，字符串留空，布尔值填 FALSE。`npcId=0` 为旁白，无需配置 NPC；非零值必须引用 NPC 表的真实编号。节点只能跳转到同一剧情内；选项的 `order` 在所属节点内唯一。

名称、台词、选项译文维护在 `#Localization.xlsx`，填写简体中文、繁体中文和英文。NPC 表现有 `name` 字段统一保存 `Npc.` 前缀的名称本地化键，头像放在 `portraitAsset`；地图编辑器会解析名称键并显示中文。台词和选项使用 `Dialogue.` 前缀的稳定语义键。当前启动流程与独立演示默认使用简体中文。

新增剧情：先配置入口和节点，再补充 NPC、选项及本地化译文。M00 使用剧情 `2`，玛拉 NPC `1500000` 使用剧情 `3`，独立演示仍使用剧情 `1`。运行 macOS/Linux 的 `bash Config/gen.sh` 或 Windows 的 `Config/gen.bat`。生成需要 .NET 和 Python 3；剧情校验器只使用 Python 标准库，不需要额外安装包。

生成脚本导出两端配置后自动检查：入口、节点及角色引用、跨剧情跳转、重复排序、选项数量、缺失译文和结束节点可达性。可单独运行 `python3 Config/validate_dialogue.py`。

## 正式接入

在已初始化 Game Framework 的 Unity 主线程调用：

```csharp
DialogueResult result = await DialogueView.Instance.PlayAsync(dialogueId, cancellationToken);
```

入口通过现有 `GF.LuBan` 读取配置，通过 `UIModuleManager` 打开 Excel 模块表中注册的 `Dialogue/DialoguePanel`。返回 `Completed`、`Cancelled`、`Failed` 或 `Busy`，并复制本次选择的选项 ID。

独立演示注入同一批生成的二进制资源，复用配置校验、Controller、Model 和正式 Prefab，不初始化登录、网络或完整 GF 运行环境。

同一时间只播放一个会话；场景调用方应传入场景生命周期令牌。取消不回滚已经设置的布尔标记，标记仅存在于本次运行内，不写存档。对话系统不暂停全局时间，也不阻断联机权威帧；M00 在地图表现层独立锁定本地移动输入，并在开场、记号调查或日志面板结束后恢复。

## UI 资源与验证

正式 Prefab：`Assets/AAAGame/Prefabs/UI/Dialogue/DialoguePanel.prefab`。面板、选项九宫格和占位头像位于 `Assets/AAAGame/Sprites/UI/Dialogue`，文字均可编辑。面板基准为 1920×1080，支持安全区和横屏缩放。

`Tools/Dialogue/Rebuild Demo Assets` 可重新生成演示资产，会覆盖该功能的 Prefab、素材和演示场景；定制美术后不要执行重建。构建器以附加场景方式工作，不保存其他场景。

验证命令：

```sh
python3 -m unittest discover -s Config -p 'test_validate_dialogue.py' -v
```

进入独立演示 Play 模式后，执行 `Tools/Dialogue/Verify Running Demo`，检查三条分支、重复交谈、重置、历史暂停、分页、并发入口及取消清理。测试会重置本地演示标记，结果写入系统临时目录的 `dialogue-verification.txt`。

已通过 13 项配置回归测试和 37 项运行时检查（包含旁白不需要 NPC 的检查）；已截图检查 1920×1080、1280×720、2560×1080 布局，四个选项没有重叠或越界。
