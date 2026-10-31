using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;
using GameFramework.Resource;
using Luban;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.Extension
{
    /// <summary>
    /// LuaBan配置表组件，比较热门的配置表方案
    /// </summary>
    public class LuBanComponent
    {
        /// <summary>
        /// LuBan配置表
        /// </summary>
        private cfg.Tables _tables;

        /// <summary>
        /// 加载回调
        /// </summary>
        private readonly LoadBinaryCallbacks _loadAssetCallbacks;

        /// <summary>
        /// 已加载的配置表字典，键为表名，值为对应的配置表对象
        /// </summary>
        private readonly Dictionary<string, Tables.IVOFun> _loadedTables;

        /// <summary>
        /// 存储已加载的配置表二进制数据，键为表名，值为对应的二进制数据。
        /// </summary>
        private readonly Dictionary<string, byte[]> _dataTableBytes;

        /// <summary>
        /// 存储已加载或正在加载的数据表的UniTaskCompletionSource，用于异步加载数据表时的完成源。
        /// </summary>
        private readonly Dictionary<string, UniTaskCompletionSource<byte[]>> _loadedTableSources;

        public LuBanComponent()
        {
            _loadAssetCallbacks = new LoadBinaryCallbacks(OnLoadAssetSuccess, OnLoadAssetFailure);
            _loadedTables = new Dictionary<string, Tables.IVOFun>();
            _dataTableBytes = new Dictionary<string, byte[]>();
            _loadedTableSources = new Dictionary<string, UniTaskCompletionSource<byte[]>>();
        }
        
        /// <summary>
        /// 配置表
        /// </summary>
        public cfg.Tables Tables
        {
            get
            {
                return _tables ??= new cfg.Tables();
            }
        }
        
        /// <summary>
        /// 异步加载并获取指定名称的配置表
        /// </summary>
        /// <typeparam name="T">配置表类型，必须实现cfg.Tables.IVOFun接口</typeparam>
        /// <param name="tableName">配置表的名称</param>
        /// <returns>返回加载的配置表对象</returns>
        public async UniTask<T> Get<T>(string tableName) where T : Tables.IVOFun
        {
            if (_loadedTables.TryGetValue(tableName, out Tables.IVOFun table))
            {
                return (T)table;
            }
 
            byte[] data = await LoadDataTable(tableName);
            // 并发调用可能在等待二进制数据期间完成同一张表的解析。
            if (_loadedTables.TryGetValue(tableName, out table)) return (T)table;
            
            if (Tables.CfgDic.TryGetValue(tableName,out Tables.IVOFun source))
            {
                source._LoadData(new ByteBuf(data));
                source.ResolveRef(Tables);
                _loadedTables.Add(tableName,source);
            }
            
            return (T)_loadedTables[tableName];
        }

        /// <summary>
        /// 异步加载指定名称的数据表文件
        /// </summary>
        /// <param name="file">数据表文件的名称</param>
        /// <returns>返回加载的数据表文件的字节数组</returns>
        public async UniTask<byte[]> LoadDataTable(string file)
        {
            if (_dataTableBytes.TryGetValue(file, out byte[] cached)) return cached;
            if (_loadedTableSources.TryGetValue(file, out var pending)) return await pending.Task;
            var source = new UniTaskCompletionSource<byte[]>();
            _loadedTableSources.Add(file, source);
            GF.Resource.LoadBinary(UtilityBuiltin.AssetsPath.GetDataTablePath(file, true),_loadAssetCallbacks);
            return await source.Task;
        }
       
        /// <summary>
        /// 当加载的资产成功时调用此方法
        /// </summary>
        /// <param name="assetName">资产名称</param>
        /// <param name="asset">加载的资产对象</param>
        /// <param name="duration">加载所花费的时间</param>
        /// <param name="userData">用户数据</param>
        private void OnLoadAssetSuccess(string assetName, object asset, float duration, object userData)
        {
            string key = System.IO.Path.GetFileNameWithoutExtension(assetName);
            byte[] text = asset as byte[];          
            _dataTableBytes.TryAdd(key,text);
            if(_loadedTableSources.TryGetValue(key,out var value))
            {
                value.TrySetResult(text);
                _loadedTableSources.Remove(key);
            }
        }

        /// <summary>
        /// 当加载配置表资源失败时调用的回调方法。
        /// </summary>
        /// <param name="configAssetName">配置表资源名称。</param>
        /// <param name="status">加载资源的状态。</param>
        /// <param name="errorMessage">错误信息。</param>
        /// <param name="userData">用户自定义数据。</param>
        private void OnLoadAssetFailure(string configAssetName, LoadResourceStatus status, string errorMessage,
            object userData)
        {
            Log.Error("LoadAssetFailure:{0} {1} {2}", configAssetName, status, errorMessage);
            string key = System.IO.Path.GetFileNameWithoutExtension(configAssetName);
            if (_loadedTableSources.TryGetValue(key, out var source))
            {
                _loadedTableSources.Remove(key);
                source.TrySetException(new System.IO.IOException(errorMessage));
            }
        }
    }
}
