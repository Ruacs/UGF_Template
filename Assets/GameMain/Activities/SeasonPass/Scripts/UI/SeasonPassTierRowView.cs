using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.SeasonPass.UI
{
    /// <summary>通行证奖励行：按 Prefab 的里程碑、免费和高级轨道分别刷新显示。</summary>
    [DisallowMultipleComponent]
    public sealed class SeasonPassTierRowView : MonoBehaviour
    {
        [Header("Milestone Lane")]
        [SerializeField] private TMP_Text m_TierText;
        [SerializeField] private TMP_Text m_RequiredChargeText;
        [Tooltip("奖励列表的总充能时间线（0 到 1），独立于主页顶部的当前 Tier 局部进度。")]
        [SerializeField] private Image m_ProgressFill;

        [Header("Reward Lanes")]
        [SerializeField] private RewardLaneBindings m_FreeLane = new RewardLaneBindings();
        [SerializeField] private RewardLaneBindings m_PremiumLane = new RewardLaneBindings();

        private int m_Tier;
        private Action<int> m_OnClaimFree;
        private Action<int> m_OnClaimPremium;
        private Action m_OnPremiumLaneClick;
        private bool m_FreeClaimListenerBound;
        private bool m_PremiumClaimListenerBound;
        private bool m_PremiumLaneListenerBound;
        private bool m_CanClaimFree;
        private bool m_CanClaimPremium;
        private bool m_PremiumActivated;

        private enum RewardState
        {
            Empty,
            Locked,
            Unavailable,
            Claimable,
            Claimed
        }

        /// <summary>两条奖励轨道共用相同的绑定和显示规则，奖励内容由 Prefab 提供。</summary>
        [Serializable]
        private sealed class RewardLaneBindings
        {
            [SerializeField] private Transform m_RewardContentRoot;
            [SerializeField] private RewardSlotView m_RewardSlot;
            [Tooltip("可选的文本奖励展示；使用图标布局时可留空。")]
            [SerializeField] private TMP_Text m_RewardsText;
            [SerializeField] private Button m_RewardButton;
            [SerializeField] private Button m_ClaimButton;
            [SerializeField] private TMP_Text m_ClaimButtonText;
            [SerializeField] private GameObject m_LockedMask;
            [SerializeField] private GameObject m_ClaimedMask;

            public Button RewardButton => m_RewardButton;
            public Button ClaimButton => m_ClaimButton;

            public void SetRewardPreviewInteractable(bool interactable)
            {
                if (m_RewardContentRoot == null) return;
                foreach (Button button in m_RewardContentRoot.GetComponentsInChildren<Button>(true))
                    button.interactable = interactable;
            }

            public void Refresh(SeasonPassRewardDefinition bundle, IReadOnlyList<RewardEntry> rewards, RewardState state,
                bool showLockedMask, bool allowRewardCardClick)
            {
                bool hasRewards = state != RewardState.Empty;
                bool claimed = state == RewardState.Claimed;
                bool canClaim = state == RewardState.Claimable;

                SetText(m_RewardsText, Describe(rewards));
                if (m_RewardSlot != null) m_RewardSlot.Bind(bundle);
                if (m_RewardContentRoot != null) m_RewardContentRoot.gameObject.SetActive(hasRewards);
                // 奖励卡不直接领取；未购买高级通行证时用于打开购买页。
                if (m_RewardButton != null) m_RewardButton.interactable = allowRewardCardClick;
                if (m_ClaimButton != null)
                {
                    m_ClaimButton.interactable = canClaim;
                    m_ClaimButton.gameObject.SetActive(canClaim);
                }

                SetActive(m_ClaimedMask, claimed);
                SetActive(m_LockedMask, showLockedMask && hasRewards);
            }
        }

        private void OnEnable()
        {
            EnsureClaimListener();
        }

        private void OnDestroy()
        {
            if (m_FreeClaimListenerBound && m_FreeLane.ClaimButton != null)
                m_FreeLane.ClaimButton.onClick.RemoveListener(ClaimFree);
            if (m_PremiumClaimListenerBound && m_PremiumLane.ClaimButton != null)
                m_PremiumLane.ClaimButton.onClick.RemoveListener(ClaimPremium);
            if (m_PremiumLaneListenerBound && m_PremiumLane.RewardButton != null)
                m_PremiumLane.RewardButton.onClick.RemoveListener(OpenGoldPassPurchase);
            m_OnClaimFree = null;
            m_OnClaimPremium = null;
            m_OnPremiumLaneClick = null;
        }

        /// <param name="tierProgressFill">本档位的局部进度；已完成为 1，未到达为 0。</param>
        public void Bind(SeasonPassTierSnapshot tier, int passCharge, float tierProgressFill, Action<int> onClaim)
        {
            Bind(tier, passCharge, tierProgressFill, onClaim, null, null, false);
        }

        public void Bind(SeasonPassTierSnapshot tier, int passCharge, float tierProgressFill, Action<int> onClaimFree,
            Action<int> onClaimPremium, Action onPremiumLaneClick, bool premiumActivated = false)
        {
            if (tier == null) throw new ArgumentNullException(nameof(tier));

            EnsureClaimListener();
            m_Tier = tier.Tier;
            m_OnClaimFree = onClaimFree;
            m_OnClaimPremium = onClaimPremium;
            m_OnPremiumLaneClick = onPremiumLaneClick;
            m_PremiumActivated = premiumActivated;

            // RequiredCharge is local to this tier; the local fill tells whether it is reached.
            bool unlocked = tierProgressFill >= 1f;
            RefreshMilestone(tier, tierProgressFill);

            RewardState freeState = GetRewardState(tier.FreeRewards, tier.FreeClaimed, unlocked, tier.CanClaimFree);
            m_CanClaimFree = freeState == RewardState.Claimable && onClaimFree != null;
            m_FreeLane.Refresh(tier.FreeReward, tier.FreeRewards, freeState, showLockedMask: false, allowRewardCardClick: false);
            m_FreeLane.SetRewardPreviewInteractable(true);

            RewardState premiumState = GetRewardState(tier.PremiumRewards, tier.PremiumClaimed, unlocked, tier.CanClaimPremium);
            m_CanClaimPremium = premiumState == RewardState.Claimable && onClaimPremium != null;
            m_PremiumLane.Refresh(tier.PremiumReward, tier.PremiumRewards, premiumState,
                showLockedMask: !premiumActivated, allowRewardCardClick: !premiumActivated);
            m_PremiumLane.SetRewardPreviewInteractable(premiumActivated);
        }

        public void ApplyFont(TMP_FontAsset font)
        {
            if (font == null) return;
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) text.font = font;
        }

        private void EnsureClaimListener()
        {
            if (!m_FreeClaimListenerBound && m_FreeLane.ClaimButton != null)
            {
                m_FreeLane.ClaimButton.onClick.AddListener(ClaimFree);
                m_FreeClaimListenerBound = true;
            }
            if (!m_PremiumClaimListenerBound && m_PremiumLane.ClaimButton != null)
            {
                m_PremiumLane.ClaimButton.onClick.AddListener(ClaimPremium);
                m_PremiumClaimListenerBound = true;
            }
            if (!m_PremiumLaneListenerBound && m_PremiumLane.RewardButton != null)
            {
                m_PremiumLane.RewardButton.onClick.AddListener(OpenGoldPassPurchase);
                m_PremiumLaneListenerBound = true;
            }
        }

        private void ClaimFree()
        {
            if (m_CanClaimFree) m_OnClaimFree?.Invoke(m_Tier);
        }

        private void ClaimPremium()
        {
            if (m_CanClaimPremium) m_OnClaimPremium?.Invoke(m_Tier);
        }

        private void OpenGoldPassPurchase()
        {
            if (!m_PremiumActivated) m_OnPremiumLaneClick?.Invoke();
        }

        private void RefreshMilestone(SeasonPassTierSnapshot tier, float tierProgressFill)
        {
            SetText(m_TierText, tier.Tier.ToString());
            SetText(m_RequiredChargeText,tier.RequiredCharge.ToString());
            if (m_ProgressFill != null)
                m_ProgressFill.fillAmount = Mathf.Clamp01(tierProgressFill);
        }

        private static RewardState GetRewardState(IReadOnlyList<RewardEntry> rewards,
            bool claimed, bool unlocked, bool canClaim)
        {
            if (rewards == null || rewards.Count == 0) return RewardState.Empty;
            if (claimed) return RewardState.Claimed;
            if (!unlocked) return RewardState.Locked;
            return canClaim ? RewardState.Claimable : RewardState.Unavailable;
        }

        private static string Describe(IReadOnlyList<RewardEntry> rewards)
        {
            if (rewards == null || rewards.Count == 0) return "—";
            var values = new string[rewards.Count];
            for (int i = 0; i < rewards.Count; i++) values[i] = RewardPresentation.FormatValue(rewards[i]);
            return string.Join("\n", values);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }
    }
}
