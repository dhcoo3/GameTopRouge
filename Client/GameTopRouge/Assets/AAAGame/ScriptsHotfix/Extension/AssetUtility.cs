using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;

public class AssetInfo: IReference
{        
    public Type AssetType;
    public string AssetName;
    public object Asset;
    public object UserData;
    public GameFrameworkAction<AssetInfo> CSharpcCallBack;
    public int LoadId;
    
    public static AssetInfo Create(int loadId,string assetName,GameFrameworkAction<AssetInfo> CSharpcCallBack,Type type = null,object userData=null)
    {
        AssetInfo assetInfo = ReferencePool.Acquire<AssetInfo>();
        assetInfo.AssetName = assetName;
        assetInfo.CSharpcCallBack = CSharpcCallBack;
        assetInfo.Asset = null;
        assetInfo.LoadId = loadId;
        assetInfo.UserData = userData;
        
        if (type != null)
        {
            assetInfo.AssetType = type;
        }
        else
        {
            assetInfo.AssetType = typeof(object);
        }
        
        return assetInfo;
    }

    public void Clear()
    {
        CSharpcCallBack = null;
    }
}


