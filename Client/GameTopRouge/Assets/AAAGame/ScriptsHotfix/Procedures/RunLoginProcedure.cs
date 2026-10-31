using System;
using AAAGame.ScriptsHotfix.UI.Core;
using cfg.UI;
using Cysharp.Threading.Tasks;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

/// <summary>
/// 初始化并显示登录模块入口，连接由游客按钮在 HTTP 认证后发起。
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class RunLoginProcedure : ProcedureBase
{
    /// <summary>
    /// 启动登录模块初始化流程。
    /// </summary>
    /// <param name="procedureOwner">当前流程所属的有限状态机。</param>
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        InitializeLoginAsync().Forget();
    }

    /// <summary>
    /// 等待 UI 模块配置和控制器初始化后打开登录入口。
    /// </summary>
    private async UniTask InitializeLoginAsync()
    {
        try
        {
            await UIModuleManager.Instance.InitializeAsync();
            await UIModuleManager.Instance.GoToModuleAsync(ModuleType.Login);
        }
        catch (Exception exception)
        {
            Log.Error($"启动登录模块失败：{exception}");
        }
    }

}
