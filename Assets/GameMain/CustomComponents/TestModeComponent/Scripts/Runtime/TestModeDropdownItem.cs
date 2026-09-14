using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// Prefab 结构：
    /// DropdownItem (HorizontalLayoutGroup, LayoutElement h=56)
    ///   ├── Label     (TextMeshProUGUI)       flexibleWidth=1
    ///   └── Dropdown  (TMP_Dropdown + Image)  preferredWidth=200
    /// </summary>
    public class TestModeDropdownItem : TestModeItemBase
    {
        [SerializeField] private TMP_Text     m_Label;
        [SerializeField] private TMP_Dropdown m_Dropdown;

        public int Value => m_Dropdown != null ? m_Dropdown.value : 0;

        public event Action<int> ValueChanged;

        private void Awake()
        {
            m_Dropdown?.onValueChanged.AddListener(v => ValueChanged?.Invoke(v));
        }

        public TestModeDropdownItem SetLabel(string text)
        {
            if (m_Label != null) m_Label.text = text;
            return this;
        }

        public TestModeDropdownItem SetOptions(IEnumerable<string> options)
        {
            if (m_Dropdown == null) return this;
            m_Dropdown.ClearOptions();
            foreach (var opt in options)
                m_Dropdown.options.Add(new TMP_Dropdown.OptionData(opt));
            m_Dropdown.RefreshShownValue();
            return this;
        }

        public TestModeDropdownItem SetValue(int index)
        {
            if (m_Dropdown != null) m_Dropdown.value = index;
            return this;
        }

        public TestModeDropdownItem OnValueChanged(Action<int> action)
        {
            ValueChanged += action;
            return this;
        }

        public void ClearListeners() => ValueChanged = null;
    }
}
