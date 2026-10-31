using Entity.Auth;
using Entity.Gate.Compoent;
using Fantasy;
using Fantasy.Async;
using Fantasy.Gate.Entity;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Gate.Component;
using Hotfix.Auth;

namespace Hotfix.Handler;

/// <summary>
/// 验证一次性票据并绑定玩家唯一游戏会话。
/// </summary>
public sealed class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    /// <summary>给 KCP 踢出通知预留的发送时间，单位为毫秒。</summary>
    private const int RevokeFlushMilliseconds = 200;
    /// <summary>登录串行锁的独立业务类型，避免和数据库锁混用。</summary>
    private const long LoginLockType = 764001;
    /// <summary>
    /// 使用票据确认身份后恢复房间会话，忽略客户端提交的玩家编号。
    /// </summary>
    /// <param name="session">发起登录的网络会话。</param>
    /// <param name="request">客户端登录请求。</param>
    /// <param name="response">返回给客户端的登录结果。</param>
    /// <param name="reply">框架提供的响应回调。</param>
    protected override async FTask Run(Session session, C2G_LoginRequest request, G2C_LoginResponse response, Action reply)
    {
        long runtimeId = session.RuntimeId;
        var scene = session.Scene;
        uint sceneConfigId = scene.SceneConfigId;
        try
        {
            var store = AuthSceneHelper.GetStore(scene);
            var identity = await store.ConsumeTicketAsync(request.Ticket, sceneConfigId);
            if (scene.IsDisposed || session.IsDisposed || session.RuntimeId != runtimeId) return;
            using (await scene.CoroutineLockComponent.Wait(LoginLockType, identity.PlayerId))
            {
                await store.ValidateGrantAsync(identity);
                if (session.IsDisposed || session.RuntimeId != runtimeId)
                    return;
                if (session.GetComponent<SessionAuthComponent>() != null)
                    throw new AuthFailureException(AuthConstants.InvalidRequest, 400, "当前连接已经完成登录。");
                await EnsurePlayerAsync(scene, identity);
                await store.ValidateGrantAsync(identity);
                if (session.IsDisposed || session.RuntimeId != runtimeId)
                    return;
                BindSession(session, identity.PlayerId);
                response.PlayerId = identity.PlayerId;
                response.PlayerName = identity.PlayerName;
                response.ErrorCode = AuthConstants.Success;
                response.Tag = "登录成功";
            }
        }
        catch (AuthFailureException exception)
        {
            response.ErrorCode = exception.Code;
            response.Tag = exception.Message;
        }
        catch (Exception exception)
        {
            Log.Error($"Gate 认证失败 Scene={sceneConfigId}, Type={exception.GetType().Name}");
            response.ErrorCode = AuthConstants.Unavailable;
            response.Tag = "游戏服务器暂不可用，请稍后重试。";
        }
    }

    /// <summary>在玩家登录锁内确保基础资料存在，保留已有玩家数据。</summary>
    /// <param name="scene">拥有 World 数据库的 Gate Scene。</param>
    /// <param name="identity">服务端验证的玩家资料。</param>
    /// <returns>持久化检查任务。</returns>
    private static async FTask EnsurePlayerAsync(Scene scene, VerifiedGuest identity)
    {
        var database = scene.World.Database;
        if (await database.First<Player>(player => player.PlayerId == identity.PlayerId) != null)
            return;
        if (scene.IsDisposed) return;
        var player = Fantasy.Entitas.Entity.Create<Player>(scene);
        try
        {
            player.PlayerId = identity.PlayerId;
            player.Name = identity.PlayerName;
            await database.Insert(player);
        }
        finally { player.Dispose(); }
    }

    /// <summary>先切换有效身份再重绑房间，最后通知并关闭旧会话。</summary>
    /// <param name="session">新登录成功的会话。</param>
    /// <param name="playerId">票据确认的玩家编号。</param>
    private static void BindSession(Session session, int playerId)
    {
        var manager = session.Scene.GetComponent<GateAuthManagerComponent>();
        manager.Sessions.TryGetValue(playerId, out var oldSession);
        var auth = session.AddComponent<SessionAuthComponent>();
        auth.PlayerId = playerId;
        auth.Manager = manager;
        manager.Sessions[playerId] = session;
        session.Scene.GetComponent<FrameRoomManagerComponent>().BindLoginSession(session, playerId);
        if (oldSession != null && !oldSession.IsDisposed && !ReferenceEquals(oldSession, session))
            RevokeOldSessionAsync(oldSession).Coroutine();
    }

    /// <summary>尽力发送踢出通知，延迟关闭时校验会话运行编号以防对象池复用。</summary>
    /// <param name="session">已失去有效身份的旧连接。</param>
    /// <returns>通知与关闭任务。</returns>
    private static async FTask RevokeOldSessionAsync(Session session)
    {
        long runtimeId = session.RuntimeId;
        var scene = session.Scene;
        using var message = G2C_LoginRevoked.Create();
        message.Reason = AuthConstants.SessionReplaced;
        message.Message = "当前游客已在其他客户端登录，请重新登录。";
        session.Send(message);
        await scene.TimerComponent.Net.WaitAsync(RevokeFlushMilliseconds);
        if (!session.IsDisposed && session.RuntimeId == runtimeId)
            session.Dispose();
    }
}
