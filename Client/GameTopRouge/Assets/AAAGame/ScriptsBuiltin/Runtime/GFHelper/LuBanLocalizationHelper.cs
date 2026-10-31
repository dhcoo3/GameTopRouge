using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using GameFramework.Localization;
[Obfuz.ObfuzIgnore]
public class LuBanLocalizationHelper : DefaultLocalizationHelper
{
    /// <summary>
    /// 解析字典。
    /// </summary>
    /// <param name="localizationManager">本地化管理器。</param>
    /// <param name="dictionaryBytes">要解析的字典二进制流。</param>
    /// <param name="startIndex">字典二进制流的起始位置。</param>
    /// <param name="length">字典二进制流的长度。</param>
    /// <param name="userData">用户自定义数据。</param>
    /// <returns>是否解析字典成功。</returns>
    public override bool ParseData(ILocalizationManager localizationManager, byte[] dictionaryBytes, int startIndex, int length, object userData)
    {
        Dictionary<string,string> dic = (Dictionary<string,string>)userData;
    
        if (dic == null)
        {
            return false;
        }
        
        foreach (KeyValuePair<string, string> item in dic)
        {
            localizationManager.AddRawString(item.Key, System.Text.RegularExpressions.Regex.Unescape(item.Value));
        }
        
        return true;
    }
}