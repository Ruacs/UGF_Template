using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// Prefab 结构（TestModeRoot）：
    ///
    /// TestModeRoot  (Canvas / 或挂在已有 Canvas 下的 Panel)
    ///   └── Panel  (Image 深色半透明背景, VerticalLayoutGroup)
    ///         ├── Header       (HorizontalLayoutGroup, h=50)
    ///         │     ├── Title  (TextMeshProUGUI "Debug Panel")
    ///         │     └── CloseBtn (Button)
    ///         ├── TabBar       (HorizontalLayoutGroup, h=58)  ← m_TabBar
    ///         └── PageContainer (RectTransform, flexibleHeight=1) ← m_PageContainer
    ///
    /// Tab 按钮 Prefab (m_TabButtonPrefab)：
    ///   TabButton (Button + Image, preferredWidth=140, preferredHeight=52)
    ///     └── Label (TextMeshProUGUI, 居中)
    ///
    /// Page Prefab → 见 TestModePage.cs 注释
    /// </summary>
    public class TestModeRoot : MonoBehaviour
    {
        [Header("References - 在 Prefab 里连好")]
        [SerializeField] private RectTransform m_PanelRect;
        [SerializeField] private RectTransform m_TabBar;
        [SerializeField] private RectTransform m_PageContainer;
        [SerializeField] private Button        m_CloseButton;

        [Header("Prefabs")]
        [SerializeField] private TestModePage  m_PagePrefab;
        [SerializeField] private Button        m_TabButtonPrefab;   // 一个只有 Button+Image+TMP_Text 的极简 Prefab
        [SerializeField] private TestModePrefabCatalog m_ItemPrefabs = new TestModePrefabCatalog();

        // ── 运行时状态 ────────────────────────────────────────────────
        private readonly List<Button> m_Tabs = new List<Button>();
        private readonly List<TestModeTabButton> m_TabViews = new List<TestModeTabButton>();
        private readonly List<TestModePage> m_Pages = new List<TestModePage>();
        private int m_SelectedIndex = 0;

        // 选中 / 未选中的 Tab 颜色
        [SerializeField] private  Color TabActive   = new Color(0.20f, 0.55f, 0.80f, 1f);
        [SerializeField] private  Color TabInactive = new Color(0.16f, 0.20f, 0.25f, 1f);
        [SerializeField] private  Color TabContentActive = Color.white;
        [SerializeField] private  Color TabContentInactive = new Color(0.60f, 0.60f, 0.60f, 1f);

        private Vector2 m_PanelStartAnchoredPosition;
        private bool m_HasPanelStartPosition;

        // ── 公共接口 ──────────────────────────────────────────────────

        public void SetPrefabs(TestModePage pagePrefab, TestModePrefabCatalog itemPrefabs)
        {
            if (pagePrefab  != null) m_PagePrefab  = pagePrefab;
            if (itemPrefabs != null) m_ItemPrefabs = itemPrefabs;
        }

        public void RecordPanelStartPosition()
        {
            RectTransform panelRect = GetPanelRect();
            if (panelRect == null)
            {
                return;
            }

            m_PanelStartAnchoredPosition = panelRect.anchoredPosition;
            m_HasPanelStartPosition = true;
        }

        public void ResetPanelPosition()
        {
            RectTransform panelRect = GetPanelRect();
            if (panelRect == null)
            {
                return;
            }

            if (!m_HasPanelStartPosition)
            {
                RecordPanelStartPosition();
            }

            panelRect.anchoredPosition = m_PanelStartAnchoredPosition;
        }

        public void Render(TestModeModuleRegistry registry)
        {
            ClearAll();

            var pageMap = new Dictionary<string, TestModePage>();

            foreach (var module in registry.Modules)
            {
                string pageKey = module.OwnerId + "/" + module.PageName;
                if (!pageMap.TryGetValue(pageKey, out var page))
                {
                    page = CreatePage(module.PageName);
                    pageMap[pageKey] = page;
                }
                module.Build(page, registry.GetContext(module));
            }

            int clamp = Mathf.Clamp(m_SelectedIndex, 0, Mathf.Max(0, m_Pages.Count - 1));
            SelectPage(clamp);
        }

        // ── 生命周期 ──────────────────────────────────────────────────

        private void Awake()
        {
            RecordPanelStartPosition();

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            ResetPanelPosition();
        }

        // ── 私有方法 ──────────────────────────────────────────────────

        private RectTransform GetPanelRect()
        {
            if (m_PanelRect != null)
            {
                return m_PanelRect;
            }

            Transform panel = transform.Find("Panel");
            if (panel != null)
            {
                m_PanelRect = panel.GetComponent<RectTransform>();
            }

            return m_PanelRect;
        }

        private void ClearAll()
        {
            // 销毁 Tab 按钮
            foreach (var tab in m_Tabs)
                if (tab != null) Destroy(tab.gameObject);
            m_Tabs.Clear();
            m_TabViews.Clear();

            // 销毁 Page
            foreach (var page in m_Pages)
                if (page != null) Destroy(page.gameObject);
            m_Pages.Clear();
        }

        private TestModePage CreatePage(string pageName)
        {
            int pageIndex = m_Pages.Count;

            // ── Tab 按钮 ──
            Button tab;
            if (m_TabButtonPrefab != null)
            {
                tab = Instantiate(m_TabButtonPrefab, m_TabBar, false);
            }
            else
            {
                // 没有配 Prefab 时用最简单的兜底，并警告
                Debug.LogWarning("[TestModeRoot] m_TabButtonPrefab 未配置，使用代码兜底。");
                var go = new GameObject(pageName, typeof(RectTransform));
                go.transform.SetParent(m_TabBar, false);
                go.AddComponent<Image>().color = TabInactive;
                tab = go.AddComponent<Button>();
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth  = 140f;
                le.preferredHeight = 52f;
                var lbl = new GameObject("Label", typeof(RectTransform)).AddComponent<TMP_Text>();
                lbl.transform.SetParent(go.transform, false);
                lbl.text      = pageName;
                lbl.fontSize  = 22;
                lbl.color     = Color.white;
                lbl.alignment = TMPro.TextAlignmentOptions.Center;
                var lrt = lbl.rectTransform;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            }

            tab.name = pageName;
            TestModeTabButton tabView = GetOrAddTabView(tab);
            tabView.SetText(pageName);
            tab.onClick.AddListener(() => SelectPage(pageIndex));
            m_Tabs.Add(tab);
            m_TabViews.Add(tabView);

            // ── Page ──
            TestModePage page;
            if (m_PagePrefab != null)
            {
                page = Instantiate(m_PagePrefab, m_PageContainer, false);
            }
            else
            {
                Debug.LogWarning("[TestModeRoot] m_PagePrefab 未配置，使用代码兜底。");
                var go = new GameObject(pageName, typeof(RectTransform));
                go.transform.SetParent(m_PageContainer, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                page = go.AddComponent<TestModePage>();
            }

            page.name = pageName;
            page.SetPrefabCatalog(m_ItemPrefabs);
            page.Build(pageName);
            m_Pages.Add(page);

            return page;
        }

        private void SelectPage(int index)
        {
            m_SelectedIndex = index;

            for (int i = 0; i < m_Pages.Count; i++)
            {
                bool active = i == index;
                m_Pages[i].gameObject.SetActive(active);

                // Tab 颜色
                m_TabViews[i].SetSelected(active, TabActive, TabInactive, TabContentActive, TabContentInactive);

                // 也更新 ColorBlock 里的 normalColor，防止 hover 恢复错误颜色
            }
        }

        private static TestModeTabButton GetOrAddTabView(Button tab)
        {
            TestModeTabButton tabView = tab.GetOrAddComponent<TestModeTabButton>();
      
            return tabView;
        }
    }
}
