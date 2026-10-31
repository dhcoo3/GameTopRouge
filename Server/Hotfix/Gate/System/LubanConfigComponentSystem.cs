using Fantasy;
using Luban;

namespace Hotfix.Gate.Component;

/// <summary>
/// Luban 配置表组件的加载和查询逻辑。
/// </summary>
public static class LubanConfigComponentSystem
{
    /// <summary>
    /// 按生成器中的表名获取强类型表对象。
    /// </summary>
    /// <typeparam name="TTable">Luban 生成的表类型。</typeparam>
    /// <param name="self">Gate 场景中的配置表组件。</param>
    /// <param name="tableName">生成表类中的表名常量。</param>
    /// <returns>请求的强类型配置表。</returns>
    public static TTable GetTable<TTable>(this LubanConfigComponent self, string tableName)
        where TTable : class, cfg.Tables.IVOFun
    {
        ArgumentNullException.ThrowIfNull(self);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        cfg.Tables tables = self.Tables ?? throw new InvalidOperationException("Luban 配置表尚未加载。");
        if (!tables.CfgDic.TryGetValue(tableName, out cfg.Tables.IVOFun? table))
        {
            throw new KeyNotFoundException($"不存在 Luban 配置表：{tableName}");
        }

        return table as TTable ?? throw new InvalidCastException(
            $"Luban 配置表 {tableName} 的类型为 {table.GetType().FullName}，无法转换为 {typeof(TTable).FullName}。");
    }
}
