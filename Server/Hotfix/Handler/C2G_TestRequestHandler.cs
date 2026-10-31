using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix.Handler;

public class C2G_TestRequestHandler:MessageRPC<C2G_TestRequest, G2C_TestResponse>
{
    protected override async FTask Run(Session session, C2G_TestRequest request, G2C_TestResponse response, Action reply)
    {
        Log.Debug($"C2G_TestMessageHandler {request.Tag}");
        response.ErrorCode = 0;
        response.Tag = "test11";
        await FTask.CompletedTask;
    }
}