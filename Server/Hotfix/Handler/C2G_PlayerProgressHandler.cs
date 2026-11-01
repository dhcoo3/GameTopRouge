using Entity.Auth;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Auth;
using Hotfix.Gate.Component;
using Hotfix.Progress;

namespace Hotfix.Handler;

/// <summary>从认证会话读取玩家身份，并加载该账号的权威进度快照。</summary>
public sealed class C2G_LoadPlayerProgressHandler : MessageRPC<C2G_LoadPlayerProgress, G2C_LoadPlayerProgress>
{
    /// <summary>使用当前认证身份加载账号进度并返回服务端快照。</summary>
    /// <param name="session">发起请求的已认证 Gate 会话。</param>
    /// <param name="request">包含进入地图编号的加载请求。</param>
    /// <param name="response">接收账号进度和错误码的 RPC 响应。</param>
    /// <param name="reply">由 Fantasy RPC 框架管理的响应回调。</param>
    protected override async FTask Run(Session session, C2G_LoadPlayerProgress request, G2C_LoadPlayerProgress response, Action reply)
    {
        
        if (!AuthSceneHelper.TryGetPlayerId(session, out int playerId))
        {
            response.ErrorCode = ProgressError.NotAuthenticated;
            response.ErrorMessage = "Not authenticated.";
            return;
        }
        try
        {
            var storage = session.Scene.GetComponent<AuthStorageComponent>();
            var config = session.Scene.GetComponent<LubanConfigComponent>();
            if (storage?.IsReady != true || config?.Tables == null) throw new InvalidOperationException("Progress service unavailable.");
            var store = new PlayerProgressStore(storage.Database, config.Tables);
            var document = await store.LoadForMapAsync(playerId, request.MapId);
            PlayerProgressStore.FillSnapshot(document, response);
            response.ErrorCode = ProgressError.Success;
        }
        catch (ProgressOperationException exception)
        {
            response.ErrorCode = exception.ErrorCode;
            response.ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            Log.Error($"读取账号进度失败 PlayerId={playerId}, Type={exception.GetType().Name}: {exception.Message}");
            response.ErrorCode = ProgressError.StorageUnavailable;
            response.ErrorMessage = "Progress storage unavailable.";
        }
    }
}

/// <summary>校验并原子提交单个任务、剧情标记或简报事件。</summary>
public sealed class C2G_SubmitPlayerProgressEventHandler : MessageRPC<C2G_SubmitPlayerProgressEvent, G2C_SubmitPlayerProgressEvent>
{
    /// <summary>从认证会话取得玩家身份，校验并提交单个进度事件。</summary>
    /// <param name="session">发起请求的已认证 Gate 会话。</param>
    /// <param name="request">客户端提交的任务、剧情标记或简报事件。</param>
    /// <param name="response">接收最新账号快照和错误码的 RPC 响应。</param>
    /// <param name="reply">由 Fantasy RPC 框架管理的响应回调。</param>
    protected override async FTask Run(Session session, C2G_SubmitPlayerProgressEvent request, G2C_SubmitPlayerProgressEvent response, Action reply)
    {
        if (!AuthSceneHelper.TryGetPlayerId(session, out int playerId))
        {
            response.ErrorCode = ProgressError.NotAuthenticated;
            response.ErrorMessage = "Not authenticated.";
            return;
        }
        try
        {
            var storage = session.Scene.GetComponent<AuthStorageComponent>();
            var config = session.Scene.GetComponent<LubanConfigComponent>();
            if (storage?.IsReady != true || config?.Tables == null) throw new InvalidOperationException("Progress service unavailable.");
            var store = new PlayerProgressStore(storage.Database, config.Tables);
            var document = await store.SubmitAsync(playerId, new ProgressRequest
            {
                RequestId = request.RequestId,
                EventType = request.EventType,
                QuestId = request.QuestId,
                ObjectiveId = request.ObjectiveId,
                MapId = request.MapId,
                MapInstanceId = request.MapInstanceId,
                SourceId = request.SourceId,
                Count = request.Count,
                DialogueId = request.DialogueId,
                DialogueNodeId = request.DialogueNodeId
            });
            PlayerProgressStore.FillSnapshot(document, response);
            response.ErrorCode = ProgressError.Success;
        }
        catch (ProgressOperationException exception)
        {
            response.ErrorCode = exception.ErrorCode;
            response.ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            Log.Error($"提交账号进度失败 PlayerId={playerId}, Event={request.EventType}, Type={exception.GetType().Name}: {exception.Message}");
            response.ErrorCode = ProgressError.StorageUnavailable;
            response.ErrorMessage = "Progress storage unavailable.";
        }
    }
}
