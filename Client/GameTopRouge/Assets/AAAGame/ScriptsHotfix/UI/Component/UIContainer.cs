using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UIContainerData
{
   public string NodeName;
   public GameObject NodeObj;
   public string NodeType;
   public string Domain;
}

[Serializable]
public class UIContainerDictionary : SerializableDictionary<string, UIContainerData> 
{    

}

[Serializable]
public class UIContainer : MonoBehaviour
{
   public UIContainerDictionary UIContainerDict
   {
      get
      {
         return mUIContainerDict;
      } 
   }
   
   [HideInInspector]
   public UIContainerDictionary mUIContainerDict = new UIContainerDictionary();

   public void AddNode(string nodeName, GameObject nodeObj, string nodeType, string domain)
   {
      if (mUIContainerDict.TryGetValue(nodeName, out UIContainerData data))
      {
         data.NodeObj = nodeObj;
         data.NodeType = nodeType;
         data.NodeName = nodeName;
         data.Domain = domain;
         return;
         
      }
      mUIContainerDict.TryAdd(nodeName, new UIContainerData() { NodeName = nodeName, NodeObj = nodeObj, NodeType = nodeType , Domain = domain });
   }

   public bool RemoveNode(string nodeName)
   {
      return mUIContainerDict.Remove(nodeName);
   }

   public void Clear()
   {
      List<string> tList = new List<string>();
      
      foreach (var data in mUIContainerDict)
      {
         if (data.Value.NodeObj == null)
         {
            tList.Add(data.Key);
         }
      }

      foreach (var key in tList)
      {
         mUIContainerDict.Remove(key);
      }
   }

   public T Get<T>(string nodeName)
   {
      if (mUIContainerDict.TryGetValue(nodeName, out UIContainerData data))
      {
         return data.NodeObj.GetComponent<T>();
      }

      throw new InvalidOperationException("对象没有指定的组件，请检查后再获取");
   }
}
