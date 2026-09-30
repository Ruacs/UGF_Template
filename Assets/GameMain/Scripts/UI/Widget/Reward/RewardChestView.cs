using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>显示宝箱图标与奖励预览，奖励领取由业务模块处理。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class RewardChestView : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private Button m_Button;
        [SerializeField] private Image m_ChestIcon;
        [SerializeField] private RewardTooltipView m_Tooltip;

        private IReadOnlyList<RewardItemViewData> m_Presentation;

        private void Awake()
        {
            ResolveBindings();
        }

        private void OnEnable()
        {
            ResolveBindings();
            BindButton();
            RefreshInteractable();
        }

        private void OnDisable()
        {
            if (m_Button != null)
                m_Button.onClick.RemoveListener(OnClick);

            HidePreview();
        }

        public void Bind(Sprite chestIcon, IReadOnlyList<RewardEntry> rewards)
        {
            BindPresentation(chestIcon, RewardPresentation.Build(rewards));
        }

        public void SetIcon(Sprite chestIcon)
        {
            ResolveBindings();
            if (m_ChestIcon == null)
                return;

            m_ChestIcon.sprite = chestIcon;
            m_ChestIcon.enabled = chestIcon != null;
        }

        public void SetRewards(IReadOnlyList<RewardEntry> rewards)
        {
            ResolveBindings();
            m_Presentation = RewardPresentation.Build(rewards);
            HidePreview();
            if (isActiveAndEnabled)
                BindButton();
            RefreshInteractable();
        }

        public void BindPresentation(Sprite chestIcon, IReadOnlyList<RewardItemViewData> rewards)
        {
            ResolveBindings();
            HidePreview();
            SetIcon(chestIcon);
            m_Presentation = rewards == null ? new List<RewardItemViewData>() : new List<RewardItemViewData>(rewards);
            if (isActiveAndEnabled) BindButton();
            RefreshInteractable();
        }

        public void ShowPreview()
        {
            if (!isActiveAndEnabled || m_Button == null || !m_Button.IsInteractable() ||
                m_Tooltip == null || !HasRewards())
                return;

            m_Tooltip.ShowPresentation(m_Presentation, (RectTransform)transform);
        }

        public void HidePreview()
        {
            if (m_Tooltip != null)
                m_Tooltip.HideImmediate();
        }

        private void OnClick()
        {
            if (!isActiveAndEnabled || m_Button == null || !m_Button.IsInteractable() || m_Tooltip == null)
                return;

            if (m_Tooltip.IsVisible)
                m_Tooltip.Hide();
            else
                ShowPreview();
        }

        private void BindButton()
        {
            ResolveBindings();
            if (m_Button == null)
                return;

            m_Button.onClick.RemoveListener(OnClick);
            m_Button.onClick.AddListener(OnClick);
        }

        private void RefreshInteractable()
        {
            if (m_Button != null)
                m_Button.interactable = m_Tooltip != null && HasRewards();
        }

        private bool HasRewards()
        {
            return m_Presentation != null && m_Presentation.Count > 0;
        }

        private void ResolveBindings()
        {
            if (m_Button == null)
                m_Button = GetComponent<Button>();
            if (m_ChestIcon == null)
            {
                Transform icon = transform.Find("Root/Icon_Chest");
                if (icon == null)
                    icon = transform.Find("Chest_Close");
                m_ChestIcon = icon != null ? icon.GetComponent<Image>() : null;
            }
            if (m_Tooltip == null)
                m_Tooltip = GetComponentInChildren<RewardTooltipView>(true);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            ResolveBindings();
        }
#endif
    }
}
