using System.Text.Json;
using Entity.Auth;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using Fantasy.Network;
using Fantasy.Network.HTTP;
using Fantasy.Platform.Net;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;

namespace Hotfix.Auth;

/// <summary>认证 Scene 存储初始化、目标服务器解析及会话身份访问。</summary>
public static class AuthSceneHelper
{
    /// <summary>为 HTTP 或 Gate Scene 初始化认证存储，失败时保持不可用状态。</summary>
    /// <param name="scene">拥有原生 World 数据库的场景。</param>
    /// <returns>初始化任务。</returns>
    public static async FTask InitializeAsync(Scene scene)
    {
        var component = scene.AddComponent<AuthStorageComponent>();
        component.IsReady = false;
        try
        {
            component.Database = (IMongoDatabase)scene.World.Database.GetDatabaseInstance;
            await new GuestAuthStore(component.Database).InitializeAsync();
            if (component.IsDisposed) return;
            component.IsReady = true;
            Log.Info($"认证存储就绪 Scene={scene.SceneConfigId}, Type={scene.SceneType}, Protocol={scene.SceneConfig.NetworkProtocol}, OuterPort={scene.SceneConfig.OuterPort}");
        }
        catch (Exception exception)
        {
            Log.Error($"认证存储初始化失败，保留原数据且拒绝登录 Scene={scene.SceneConfigId}, {exception.GetType().Name}: {exception.Message}");
        }
    }

    /// <summary>只在当前 Scene 存储就绪后创建轻量数据访问对象。</summary>
    /// <param name="scene">认证业务所属场景。</param>
    /// <returns>可操作当前 World 认证数据的存储对象。</returns>
    public static GuestAuthStore GetStore(Scene scene)
    {
        var component = scene.GetComponent<AuthStorageComponent>();
        if (component == null || !component.IsReady)
            throw new AuthFailureException(AuthConstants.Unavailable, 503, "认证服务尚未就绪，请稍后重试。");
        return new GuestAuthStore(component.Database);
    }

    /// <summary>从同 World 配置解析第一版唯一的 KCP Gate，不额外硬编码地址。</summary>
    /// <param name="scene">HTTP 登录服务所属场景。</param>
    /// <returns>目标游戏服地址。</returns>
    public static AuthTicketResponse GetGate(Scene scene)
    {
        var gates = SceneConfigData.Instance.List.Where(config => config.WorldConfigId == scene.SceneConfig.WorldConfigId &&
            config.SceneTypeString == "Gate" && config.NetworkProtocol == "KCP" && config.OuterPort > 0).ToArray();
        if (gates.Length != 1)
            throw new AuthFailureException(AuthConstants.Unavailable, 503, "当前 World 需要配置唯一的 KCP 游戏服务器。");
        var gate = gates[0];
        var process = ProcessConfigData.Instance.Get(gate.ProcessConfigId);
        var machine = MachineConfigData.Instance.Get(process.MachineId);
        return new AuthTicketResponse { GateId = gate.Id, Host = machine.OuterIP, Port = gate.OuterPort };
    }

    /// <summary>读取当前仍被 Gate 管理器认可的会话身份。</summary>
    /// <param name="session">发送业务消息的会话。</param>
    /// <param name="playerId">认证成功时输出玩家编号。</param>
    /// <returns>该会话为玩家当前唯一会话时返回真。</returns>
    public static bool TryGetPlayerId(Session session, out int playerId)
    {
        playerId = 0;
        var auth = session.GetComponent<SessionAuthComponent>();
        if (auth == null || auth.Manager.IsDisposed || !auth.Manager.Sessions.TryGetValue(auth.PlayerId, out var current) ||
            !ReferenceEquals(current, session) || session.IsDisposed)
            return false;
        playerId = auth.PlayerId;
        return true;
    }
}

/// <summary>统一 HTTP JSON 字段命名；Controller 注册和路由映射由 Fantasy 完成。</summary>
public sealed class OnConfigureHttpServices_Authentication : AsyncEventSystem<OnConfigureHttpServices>
{
    /// <summary>为登录 HTTP Scene 设置客户端共同使用的驼峰字段名。</summary>
    /// <param name="self">Fantasy HTTP 服务配置事件。</param>
    /// <returns>同步完成的配置任务。</returns>
    protected override async FTask Handler(OnConfigureHttpServices self)
    {
        if (self.Scene.SceneType == SceneType.LoginHttp)
        {
            self.MvcBuilder.AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
            Log.Info($"HTTP 认证服务 MVC 路由配置完成 Scene={self.Scene.SceneConfigId}, Port={self.Scene.SceneConfig.OuterPort}");
        }
        await FTask.CompletedTask;
    }
}

/// <summary>会话销毁时清理索引；旧会话不能删除后来绑定的新会话。</summary>
public sealed class SessionAuthComponentDestroySystem : DestroySystem<SessionAuthComponent>
{
    /// <summary>仅当索引仍属于正在销毁的会话时移除玩家在线状态。</summary>
    /// <param name="self">即将销毁的认证组件。</param>
    protected override void Destroy(SessionAuthComponent self)
    {
        if (self.Manager != null && !self.Manager.IsDisposed &&
            self.Manager.Sessions.TryGetValue(self.PlayerId, out var current) && ReferenceEquals(current, self.Parent))
            self.Manager.Sessions.Remove(self.PlayerId);
    }
}
