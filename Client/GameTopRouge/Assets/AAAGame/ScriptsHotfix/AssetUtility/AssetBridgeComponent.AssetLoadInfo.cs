using System.Text;
using GameFramework;

namespace AAAGame.ScriptsHotfix.AssetUtility
{
    public partial class AssetBridgeComponent
    {
        /// <summary>
        /// 资源加载信息
        /// </summary>
        public class AssetLoadInfo : IReference
        {
            public StringBuilder AssetName = new StringBuilder(100);
            public object Asset;
            public object UserData;
            public GameFrameworkAction<AssetLoadInfo> CSharpCallBack;
            public int LoadId;
            public bool IsLocalized = false;

            public static AssetLoadInfo Create(int loadId, string assetName,GameFrameworkAction<AssetLoadInfo> CSharpcCallBack,object uaerData = null)
            {
                AssetLoadInfo assetInfo = ReferencePool.Acquire<AssetLoadInfo>();
                assetInfo.AssetName.Clear();
                assetInfo.AssetName.Append(assetName);
                assetInfo.CSharpCallBack = CSharpcCallBack;
                assetInfo.UserData = uaerData;
                assetInfo.Asset = null;
                assetInfo.LoadId = loadId;            
                return assetInfo;
            }


            public void Clear()
            {
                CSharpCallBack = null;
            }
        }

    }
}
