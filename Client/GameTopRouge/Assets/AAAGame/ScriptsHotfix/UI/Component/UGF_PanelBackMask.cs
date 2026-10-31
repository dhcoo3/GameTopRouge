using System;
using GameFramework;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace AAAGame.ScriptsHotfix.UI.Component
{
    public class UGF_PanelBackMask:MonoBehaviour
    {
        /// <summary>
        /// 透明背景遮罩
        /// </summary>
        private UISubjoinUIBgLogic m_UIBackGrounpMask;

        private int m_UIBackGroupId;
        
        /// <summary>
        ///透明度设置 
        /// </summary>
        public int MaskAlpha = 128;
        
        /// <summary>
        /// 打开时，添加一个提示文本
        /// </summary>
        public bool HintText;

        /// <summary>
        /// 提示文本位置
        /// </summary>
        public Vector3 HintTextPos = new Vector3(0,-700,0);

        /// <summary>
        /// 提示文本Id
        /// </summary>
        public String HintTextId = "167";

        /// <summary>
        /// 背景遮罩使用渐变
        /// </summary>
        public bool BackgrounpMaskUseFade = true;

        private void OnEnable()
        {
            if (m_UIBackGrounpMask == null)
            {
                AddFullScreenBgMask();
            }
            else
            {
                //m_UIBackGrounpMask.UpdateLayer(m_CachedCanvas.sortingOrder,m_LayerName);
            }            
        }

        private void OnDisable()
        {
            RemoveBackGroup();
        }

        /// <summary>
        /// 增加透明背景遮罩
        /// </summary>
        public void AddFullScreenBgMask()
        {
            //Debug.Log("AddFullScreenBgMask");

            /*if (m_UIBackGroupId == 0 && ShowBgMask)
            {
                    m_UIBackGroupId = UIFramework.UISubjoin.LoadUISubjoin(UIFramework.AssetBridge.GetUIFormAsset("Global/UIBackgrounpMask"),
                    "UISubjoinUIBg",
                    bg =>
                    {
                        UISubjoinUIBgLogic logic = (UISubjoinUIBgLogic)bg;

                        m_UIBackGrounpMask = logic;

                        if (MaskAlpha > 0)
                        {
                            logic.Init(UTransform, m_CachedCanvas.sortingOrder, m_LayerName, MaskAlpha % 256);
                        }
                        else
                        {
                            logic.Init(UTransform, m_CachedCanvas.sortingOrder, m_LayerName);
                        }

                        logic.InitText(HintTextId, HintText, HintTextPos.x, HintTextPos.y, HintTextPos.z);
                        logic.Show(BackgrounpMaskUseFade);
                        logic.SetBackgrounpOnClick(BackgrounpOnClick);
                    }
                );
            }*/
        }

        public void BackgrounpOnClick()
        {
            //m_CallBackListener.CallLua(CallBackType.UIBackGroupClick,GetInstanceID());
        }
        
        /// <summary>
        /// 点击背景回调 Lua层回调
        /// </summary>
        /// <param name="luaFunction"></param>
        public void SetLuaBackgrounpOnClick(GameFrameworkAction luaFunction)
        {
            //m_CallBackListener.AddLua(CallBackType.UIBackGroupClick,m_InstanceId,luaFunction);
        }

        /// <summary>
        /// 移除面板通用背景
        /// </summary>
        private void RemoveBackGroup()
        {
            //UIFramework.UISubjoin.RemoveUISubjoin(m_UIBackGroupId);
            m_UIBackGroupId = 0;
            m_UIBackGrounpMask = null;
        }

        public void SetBackGroupPosition(float x, float y, float z)
        {
            /*if (m_UIBackGrounpMask)
            {
                m_UIBackGrounpMask.SetAnchoredPositionXYZ(x, y, z);
            }*/
        }
    }
}