using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Lokas
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: null, sourceClassName: "UI_RewardTipsBox")]
    public class RewardTooltipView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [SerializeField] private RewardItemView m_ItemPropPrefab;
        [SerializeField] private RectTransform m_ItemPropRoot;
        [SerializeField] private List<RewardItemView> m_ItemPropList;
        [Header("Width")]
        [SerializeField, Min(0f)] private float m_BaseWidth = 280f;
        [SerializeField, Min(1)] private int m_BaseItemCount = 2;

        private bool m_isShow;
        private bool m_HideOnOutsideInput;
        private int m_ShowFrame = -1;
        private RectTransform m_Anchor;
        private bool m_HasAnchor;
        private Tween m_ScaleTween;

        public bool IsVisible => m_isShow;

        private void Awake()
        {
            HideImmediate();
        }

        private void OnDisable()
        {
            HideImmediate();
        }

        private void Update()
        {
            if (!m_isShow || !m_HideOnOutsideInput || Time.frameCount <= m_ShowFrame)
                return;

            if (m_HasAnchor && (m_Anchor == null || !m_Anchor.gameObject.activeInHierarchy))
            {
                HideImmediate();
                return;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                    HideIfOutside(touch.position);
            }
            else if (Input.GetMouseButtonDown(0))
            {
                HideIfOutside(Input.mousePosition);
            }
        }

        public void Show(IReadOnlyList<RewardEntry> rewards)
        {
            Show(rewards, null);
        }

        public void Show(IReadOnlyList<RewardEntry> rewards, RectTransform anchor)
        {
            ShowPresentation(RewardPresentation.Build(rewards), anchor);
        }

        /// <summary>填充并持续显示嵌入页面内的奖励提示，不播放弹出音效，也不因点击外部区域关闭。</summary>
        public void ShowPersistent(IReadOnlyList<RewardEntry> rewards)
        {
            ShowPersistentPresentation(RewardPresentation.Build(rewards));
        }

        public void ShowPersistentPresentation(IReadOnlyList<RewardItemViewData> rewardDatas)
        {
            if (!isActiveAndEnabled || !RefreshItems(rewardDatas))
            {
                HideImmediate();
                return;
            }

            m_Anchor = null;
            m_HasAnchor = false;
            m_isShow = true;
            m_HideOnOutsideInput = false;
            m_ShowFrame = -1;
            m_ScaleTween?.Kill();
            m_ScaleTween = null;
            Vector3 scale = transform.localScale;
            scale.x = 1f;
            transform.localScale = scale;
        }

        public void ShowPresentation(IReadOnlyList<RewardItemViewData> rewardDatas, RectTransform anchor = null)
        {
            if (!isActiveAndEnabled || !RefreshItems(rewardDatas))
            {
                HideImmediate();
                return;
            }

            if (!m_isShow && Application.isPlaying && GameEntry.Sound != null)
                GameEntry.Sound.PlaySound(SoundId.SFX_chest_tips);

            m_Anchor = anchor;
            m_HasAnchor = anchor != null;
            Canvas tooltipCanvas = GetComponent<Canvas>();
            Canvas ownerCanvas = anchor != null ? anchor.GetComponentInParent<Canvas>() : null;
            if (tooltipCanvas != null && ownerCanvas != null && tooltipCanvas != ownerCanvas)
            {
                tooltipCanvas.overrideSorting = true;
                tooltipCanvas.sortingLayerID = ownerCanvas.sortingLayerID;
                tooltipCanvas.sortingOrder = ownerCanvas.sortingOrder + 1;
            }
            m_isShow = true;
            m_HideOnOutsideInput = true;
            m_ShowFrame = Time.frameCount;
            SetVisibleScale(true);
        }

        public void Hide()
        {
            if (!m_isShow)
                return;

            m_isShow = false;
            m_HideOnOutsideInput = false;
            m_Anchor = null;
            m_HasAnchor = false;
            SetVisibleScale(false);
        }

        public void HideImmediate()
        {
            m_isShow = false;
            m_HideOnOutsideInput = false;
            m_ShowFrame = -1;
            m_Anchor = null;
            m_HasAnchor = false;
            m_ScaleTween?.Kill();
            m_ScaleTween = null;
            transform.localScale = new Vector3(0f, 1f, 1f);
        }

        // 提示框内的点击由自身接收，避免继续触发父级宝箱按钮。
        public void OnPointerDown(PointerEventData eventData) { }
        public void OnPointerClick(PointerEventData eventData) { }

        private bool RefreshItems(IReadOnlyList<RewardItemViewData> rewardDatas)
        {
            if (m_ItemPropPrefab == null || m_ItemPropRoot == null)
                return false;

            if (m_ItemPropList == null)
                m_ItemPropList = new List<RewardItemView>();

            int visibleCount = 0;
            if (rewardDatas != null)
            {
                foreach (RewardItemViewData reward in rewardDatas)
                {
                    if (reward == null)
                        continue;

                    if (visibleCount == m_ItemPropList.Count)
                        m_ItemPropList.Add(Instantiate(m_ItemPropPrefab, m_ItemPropRoot));
                    else if (m_ItemPropList[visibleCount] == null)
                        m_ItemPropList[visibleCount] = Instantiate(m_ItemPropPrefab, m_ItemPropRoot);

                    RewardItemView item = m_ItemPropList[visibleCount++];
                    item.gameObject.SetActive(true);
                    item.Bind(reward);
                }
            }

            for (int i = visibleCount; i < m_ItemPropList.Count; i++)
            {
                if (m_ItemPropList[i] != null)
                    m_ItemPropList[i].gameObject.SetActive(false);
            }

            RefreshWidth(visibleCount);
            return visibleCount > 0;
        }

        private void RefreshWidth(int itemCount)
        {
            RectTransform tooltipRect = transform as RectTransform;
            if (tooltipRect == null)
                return;

            float itemWidth = GetItemWidth();
            float width = m_BaseWidth + Mathf.Max(0, itemCount - m_BaseItemCount) * itemWidth;
            tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_ItemPropRoot);
        }

        private float GetItemWidth()
        {
            RectTransform itemRect = m_ItemPropPrefab.transform as RectTransform;
            if (itemRect == null)
                return 0f;

            float preferredWidth = LayoutUtility.GetPreferredWidth(itemRect);
            return preferredWidth > 0f ? preferredWidth : itemRect.rect.width;
        }

        private void SetVisibleScale(bool visible)
        {
            m_ScaleTween?.Kill();
            m_ScaleTween = null;
            if (!Application.isPlaying)
            {
                transform.localScale = new Vector3(visible ? 1f : 0f, 1f, 1f);
                return;
            }

            m_ScaleTween = transform.DOScaleX(visible ? 1f : 0f, 0.3f)
                .SetEase(visible ? Ease.OutBack : Ease.InBack)
                .OnKill(() => m_ScaleTween = null);
        }

        private void HideIfOutside(Vector2 screenPosition)
        {
            if (ContainsScreenPoint((RectTransform)transform, screenPosition) ||
                (m_Anchor != null && ContainsScreenPoint(m_Anchor, screenPosition)))
                return;

            Hide();
        }

        private static bool ContainsScreenPoint(RectTransform rect, Vector2 screenPosition)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
            Camera camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, camera);
        }
    }
}
