using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using AAAGame.ScriptsHotfix.UI.Module.Login;

namespace AAAGame.ScriptsHotfix.UI.Module.Login.Protocol
{
    /// <summary>接收游戏服顶号通知并交给 LoginController 处理认证生命周期。</summary>
    public sealed class G2C_LoginRevokedHandler : Message<G2C_LoginRevoked>
    {
        /// <summary>只处理当前连接的撤销通知，复制消息中的退出原因。</summary>
        /// <param name="session">收到消息的连接。</param>
        /// <param name="message">服务端撤销原因。</param>
        /// <returns>消息处理任务。</returns>
        protected override async FTask Run(Session session, G2C_LoginRevoked message)
        {
            LoginController.Instance.HandleLoginRevoked(session, message.Message);
            await FTask.CompletedTask;
        }
    }
}
