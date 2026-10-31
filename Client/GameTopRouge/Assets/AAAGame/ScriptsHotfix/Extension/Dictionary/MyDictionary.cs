
using System;
using UnityEditor;
using UnityEngine;


[Serializable]
public class MyDictionary : SerializableDictionary<string, GameObject> 
{    

}

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MyDictionary))]
    public class MyDictionaryDrawer : DictionaryDrawer<string, GameObject> { }
#endif


[Serializable]
public class StringDictionary : SerializableDictionary<string, string> { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(StringDictionary))]
public class StringDictionaryDrawer : DictionaryDrawer<string, string> { }
#endif


[Serializable]
public class IntDictionary : SerializableDictionary<int, GameObject> 
{    

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(IntDictionary))]
public class IntDictionaryDrawer : DictionaryDrawer<int, GameObject> { }
#endif

[Serializable]
public class FloatDictionary : SerializableDictionary<string, float> { }



