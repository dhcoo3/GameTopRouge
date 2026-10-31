using UnityEngine;

namespace AAAGame.ScriptsHotfix.UI.Core
{
    public class GameGlobalSetting
    {
        /// <summary>
        /// 开发宽度
        /// </summary>
        public static float DesignWidth = 1080;
    
        /// <summary>
        /// 开发高度
        /// </summary>
        public static float DesignHeight = 1920;
        
        /// <summary>
        /// 主相机
        /// </summary>
        public static Camera MainCamera;
    
        /// <summary>
        /// UI相机
        /// </summary>
        public static Camera UICamera;

        /// <summary>
        /// UIROOT
        /// </summary>
        public static Transform UIRoot;

        public enum SortingLayers
        {
            Default,
            UI,
        }

        /// <summary>
        /// 顶部刘海区域
        /// </summary>
        private static float m_OffsetHeight = -1;

        /// <summary>
        /// 顶部刘海区域
        /// </summary>
        public static float OffsetHeight
        {
            get
            {
                if(m_OffsetHeight == -1)
                {
                    m_OffsetHeight = GetSafeArea();
                }

                return m_OffsetHeight;
            }
            set
            {
                m_OffsetHeight = value;
            }
        }

        /// <summary>
        /// 计算顶部刘海区域
        /// </summary>
        private static float GetSafeArea()
        {
            float offsetHeight = 0;
       
#if UNITY_WEBGL
       string platform = UnityEngine.PlayerPrefs.GetString("MiniGamePlatform","");
       
       if (string.IsNullOrEmpty(platform) || platform == "wx_android"|| platform == "wx_ios")
       {
           offsetHeight = 85f;
       }
       
       Debug.Log("刘海屏宽度:"+offsetHeight + "  " + platform);
#else
            foreach (var t in Screen.cutouts)
            {
                if (t.height > offsetHeight)
                {
                    offsetHeight = t.height;
                }
            }
#endif


#if MINIGAME_ZHB
        offsetHeight = 160f;
#endif        
            return offsetHeight;
        }
    
        /// <summary>
        /// 是否执行引导流程中
        /// </summary>
        public static bool IsGuideIng = false;
    }
}
