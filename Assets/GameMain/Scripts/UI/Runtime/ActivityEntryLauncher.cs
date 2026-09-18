using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// 首页的通用活动入口。它只读取宿主摘要并支持多个同时可见的活动，不引用任一活动的类型、页面或资源。
    /// </summary>
    public sealed class ActivityEntryLauncher : MonoBehaviour
    {
        private sealed class EntryView
        {
            public ActivityEntrySnapshot Entry;
            public GameObject Root;
            public Image Background;
            public Button Button;
            public TMP_Text Label;
            public TMP_Text Reason;
            public TMP_Text Badge;
        }

        private readonly List<EntryView> m_Views = new List<EntryView>();
        private ActivityModuleHost m_Host;
        private RectTransform m_Root;

        public void Bind()
        {
            if (!ReferenceEquals(m_Host, GameEntry.Activities))
            {
                Unbind();
                m_Host = GameEntry.Activities;
                if (m_Host != null) m_Host.EntriesChanged += Refresh;
            }
            BuildIfNeeded();
            Refresh();
        }

        public void Unbind()
        {
            if (m_Host != null) m_Host.EntriesChanged -= Refresh;
            m_Host = null;
            foreach (EntryView view in m_Views) view.Entry = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void BuildIfNeeded()
        {
            if (m_Root != null) return;
            // 旧实现仅保留一个运行时按钮。升级后清理这个无序列化的动态节点，避免域重载时重复展示。
            Transform legacy = transform.Find("ActivityEntry");
            if (legacy != null) Destroy(legacy.gameObject);
            Transform staleRoot = transform.Find("ActivityEntries");
            if (staleRoot != null) Destroy(staleRoot.gameObject);

            var root = new GameObject("ActivityEntries", typeof(RectTransform), typeof(VerticalLayoutGroup));
            root.layer = 5;
            root.transform.SetParent(transform, false);
            m_Root = root.GetComponent<RectTransform>();
            m_Root.anchorMin = new Vector2(0.66f, 0.05f);
            m_Root.anchorMax = new Vector2(0.96f, 0.30f);
            m_Root.offsetMin = Vector2.zero;
            m_Root.offsetMax = Vector2.zero;
            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void Refresh()
        {
            if (m_Root == null) return;
            var entries = new List<ActivityEntrySnapshot>();
            if (m_Host != null)
            {
                foreach (ActivityEntrySnapshot entry in m_Host.GetEntries())
                    if (entry.Entry.Visible) entries.Add(entry);
            }

            EnsureViews(entries.Count);
            for (int index = 0; index < m_Views.Count; index++)
            {
                EntryView view = m_Views[index];
                bool visible = index < entries.Count;
                view.Root.SetActive(visible);
                if (visible) BindView(view, entries[index]);
                else view.Entry = null;
            }
            m_Root.gameObject.SetActive(entries.Count > 0);
        }

        private void EnsureViews(int count)
        {
            while (m_Views.Count < count) m_Views.Add(CreateView());
        }

        private EntryView CreateView()
        {
            var root = new GameObject("ActivityEntry", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            root.layer = 5;
            root.transform.SetParent(m_Root, false);
            root.GetComponent<LayoutElement>().preferredHeight = 70f;
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.12f, 0.46f, 0.70f, 0.96f);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;
            TMP_FontAsset font = GameEntry.TMPFont?.GetCurrentFontAsset()
                ?? UGuiForm.TMP_MainFont
                ?? GetComponentInChildren<TMP_Text>(true)?.font;
            var view = new EntryView
            {
                Root = root,
                Background = background,
                Button = button,
                Label = CreateText(root.transform, "Label", 22f, Color.white, TextAlignmentOptions.Left, font),
                Reason = CreateText(root.transform, "Reason", 15f, new Color(0.85f, 0.92f, 1f), TextAlignmentOptions.Left, font),
                Badge = CreateText(root.transform, "Badge", 18f, new Color(1f, 0.84f, 0.25f), TextAlignmentOptions.Right, font)
            };
            view.Label.rectTransform.anchorMin = new Vector2(0.08f, 0.43f);
            view.Label.rectTransform.anchorMax = new Vector2(0.78f, 0.96f);
            view.Label.rectTransform.offsetMin = Vector2.zero;
            view.Label.rectTransform.offsetMax = Vector2.zero;
            view.Reason.rectTransform.anchorMin = new Vector2(0.08f, 0.05f);
            view.Reason.rectTransform.anchorMax = new Vector2(0.82f, 0.47f);
            view.Reason.rectTransform.offsetMin = Vector2.zero;
            view.Reason.rectTransform.offsetMax = Vector2.zero;
            view.Badge.rectTransform.anchorMin = new Vector2(0.78f, 0f);
            view.Badge.rectTransform.anchorMax = new Vector2(0.94f, 1f);
            view.Badge.rectTransform.offsetMin = Vector2.zero;
            view.Badge.rectTransform.offsetMax = Vector2.zero;
            button.onClick.AddListener(() => OnClick(view));
            return view;
        }

        private static void BindView(EntryView view, ActivityEntrySnapshot entry)
        {
            view.Entry = entry;
            view.Label.text = entry.Entry.TitleKey;
            view.Reason.text = entry.Entry.UnavailableReasonKey ?? string.Empty;
            view.Badge.text = entry.Entry.BadgeCount > 0 ? entry.Entry.BadgeCount.ToString() : string.Empty;
            view.Button.interactable = entry.Entry.Interactable;
            view.Background.color = entry.Entry.Interactable
                ? new Color(0.12f, 0.46f, 0.70f, 0.96f)
                : new Color(0.20f, 0.30f, 0.40f, 0.86f);
        }

        private void OnClick(EntryView view)
        {
            if (m_Host == null || view?.Entry == null) return;
            m_Host.OpenEntryAsync(view.Entry).Forget(Debug.LogException);
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, Color color,
            TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.layer = 5;
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }
    }
}
