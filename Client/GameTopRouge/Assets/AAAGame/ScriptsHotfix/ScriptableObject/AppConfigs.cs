using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>客户端启动流程、数据资源及登录服务的运行配置。</summary>
[CreateAssetMenu(fileName = "AppConfigs", menuName = "GF/AppConfigs [配置App运行时所需数据表、配置表、流程]")]
public class AppConfigs : ScriptableObject
{
    /// <summary>游客认证服务地址，本机开发使用 HTTP，部署可设置为 HTTPS。</summary>
    [Header("登录认证服务")]
    [SerializeField] private string m_AuthBaseUrl = "http://127.0.0.1:20001";
    /// <summary>当前环境的登录服务基础地址。</summary>
    public string AuthBaseUrl => string.IsNullOrWhiteSpace(m_AuthBaseUrl) ? "http://127.0.0.1:20001" : m_AuthBaseUrl;
    /// <summary>当前已加载的配置资源。</summary>
    private static AppConfigs mInstance = null;

    /// <summary>是否使用二进制数据源加载配置。</summary>
    [SerializeField] bool m_LoadFromBytes = false;
    /// <summary>配置数据的加载方式。</summary>
    public bool LoadFromBytes
    {
        get => m_LoadFromBytes;
        set => m_LoadFromBytes = value;
    }
    /// <summary>启动需要预加载的数据表名称。</summary>
    [Header("数据表")]
    [SerializeField] string[] mDataTables;
    /// <summary>启动数据表清单。</summary>
    public string[] DataTables => mDataTables;


    /// <summary>启动需要加载的配置表名称。</summary>
    [Header("配置表")]
    [SerializeField] string[] mConfigs;
    /// <summary>配置表清单。</summary>
    public string[] Configs => mConfigs;

    /// <summary>启动需要加载的多语言表名称。</summary>
    [Header("多语言表")]
    [SerializeField] string[] mLanguages;
    /// <summary>多语言表清单。</summary>
    public string[] Languages => mLanguages;

    /// <summary>热更流程状态机使用的流程类型名称。</summary>
    [Header("已启用流程列表")]
    [SerializeField] string[] mProcedures;

    /// <summary>启用的流程类型清单。</summary>
    public string[] Procedures => mProcedures;

    /// <summary>资源加载后记录单例引用。</summary>
    private void Awake()
    {
        mInstance = this;
    }


#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下获取实例
    /// </summary>
    /// <returns></returns>
    public static AppConfigs GetInstanceEditor()
    {
        if (mInstance == null)
        {
            var configAsset = UtilityBuiltin.AssetsPath.GetScriptableAsset("Core/AppConfigs");
            mInstance = AssetDatabase.LoadAssetAtPath<AppConfigs>(configAsset);
        }
        return mInstance;
    }
#endif
    /// <summary>
    /// 运行时获取实例
    /// </summary>
    /// <returns></returns>
    public static async UniTask<AppConfigs> GetInstanceSync()
    {
        var configAsset = UtilityBuiltin.AssetsPath.GetScriptableAsset("Core/AppConfigs");
        if (mInstance == null)
        {
            mInstance = await GFBuiltin.Resource.LoadAssetAwait<AppConfigs>(configAsset);
        }
        return mInstance;
    }

}
