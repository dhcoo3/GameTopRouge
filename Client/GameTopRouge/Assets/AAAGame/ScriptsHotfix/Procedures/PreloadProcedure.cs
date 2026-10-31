
using UnityEngine;
using GameFramework.Event;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using GameFramework.Fsm;
using System.Collections.Generic;
using GameFramework;
using System;
using System.IO;
using System.Text;
using cfg;
using GameFramework.Resource;
using Cysharp.Threading.Tasks;
using Luban;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class PreloadProcedure : ProcedureBase
{
    private int totalProgress;
    private int loadedProgress;
    private float smoothProgress;
    private bool preloadAllCompleted;
    private float progressSmoothSpeed = 10f;
    
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        GF.BuiltinView.ShowLoadingProgress();
        GF.Log("进入HybridCLR热更流程! 预加载游戏数据...");

        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
        GF.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);
      
        InitAppSettings();
        PreloadAndInitData();
    }


    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
        GF.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);
        GF.BuiltinView.HideLoadingProgress();
        base.OnLeave(procedureOwner, isShutdown);
    }


    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (totalProgress <= 0 || preloadAllCompleted) return;

        smoothProgress = Mathf.Lerp(smoothProgress, loadedProgress / (float)totalProgress, elapseSeconds * progressSmoothSpeed);

        GF.BuiltinView.SetLoadingProgress(smoothProgress);
        
        //预加载完成 切换场景
        if (loadedProgress >= totalProgress && smoothProgress >= 0.99f)
        {
            preloadAllCompleted = true;
            GF.Log("预加载完成, 进入游戏场景.");
            procedureOwner.SetData<VarString>(ChangeSceneProcedure.P_SceneName, "Game");
            ChangeState<ChangeSceneProcedure>(procedureOwner);
        }
    }
    private void InitAppSettings()
    {
        //if (string.IsNullOrWhiteSpace(GF.Setting.GetABTestGroup()))
        //{
        //    GF.Setting.SetABTestGroup("B");//设置A/B测试组; 应由服务器分配该新用户所属测试组
        //}
    }
    
    /// <summary>
    /// 预加载数据表、游戏配置,以及初始化游戏数据
    /// </summary>
    private async void PreloadAndInitData()
    {
        preloadAllCompleted = false;
        smoothProgress = 0;
        loadedProgress = 0;
        totalProgress =  1;
        CreateGFExtension();
    }
    
    private async void LoadConfigsAndDataTables()
    {
        LocalLanguage();
    }
    
    private void CreateGFExtension()
    {
        GF.Resource.LoadAsset(UtilityBuiltin.AssetsPath.GetPrefab("Core/GFExtension"), typeof(GameObject), new GameFramework.Resource.LoadAssetCallbacks(OnLoadGFExtensionSuccess, OnLoadGFExtensionFailed));
    }

    /// <summary>
    /// 初始化语言
    /// </summary>
    private async void LocalLanguage()
    {
        GameFramework.Localization.Language language = GF.Setting.GetLanguage();
        
        if (language == GameFramework.Localization.Language.Unspecified)
        {
#if UNITY_EDITOR
            language = GF.Base.EditorLanguage;
#else
            language = GFBuiltin.Localization.SystemLanguage;//默认语言跟随用户操作系统语言
#endif
        }

        GF.Setting.SetLanguage(language, false);
        
        GF.Log(Utility.Text.Format("初始化游戏设置. 游戏语言:{0},系统语言:{1}", language, GFBuiltin.Localization.SystemLanguage));
        
        GF.Localization.RemoveAllRawStrings();

        TbLocalization tbLocalization = await GF.LuBan.Get<TbLocalization>(Tables.tblocalization);
        
        ///把多语言表的数据导入到组件
        Dictionary<string,string> dic = new Dictionary<string, string>(tbLocalization.DataList.Count);
        
        foreach (var data in tbLocalization.DataList)
        {
            dic.Add(data.Id, data.ChineseSimplified);
        }
        
        GF.Localization.ReadData(UtilityBuiltin.AssetsPath.GetDataTablePath(Tables.tblocalization, true),dic);
    }

    private void OnLoadGFExtensionSuccess(string assetName, object asset, float duration, object userData)
    {
        var gfExtPfb = asset as GameObject;
        if (null != GameObject.Instantiate(gfExtPfb, Vector3.zero, Quaternion.identity, GF.Base.transform))
        {
            GF.Log("GF框架扩展成功!");
            loadedProgress++;
            LoadConfigsAndDataTables();
        }
    }
    private void OnLoadGFExtensionFailed(string assetName, LoadResourceStatus status, string errorMessage, object userData)
    {
        GF.LogError(Utility.Text.Format("GF框架扩展加载失败:{0}, Error:{1}", assetName, errorMessage));
    }
    
    /// <summary>
    /// 加载本地化文件成功
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoadDictionarySuccess(object sender, GameEventArgs e)
    {
        LoadDictionarySuccessEventArgs ne = (LoadDictionarySuccessEventArgs)e;
        loadedProgress++;
        Log.Info("Load Dictionary '{0}' OK.", ne.DictionaryAssetName);
    }

    /// <summary>
    /// 加载本地化文件失败
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoadDictionaryFailure(object sender, GameEventArgs e)
    {
        LoadDictionaryFailureEventArgs ne = (LoadDictionaryFailureEventArgs)e;
        Log.Error("Can not load dictionary '{0}' from '{1}' with error message '{2}'.", ne.DictionaryAssetName, ne.DictionaryAssetName, ne.ErrorMessage);
    }
}
