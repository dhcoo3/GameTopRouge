using GameFramework;

namespace AAAGame.ScriptsHotfix.AssetUtility
{
    public partial class AssetBridgeComponent
    {   
        /// <summary>
        /// UI面板路径
        /// </summary>
        public const string UIFormAssetPath = "Assets/AAAGame/Prefabs/UI/";

        /// <summary>
        /// 字体路径
        /// </summary>
        public const string FontAssetPath = "Assets/Res/Font/";

        /// <summary>
        /// UI图版路径，非图集
        /// </summary>
        public const string UISpritesAssetPath = "Assets/Res/Sprite/";

        /// <summary>
        /// 材质球路径
        /// </summary>
        public const string MaterialsPath = "Assets/Res/Material/";

        /// <summary>
        /// 本地化语言目录
        /// </summary>
        public const string LocalizationPath = "Assets/Res/Config/";

        /// <summary>
        /// 特效目录
        /// </summary>
        public const string EffectPath = "Assets/AAAGame/Prefabs/Effect/";

        public const string UIAtlasPath = "Assets/Res/Atlas/";

        /// <summary>
        /// 通用置灰材质球
        /// </summary>
        public const string GrayMaterial = "ImageGrey";

        public const string SpinePath = "Assets/Res/Spine/";
        
        public const string BulletPath = "Assets/AAAGame/Prefabs/Bullet/";
        
        public const string AoePath = "Assets/AAAGame/Prefabs/Aoe/";
        
        public const string Terrain = "Assets/AAAGame/Prefabs/Terrain/";

        public string GetFontAsset(string assetName)
        {
            return Utility.Text.Format("{0}{1}", FontAssetPath, assetName);
        }

        public string GetUIFormAsset(string assetName)
        {
            return Utility.Text.Format("{0}{1}.prefab", UIFormAssetPath, assetName);
        }

        public string GetMaterialsAsset(string assetName)
        {
            return Utility.Text.Format("{0}{1}.mat", MaterialsPath, assetName);
        }

        public string GetUISpritesAsset(string assetName)
        {
            return Utility.Text.Format("{0}{1}.png", UISpritesAssetPath, assetName);
        }    

        public string GetLocalization(string assetName)
        {
            return Utility.Text.Format("{0}{1}.txt", LocalizationPath, assetName);
        }

        public string GetEffect(string assetName)
        {
            return Utility.Text.Format("{0}{1}.prefab", EffectPath, assetName);
        }

        public string GetUIAtlas(string assetName)
        {
            return Utility.Text.Format("{0}{1}.spriteatlas", UIAtlasPath, assetName);
        }

        public string GetSpine(string assetName)
        {
            return Utility.Text.Format("{0}{1}.prefab", SpinePath, assetName);
        }
        
        public string GetBullet(string assetName)
        {
            return Utility.Text.Format("{0}{1}.prefab", BulletPath, assetName);
        }
        
        public string GetAoe(string assetName)
        {
            return Utility.Text.Format("{0}{1}.prefab", AoePath, assetName);
        }

        public string GetTerrain(string assetName)
        {
            return Utility.Text.Format("{0}{1}.prefab", Terrain, assetName);
        }
    }
}

