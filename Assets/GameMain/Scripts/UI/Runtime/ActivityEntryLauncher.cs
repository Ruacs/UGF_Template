using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>首页的通用活动入口。它只读取宿主条目，不引用任何具体活动页面或资源。</summary>
    public sealed class ActivityEntryLauncher : MonoBehaviour
    {
        private ActivityModuleHost m_Host;
        private ActivityEntrySnapshot m_Entry;
        private RectTransform m_Root;
        private Button m_Button;
        private TMP_Text m_Label;
        private TMP_Text m_Badge;

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
            m_Entry = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void BuildIfNeeded()
        {
            if (m_Root != null) return;
            var root = new GameObject("ActivityEntry", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.layer = 5;
            root.transform.SetParent(transform, false);
            m_Root = root.GetComponent<RectTransform>();
            m_Root.anchorMin = new Vector2(0.69f, 0.10f);
            m_Root.anchorMax = new Vector2(0.94f, 0.18f);
            m_Root.offsetMin = Vector2.zero;
            m_Root.offsetMax = Vector2.zero;
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.12f, 0.46f, 0.70f, 0.96f);
            m_Button = root.GetComponent<Button>();
            m_Button.targetGraphic = image;
            m_Button.onClick.AddListener(OnClick);

            TMP_FontAsset font = GameEntry.TMPFont?.GetCurrentFontAsset()
                ?? UGuiForm.TMP_MainFont
                ?? GetComponentInChildren<TMP_Text>(true)?.font;
            m_Label = CreateText(root.transform, "Label", 20f, Color.white, TextAlignmentOptions.Center, font);
            m_Label.rectTransform.anchorMin = new Vector2(0.07f, 0f);
            m_Label.rectTransform.anchorMax = new Vector2(0.84f, 1f);
            m_Label.rectTransform.offsetMin = Vector2.zero;
            m_Label.rectTransform.offsetMax = Vector2.zero;
            m_Badge = CreateText(root.transform, "Badge", 16f, new Color(1f, 0.84f, 0.25f, 1f), TextAlignmentOptions.Right, font);
            m_Badge.rectTransform.anchorMin = new Vector2(0.76f, 0f);
            m_Badge.rectTransform.anchorMax = new Vector2(0.96f, 1f);
            m_Badge.rectTransform.offsetMin = Vector2.zero;
            m_Badge.rectTransform.offsetMax = Vector2.zero;
        }

        private void Refresh()
        {
            if (m_Root == null) return;
            m_Entry = null;
            if (m_Host != null)
            {
                foreach (ActivityEntrySnapshot entry in m_Host.GetEntries())
                {
                    if (!entry.Entry.Visible) continue;
                    m_Entry = entry;
                    break;
                }
            }

            bool visible = m_Entry != null;
            m_Root.gameObject.SetActive(visible);
            if (!visible) return;
            m_Label.text = m_Entry.Entry.TitleKey;
            m_Badge.text = m_Entry.Entry.BadgeCount > 0 ? m_Entry.Entry.BadgeCount.ToString() : string.Empty;
            m_Button.interactable = m_Entry.Entry.Interactable;
        }

        private void OnClick()
        {
            if (m_Host == null || m_Entry == null) return;
            m_Host.OpenEntryAsync(m_Entry).Forget(Debug.LogException);
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, Color color, TextAlignmentOptions alignment, TMP_FontAsset font)
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
