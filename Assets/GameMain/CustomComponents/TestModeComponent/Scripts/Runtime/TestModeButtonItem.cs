using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// Prefab 结构：
    /// ButtonItem (HorizontalLayoutGroup, LayoutElement h=56)
    ///   ├── Label      (TextMeshProUGUI)  flexibleWidth=1
    ///   └── Button     (Button + Image)   preferredWidth=150
    ///         └── Text (TextMeshProUGUI)
    /// </summary>
    public class TestModeButtonItem : TestModeItemBase
    {
        [SerializeField] private TMP_Text m_Label;
        [SerializeField] private Button   m_Button;
        [SerializeField] private TMP_Text m_ButtonText;

        public event Action Clicked;

        private void Awake()
        {
            m_Button?.onClick.AddListener(() => Clicked?.Invoke());
        }

        public TestModeButtonItem SetLabel(string text)
        {
            if (m_Label != null) m_Label.text = text;
            return this;
        }

        public TestModeButtonItem SetButtonText(string text)
        {
            if (m_ButtonText != null) m_ButtonText.text = text;
            return this;
        }

        public TestModeButtonItem OnClick(Action action)
        {
            Clicked += action;
            return this;
        }

        public void ClearListeners() => Clicked = null;
    }
}
