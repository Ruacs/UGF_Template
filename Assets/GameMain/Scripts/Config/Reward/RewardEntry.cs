using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>一项奖励。Amount 为数量或秒数；只包含配置，不持有玩家的到期时间。</summary>
    [Serializable]
    public sealed class RewardEntry
    {
        [SerializeField] private RewardDefinitionSO m_Definition;
        [SerializeField] private RewardGrantMode m_GrantMode;
        [SerializeField] private long m_Amount = 1;
        // 保留旧活动 minute 请求的单位，避免仅迁移展示数据就改变已有回执指纹。
        [SerializeField, HideInInspector] private bool m_LegacyMinuteRequest;

        public RewardDefinitionSO Definition => m_Definition;
        public RewardGrantMode GrantMode => m_GrantMode;
        public long Amount => m_Amount;
        public string DisplayName => m_Definition != null ? m_Definition.displayName : "?";
        public string ResourceKey => m_Definition != null ? m_Definition.GetGrantKey(m_GrantMode) : null;
        public string Unit => m_GrantMode == RewardGrantMode.AddQuantity ? "count" : UsesLegacyMinutes ? "minute" : "second";
        public long RequestAmount => UsesLegacyMinutes ? m_Amount / 60 : m_Amount;
        private bool UsesLegacyMinutes => m_LegacyMinuteRequest && m_GrantMode == RewardGrantMode.UnlimitedUse && m_Amount % 60 == 0;

        public RewardEntry(RewardDefinitionSO definition, RewardGrantMode grantMode, long amount, bool legacyMinuteRequest = false)
        {
            m_Definition = definition;
            m_GrantMode = grantMode;
            m_Amount = amount;
            m_LegacyMinuteRequest = legacyMinuteRequest;
        }

        public void Validate()
        {
            if (m_Definition == null) throw new InvalidOperationException("Reward entry has no resource definition.");
            m_Definition.Validate(m_GrantMode);
            if (m_Amount <= 0) throw new InvalidOperationException($"Reward '{ResourceKey}' amount must be positive.");
        }
    }
}
