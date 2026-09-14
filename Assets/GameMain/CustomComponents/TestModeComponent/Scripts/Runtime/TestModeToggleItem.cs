using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// Prefab 结构：
    /// ToggleItem (HorizontalLayoutGroup, LayoutElement h=56)
    ///   ├── Label      (TextMeshProUGUI)  flexibleWidth=1
    ///   └── Toggle     (Toggle)           preferredWidth=52
    ///         ├── Background  (Image)
    ///         └── Checkmark   (Image)     — 绿色，Toggle.graphic 指向它
    /// </summary>
    public class TestModeToggleItem : TestModeItemBase
    {
        [SerializeField] private TMP_Text m_Label;
        [SerializeField] private Toggle   m_Toggle;

        public event Action<bool> ValueChanged;

        private void Awake()
        {
            m_Toggle?.onValueChanged.AddListener(v => ValueChanged?.Invoke(v));
        }

        public TestModeToggleItem SetLabel(string text)
        {
            if (m_Label != null) m_Label.text = text;
            return this;
        }

        public TestModeToggleItem SetValue(bool value)
        {
            if (m_Toggle != null) m_Toggle.isOn = value;
            return this;
        }

        public bool GetValue() => m_Toggle != null && m_Toggle.isOn;

        public TestModeToggleItem OnValueChanged(Action<bool> action)
        {
            ValueChanged += action;
            return this;
        }

        public void ClearListeners() => ValueChanged = null;
    }
}
