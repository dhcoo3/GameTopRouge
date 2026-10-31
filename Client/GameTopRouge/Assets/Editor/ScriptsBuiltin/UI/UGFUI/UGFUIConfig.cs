using UnityEditor;

public class UGFUIConfig
{   
     static public T LoadAsset<T>(string assetName) where T : UnityEngine.Object
    {
        string[] asset = AssetDatabase.FindAssets(assetName);
        if (asset.Length > 0)
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(asset[0]));
            return obj as T;
        }

        return null;
    }

    //[MenuItem("UGFUI/CreateConfig/UGFUICreateConfig")]
    public static void CreateUGFUICreateConfig()
    {
        CustomConfigManager<UGFUICreateConfig>.CreateAsset("Assets/UGFUICreateConfig.asset");
    }
}
