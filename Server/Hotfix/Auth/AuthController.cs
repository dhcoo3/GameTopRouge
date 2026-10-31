using Entity.Auth;
using Fantasy;
using Fantasy.Network.HTTP;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Hotfix.Auth;

/// <summary>游客登录与重连票据的 HTTP 入口，业务操作运行于所属 Scene 上下文。</summary>
[ApiController]
[Route("api/auth")]
[RequestSizeLimit(MaximumRequestBytes)]
[ServiceFilter(typeof(SceneContextFilter))]
public sealed class AuthController : ControllerBase
{
    /// <summary>游客登录 JSON 请求的最大字节数。</summary>
    private const int MaximumRequestBytes = 4096;
    /// <summary>HTTP 服务所属的 Fantasy Scene。</summary>
    private readonly Scene _scene;

    /// <summary>由 Fantasy HTTP 服务注入所属场景。</summary>
    /// <param name="scene">LoginHttp Scene。</param>
    public AuthController(Scene scene) => _scene = scene;

    /// <summary>验证或首次注册游客，签发新的登录授权和一次性游戏服票据。</summary>
    /// <param name="request">客户端持久游客凭据。</param>
    /// <returns>认证结果或统一错误。</returns>
    [HttpPost("guest/login")]
    public Task<IActionResult> Login([FromBody] GuestLoginRequest request) => ExecuteAsync(async store =>
        await store.LoginAsync(request, AuthSceneHelper.GetGate(_scene)));

    /// <summary>使用当前 Bearer 授权申请新的游戏服票据。</summary>
    /// <returns>连接信息和新票据，授权失效时返回 401。</returns>
    [HttpPost("ticket")]
    public Task<IActionResult> Ticket() => ExecuteAsync(async store =>
    {
        string header = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new AuthFailureException(AuthConstants.SessionReplaced, 401, "登录授权缺失，请重新登录。");
        return await store.RefreshTicketAsync(header[prefix.Length..].Trim(), AuthSceneHelper.GetGate(_scene));
    });

    /// <summary>统一处理认证错误，隐藏数据库和异常内部详情。</summary>
    /// <param name="operation">具体认证操作。</param>
    /// <returns>HTTP 成功结果或统一错误对象。</returns>
    private async Task<IActionResult> ExecuteAsync(Func<GuestAuthStore, Task<AuthTicketResponse>> operation)
    {
        string requestId = HttpContext.TraceIdentifier;
        string path = Request.Path.Value ?? "";
        Stopwatch stopwatch = Stopwatch.StartNew();
        Log.Info($"HTTP 认证请求开始 Id={requestId}, Method={Request.Method}, Path={path}, Scene={_scene.SceneConfigId}");
        try
        {
            Response.Headers.CacheControl = "no-store";
            AuthTicketResponse result = await operation(AuthSceneHelper.GetStore(_scene));
            stopwatch.Stop();
            Log.Info($"HTTP 认证请求成功 Id={requestId}, Path={path}, Status=200, ElapsedMs={stopwatch.ElapsedMilliseconds}");
            return Ok(result);
        }
        catch (AuthFailureException exception)
        {
            stopwatch.Stop();
            Log.Warning($"HTTP 认证请求被拒绝 Id={requestId}, Path={path}, Status={exception.Status}, Code={exception.Code}, ElapsedMs={stopwatch.ElapsedMilliseconds}");
            return StatusCode(exception.Status, new AuthErrorResponse { Code = exception.Code, Message = exception.Message });
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            Log.Error($"HTTP 认证请求异常 Id={requestId}, Path={path}, Scene={_scene.SceneConfigId}, Type={exception.GetType().Name}, Message={exception.Message}, ElapsedMs={stopwatch.ElapsedMilliseconds}");
            return StatusCode(503, new AuthErrorResponse { Code = AuthConstants.Unavailable, Message = "认证服务暂不可用，请稍后重试。" });
        }
    }
}
