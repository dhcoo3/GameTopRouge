using AAAGame.ScriptsHotfix.GamePlay.Logic.Common;
using AAAGame.ScriptsHotfix.GamePlay;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Frame.Handler
{
    /// <summary>
    /// 接收服务端实时推送或历史补取的权威帧，并同步转交给 FrameProxy。
    /// </summary>
    public sealed class G2C_FrameCommandHandler : Message<G2C_FrameCommand>
    {
        /// <summary>
        /// 在网络消息生命周期内同步广播帧事件，使 FrameProxy 立即复制协议数据。
        /// </summary>
        /// <param name="session">发送帧消息的网络会话。</param>
        /// <param name="message">包含一个或多个权威帧的服务端消息。</param>
        protected override async FTask Run(Session session, G2C_FrameCommand message)
        {
            if (message == null)
            {
                return;
            }

            GF.Event.Fire(this, GameEvent.Create(GamePlayEvent.EUpdateFrame, message));
            await FTask.CompletedTask;
        }
    }

    /// <summary>
    /// 接收服务端固定的对局描述，并由 Facade 重建或恢复确定性世界。
    /// </summary>
    public sealed class G2C_FrameMatchBootstrapHandler : Message<G2C_FrameMatchBootstrap>
    {
        /// <summary>
        /// 校验消息后将对局描述交给玩法入口处理。
        /// </summary>
        /// <param name="session">发送对局描述的网络会话。</param>
        /// <param name="message">服务端下发的固定对局描述。</param>
        protected override async FTask Run(Session session, G2C_FrameMatchBootstrap message)
        {
            if (message != null && message.MatchId > 0)
            {
                GamePlayFacade.Instance.ApplyServerMatchBootstrap(message);
            }

            await FTask.CompletedTask;
        }
    }
}
