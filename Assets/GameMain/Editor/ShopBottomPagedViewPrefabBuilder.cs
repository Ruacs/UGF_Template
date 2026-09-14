using Lokas;
using UI.Pagination;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Editor
{
    [InitializeOnLoad]
    public static class ShopBottomPagedViewPrefabBuilder
    {
        private const string PrefabPath = "Assets/GameMain/UI/UIPrefabs/ShopBottomPagedView.prefab";
        private const string PagePreviewsPrefabPath = "Assets/Plugins/Pagination/Resources/Prefabs/Page Previews - Horizontal.prefab";
        private const string ShopItemPrefabPath = "Assets/GameMain/CustomComponents/Shop/Prefabs/ShopItemView.prefab";

        static ShopBottomPagedViewPrefabBuilder()
        {
            EditorApplication.delayCall += BuildIfMissing;
        }

        private static void BuildIfMissing()
        {
            if (Application.isPlaying)
                return;

            if (IsPreviewPrefabReady())
                return;

            Build();
        }

        private static bool IsPreviewPrefabReady()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                return false;

            PagedRect pagedRect = prefab.GetComponentInChildren<PagedRect>(true);
            return pagedRect != null && pagedRect.ShowPagePreviews;
        }

        [MenuItem("Tools/Shop/Rebuild Shop Bottom Paged View Prefab")]
        public static void Build()
        {
            Debug.Log($"[ShopBottomPagedViewPrefabBuilder] Building from {PagePreviewsPrefabPath}");
            GameObject pagePreviewsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PagePreviewsPrefabPath);
            ShopItemView itemPrefab = AssetDatabase.LoadAssetAtPath<ShopItemView>(ShopItemPrefabPath);

            if (pagePreviewsPrefab == null)
            {
                Debug.LogError($"[ShopBottomPagedViewPrefabBuilder] Missing page previews prefab: {PagePreviewsPrefabPath}");
                return;
            }

            if (itemPrefab == null)
            {
                Debug.LogError($"[ShopBottomPagedViewPrefabBuilder] Missing shop item prefab: {ShopItemPrefabPath}");
                return;
            }

            GameObject root = new GameObject("ShopBottomPagedView", typeof(RectTransform));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1030f, 400f);

                ShopBottomPagedView bottomView = root.AddComponent<ShopBottomPagedView>();

                GameObject previewsInstance = (GameObject)PrefabUtility.InstantiatePrefab(pagePreviewsPrefab);
                previewsInstance.name = "PagedRectWithPreviews";
                previewsInstance.transform.SetParent(root.transform, false);
                StretchToParent(previewsInstance.GetComponent<RectTransform>());

                PagedRect pagedRect = previewsInstance.GetComponent<PagedRect>();
                if (pagedRect == null)
                    pagedRect = previewsInstance.GetComponentInChildren<PagedRect>(true);

                if (pagedRect == null)
                {
                    Debug.LogError("[ShopBottomPagedViewPrefabBuilder] PagedRect component was not found.");
                    return;
                }

                Page pageTemplate = CreatePageTemplate(pagedRect);
                ConfigurePagedRect(pagedRect, pageTemplate);
                ConfigureBottomView(bottomView, pagedRect, pageTemplate, itemPrefab);

                AssetDatabase.DeleteAsset(PrefabPath);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[ShopBottomPagedViewPrefabBuilder] Created {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static Page CreatePageTemplate(PagedRect pagedRect)
        {
            Transform pageRoot = GetPageRoot(pagedRect);
            ClearExistingPages(pageRoot);

            GameObject templateObject = new GameObject("PageTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Page), typeof(LayoutElement));
            templateObject.transform.SetParent(pageRoot, false);
            templateObject.SetActive(false);

            RectTransform rectTransform = templateObject.GetComponent<RectTransform>();
            StretchToParent(rectTransform);

            LayoutElement layout = templateObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 1030f;
            layout.preferredHeight = 300f;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 1f;

            return templateObject.GetComponent<Page>();
        }

        private static Transform GetPageRoot(PagedRect pagedRect)
        {
            if (pagedRect.Viewport != null)
                return pagedRect.Viewport.transform;

            if (pagedRect.ScrollRect != null && pagedRect.ScrollRect.content != null)
                return pagedRect.ScrollRect.content;

            return pagedRect.transform;
        }

        private static void ClearExistingPages(Transform pageRoot)
        {
            for (int i = pageRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = pageRoot.GetChild(i);
                if (child.GetComponent<Page>() != null)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void ConfigurePagedRect(PagedRect pagedRect, Page pageTemplate)
        {
            pagedRect.DefaultPage = 1;
            pagedRect.NewPageTemplate = pageTemplate;
            pagedRect.Pages.Clear();
            pagedRect.ShowPagination = true;
            pagedRect.ShowPageButtons = true;
            pagedRect.ShowFirstAndLastButtons = false;
            pagedRect.ShowPreviousAndNextButtons = true;
            pagedRect.ShowNumbersOnButtons = false;
            pagedRect.MonitorPageCollectionForChanges = false;
            pagedRect.SpaceBetweenPages = 44f;
            pagedRect.LoopSeamlessly = false;
            pagedRect.AutomaticallyMoveToNextPage = true;
            pagedRect.DelayBetweenPages = 5f;
            pagedRect.LoopEndlessly = true;
            pagedRect.ShowScrollBar = false;
            pagedRect.ShowPagePreviews = true;
            pagedRect.PagePreviewScale = 0.08f;
            pagedRect.UseClippedPagePreviews = true;
            pagedRect.ClippedCurrentPageScale = 1f;
            pagedRect.ClippedPagePreviewScale = 0.9f;
            pagedRect.LockOneToOneScaleRatio = true;
            pagedRect.EnablePagePreviewOverlays = true;
            pagedRect.PagePreviewOverlayScaleOverride = 1f;

            if (pagedRect.ScrollRect != null)
                pagedRect.ScrollRect.DisableDragging = false;
        }

        private static void ConfigureBottomView(ShopBottomPagedView bottomView, PagedRect pagedRect, Page pageTemplate, ShopItemView itemPrefab)
        {
            SerializedObject serializedObject = new SerializedObject(bottomView);
            serializedObject.FindProperty("m_PagedRect").objectReferenceValue = pagedRect;
            serializedObject.FindProperty("m_PageTemplate").objectReferenceValue = pageTemplate;
            serializedObject.FindProperty("m_ItemPrefab").objectReferenceValue = itemPrefab;
            serializedObject.FindProperty("m_RefreshOnEnable").boolValue = true;
            serializedObject.FindProperty("m_HideWhenEmpty").boolValue = true;
            serializedObject.FindProperty("m_ItemReferenceSize").vector2Value = new Vector2(1030f, 455f);
            serializedObject.FindProperty("m_ItemMaxSize").vector2Value = new Vector2(920f, 407f);
            serializedObject.FindProperty("m_ItemCenterOffset").vector2Value = new Vector2(40f, 0f);
            serializedObject.FindProperty("m_SpaceBetweenPages").floatValue = 44f;
            serializedObject.FindProperty("m_CurrentPageScale").floatValue = 1f;
            serializedObject.FindProperty("m_PreviewPageScale").floatValue = 0.9f;
            serializedObject.FindProperty("m_LoopSeamlessly").boolValue = false;
            serializedObject.FindProperty("m_AutomaticallyMoveToNextPage").boolValue = true;
            serializedObject.FindProperty("m_DelayBetweenPages").floatValue = 5f;
            serializedObject.FindProperty("m_LoopEndlessly").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
}

