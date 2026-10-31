using Entity.Gate;
using Entity.Gate.Compoent;
using Fantasy;
using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Gate.Entity;
using Hotfix.Gate.Component;
using Entity.Auth;
using Hotfix.Auth;

namespace Hotfix;

/// <summary>初始化 Gate 玩法组件和登录认证相关 Scene 的存储。</summary>
public sealed class OnCreateScene_Config:AsyncEventSystem<OnCreateScene>
{
    /// <summary>根据场景类型加载玩法配置或认证索引，未就绪时拒绝登录。</summary>
    /// <param name="self">已建立监听的 Scene 创建事件。</param>
    /// <returns>场景业务初始化任务。</returns>
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;
        switch (scene.SceneType)
        {
            case SceneType.Gate:
            {
                LubanConfigComponent config = scene.AddComponent<LubanConfigComponent>();
                /*cfg.item.TbItem itemTable = config.GetTable<cfg.item.TbItem>(cfg.Tables.item_tbitem);
                var firstItem = itemTable.DataList.FirstOrDefault();
                Log.Debug($"Luban item_tbitem loaded; rows={itemTable.DataList.Count}, first={firstItem}");*/

                scene.AddComponent<FrameRoomManagerComponent>();
                scene.AddComponent<GateAuthManagerComponent>();
                await AuthSceneHelper.InitializeAsync(scene);
                break;
            }
            case SceneType.LoginHttp:
            {
                await AuthSceneHelper.InitializeAsync(scene);
                break;
            }
        }
        await FTask.CompletedTask;
    }
}
