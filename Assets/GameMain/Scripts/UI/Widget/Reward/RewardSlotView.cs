using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>一个奖励位置：空包隐藏，单项直接显示，多项显示包配置的宝箱。</summary>
    [DisallowMultipleComponent]
    public sealed class RewardSlotView : MonoBehaviour
    {
        [SerializeField] private RewardItemView m_ItemView;
        [SerializeField] private RewardChestView m_ChestView;

        public void Bind(RewardDataSO bundle)
        {
            if (m_ChestView != null) m_ChestView.HidePreview();
            SetActive(m_ItemView, false);
            SetActive(m_ChestView, false);
            if (bundle == null) return;
            try
            {
                var items = RewardPresentation.Build(bundle);
                if (items.Count == 1 && m_ItemView != null)
                {
                    m_ItemView.Bind(items[0]);
                    SetActive(m_ItemView, true);
                }
                else if (items.Count > 1 && m_ChestView != null)
                {
                    if (bundle.ChestStyle == null || bundle.ChestStyle.sprite == null)
                        throw new InvalidOperationException($"Reward bundle '{bundle.name}' has no chest appearance.");
                    m_ChestView.BindPresentation(bundle.ChestStyle.sprite, items);
                    SetActive(m_ChestView, true);
                }
            }
            catch (InvalidOperationException error)
            {
                Debug.LogError(error.Message, bundle);
            }
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }
}
