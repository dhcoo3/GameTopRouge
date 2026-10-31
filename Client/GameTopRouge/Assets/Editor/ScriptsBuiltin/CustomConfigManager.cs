using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

public class CustomConfigManager<T> where T : ScriptableObject
{

    private static CustomConfigManager<T> _instance;

    public static CustomConfigManager<T> GetInstance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CustomConfigManager<T>();
            }

            return _instance;
        }
    }

    private CustomConfigManager()
    {
    }

    /// <summary>
    /// 将类实例化为配置文件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static void CreateAsset(string outPath)
    {
        //将对象实例化
        ScriptableObject so = ScriptableObject.CreateInstance(typeof(T));

        if (so == null)
        {
            Debug.Log("该对象无效，无法将对象实例化");
            return;
        }
   
        //按指定路径生成配置文件
        AssetDatabase.CreateAsset(so, outPath);
    }
}