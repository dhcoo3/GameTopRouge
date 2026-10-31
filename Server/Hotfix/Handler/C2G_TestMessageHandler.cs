using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix.Handler;

public class C2G_TestMessageHandler:Message<C2G_TestMessage>
{
    protected override async FTask Run(Session session, C2G_TestMessage message)
    {
        Log.Debug($"C2G_TestMessageHandler {message.Tag}");
       await FTask.CompletedTask;
    }
}