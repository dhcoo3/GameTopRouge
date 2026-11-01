using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>任务虚拟列表，封装 LoopScrollRect 数据源和实例回收。</summary>
    [RequireComponent(typeof(UIContainer))]
    public sealed class QuestListView : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
    {
        private LoopVerticalScrollRect m_Scroll;
        private QuestRowView m_Template;
        private Transform m_PoolRoot;
        private RectTransform m_Viewport;
        private readonly Stack<QuestRowView> _pool = new Stack<QuestRowView>();
        private readonly Dictionary<Transform, QuestRowView> _instances = new Dictionary<Transform, QuestRowView>();
        private QuestListItem[] _items = Array.Empty<QuestListItem>();
        private int _selectedQuestId;
        public event Action<int> Selected;
        public int InstanceCount => _instances.Count;
        public int ItemCount => _items.Length;

        private void BindControls()
        {
            if (m_Scroll != null) return;
            var container = GetComponent<UIContainer>();
            m_Scroll = container.Get<LoopVerticalScrollRect>("m_Scroll");
            m_Template = container.Get<QuestRowView>("m_RowTemplate");
            m_PoolRoot = container.Get<Transform>("m_PoolRoot");
            m_Viewport = container.Get<RectTransform>("m_Viewport");
            m_Scroll.prefabSource = this;
            m_Scroll.dataSource = this;
        }

        /// <summary>更新数据；常规刷新保留首个可见项和像素偏移。</summary>
        public void SetItems(QuestListItem[] items, int selectedQuestId, bool resetPosition)
        {
            BindControls();
            int first = 0;
            float offset = 0;
            if (!resetPosition && _items.Length > 0) first = m_Scroll.GetFirstItem(out offset);
            int anchorId = first >= 0 && first < _items.Length ? _items[first].QuestId : 0;
            _items = items ?? Array.Empty<QuestListItem>();
            _selectedQuestId = selectedQuestId;
            m_Scroll.StopAllCoroutines();
            m_Scroll.StopMovement();
            if (_items.Length == 0)
            {
                m_Scroll.ClearCells();
                return;
            }
            int anchorIndex = Array.FindIndex(_items, item => item.QuestId == anchorId);
            first = resetPosition ? 0 : anchorIndex >= 0 ? anchorIndex : Mathf.Clamp(first, 0, _items.Length - 1);
            m_Scroll.totalCount = _items.Length;
            m_Scroll.RefillCells(first, resetPosition ? 0 : offset);
        }

        /// <summary>仅刷新选中样式；键盘导航时保证选中项可见并交接焦点。</summary>
        public void SetSelected(int questId, bool ensureVisible)
        {
            BindControls();
            _selectedQuestId = questId;
            int index = Array.FindIndex(_items, item => item.QuestId == questId);
            if (index < 0) return;
            QuestRowView target = null;
            foreach (var row in _instances.Values)
            {
                if (!row.gameObject.activeSelf) continue;
                row.SetSelected(row.QuestId == questId);
                if (row.QuestId == questId) target = row;
            }
            if (!ensureVisible) return;
            bool fullyVisible = false;
            if (target != null)
            {
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(m_Viewport, target.transform);
                fullyVisible = bounds.min.y >= m_Viewport.rect.yMin && bounds.max.y <= m_Viewport.rect.yMax;
            }
            if (!fullyVisible)
            {
                m_Scroll.StopAllCoroutines();
                m_Scroll.StopMovement();
                m_Scroll.RefillCells(index);
                Canvas.ForceUpdateCanvases();
                foreach (var row in _instances.Values)
                    if (row.gameObject.activeSelf && row.QuestId == questId) target = row;
            }
            if (target != null) EventSystem.current?.SetSelectedGameObject(target.gameObject);
        }

        GameObject LoopScrollPrefabSource.GetObject(int index)
        {
            QuestRowView row;
            if (_pool.Count > 0) row = _pool.Pop();
            else
            {
                row = Instantiate(m_Template, m_PoolRoot);
                _instances.Add(row.transform, row);
            }
            return row.gameObject;
        }

        void LoopScrollPrefabSource.ReturnObject(Transform trans)
        {
            var row = _instances[trans];
            row.ResetForReuse();
            row.gameObject.SetActive(false);
            trans.SetParent(m_PoolRoot, false);
            _pool.Push(row);
        }

        void LoopScrollDataSource.ProvideData(Transform trans, int index)
        {
            _instances[trans].Show(_items[index], _items[index].QuestId == _selectedQuestId, OnSelected);
        }

        private void OnSelected(int questId) => Selected?.Invoke(questId);

        /// <summary>关闭时回收活动行，保留池供同一面板再次打开。</summary>
        public void Clear()
        {
            BindControls();
            m_Scroll.StopAllCoroutines();
            m_Scroll.StopMovement();
            m_Scroll.ClearCells();
            _items = Array.Empty<QuestListItem>();
            _selectedQuestId = 0;
            Selected = null;
        }

        private void OnDestroy()
        {
            Selected = null;
            _instances.Clear();
            _pool.Clear();
        }
    }
}
