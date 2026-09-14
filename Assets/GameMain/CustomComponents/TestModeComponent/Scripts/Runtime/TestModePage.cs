using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// Prefab 结构：
    /// Page (RectTransform, 铺满父节点)
    ///   ├── Title        (TextMeshProUGUI)
    ///   └── ScrollView   (ScrollRect)
    ///         └── Viewport (Mask + Image)
    ///               └── Content (VerticalLayoutGroup + ContentSizeFitter)
    ///                     ← Item 动态 Instantiate 到这里
    ///
    /// ScrollView 设置：
    ///   Horizontal = false, Vertical = true
    ///   Movement Type = Clamped
    ///   Viewport → Mask (Show Mask Graphic = false)
    ///   Content → Vertical Layout Group
    ///              spacing=8, padding=(12,12,8,8)
    ///              Control Child Size Width=true, Height=true
    ///              Child Force Expand Width=true, Height=false
    ///           → Content Size Fitter  Vertical=PreferredSize
    /// </summary>
    public class TestModePage : MonoBehaviour
    {
        [SerializeField] private TMP_Text      m_TitleText;
        [SerializeField] private RectTransform m_Content;   // ScrollView/Viewport/Content

        private TestModePrefabCatalog m_PrefabCatalog;

        public void SetPrefabCatalog(TestModePrefabCatalog catalog)
        {
            m_PrefabCatalog = catalog;
        }

        public void Build(string title)
        {
            if (m_TitleText != null) m_TitleText.text = title;
        }

        /// <summary>向 Content 中添加一个 Item，自动从 PrefabCatalog 取预制体。</summary>
        public T AddItem<T>(string itemName) where T : TestModeItemBase
        {
            T prefab = m_PrefabCatalog?.GetItemPrefab<T>();

            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab.gameObject, m_Content, false);
            }
            else
            {
                // Catalog 里没配 Prefab 时给出明确警告，不静默失败
                if (typeof(T) != typeof(TestModeInfoItem))
                {
                    Debug.LogWarning($"[TestModePage] 未找到 {typeof(T).Name} 的 Prefab，请在 TestModePrefabCatalog 中配置。");
                }
                go = new GameObject(itemName, typeof(RectTransform));
                go.transform.SetParent(m_Content, false);
                return go.AddComponent<T>();
            }

            go.name = itemName;
            var item = go.GetComponent<T>();
            if (item == null) item = go.AddComponent<T>();
            return item;
        }

        /// <summary>清除 Content 下所有子节点（切换关卡 / 刷新时调用）。</summary>
        public void Clear()
        {
            if (m_Content == null) return;
            for (int i = m_Content.childCount - 1; i >= 0; i--)
                Destroy(m_Content.GetChild(i).gameObject);
        }
    }
}
