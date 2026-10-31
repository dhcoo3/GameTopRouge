# 地图 NPC 与传送点

入口：`Tools/地图/NPC 与传送点编辑器`。

1. 选择地图，点击“打开场景并读取摆放”。
2. 从 Npc 表选择 NPC 并添加，或添加传送点。在 Scene 中移动对象。
3. 选中 NPC，在 Inspector 编辑触发半径；选中传送点，填写目标地图 ID、标题或场景名。
4. 点击“保存当前地图配置”，再点击“导出 Luban 配置（客户端与服务端）”。导出要求系统已安装 dotnet。

摆放保存在 MapData.Npc / MapData.Portal；Npc 表只保存基础资源，同 ID 可重复摆放。列表项用分号分隔，项内字段用逗号分隔：

- NpcPlacement：npcId,x,y,z,radius
- Portal：x,y,z,targetMapId,radius

坐标为世界 XY 平面，保留 Z。预览对象使用 DontSave，不需保存到场景。重新读取或切换地图会提示放弃未保存修改。关闭窗口会清理预览，请先保存。

运行时通过 MapRender 生成被动对象，不触发对话或传送。MapPlacementWorkbook.Migrate 是一次性旧表迁移入口；旧表没有摆放列表时不会依据 Npc.mapId 自动添加新实例。

传送点和 NPC 均可编辑触发半径（默认 1），在 XY 平面显示圆形范围。勾选“显示网格区域”可查看当前地图：绿色为可行走格，红色为碰撞格；开关仅影响编辑预览，不改变配置或游戏行为。

## 固定物件

在 `Config/DataTables/Datas/Map/#MapObject.xlsx` 添加基础配置：id、name、res、type（1 任务标识、2 场景道具）。初始表为空，请填入项目实际使用的预制体路径。

打开地图后，在“固定物件”区域按类型筛选并添加。同配置支持多实例；使用“复制固定物件”按钮分配新的地图内实例编号。实例编号在修改、保存后保持不变。Unity 自带复制产生的重复编号会被保存校验拦截。

在 Inspector 或 Scene 面板修改世界坐标、Z 轴朝向、缩放、触发半径，也可使用 Unity 的变换工具。保存后导出 Luban。MapData.Objects 格式为：
`instanceId,objectId,x,y,z,rotationZ,scaleX,scaleY,scaleZ,radius`，实例用分号分隔。

运行时使用独立 MapObjectEntity 加载到 Entity Group - Map，保留预制体排序，不附加任务、奖励、对话、传送行为。状态持久化和奖励逻辑尚未接入。
