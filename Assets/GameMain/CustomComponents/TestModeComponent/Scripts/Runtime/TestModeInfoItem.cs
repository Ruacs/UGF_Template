using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// Read-only item for displaying runtime data, such as "GameState: Playing".
    /// </summary>
    public class TestModeInfoItem : TestModeItemBase
    {
        [SerializeField] private TMP_Text m_Label;
        [SerializeField] private TMP_Text m_ValueText;
        [SerializeField] private float m_RefreshInterval = 0.2f;

        private Func<string> m_ValueGetter;
        private float m_NextRefreshTime;

        public string Value => m_ValueText != null ? m_ValueText.text : string.Empty;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            RefreshValue(true);
        }

        private void Update()
        {
            RefreshValue(false);
        }

        public TestModeInfoItem SetLabel(string text)
        {
            EnsureReferences();
            if (m_Label != null) m_Label.text = text;
            return this;
        }

        public TestModeInfoItem SetValue(string text)
        {
            m_ValueGetter = null;
            SetValueText(text);
            return this;
        }

        public TestModeInfoItem SetValueGetter(Func<string> valueGetter)
        {
            m_ValueGetter = valueGetter;
            RefreshValue(true);
            return this;
        }

        public TestModeInfoItem SetRefreshInterval(float interval)
        {
            m_RefreshInterval = Mathf.Max(0f, interval);
            return this;
        }

        private void RefreshValue(bool force)
        {
            if (m_ValueGetter == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (!force && m_RefreshInterval > 0f && now < m_NextRefreshTime)
            {
                return;
            }

            m_NextRefreshTime = now + m_RefreshInterval;
            SetValueText(m_ValueGetter.Invoke());
        }

        private void SetValueText(string text)
        {
            EnsureReferences();
            if (m_ValueText != null)
            {
                m_ValueText.text = text ?? string.Empty;
            }
        }

        private void EnsureReferences()
        {
            if (m_Label == null)
            {
                m_Label = FindComponent<TMP_Text>("Label");
            }

            if (m_ValueText == null)
            {
                m_ValueText = FindComponent<TMP_Text>("Value");
            }

            if (m_Label == null || m_ValueText == null)
            {
                BuildFallbackLayout();
            }
        }

        private void BuildFallbackLayout()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = new Vector2(0f, 100f);

            HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = 100f;
            layoutElement.layoutPriority = 1;

            if (m_Label == null)
            {
                m_Label = CreateText("Label", "Label", TextAlignmentOptions.MidlineLeft, 400f, -1f);
            }

            if (m_ValueText == null)
            {
                m_ValueText = CreateText("Value", string.Empty, TextAlignmentOptions.MidlineRight, 260f, 1f);
            }
        }

        private TMP_Text CreateText(string name, string text, TextAlignmentOptions alignment, float preferredWidth, float flexibleWidth)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = Color.white;
            tmp.fontSize = 45f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = alignment;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = 45f;
            tmp.fontSizeMin = 12f;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.preferredHeight = 100f;
            layoutElement.flexibleWidth = flexibleWidth;

            return tmp;
        }

        private T FindComponent<T>(string path) where T : Component
        {
            Transform child = transform.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }
    }
}
