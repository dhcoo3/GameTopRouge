using GameFramework;
using UnityGameFramework.Runtime;

namespace AAAGame.ScriptsHotfix.UI.Core
{
    /// <summary>
    /// 保存界面打开选项、回调和业务数据的可池化参数对象。
    /// </summary>
    public class UIParams : RefParams
    {
        /// <summary>标记该参数是否已经归还对象池，避免多个失败路径重复归还。</summary>
        private bool _released;

        /// <summary>归还对象池前同步通知 UIService 清理本次参数所有权。</summary>
        internal GameFrameworkAction<UIParams> ReleasingCallback { get; set; }

        /// <summary>是否允许通过 Escape 键关闭界面；为空时使用界面默认行为。</summary>
        public bool? AllowEscapeClose { get; set; } = null;

        /// <summary>界面排序值；为空时使用 UI 组和面板自身计算的默认排序。</summary>
        public int? SortOrder { get; set; } = null;

        /// <summary>标记该表单是否作为另一个表单的子界面打开。</summary>
        public bool IsSubUIForm { get; set; } = false;

        /// <summary>表单打开完成后调用的回调。</summary>
        public GameFrameworkAction<UIFormLogic> OpenCallback { get; set; } = null;

        /// <summary>表单关闭时调用的回调。</summary>
        public GameFrameworkAction<UIFormLogic> CloseCallback { get; set; } = null;

        /// <summary>表单按钮触发时调用的回调。</summary>
        public GameFrameworkAction<object, string> ButtonClickCallback { get; set; } = null;

        /// <summary>
        /// 从对象池创建界面参数，并设置基础关闭与排序选项。
        /// </summary>
        /// <param name="allowEscape">是否允许 Escape 关闭；为空时保留表单默认值。</param>
        /// <param name="sortOrder">指定的界面排序值；为空时由界面层级计算。</param>
        /// <returns>可传给 UIService 或表单的参数对象。</returns>
        public static UIParams Create(bool? allowEscape = null, int? sortOrder = null)
        {
            var uiParms = ReferencePool.Acquire<UIParams>();
            uiParms._released = false;
            uiParms.CreateRoot();
            uiParms.AllowEscapeClose = allowEscape;
            uiParms.SortOrder = sortOrder;
            uiParms.IsSubUIForm = false;
            return uiParms;
        }

        /// <summary>
        /// 将参数对象归还对象池；重复调用不会重复归还同一对象。
        /// </summary>
        public void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            try
            {
                // UI 关闭事件在下一帧派发，所有权必须先于池复用同步解除。
                ReleasingCallback?.Invoke(this);
            }
            finally
            {
                ReferencePool.Release(this);
            }
        }

        /// <summary>
        /// 清理对象池复用时的界面选项、回调和业务变量。
        /// </summary>
        protected override void ResetProperties()
        {
            base.ResetProperties();
            ReleasingCallback = null;
            AllowEscapeClose = null;
            SortOrder = null;
            OpenCallback = null;
            CloseCallback = null;
            ButtonClickCallback = null;
            IsSubUIForm = false;
        }
    }
}
