using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.SeasonPass
{
    [Serializable]
    public sealed class LegacySeasonPassRewardDefinition
    {
        [SerializeField] private string m_DisplayName;
        [SerializeField] private string m_ResourceKey;
        [SerializeField] private string m_Unit = "count";
        [SerializeField] private int m_Amount;

        public string DisplayName => m_DisplayName;
        public string ResourceKey => m_ResourceKey;
        public string Unit => m_Unit;
        public int Amount => m_Amount;

        public LegacySeasonPassRewardDefinition(string displayName, string resourceKey, int amount, string unit = "count")
        {
            m_DisplayName = displayName;
            m_ResourceKey = resourceKey;
            m_Amount = amount;
            m_Unit = unit;
        }

        public ActivityRewardItem ToRewardItem()
        {
            return new ActivityRewardItem(m_ResourceKey, m_Unit, m_Amount);
        }

        internal void Validate(int tier, string track)
        {
            if (string.IsNullOrWhiteSpace(m_DisplayName))
                throw new InvalidOperationException($"Season pass tier {tier} {track} reward has no display name.");
            ActivityContract.RequireId(m_ResourceKey, nameof(m_ResourceKey));
            ActivityContract.RequireId(m_Unit, nameof(m_Unit));
            if (m_Amount <= 0)
                throw new InvalidOperationException($"Season pass tier {tier} {track} reward amount must be positive.");
        }
    }

    [Serializable]
    public sealed class SeasonPassTierDefinition
    {
        [SerializeField] private int m_Tier;
        [SerializeField] private int m_RequiredCharge;
        [SerializeField] private SeasonPassRewardDefinition m_FreeReward;
        [SerializeField] private SeasonPassRewardDefinition m_PremiumReward;
        // 原字段按值保留到显式迁移完成，避免把内嵌对象误读成 SO 引用。
        [SerializeField, HideInInspector] private LegacySeasonPassRewardDefinition[] m_FreeRewards = Array.Empty<LegacySeasonPassRewardDefinition>();
        [SerializeField, HideInInspector] private LegacySeasonPassRewardDefinition[] m_PremiumRewards = Array.Empty<LegacySeasonPassRewardDefinition>();
        [SerializeField, HideInInspector] private bool m_RewardAssetsMigrated;

        public int Tier => m_Tier;
        public int RequiredCharge => m_RequiredCharge;
        public SeasonPassRewardDefinition FreeReward => m_FreeReward;
        public SeasonPassRewardDefinition PremiumReward => m_PremiumReward;
        public IReadOnlyList<RewardEntry> FreeRewards => m_FreeReward != null ? m_FreeReward.Entries : Array.Empty<RewardEntry>();
        public IReadOnlyList<RewardEntry> PremiumRewards => m_PremiumReward != null ? m_PremiumReward.Entries : Array.Empty<RewardEntry>();

        public SeasonPassTierDefinition(int tier, int requiredCharge, LegacySeasonPassRewardDefinition[] freeRewards,
            LegacySeasonPassRewardDefinition[] premiumRewards)
        {
            m_Tier = tier;
            m_RequiredCharge = requiredCharge;
            m_FreeRewards = freeRewards ?? Array.Empty<LegacySeasonPassRewardDefinition>();
            m_PremiumRewards = premiumRewards ?? Array.Empty<LegacySeasonPassRewardDefinition>();
        }

        public SeasonPassTierDefinition(int tier, int requiredCharge, SeasonPassRewardDefinition freeReward,
            SeasonPassRewardDefinition premiumReward = null)
        {
            m_Tier = tier;
            m_RequiredCharge = requiredCharge;
            m_FreeReward = freeReward;
            m_PremiumReward = premiumReward;
            m_RewardAssetsMigrated = true;
        }

        internal void Validate(int expectedTier)
        {
            if (m_Tier != expectedTier)
                throw new InvalidOperationException($"Season pass tiers must be sequential. Expected {expectedTier}, got {m_Tier}.");
            if (m_RequiredCharge < 0)
                throw new InvalidOperationException($"Season pass tier {m_Tier} required charge cannot be negative.");
            if (!m_RewardAssetsMigrated && ((m_FreeRewards?.Length ?? 0) > 0 || (m_PremiumRewards?.Length ?? 0) > 0))
                throw new InvalidOperationException($"Season pass tier {m_Tier} needs reward SO migration. Run Install Config And Validate UI.");
            if (m_FreeReward != null) m_FreeReward.ValidateEntries();
            if (m_PremiumReward != null) m_PremiumReward.ValidateEntries();
        }

#if UNITY_EDITOR
        public bool NeedsRewardMigration => !m_RewardAssetsMigrated;
        public IReadOnlyList<LegacySeasonPassRewardDefinition> LegacyFreeRewards => m_FreeRewards;
        public IReadOnlyList<LegacySeasonPassRewardDefinition> LegacyPremiumRewards => m_PremiumRewards;
        public void SetRewardAssets(SeasonPassRewardDefinition free, SeasonPassRewardDefinition premium)
        {
            m_FreeReward = free;
            m_PremiumReward = premium;
            m_RewardAssetsMigrated = true;
        }
#endif
    }

    [CreateAssetMenu(fileName = "SeasonPassActivityConfig", menuName = "GF/Activity/Season Pass/Config")]
    public sealed class SeasonPassActivityConfig : ScriptableObject
    {
        [SerializeField] private string m_PassId = "school_pass_202609";
        [SerializeField] private string m_DisplayName = "开学通行证";
        [SerializeField] private string m_EntryLabel = "PASS";
        [SerializeField] private long m_StartUtcUnixSeconds;
        [SerializeField] private long m_EndUtcUnixSeconds;
        [SerializeField] private int m_UnlockLevel = 50;
        [SerializeField] private int m_ChargePerCompletedLevel = 1;
        [SerializeField] private bool m_EditorAlwaysUnlocked = true;
        [SerializeField] private SeasonPassTierDefinition[] m_Tiers = Array.Empty<SeasonPassTierDefinition>();

        public string PassId => m_PassId;
        public string DisplayName => m_DisplayName;
        /// <summary>首页通用入口使用的短标签，不承担活动正文的本地化职责。</summary>
        public string EntryLabel => m_EntryLabel;
        public int UnlockLevel => m_UnlockLevel;
        public int ChargePerCompletedLevel => m_ChargePerCompletedLevel;
        public bool EditorAlwaysUnlocked => m_EditorAlwaysUnlocked;
        public IReadOnlyList<SeasonPassTierDefinition> Tiers => m_Tiers;
        public DateTimeOffset StartUtc => DateTimeOffset.FromUnixTimeSeconds(m_StartUtcUnixSeconds);
        public DateTimeOffset EndUtc => DateTimeOffset.FromUnixTimeSeconds(m_EndUtcUnixSeconds);

        public bool IsActiveAt(DateTimeOffset utcNow)
        {
            DateTimeOffset now = utcNow.ToUniversalTime();
            return now >= StartUtc && now < EndUtc;
        }

        public SeasonPassTierDefinition GetTier(int tier)
        {
            if (tier <= 0 || tier > m_Tiers.Length) return null;
            SeasonPassTierDefinition result = m_Tiers[tier - 1];
            return result != null && result.Tier == tier ? result : null;
        }

        public void ValidateConfiguration()
        {
            ActivityContract.RequireId(m_PassId, nameof(m_PassId));
            if (string.IsNullOrWhiteSpace(m_DisplayName)) throw new InvalidOperationException("Season pass display name is required.");
            if (string.IsNullOrWhiteSpace(m_EntryLabel)) throw new InvalidOperationException("Season pass entry label is required.");
            if (m_EndUtcUnixSeconds <= m_StartUtcUnixSeconds) throw new InvalidOperationException("Season pass end time must be after start time.");
            if (m_UnlockLevel < 0) throw new InvalidOperationException("Season pass unlock level cannot be negative.");
            if (m_ChargePerCompletedLevel <= 0) throw new InvalidOperationException("Season pass charge per completed level must be positive.");
            if (m_Tiers == null || m_Tiers.Length == 0) throw new InvalidOperationException("Season pass needs at least one tier.");

            long totalRequiredCharge = 0;
            for (int i = 0; i < m_Tiers.Length; i++)
            {
                if (m_Tiers[i] == null) throw new InvalidOperationException($"Season pass tier {i + 1} is null.");
                m_Tiers[i].Validate(i + 1);
                totalRequiredCharge += m_Tiers[i].RequiredCharge;
                if (totalRequiredCharge > int.MaxValue)
                    throw new InvalidOperationException("Season pass total required charge exceeds the supported range.");
            }
        }

#if UNITY_EDITOR
        public void SetInstallationData(string passId, string displayName, long startUtcUnixSeconds, long endUtcUnixSeconds,
            int unlockLevel, int chargePerCompletedLevel, bool editorAlwaysUnlocked, SeasonPassTierDefinition[] tiers,
            string entryLabel = "PASS")
        {
            m_PassId = passId;
            m_DisplayName = displayName;
            m_EntryLabel = entryLabel;
            m_StartUtcUnixSeconds = startUtcUnixSeconds;
            m_EndUtcUnixSeconds = endUtcUnixSeconds;
            m_UnlockLevel = unlockLevel;
            m_ChargePerCompletedLevel = chargePerCompletedLevel;
            m_EditorAlwaysUnlocked = editorAlwaysUnlocked;
            m_Tiers = tiers ?? Array.Empty<SeasonPassTierDefinition>();
        }
#endif
    }
}
