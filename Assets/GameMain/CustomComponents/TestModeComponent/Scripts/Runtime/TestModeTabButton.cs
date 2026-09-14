using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class TestModeTabButton : MonoBehaviour
    {
        [SerializeField] private Image m_Background;
        [SerializeField] private TMP_Text m_Content;

        private Button m_Button;

        private void Awake()
        {
            EnsureReferences();
        }

        public void SetText(string text)
        {
            EnsureReferences();
            if (m_Content != null)
            {
                m_Content.text = text;
            }
        }

        public void SetSelected(
            bool selected,
            Color activeBackgroundColor,
            Color inactiveBackgroundColor,
            Color activeContentColor,
            Color inactiveContentColor)
        {
            EnsureReferences();

            Color backgroundColor = selected ? activeBackgroundColor : inactiveBackgroundColor;
            Color contentColor = selected ? activeContentColor : inactiveContentColor;

            if (m_Background != null)
            {
                m_Background.color = backgroundColor;
            }

            if (m_Content != null)
            {
                m_Content.color = contentColor;
            }

            if (m_Button != null)
            {
                ColorBlock colors = m_Button.colors;
                colors.normalColor = backgroundColor;
                colors.selectedColor = backgroundColor;
                m_Button.colors = colors;
            }
        }

        private void EnsureReferences()
        {
            if (m_Background == null)
            {
                m_Background = GetComponent<Image>();
            }

            if (m_Button == null)
            {
                m_Button = GetComponent<Button>();
            }

            if (m_Content == null)
            {
                Transform content = transform.Find("Content");
                m_Content = content != null
                    ? content.GetComponent<TMP_Text>()
                    : GetComponentInChildren<TMP_Text>(true);
            }
        }
    }
}
