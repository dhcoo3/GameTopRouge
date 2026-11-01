using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>任务列表展示数据，只携带稳定编号和本地化键。</summary>
    public readonly struct QuestListItem
    {
        public readonly int QuestId;
        public readonly string TitleKey;
        public readonly string StatusKey;
        public QuestListItem(int questId, string titleKey, string statusKey)
        {
            QuestId = questId;
            TitleKey = titleKey;
            StatusKey = statusKey;
        }
    }

    /// <summary>可复用任务行，独立管理内部控件和点击回调。</summary>
    [RequireComponent(typeof(UIContainer))]
    public sealed class QuestRowView : MonoBehaviour
    {
        private UGF_Button m_Button;
        private UGF_Image m_Background;
        private TMP_Text m_Title;
        private TMP_Text m_Status;
        private Action<int> _onSelected;
        public int QuestId { get; private set; }

        private void BindControls()
        {
            if (m_Button != null) return;
            var container = GetComponent<UIContainer>();
            m_Button = container.Get<UGF_Button>("m_Button");
            m_Background = container.Get<UGF_Image>("m_Background");
            m_Title = container.Get<TMP_Text>("m_Title");
            m_Status = container.Get<TMP_Text>("m_Status");
        }

        /// <summary>覆盖复用对象的全部展示数据和回调。</summary>
        public void Show(QuestListItem item, bool selected, Action<int> onSelected)
        {
            BindControls();
            ResetForReuse();
            QuestId = item.QuestId;
            name = "m_QuestRow_" + QuestId;
            m_Title.text = GF.Localization.GetText(item.TitleKey);
            m_Status.text = GF.Localization.GetText(item.StatusKey);
            _onSelected = onSelected;
            m_Button.SetClickListener(OnClicked);
            SetSelected(selected);
        }

        private void OnClicked() => _onSelected?.Invoke(QuestId);

        public void SetSelected(bool selected)
        {
            BindControls();
            m_Background.color = selected ? new Color32(119, 91, 51, 255) : new Color32(48, 43, 36, 250);
        }

        /// <summary>回收或重新绑定前清除回调和旧焦点。</summary>
        public void ResetForReuse()
        {
            BindControls();
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
            m_Button.SetClickListener(null);
            _onSelected = null;
            QuestId = 0;
        }
    }
}
