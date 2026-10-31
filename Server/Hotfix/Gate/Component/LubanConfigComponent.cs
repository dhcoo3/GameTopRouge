using Fantasy;
using Fantasy.Entitas.Interface;
using Fantasy.Gate.Entity;
using Luban;

namespace Hotfix.Gate.Component;

// Awake 事件（实体创建时触发）
public class LubanConfigComponentAwakeSystem : AwakeSystem<LubanConfigComponent>
{
    protected override void Awake(LubanConfigComponent self)
    {
        self.Load();
    }
}

/// <summary>
/// Gate 场景持有的 Luban 配置表集合。
/// </summary>
public sealed class LubanConfigComponent : Fantasy.Entitas.Entity
{
    /// <summary>已加载并完成表间引用解析的配置表集合。</summary>
    public cfg.Tables? Tables { get; internal set; }
    
    /// <summary>
    /// 从指定目录加载全部表数据，并在所有数据加载完成后解析表间引用。
    /// </summary>
    /// <param name="self">Gate 场景中的配置表组件。</param>
    /// <param name="dataDirectory">服务器 .bytes 文件目录；省略时使用程序目录下的 Config/Data。</param>
    public  void Load( string? dataDirectory = null)
    {
        if (Tables != null)
        {
            throw new InvalidOperationException("Luban 配置表组件已经完成加载。");
        }

        string resolvedDataDirectory = Path.GetFullPath(dataDirectory ?? Path.Combine(AppContext.BaseDirectory, "Config", "Data"));
        cfg.Tables tables = new();

        foreach ((string tableName, cfg.Tables.IVOFun table) in tables.CfgDic)
        {
            string dataPath = Path.Combine(resolvedDataDirectory, $"{tableName}.bytes");
            if (!File.Exists(dataPath))
            {
                throw new FileNotFoundException($"Luban 配置表数据文件不存在：{dataPath}", dataPath);
            }

            try
            {
                table._LoadData(ByteBuf.Wrap(File.ReadAllBytes(dataPath)));
            }
            catch (Exception exception)
            {
                throw new InvalidDataException($"加载 Luban 配置表 {tableName} 失败：{dataPath}", exception);
            }
        }

        foreach (cfg.Tables.IVOFun table in tables.CfgDic.Values)
        {
            table.ResolveRef(tables);
        }

        Tables = tables;
        Log.Debug($"Luban 配置表加载完成；tables={tables.CfgDic.Count}, directory={resolvedDataDirectory}");
    }

}
