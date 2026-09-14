using System;
using UnityEngine;

namespace Lokas
{
    [Serializable]
    public sealed class TestModePrefabCatalog
    {
        [SerializeField] private TestModeButtonItem m_ButtonItemPrefab;
        [SerializeField] private TestModeIntStepperItem m_IntStepperItemPrefab;
        [SerializeField] private TestModeToggleItem m_ToggleItemPrefab;
        [SerializeField] private TestModeDropdownItem m_DropdownItemPrefab;
        [SerializeField] private TestModeSliderItem m_SliderItemPrefab;
        [SerializeField] private TestModeInfoItem m_InfoItemPrefab;

        public T GetItemPrefab<T>() where T : TestModeItemBase
        {
            if (typeof(T) == typeof(TestModeButtonItem))
            {
                return m_ButtonItemPrefab as T;
            }

            if (typeof(T) == typeof(TestModeIntStepperItem))
            {
                return m_IntStepperItemPrefab as T;
            }

            if (typeof(T) == typeof(TestModeToggleItem))
            {
                return m_ToggleItemPrefab as T;
            }

            if (typeof(T) == typeof(TestModeDropdownItem))
            {
                return m_DropdownItemPrefab as T;
            }

            if (typeof(T) == typeof(TestModeSliderItem))
            {
                return m_SliderItemPrefab as T;
            }

            if (typeof(T) == typeof(TestModeInfoItem))
            {
                return m_InfoItemPrefab as T;
            }

            return null;
        }
    }
}
