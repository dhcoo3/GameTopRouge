using Entity.Gate.Compoent;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Gate.Component;

namespace Hotfix.Handler;

/// <summary>
/// 创建等待第二名玩家加入的双人帧同步房间。
/// </summary>
public sealed class C2G_CreateFrameRoomHandler : MessageRPC<C2G_CreateFrameRoom, G2C_CreateFrameRoom>
{
    /// <summary>
    /// 根据已登录会话创建一个独立房间。
    /// </summary>
    /// <param name="session">创建者的网络会话。</param>
    /// <param name="request">副本和英雄选择请求。</param>
    /// <param name="response">创建结果和房间标识。</param>
    /// <param name="reply">框架提供的响应回调。</param>
    protected override async FTask Run(Session session, C2G_CreateFrameRoom request, G2C_CreateFrameRoom response, Action reply)
    {
        FrameRoomManagerComponent roomManager = session.Scene.GetComponent<FrameRoomManagerComponent>();
        if (roomManager.TryCreateRoom(session, request.DungeonId, request.DungeonType, request.HeroId,
                out int roomId, out string errorMessage))
        {
            response.RoomId = roomId;
            response.ErrorCode = 0;
        }
        else
        {
            response.ErrorCode = 1;
            response.ErrorMessage = errorMessage;
        }

        await FTask.CompletedTask;
    }
}

/// <summary>
/// 加入等待中的双人帧同步房间，第二名玩家加入后立即开局。
/// </summary>
public sealed class C2G_JoinFrameRoomHandler : MessageRPC<C2G_JoinFrameRoom, G2C_JoinFrameRoom>
{
    /// <summary>
    /// 校验会话与房间后加入房间并触发开局。
    /// </summary>
    /// <param name="session">加入者的网络会话。</param>
    /// <param name="request">目标房间和英雄选择请求。</param>
    /// <param name="response">加入结果和房间标识。</param>
    /// <param name="reply">框架提供的响应回调。</param>
    protected override async FTask Run(Session session, C2G_JoinFrameRoom request, G2C_JoinFrameRoom response, Action reply)
    {
        FrameRoomManagerComponent roomManager = session.Scene.GetComponent<FrameRoomManagerComponent>();
        if (roomManager.TryJoinRoom(session, request.RoomId, request.HeroId, out string errorMessage))
        {
            response.RoomId = request.RoomId;
            response.ErrorCode = 0;
        }
        else
        {
            response.ErrorCode = 1;
            response.ErrorMessage = errorMessage;
        }

        await FTask.CompletedTask;
    }
}

/// <summary>
/// 处理客户端按连续帧号发起的权威历史补取请求。
/// </summary>
public sealed class C2G_FrameFetchRequestHandler : Message<C2G_FrameFetchRequest>
{
    /// <summary>
    /// 仅向所属对局成员返回合法的连续历史帧。
    /// </summary>
    /// <param name="session">发起请求的网络会话。</param>
    /// <param name="message">包含对局和起始帧号的补帧请求。</param>
    protected override async FTask Run(Session session, C2G_FrameFetchRequest message)
    {
        FrameRoomManagerComponent roomManager = session.Scene.GetComponent<FrameRoomManagerComponent>();
        if (roomManager.TryGetMatchBySession(session, message.MatchId, out FSManagerComponent frameManager, out var unit))
        {
            frameManager.SendFrameHistory(unit, message.StartFrameId);
        }
        else
        {
            Log.Error($"拒绝无效补帧请求 match:{message.MatchId} start:{message.StartFrameId}");
        }

        await FTask.CompletedTask;
    }
}
