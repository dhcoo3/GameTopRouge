using Entity.Gate.Compoent;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Hotfix.Gate.Component;

namespace Hotfix.Handler;

/// <summary>
/// 接收客户端房间输入，并根据会话归属写入对应对局的未封帧状态。
/// </summary>
public sealed class C2G_NextFrameCommandHandler : Message<C2G_NextFrameCommand>
{
    /// <summary>
    /// 验证发送会话后处理移动、技能、确认帧和输入序号。
    /// </summary>
    /// <param name="session">发送输入包的网络会话。</param>
    /// <param name="message">客户端输入包。</param>
    protected override async FTask Run(Session session, C2G_NextFrameCommand message)
    {
        FrameRoomManagerComponent roomManager = session.Scene.GetComponent<FrameRoomManagerComponent>();
        if (roomManager.TryGetPlayingUnit(session, out FSManagerComponent frameManager, out var unit))
        {
            frameManager.InputFrameCommands(unit, message.AckFrameId, message.InputSequence, message.Commands);
        }

        await FTask.CompletedTask;
    }
}

/// <summary>
/// 记录客户端已经连续执行的权威帧号。
/// </summary>
public sealed class C2G_FrameAckHandler : Message<C2G_FrameAck>
{
    /// <summary>
    /// 仅更新会话所属对局内该玩家的诊断确认位置。
    /// </summary>
    /// <param name="session">发送确认的网络会话。</param>
    /// <param name="message">客户端最后执行帧确认。</param>
    protected override async FTask Run(Session session, C2G_FrameAck message)
    {
        FrameRoomManagerComponent roomManager = session.Scene.GetComponent<FrameRoomManagerComponent>();
        if (roomManager.TryGetPlayingUnit(session, out FSManagerComponent frameManager, out var unit))
        {
            frameManager.AcknowledgeFrame(unit, message.AckFrameId);
        }

        await FTask.CompletedTask;
    }
}
