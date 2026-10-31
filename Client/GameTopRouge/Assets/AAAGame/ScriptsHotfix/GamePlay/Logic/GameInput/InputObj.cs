using GameFramework;
using UnityEngine;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.GameInput
{
    public class InputObj:IReference
    {
        public fix Horizontal;
        public fix Vertica;
        public int PlayerId;
        public KeyCode Key = KeyCode.None;
        
        public static InputObj Create(fix horizontal, fix vertica)
        {
            InputObj inputObj = ReferencePool.Acquire<InputObj>();
            inputObj.Horizontal = horizontal;
            inputObj.Vertica = vertica;
            return inputObj;
        }
        
        public static InputObj Create(KeyCode key)
        {
            InputObj inputObj = ReferencePool.Acquire<InputObj>();
            inputObj.Key = key;
            return inputObj;
        }

        public void Clone(InputObj inputObj)
        {
            Horizontal = inputObj.Horizontal;
            Vertica = inputObj.Vertica;
            PlayerId = inputObj.PlayerId;
            Key = inputObj.Key;
        }
        
        public void Clear()
        {
            Horizontal = fix.Zero;
            Vertica = fix.Zero;
            PlayerId = 0;
            Key = KeyCode.None;
        }
    }
}
