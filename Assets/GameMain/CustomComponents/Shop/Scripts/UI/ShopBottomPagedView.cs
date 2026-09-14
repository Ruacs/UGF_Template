using System;
using System.Collections;
using System.Collections.Generic;
using UI.Pagination;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class ShopBottomPagedView : MonoBehaviour
    {
        private const string HiddenProductId = "10001";
        private const string GeneratedPageNamePrefix = "ShopPage_";

        [SerializeField] private PagedRect m_PagedRect;
        [SerializeField] private Page m_PageTemplate;
        [SerializeField] private ShopItemView m_ItemPrefab;
        [SerializeField] private ShopCatalogSO m_CatalogOverride;
        [SerializeField] private bool m_RefreshOnEnable = true;
        [SerializeField] private bool m_HideWhenEmpty = true;
        [SerializeField] private Vector2 m_ItemReferenceSize = new(1030f, 455f);
        [SerializeField] private Vector2 m_ItemMaxSize = new(920f, 407f);
        [SerializeField] private Vector2 m_ItemCenterOffset = new(0f, 0f);
        [SerializeField] private float m_SpaceBetweenPages = 44f;
        [SerializeField] private float m_CurrentPageScale = 1f;
        [SerializeField] private float m_PreviewPageScale = 0.8f;
        [SerializeField] private bool m_LoopSeamlessly = false;
        [SerializeField] private bool m_AutomaticallyMoveToNextPage = true;
        [SerializeField] private float m_DelayBetweenPages = 5f;
        [SerializeField] private float m_PauseAutoScrollAfterBuySeconds = 6f;
        [SerializeField] private bool m_LoopEndlessly = true;
        [SerializeField] private string m_AnalyticsPage = ShopAnalytics.PageShop;

        private readonly List<Page> m_Pages = new();
        private readonly List<ShopItemView> m_ItemViews = new();
        private readonly List<int> m_CurrentItemIds = new();
        private readonly HashSet<int> m_TrackedBuyItemIds = new();
        private object m_RewardFlyTargetOverride;
        private Action<RewardData> m_OnRewardFlyArrived;
        private Action m_OnAllRewardsFlyArrived;
        private Coroutine m_ResumeAutoScrollCoroutine;
        private bool m_AutoScrollPausedForPurchase;

        private void Awake()
        {
            BindReferences();
            ApplyPagedRectSettings();
        }

        private void OnEnable()
        {
            ApplyPagedRectSettings();
            BindShopEvents();

            if (m_RefreshOnEnable)
                RefreshPages();
        }

        private void OnValidate()
        {
            ApplyEditorSettings();
        }

        public void ApplyEditorSettings()
        {
            BindReferences();
            ApplyPagedRectSettings();
            ApplyItemLayout();
        }

        private void OnDisable()
        {
            UnbindShopEvents();
            StopAutoScrollResumeCoroutine();
            m_AutoScrollPausedForPurchase = false;
            m_TrackedBuyItemIds.Clear();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyPagedRectSettings();
        }

        public void RefreshPages()
        {
            BindReferences();
            ApplyPagedRectSettings();

            if (GameEntry.Shop == null)
            {
                Log.Warning("[ShopBottomPagedView] GameEntry.Shop is missing.");
                SetVisible(false);
                return;
            }

            if (!GameEntry.Shop.IsAvailable)
            {
                SetVisible(false);
                return;
            }

            if (m_CatalogOverride != null)
                GameEntry.Shop.Catalog = m_CatalogOverride;

            List<ShopItemConfigSO> items = GetBottomVisibleItems(GameEntry.Shop.GetVisibleItems());
            bool hasItems = items != null && items.Count > 0;
            SetVisible(hasItems || !m_HideWhenEmpty);

            if (!hasItems)
            {
                ClearPages();
                return;
            }

            if (m_PagedRect == null || m_PageTemplate == null || m_ItemPrefab == null)
            {
                Log.Warning("[ShopBottomPagedView] Missing PagedRect, PageTemplate, or ItemPrefab reference.");
                return;
            }

            m_PageTemplate.gameObject.SetActive(false);

            if (CanReuseCurrentPages(items))
            {
                ApplyItemLayout();
                RefreshItemViews();
                m_PagedRect.UpdatePagination();
                return;
            }

            BeginPageRebuild();
            ClearPages();

            for (int i = 0; i < items.Count; i++)
            {
                Page page = CreatePage(i);
                ShopItemView view = CreateItemView(page, items[i], i);

                m_Pages.Add(page);
                m_ItemViews.Add(view);
                m_CurrentItemIds.Add(items[i].id);
            }

            EndPageRebuild();
        }

        public void RefreshVisibleState()
        {
            if (GameEntry.Shop == null || !GameEntry.Shop.IsAvailable)
            {
                SetVisible(false);
                return;
            }

            if (m_ItemViews.Count > 0 || HasGeneratedPages())
            {
                SetVisible(true);
                RefreshItemViews();
                return;
            }

            RefreshPages();
        }

        private static List<ShopItemConfigSO> GetBottomVisibleItems(IReadOnlyList<ShopItemConfigSO> sourceItems)
        {
            List<ShopItemConfigSO> items = new();
            if (sourceItems == null)
                return items;

            for (int i = 0; i < sourceItems.Count; i++)
            {
                ShopItemConfigSO item = sourceItems[i];
                if (item == null || item.GetProductId() == HiddenProductId)
                    continue;

                items.Add(item);
            }

            return items;
        }

        public void RefreshItemViews()
        {
            for (int i = 0; i < m_ItemViews.Count; i++)
                m_ItemViews[i]?.RefreshView();
        }

        public void RefreshItemStates()
        {
            for (int i = 0; i < m_ItemViews.Count; i++)
                m_ItemViews[i]?.RefreshState();
        }

        private bool CanReuseCurrentPages(IReadOnlyList<ShopItemConfigSO> items)
        {
            if (items == null || m_PagedRect == null)
                return false;

            if (m_ItemViews.Count != items.Count || m_Pages.Count != items.Count || m_CurrentItemIds.Count != items.Count)
                return false;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null || m_ItemViews[i] == null || m_Pages[i] == null)
                    return false;

                if (m_CurrentItemIds[i] != items[i].id)
                    return false;
            }

            return true;
        }

        public void SetRewardStartScreenPosition(object screenPosition)
        {
            SetRewardFlyTarget(screenPosition);
        }

        public void SetAnalyticsPage(string page)
        {
            m_AnalyticsPage = string.IsNullOrWhiteSpace(page) ? ShopAnalytics.PageShop : page;
        }

        public void SetRewardFlyTarget(object target, Action<RewardData> onRewardArrived = null, Action onAllArrived = null)
        {
            m_RewardFlyTargetOverride = target;
            m_OnRewardFlyArrived = onRewardArrived;
            m_OnAllRewardsFlyArrived = onAllArrived;
        }

        public void ClearRewardStartScreenPosition()
        {
            m_RewardFlyTargetOverride = null;
            m_OnRewardFlyArrived = null;
            m_OnAllRewardsFlyArrived = null;
        }

        private Page CreatePage(int index)
        {
            Page page = m_PagedRect.AddPageUsingTemplate();
            page.name = $"{GeneratedPageNamePrefix}{index + 1}";
            page.PageTitle = (index + 1).ToString();
            page.gameObject.SetActive(true);
            return page;
        }

        private ShopItemView CreateItemView(Page page, ShopItemConfigSO item, int index)
        {
            ShopItemView view = Instantiate(m_ItemPrefab, page.transform);
            view.name = $"{m_ItemPrefab.name}_{index + 1}";
            view.transform.localScale = Vector3.one;
            FitItemToPage(view.transform as RectTransform);
            ConfigureDragPassthrough(view);
            view.SetData(item, OnClickBuy);
            view.InitLocalization();
            view.gameObject.SetActive(true);
            return view;
        }

        private void ConfigureDragPassthrough(ShopItemView view)
        {
            if (view == null || m_PagedRect == null || m_PagedRect.ScrollRect == null)
                return;

            ShopPagedViewDragPassthrough passthrough = view.GetComponent<ShopPagedViewDragPassthrough>();
            if (passthrough == null)
                passthrough = view.gameObject.AddComponent<ShopPagedViewDragPassthrough>();

            passthrough.Initialize(m_PagedRect.ScrollRect);
        }

        private void OnClickBuy(ShopItemConfigSO config)
        {
            PauseAutoScrollForPurchase();

            if (GameEntry.Shop == null)
            {
                ShopAnalytics.ReportBuyFail(m_AnalyticsPage, config, "ShopMissing");
                ShopBuyResultToast.Show(ShopBuyResult.PaymentUnavailable);
                ScheduleAutoScrollResume();
                return;
            }
            

            ShopAnalytics.ReportBuyRequest(m_AnalyticsPage, config);
            TrackBuyItem(config);
            ShopBuyResult result = GameEntry.Shop.TryBuy(config);
            if (result == ShopBuyResult.Pending)
                ShopAnalytics.ReportBuyPending(m_AnalyticsPage, config, result);

            if (result == ShopBuyResult.Success || result == ShopBuyResult.Pending)
                RefreshItemStates();
        }

        private void BindShopEvents()
        {
            if (GameEntry.Shop == null)
                return;

            GameEntry.Shop.OnBuyFinished -= OnShopBuyFinished;
            GameEntry.Shop.OnBuyFinished += OnShopBuyFinished;
            GameEntry.Shop.OnProductInfoUpdated -= RefreshItemViews;
            GameEntry.Shop.OnProductInfoUpdated += RefreshItemViews;
            GameEntry.Shop.OnEnableShopChanged -= OnEnableShopChanged;
            GameEntry.Shop.OnEnableShopChanged += OnEnableShopChanged;
        }

        private void UnbindShopEvents()
        {
            if (GameEntry.Shop == null)
                return;

            GameEntry.Shop.OnBuyFinished -= OnShopBuyFinished;
            GameEntry.Shop.OnProductInfoUpdated -= RefreshItemViews;
            GameEntry.Shop.OnEnableShopChanged -= OnEnableShopChanged;
        }

        private void OnShopBuyFinished(ShopItemConfigSO item, ShopBuyResult result, string message)
        {
            bool shouldReportBuyEvent = IsTrackedBuyItem(item);
            if (result == ShopBuyResult.Success)
            {
                if (shouldReportBuyEvent)
                {
                    ShopAnalytics.ReportBuySuccess(m_AnalyticsPage, item, result, message);
                    ShopBuyResultToast.Show(result, message);
                    ConsumeTrackedBuyItem(item);
                }

                OpenPropRewardClaimPanel(item);
                RefreshPages();
                if (shouldReportBuyEvent)
                    ScheduleAutoScrollResume();
                return;
            }

            if (shouldReportBuyEvent && result != ShopBuyResult.Pending)
            {
                ShopAnalytics.ReportBuyFail(m_AnalyticsPage, item, result, message);
                ShopBuyResultToast.Show(result, message);
                ConsumeTrackedBuyItem(item);
                ScheduleAutoScrollResume();
            }

            RefreshItemStates();
        }

        private void TrackBuyItem(ShopItemConfigSO item)
        {
            if (item != null)
                m_TrackedBuyItemIds.Add(item.id);
        }

        private bool ConsumeTrackedBuyItem(ShopItemConfigSO item)
        {
            return item != null && m_TrackedBuyItemIds.Remove(item.id);
        }

        private bool IsTrackedBuyItem(ShopItemConfigSO item)
        {
            return item != null && m_TrackedBuyItemIds.Contains(item.id);
        }

        private void OnEnableShopChanged(bool enableShop)
        {
            RefreshPages();
        }

        private void ClearPages()
        {
            if (m_PagedRect != null)
            {
                m_PagedRect.UpdatePages(true, true, false);

                List<Page> pagesToRemove = new();
                for (int i = 0; i < m_PagedRect.Pages.Count; i++)
                {
                    Page page = m_PagedRect.Pages[i];
                    if (page != null && IsGeneratedShopPage(page))
                        pagesToRemove.Add(page);
                }

                for (int i = m_Pages.Count - 1; i >= 0; i--)
                {
                    Page page = m_Pages[i];
                    if (page != null && !pagesToRemove.Contains(page))
                        pagesToRemove.Add(page);
                }

                for (int i = pagesToRemove.Count - 1; i >= 0; i--)
                {
                    Page page = pagesToRemove[i];
                    if (page == null)
                        continue;

                    if (m_PagedRect.Pages.Contains(page))
                        m_PagedRect.RemovePage(page, true);
                    else
                        DestroyPageObject(page.gameObject);
                }

                ClearGeneratedPageChildren();
            }

            m_Pages.Clear();
            m_ItemViews.Clear();
            m_CurrentItemIds.Clear();
        }

        private void BeginPageRebuild()
        {
            if (m_PagedRect == null)
                return;

            m_PagedRect.LoopSeamlessly = false;
            m_PagedRect.AutomaticallyMoveToNextPage = false;
            ResetPagedRectScrollState();
        }

        private void EndPageRebuild()
        {
            if (m_PagedRect == null)
                return;

            m_PagedRect.UpdatePages(true, true, true);
            ResetPagedRectScrollState();

            if (m_PagedRect.NumberOfPages > 0)
                m_PagedRect.SetCurrentPage(GetRebuildStartPage(), true);

            ApplyEffectiveLoopSeamlessly();
            ApplyAutoScrollState();
            m_PagedRect.LoopEndlessly = m_LoopEndlessly;
            m_PagedRect.DelayBetweenPages = Mathf.Max(0f, m_DelayBetweenPages);
            m_PagedRect.UpdatePagination();
            m_PagedRect.CenterScrollRectOnCurrentPage(true);
        }

        private void ResetPagedRectScrollState()
        {
            if (m_PagedRect == null || m_PagedRect.ScrollRect == null)
                return;

            m_PagedRect.ScrollRect.StopMovement();
            m_PagedRect.ScrollRect.velocity = Vector2.zero;
            m_PagedRect.ScrollRect.horizontalNormalizedPosition = 0f;
            m_PagedRect.ScrollRect.verticalNormalizedPosition = 1f;

            if (m_PagedRect.ScrollRect.content != null)
                m_PagedRect.ScrollRect.content.anchoredPosition = Vector2.zero;
        }

        private void PauseAutoScrollForPurchase()
        {
            m_AutoScrollPausedForPurchase = true;
            StopAutoScrollResumeCoroutine();
            ApplyAutoScrollState(resetTimer: true);
        }

        private void ScheduleAutoScrollResume()
        {
            m_AutoScrollPausedForPurchase = true;
            StopAutoScrollResumeCoroutine();
            ApplyAutoScrollState(resetTimer: true);

            if (!isActiveAndEnabled || !m_AutomaticallyMoveToNextPage || m_PagedRect == null)
                return;

            float delay = Mathf.Max(0f, m_PauseAutoScrollAfterBuySeconds);
            if (delay <= 0f)
            {
                ResumeAutoScroll();
                return;
            }

            m_ResumeAutoScrollCoroutine = StartCoroutine(ResumeAutoScrollAfterDelay(delay));
        }

        private IEnumerator ResumeAutoScrollAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            m_ResumeAutoScrollCoroutine = null;
            ResumeAutoScroll();
        }

        private void ResumeAutoScroll()
        {
            m_AutoScrollPausedForPurchase = false;
            ApplyAutoScrollState(resetTimer: true);
        }

        private void StopAutoScrollResumeCoroutine()
        {
            if (m_ResumeAutoScrollCoroutine == null)
                return;

            StopCoroutine(m_ResumeAutoScrollCoroutine);
            m_ResumeAutoScrollCoroutine = null;
        }

        private void ApplyAutoScrollState(bool resetTimer = false)
        {
            if (m_PagedRect == null)
                return;

            bool shouldAutoScroll = m_AutomaticallyMoveToNextPage && !m_AutoScrollPausedForPurchase;
            if (resetTimer)
                m_PagedRect.SetAutomaticallyMoveToNextPage(shouldAutoScroll);
            else
                m_PagedRect.AutomaticallyMoveToNextPage = shouldAutoScroll;
        }

        private bool HasGeneratedPages()
        {
            if (m_PagedRect == null)
                return false;

            m_PagedRect.UpdatePages(true, true, false);
            for (int i = 0; i < m_PagedRect.Pages.Count; i++)
            {
                if (IsGeneratedShopPage(m_PagedRect.Pages[i]))
                    return true;
            }

            return false;
        }

        private bool IsGeneratedShopPage(Page page)
        {
            return page != null && page != m_PageTemplate && page.name.StartsWith(GeneratedPageNamePrefix);
        }

        private void ClearGeneratedPageChildren()
        {
            if (m_PagedRect == null || m_PagedRect.ScrollRect == null || m_PagedRect.ScrollRect.content == null)
                return;

            RectTransform content = m_PagedRect.ScrollRect.content;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                if (child == null || !child.name.StartsWith(GeneratedPageNamePrefix))
                    continue;

                DestroyPageObject(child.gameObject);
            }
        }

        private static void DestroyPageObject(GameObject pageObject)
        {
            if (pageObject == null)
                return;

            pageObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(pageObject);
            else
                DestroyImmediate(pageObject);
        }

        private void BindReferences()
        {
            if (m_PagedRect == null)
                m_PagedRect = GetComponentInChildren<PagedRect>(true);

            if (m_PageTemplate == null && m_PagedRect != null)
                m_PageTemplate = m_PagedRect.NewPageTemplate;
        }

        private void SetVisible(bool visible)
        {
            if (m_HideWhenEmpty)
                gameObject.SetActive(visible);
        }

        private void FitItemToPage(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = m_ItemCenterOffset;

            Vector2 referenceSize = m_ItemReferenceSize;
            if (referenceSize.x <= 0f || referenceSize.y <= 0f)
                referenceSize = rectTransform.sizeDelta;

            if (referenceSize.x <= 0f || referenceSize.y <= 0f)
                referenceSize = new Vector2(1030f, 455f);

            Vector2 maxSize = m_ItemMaxSize;
            if (maxSize.x <= 0f || maxSize.y <= 0f)
                maxSize = referenceSize;

            float scale = Mathf.Min(maxSize.x / referenceSize.x, maxSize.y / referenceSize.y);
            rectTransform.sizeDelta = referenceSize;
            rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private void ApplyPagedRectSettings()
        {
            if (m_PagedRect == null)
                return;

            m_PagedRect.SpaceBetweenPages = CalculatePagedRectSpacing();
            m_PagedRect.ClippedCurrentPageScale = m_CurrentPageScale;
            m_PagedRect.ClippedPagePreviewScale = m_PreviewPageScale;
            ApplyAutoScrollState();
            m_PagedRect.DelayBetweenPages = Mathf.Max(0f, m_DelayBetweenPages);
            m_PagedRect.LoopEndlessly = m_LoopEndlessly;
            ApplyEffectiveLoopSeamlessly();

            if (m_PagedRect.gameObject.activeInHierarchy)
            {
                m_PagedRect.UpdateDisplay();
                m_PagedRect.CenterScrollRectOnCurrentPage(true);
            }
        }

        private void ApplyItemLayout()
        {
            for (int i = 0; i < m_ItemViews.Count; i++)
                FitItemToPage(m_ItemViews[i] != null ? m_ItemViews[i].transform as RectTransform : null);
        }

        private void ApplyEffectiveLoopSeamlessly()
        {
            if (m_PagedRect == null)
                return;

            m_PagedRect.LoopSeamlessly = m_LoopSeamlessly;
        }

        private int GetRebuildStartPage()
        {
            if (m_PagedRect == null || m_PagedRect.NumberOfPages <= 0)
                return 1;

            if (m_LoopSeamlessly && m_PagedRect.NumberOfPages > 2)
                return 2;

            return 1;
        }

        private void OpenPropRewardClaimPanel(ShopItemConfigSO item)
        {
        }

        private static List<RewardData> GetPropRewardDatas(ShopItemConfigSO item)
        {
            return new List<RewardData>();
        }

        private static bool IsPropReward(ShopRewardTarget target)
        {
            return target == ShopRewardTarget.GameProp ||
                target == ShopRewardTarget.CommonProp;
        }

        private static int GetShopChestSkinIndex(ShopItemConfigSO item)
        {
            if (item == null)
                return 0;

            return Mathf.Clamp(item.sortOrder, 0, 3);
        }

        private object GetRewardFlyTargetData()
        {
            return GetRewardStartScreenPosition();
        }

        private object GetRewardStartScreenPosition()
        {
            if (m_RewardFlyTargetOverride != null)
                return m_RewardFlyTargetOverride;

            Camera uiCamera = GameEntry.UI != null ? GameEntry.UI.UICamera : null;
            return uiCamera != null
                ? uiCamera.WorldToScreenPoint(transform.position)
                : transform.position;
        }

        private float CalculatePagedRectSpacing()
        {
            if (m_PagedRect == null || m_PagedRect.sizingTransform == null)
                return m_SpaceBetweenPages;

            Rect pageRect = m_PagedRect.sizingTransform.rect;
            bool horizontal = m_PagedRect.ScrollRect == null || m_PagedRect.ScrollRect.horizontal;
            float pageSize = horizontal ? pageRect.width : pageRect.height;
            float itemSize = horizontal ? GetFittedItemSize().x : GetFittedItemSize().y;
            float itemOffset = horizontal ? m_ItemCenterOffset.x : m_ItemCenterOffset.y;

            float currentScale = Mathf.Clamp(m_CurrentPageScale, 0.01f, 2f);
            float previewScale = Mathf.Clamp(m_PreviewPageScale, 0.01f, 2f);

            return m_SpaceBetweenPages
                   - ((currentScale + previewScale) * 0.5f * (pageSize - itemSize))
                   - (itemOffset * (previewScale - currentScale));
        }

        private Vector2 GetFittedItemSize()
        {
            Vector2 referenceSize = m_ItemReferenceSize;
            if (referenceSize.x <= 0f || referenceSize.y <= 0f)
                referenceSize = new Vector2(1030f, 455f);

            Vector2 maxSize = m_ItemMaxSize;
            if (maxSize.x <= 0f || maxSize.y <= 0f)
                maxSize = referenceSize;

            float scale = Mathf.Min(maxSize.x / referenceSize.x, maxSize.y / referenceSize.y);
            return referenceSize * scale;
        }
    }
}



