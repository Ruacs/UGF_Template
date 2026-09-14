using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lokas
{

    public class TestModeIntStepperItem : TestModeItemBase
    {
        [SerializeField, FormerlySerializedAs("m_Label")] private TMP_Text m_TitleText;
        [SerializeField, FormerlySerializedAs("m_ValueText")] private TMP_InputField m_InputField;
        [SerializeField] private TMP_Text m_InputText;
        [SerializeField, FormerlySerializedAs("m_DecButton")] private Button m_SubButton;
        [SerializeField, FormerlySerializedAs("m_IncButton")] private Button m_AddButton;
        [SerializeField, FormerlySerializedAs("m_ApplyBotton")] private Button m_ApplyButton;
        [SerializeField] private float m_ClickInterval = 0.2f;
        [SerializeField] private bool m_NotifyOnStepButtonOrInputEnd;

        private int m_Value;
        private int m_Min = 0;
        private int m_Max = 100;
        private int m_Step = 1;
        private float m_LastStepButtonClickTime = float.NegativeInfinity;

        public int Value => m_Value;

        public event Action<int> ValueChanged;

        private void Awake()
        {
            EnsureReferences();
            m_SubButton?.onClick.AddListener(Decrement);
            m_AddButton?.onClick.AddListener(Increment);
            m_ApplyButton?.onClick.AddListener(ApplyValue);
            m_InputField?.onEndEdit.AddListener(OnInputEndEdit);
        }

        public TestModeIntStepperItem SetLabel(string text)
        {
            EnsureReferences();
            if (m_TitleText != null) m_TitleText.text = text;
            return this;
        }

        public TestModeIntStepperItem SetRange(int min, int max, int step = 1)
        {
            m_Min = min;
            m_Max = max;
            m_Step = step;
            SetValue(Mathf.Clamp(m_Value, m_Min, m_Max));
            return this;
        }

        public TestModeIntStepperItem SetValue(int value)
        {
            m_Value = Mathf.Clamp(value, m_Min, m_Max);
            RefreshDisplay();
            return this;
        }

        public TestModeIntStepperItem OnValueChanged(Action<int> action)
        {
            ValueChanged += action;
            return this;
        }

        public TestModeIntStepperItem SetClickInterval(float interval)
        {
            m_ClickInterval = Mathf.Max(0f, interval);
            return this;
        }

        public TestModeIntStepperItem SetNotifyOnStepButtonOrInputEnd(bool notify)
        {
            m_NotifyOnStepButtonOrInputEnd = notify;
            m_ApplyButton?.gameObject.SetActive(!notify);
            return this;
        }

        private void Increment()
        {
            if (!CanClickStepButton())
            {
                return;
            }

            ReadInputValue();
            m_Value = Mathf.Min(m_Value + m_Step, m_Max);
            RefreshDisplay();
            NotifyIfImmediateChangeEnabled();
        }

        private void Decrement()
        {
            if (!CanClickStepButton())
            {
                return;
            }

            ReadInputValue();
            m_Value = Mathf.Max(m_Value - m_Step, m_Min);
            RefreshDisplay();
            NotifyIfImmediateChangeEnabled();
        }

        private void ApplyValue()
        {
            ReadInputValue();
            RefreshDisplay();
            ValueChanged?.Invoke(m_Value);
        }

        private void OnInputEndEdit(string _)
        {
            ReadInputValue();
            RefreshDisplay();
            NotifyIfImmediateChangeEnabled();
        }

        private void RefreshDisplay()
        {
            EnsureReferences();
            string valueText = m_Value.ToString();
            if (m_InputField != null)
            {
                m_InputField.SetTextWithoutNotify(valueText);
            }

            if (m_InputText != null)
            {
                m_InputText.text = valueText;
            }
        }

        private void ReadInputValue()
        {
            EnsureReferences();
            if (m_InputField != null && int.TryParse(m_InputField.text, out int value))
            {
                m_Value = Mathf.Clamp(value, m_Min, m_Max);
            }
            else
            {
                m_Value = Mathf.Clamp(m_Value, m_Min, m_Max);
            }
        }

        private void NotifyIfImmediateChangeEnabled()
        {
            if (m_ApplyButton == null || m_NotifyOnStepButtonOrInputEnd)
            {
                ValueChanged?.Invoke(m_Value);
            }


        }

        private bool CanClickStepButton()
        {
            if (m_ClickInterval <= 0f)
            {
                return true;
            }

            float now = Time.unscaledTime;
            if (now - m_LastStepButtonClickTime < m_ClickInterval)
            {
                return false;
            }

            m_LastStepButtonClickTime = now;
            return true;
        }

        private void EnsureReferences()
        {
            if (m_TitleText == null)
            {
                m_TitleText = FindComponent<TMP_Text>("IntStepperItem_TitleText");
            }

            if (m_SubButton == null)
            {
                m_SubButton = FindComponent<Button>("IntStepperItem_ContentRoot/IntStepperItem_SubButton");
            }

            if (m_InputField == null)
            {
                m_InputField = FindComponent<TMP_InputField>("IntStepperItem_ContentRoot/IntStepperItem_InputField");
            }

            if (m_InputText == null)
            {
                m_InputText = FindComponent<TMP_Text>("IntStepperItem_ContentRoot/IntStepperItem_InputField/IntStepperItem_InputText");
            }

            if (m_InputField != null && m_InputField.textComponent == null)
            {
                m_InputField.textComponent = m_InputText;
            }

            if (m_AddButton == null)
            {
                m_AddButton = FindComponent<Button>("IntStepperItem_ContentRoot/IntStepperItem_AddButton");
            }

            if (m_ApplyButton == null)
            {
                m_ApplyButton = FindComponent<Button>("IntStepperItem_ContentRoot/IntStepperItem_ApplyButton");
            }
        }

        private T FindComponent<T>(string path) where T : Component
        {
            Transform child = transform.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }
    }
}
