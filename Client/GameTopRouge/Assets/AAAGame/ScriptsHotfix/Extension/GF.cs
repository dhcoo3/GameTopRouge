using GameFramework;
using System;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.AssetUtility;
using AAAGame.ScriptsHotfix.Extension;
using AAAGame.ScriptsHotfix.GameNetwork;
using Unity.Burst;
using UnityEngine;
using UnityGameFramework.Runtime;
using Random = UnityEngine.Random;

public class GF : GFBuiltin
{
    public static DataModelComponent DataModel { get; private set; }
    //替代DataNode, 使用Jobs的高性能变量存储池,用于海量数据存储
    public static VariablePoolComponent VariablePool { get; private set; }
    //public static ADComponent AD { get; private set; }
    public static GameNetworkComponent GameNetwork { get; private set; }
    
    
    public static LuBanComponent m_LuBan;
    public static LuBanComponent LuBan {
        get
        {
            if (m_LuBan == null)
            {
                m_LuBan = new LuBanComponent();
            }
            return m_LuBan;
        } 
    }

    public static AssetBridgeComponent AssetBridge { get; private set; }
    
    public static readonly List<GameFrameworkAction<float>> FixedUpdateActions = new List<GameFrameworkAction<float>>();
    
    public static readonly List<GameFrameworkAction<float>> UpdateActions = new List<GameFrameworkAction<float>>();
    
    public static RectTransform UIRoot;
    
    public static Camera MainCamera;

    public static int UserId;
    
    private void Start()
    {
        DataModel = GameEntry.GetComponent<DataModelComponent>();     
        VariablePool = GameEntry.GetComponent<VariablePoolComponent>();
        AssetBridge = GameEntry.GetComponent<AssetBridgeComponent>();
        GameNetwork = GameEntry.GetComponent<GameNetworkComponent>();
        MainCamera = Camera.main;
        UIRoot = GameObject.FindWithTag("UIRoot").GetComponent<RectTransform>();
    }

    private void OnApplicationQuit()
    {
        OnExitGame();
    }

    private void OnApplicationPause(bool pause)
    {
        //Log.Info("OnApplicationPause:{0}", pause);
        if (Application.isMobilePlatform && pause)
        {
            OnExitGame();
        }
    }

    [BurstCompile]
    private void FixedUpdate()
    {
        foreach (var action in FixedUpdateActions)
        {
            action.Invoke(Time.fixedDeltaTime);
        }
    }

    [BurstCompile]
    private void Update()
    {
        foreach (var action in UpdateActions)
        {
            action.Invoke(Time.deltaTime);
        }
    }

    private void OnExitGame()
    {        
        var exit_time = DateTime.UtcNow.ToString();
        GF.Setting.SetString(ConstBuiltin.Setting.QuitAppTime, exit_time);
        GF.Setting.Save();
        UnityGameFramework.Runtime.Log.Info("Application Quit:{0}", exit_time);
    }
}
