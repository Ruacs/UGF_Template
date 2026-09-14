using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// Prefab 结构：
    /// SliderItem (VerticalLayoutGroup, LayoutElement h=72)
    ///   ├── TopRow  (HorizontalLayoutGroup, preferredHeight=26)
    ///   │     ├── Label      (TextMeshProUGUI)  flexibleWidth=1
    ///   │     └── ValueText  (TextMeshProUGUI)  preferredWidth=64, 右对齐, 蓝色
    ///   └── Slider  (Slider)                    preferredHeight=28
    ///         ├── Background  (Image, 深色)
    ///         ├── Fill Area
    ///         │     └── Fill  (Image, 蓝色)
    ///         └── Handle Slide Area
    ///               └── Handle (Image, 白色圆形)
    /// </summary>
    public class TestModeSliderItem : TestModeItemBase
    {
        [SerializeField] private TMP_Text m_Label;
        [SerializeField] private TMP_Text m_ValueText;
        [SerializeField] private Slider   m_Slider;

        [Header("Format")]
        [SerializeField] private bool  m_WholeNumber = true;
        [SerializeField] private int   m_Decimals    = 1;   // 非整数时保留小数位

        public float Value => m_Slider != null ? m_Slider.value : 0f;

        public event Action<float> ValueChanged;

        private void Awake()
        {
            if (m_Slider != null)
            {
                m_Slider.wholeNumbers = m_WholeNumber;
                m_Slider.onValueChanged.AddListener(OnSliderChanged);
                RefreshDisplay(m_Slider.value);
            }
        }

        public TestModeSliderItem SetLabel(string text)
        {
            if (m_Label != null) m_Label.text = text;
            return this;
        }

        

        public TestModeSliderItem SetRange(float min, float max, bool wholeNumber = true)
        {
            if (m_Slider == null) return this;
            m_WholeNumber        = wholeNumber;
            m_Slider.minValue    = min;
            m_Slider.maxValue    = max;
            m_Slider.wholeNumbers = wholeNumber;
            return this;
        }

        public TestModeSliderItem SetDecimals(int decimals)
        {
            m_Decimals = Mathf.Max(0, decimals);
            RefreshDisplay(Value);
            return this;
        }

        public TestModeSliderItem SetValue(float value)
        {
            if (m_Slider != null) m_Slider.value = value;
            return this;
        }

        public TestModeSliderItem OnValueChanged(Action<float> action)
        {
            ValueChanged += action;
            return this;
        }

        private void OnSliderChanged(float value)
        {
            RefreshDisplay(value);
            ValueChanged?.Invoke(value);
        }

        private void RefreshDisplay(float value)
        {
            if (m_ValueText == null) return;
            m_ValueText.text = m_WholeNumber
                ? ((int)value).ToString()
                : value.ToString("F" + m_Decimals);
        }
    }
}
