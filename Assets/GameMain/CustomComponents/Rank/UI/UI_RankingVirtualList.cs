using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public sealed class UI_RankingVirtualList
    {
        private readonly List<UI_RankingItem> m_Items = new();
        private readonly Dictionary<int, UI_RankingItem> m_VisibleItems = new();

        private ScrollRect m_ScrollRect;
        private RectTransform m_Content;
        private UI_RankingItem m_Prefab;
        private Action<int, UI_RankingItem> m_BindItem;
        private int m_Count;
        private int m_FirstVisibleIndex = -1;
        private int m_LastVisibleIndex = -1;
        private float m_ItemHeight = 1f;
        private float m_Spacing;
        private RectOffset m_Padding = new();
        private bool m_Initialized;
        private readonly HashSet<UI_RankingItem> m_LockedItems = new();

        public int Count => m_Count;
        public float ItemStride => m_ItemHeight + m_Spacing;

        public void Initialize(ScrollRect scrollRect, UI_RankingItem prefab, RectTransform content, Action<int, UI_RankingItem> bindItem)
        {
            if (m_Initialized)
                return;

            m_ScrollRect = scrollRect;
            m_Content = content;
            m_Prefab = prefab;
            m_BindItem = bindItem;

            if (m_ScrollRect != null)
                m_ScrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            CacheLayoutSettings();
            DisableDrivenLayout();
            m_Initialized = true;
        }

        public void SetDataCount(int count, bool resetPosition)
        {
            if (!m_Initialized || m_Content == null)
                return;

            m_Count = Mathf.Max(0, count);
            m_FirstVisibleIndex = -1;
            m_LastVisibleIndex = -1;
            ResizeContent();

            if (resetPosition)
                SetContentY(0f);

            RefreshVisible(true);
        }

        public void Refresh()
        {
            RefreshVisible(true);
        }

        public void LockItem(UI_RankingItem item)
        {
            if (item != null)
                m_LockedItems.Add(item);
        }

        public void UnlockItem(UI_RankingItem item)
        {
            if (item != null)
                m_LockedItems.Remove(item);
        }

        public void UnlockAllItems()
        {
            m_LockedItems.Clear();
        }

        public UI_RankingItem FindVisibleItem(Predicate<UI_RankingItem> predicate)
        {
            if (predicate == null)
                return null;

            for (int i = 0; i < m_Items.Count; i++)
            {
                UI_RankingItem item = m_Items[i];
                if (item != null && item.gameObject.activeSelf && predicate(item))
                    return item;
            }

            return null;
        }

        public UI_RankingItem GetVisibleItem(int index)
        {
            m_VisibleItems.TryGetValue(index, out UI_RankingItem item);
            return item;
        }

        public float GetContentYForIndex(int index)
        {
            if (m_ScrollRect == null)
                return 0f;

            RectTransform viewport = m_ScrollRect.viewport != null
                ? m_ScrollRect.viewport
                : m_ScrollRect.GetComponent<RectTransform>();
            float viewHeight = viewport != null ? viewport.rect.height : 0f;
            float maxY = Mathf.Max(0f, m_Content.rect.height - viewHeight);
            float targetY = m_Padding.top + Mathf.Max(0, index) * ItemStride - viewHeight * 0.5f + m_ItemHeight * 0.5f;
            return Mathf.Clamp(targetY, 0f, maxY);
        }

        public void ScrollToIndexImmediate(int index)
        {
            if (m_Content == null)
                return;

            SetContentY(GetContentYForIndex(index));
            RefreshVisible(true);
        }

        public Tween ScrollToIndexTween(int index, float duration)
        {
            if (m_Content == null)
                return null;

            float targetY = GetContentYForIndex(index);
            return m_Content.DOAnchorPosY(targetY, duration).SetEase(Ease.Linear).OnUpdate(() => RefreshVisible(false));
        }

        public Vector2 GetItemAnchoredPosition(int index)
        {
            return new Vector2(0f, -m_Padding.top - Mathf.Max(0, index) * ItemStride);
        }

        private void OnScrollValueChanged(Vector2 value)
        {
            RefreshVisible(false);
        }

        private void CacheLayoutSettings()
        {
            if (m_Content == null)
                return;

            if (m_Content.TryGetComponent(out VerticalLayoutGroup verticalLayout))
            {
                m_Spacing = verticalLayout.spacing;
                m_Padding = verticalLayout.padding ?? new RectOffset();
            }

            RectTransform prefabRT = m_Prefab != null ? m_Prefab.transform as RectTransform : null;
            if (prefabRT != null)
                m_ItemHeight = Mathf.Max(1f, prefabRT.rect.height, prefabRT.sizeDelta.y);

            if (m_Prefab != null && m_Prefab.TryGetComponent(out LayoutElement layoutElement))
            {
                if (layoutElement.preferredHeight > 0f)
                    m_ItemHeight = layoutElement.preferredHeight;
                else if (layoutElement.minHeight > 0f)
                    m_ItemHeight = layoutElement.minHeight;
            }
        }

        private void DisableDrivenLayout()
        {
            if (m_Content == null)
                return;

            if (m_Content.TryGetComponent(out LayoutGroup layoutGroup))
                layoutGroup.enabled = false;

            if (m_Content.TryGetComponent(out ContentSizeFitter fitter))
                fitter.enabled = false;
        }

        private void ResizeContent()
        {
            float height = m_Padding.top + m_Padding.bottom;
            if (m_Count > 0)
                height += m_Count * m_ItemHeight + (m_Count - 1) * m_Spacing;

            Vector2 sizeDelta = m_Content.sizeDelta;
            sizeDelta.y = height;
            m_Content.sizeDelta = sizeDelta;
        }

        private void RefreshVisible(bool forceRebind)
        {
            if (m_ScrollRect == null || m_Content == null || m_BindItem == null)
                return;

            RectTransform viewport = m_ScrollRect.viewport != null
                ? m_ScrollRect.viewport
                : m_ScrollRect.GetComponent<RectTransform>();
            if (viewport == null)
                return;

            if (m_Count <= 0)
            {
                SetAllInactive();
                return;
            }

            float viewHeight = viewport.rect.height;
            int poolCount = Mathf.Clamp(Mathf.CeilToInt(viewHeight / ItemStride) + 4, 1, m_Count);
            int requiredPoolCount = poolCount + m_LockedItems.Count;
            EnsurePoolCount(requiredPoolCount);

            float y = Mathf.Max(0f, m_Content.anchoredPosition.y - m_Padding.top);
            int first = Mathf.Clamp(Mathf.FloorToInt(y / ItemStride) - 2, 0, m_Count - 1);
            int last = Mathf.Clamp(first + poolCount - 1, 0, m_Count - 1);
            first = Mathf.Max(0, last - poolCount + 1);

            if (!forceRebind && first == m_FirstVisibleIndex && last == m_LastVisibleIndex)
                return;

            m_FirstVisibleIndex = first;
            m_LastVisibleIndex = last;
            m_VisibleItems.Clear();

            int itemIndex = 0;
            for (int dataIndex = first; dataIndex <= last; dataIndex++)
            {
                UI_RankingItem item = GetReusableItem(ref itemIndex);
                if (item == null)
                    break;

                RectTransform rt = item.transform as RectTransform;
                PrepareItemRect(rt);
                rt.anchoredPosition = GetItemAnchoredPosition(dataIndex);
                item.transform.DOKill();
                item.gameObject.SetActive(true);
                m_BindItem(dataIndex, item);
                m_VisibleItems[dataIndex] = item;
            }

            for (; itemIndex < m_Items.Count; itemIndex++)
            {
                if (m_LockedItems.Contains(m_Items[itemIndex]))
                    continue;

                m_Items[itemIndex].gameObject.SetActive(false);
            }
        }

        private UI_RankingItem GetReusableItem(ref int startIndex)
        {
            while (startIndex < m_Items.Count)
            {
                UI_RankingItem item = m_Items[startIndex++];
                if (!m_LockedItems.Contains(item))
                    return item;
            }

            return null;
        }

        private void EnsurePoolCount(int count)
        {
            if (m_Prefab == null || m_Content == null)
                return;

            while (m_Items.Count < count)
            {
                UI_RankingItem item = UnityEngine.Object.Instantiate(m_Prefab, m_Content);
                PrepareItemRect(item.transform as RectTransform);
                item.gameObject.SetActive(false);
                m_Items.Add(item);
            }
        }

        private void PrepareItemRect(RectTransform rt)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, m_ItemHeight);
        }

        private void SetContentY(float y)
        {
            if (m_ScrollRect != null)
            {
                m_ScrollRect.StopMovement();
                m_ScrollRect.velocity = Vector2.zero;
            }

            m_Content.anchoredPosition = new Vector2(m_Content.anchoredPosition.x, Mathf.Max(0f, y));
        }

        private void SetAllInactive()
        {
            m_VisibleItems.Clear();
            for (int i = 0; i < m_Items.Count; i++)
                if (m_Items[i] != null)
                {
                    if (m_LockedItems.Contains(m_Items[i]))
                        continue;

                    m_Items[i].gameObject.SetActive(false);
                }
        }
    }
}
