using System;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.SeasonPass.UI
{
    /// <summary>显示完成全部常规通行证档位后的 Gold Pass Bonus Bank 进度。</summary>
    public sealed class SeasonPassBonusBankView : MonoBehaviour
    {
        private const int CoinsPerBonusBankTier = 1000;
        private const float ProgressTweenDuration = 0.35f;

        [SerializeField] private GameObject m_LockedMask;
        [SerializeField] private GameObject m_RewardContentRoot;
        [SerializeField] private Image m_TokenImg;
        [SerializeField] private TMP_Text m_TierText;
        [SerializeField] private TMP_Text m_ProgressText;
        [SerializeField] private Image m_ProgressFill;
        [SerializeField] private TMP_Text m_BankCoinsText;
        [SerializeField] private Button m_BonusBankButton;

        private Action m_OnGoldPassPurchase;
        private bool m_GoldPassActivated;
        private bool m_BonusBankButtonListenerBound;
        private Tween m_ProgressTween;
        private float m_ProgressTarget;
        private bool m_HasProgressTarget;

        private void OnEnable()
        {
            if (m_BonusBankButton == null || m_BonusBankButtonListenerBound) return;
            m_BonusBankButton.onClick.AddListener(OpenGoldPassPurchase);
            m_BonusBankButtonListenerBound = true;
        }

        private void OnDisable()
        {
            m_ProgressTween?.Kill();
            m_ProgressTween = null;
            m_HasProgressTarget = false;
        }

        private void OnDestroy()
        {
            if (m_BonusBankButtonListenerBound && m_BonusBankButton != null)
                m_BonusBankButton.onClick.RemoveListener(OpenGoldPassPurchase);
            m_BonusBankButtonListenerBound = false;
            m_OnGoldPassPurchase = null;
        }

        public void Bind(bool visible, int tier, int currentProgress, int requiredProgress, bool goldPassActivated,
            Action onGoldPassPurchase)
        {
            m_GoldPassActivated = goldPassActivated;
            m_OnGoldPassPurchase = onGoldPassPurchase;
            gameObject.SetActive(visible);
            if (!visible) return;

            int target = Mathf.Max(1, requiredProgress);
            int current = Mathf.Clamp(currentProgress, 0, target);
            int currentTier = Mathf.Max(0, tier);
            if (m_ProgressText != null) m_ProgressText.SetText("{0}/{1}", current, target);
            SetProgress(current / (float)target);
            if (m_LockedMask != null) m_LockedMask.SetActive(!goldPassActivated);
            if (m_RewardContentRoot != null) m_RewardContentRoot.SetActive(goldPassActivated);

            if (m_TierText != null) m_TierText.SetText("{0}", currentTier);
            if (m_BankCoinsText != null)
                m_BankCoinsText.text = ((long)currentTier * CoinsPerBonusBankTier).ToString(CultureInfo.InvariantCulture);
        }

        private void OpenGoldPassPurchase()
        {
            if (!m_GoldPassActivated) m_OnGoldPassPurchase?.Invoke();
        }

        private void SetProgress(float target)
        {
            if (m_ProgressFill == null) return;
            target = Mathf.Clamp01(target);

            if (!m_HasProgressTarget)
            {
                m_ProgressFill.fillAmount = target;
                m_ProgressTarget = target;
                m_HasProgressTarget = true;
                return;
            }

            if (Mathf.Approximately(m_ProgressTarget, target)) return;
            m_ProgressTween?.Kill();
            m_ProgressTarget = target;
            m_ProgressTween = DOTween.To(() => m_ProgressFill.fillAmount,
                    value => m_ProgressFill.fillAmount = value, target, ProgressTweenDuration)
                .SetEase(Ease.OutCubic)
                .SetTarget(this);
        }
    }
}
