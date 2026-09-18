using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.Collector
{
    /// <summary>Collector 一个收集里程碑的静态配置；玩家进度和领奖状态不保存在此对象中。</summary>
    [Serializable]
    public sealed class CollectorTaskDefinition
    {
        [SerializeField] private int m_Tier;
        [Tooltip("本档需要收集的数量；不是跨档累计阈值。")]
        [SerializeField] private int m_RequiredCount;
        [SerializeField] private CollectorRewardDefinition m_Reward;

        public int Tier => m_Tier;
        public int RequiredCount => m_RequiredCount;
        public CollectorRewardDefinition Reward => m_Reward;
        public IReadOnlyList<RewardEntry> Rewards => m_Reward != null ? m_Reward.Entries : Array.Empty<RewardEntry>();

        public CollectorTaskDefinition(int tier, int requiredCount, CollectorRewardDefinition reward)
        {
            m_Tier = tier;
            m_RequiredCount = requiredCount;
            m_Reward = reward;
        }

        internal void Validate(int previousTier)
        {
            if (m_Tier <= previousTier)
                throw new InvalidOperationException($"Collector tiers must be strictly ascending. Expected a tier after {previousTier}, got {m_Tier}.");
            if (m_RequiredCount <= 0)
                throw new InvalidOperationException($"Collector tier {m_Tier} required count must be positive.");
            if (m_Reward == null)
                throw new InvalidOperationException($"Collector tier {m_Tier} has no reward bundle.");
            m_Reward.ValidateEntries();
        }
    }

    /// <summary>
    /// Collector 的数值和奖励清单。它只描述活动静态内容，不携带当前收集数量、已领奖档位或存档状态。
    /// </summary>
    [CreateAssetMenu(fileName = "CollectorActivityConfig", menuName = "GF/Activity/Collector/Config")]
    public sealed class CollectorActivityConfig : ScriptableObject
    {
        [SerializeField] private string m_CollectorId = "hexa_collector_202609";
        [SerializeField] private string m_DisplayName = "HEXA COLLECTOR";
        [SerializeField] private string m_EntryLabel = "COLLECTOR";
        [SerializeField] private long m_StartUtcUnixSeconds;
        [SerializeField] private long m_EndUtcUnixSeconds;
        [SerializeField] private int m_UnlockLevel = 1;
        [SerializeField] private bool m_EditorAlwaysUnlocked = true;
        [SerializeField] private int m_TargetCount = 50;
        [SerializeField, Min(1)] private int m_CollectionPerWin = 1;
        [SerializeField] private Sprite m_CollectibleIcon;
        [SerializeField] private CollectorTaskDefinition[] m_Tasks = Array.Empty<CollectorTaskDefinition>();

        public string CollectorId => m_CollectorId;
        public string DisplayName => m_DisplayName;
        public string EntryLabel => m_EntryLabel;
        public int UnlockLevel => m_UnlockLevel;
        public bool EditorAlwaysUnlocked => m_EditorAlwaysUnlocked;
        public int TargetCount => m_TargetCount;
        /// <summary>每个目标游戏的胜利事实增加的收集数量。</summary>
        public int CollectionPerWin => m_CollectionPerWin;
        public Sprite CollectibleIcon => m_CollectibleIcon;
        public IReadOnlyList<CollectorTaskDefinition> Tasks => m_Tasks;
        public DateTimeOffset StartUtc => DateTimeOffset.FromUnixTimeSeconds(m_StartUtcUnixSeconds);
        public DateTimeOffset EndUtc => DateTimeOffset.FromUnixTimeSeconds(m_EndUtcUnixSeconds);

        public bool IsActiveAt(DateTimeOffset utcNow)
        {
            DateTimeOffset now = utcNow.ToUniversalTime();
            return now >= StartUtc && now < EndUtc;
        }

        public CollectorTaskDefinition GetTask(int tier)
        {
            if (m_Tasks == null) return null;
            foreach (CollectorTaskDefinition task in m_Tasks)
                if (task != null && task.Tier == tier) return task;
            return null;
        }

        /// <summary>
        /// 按当前档索引返回活动要处理的任务；全部完成时返回 null。
        /// 索引而不是累计收集数是任务推进的依据，因为每档完成后进度会清零。
        /// </summary>
        public CollectorTaskDefinition GetNextTask(int currentTaskIndex)
        {
            if (m_Tasks == null) return null;
            int index = Mathf.Max(0, currentTaskIndex);
            return index < m_Tasks.Length ? m_Tasks[index] : null;
        }

        public void ValidateConfiguration()
        {
            ActivityContract.RequireId(m_CollectorId, nameof(m_CollectorId));
            if (string.IsNullOrWhiteSpace(m_DisplayName))
                throw new InvalidOperationException("Collector display name is required.");
            if (string.IsNullOrWhiteSpace(m_EntryLabel))
                throw new InvalidOperationException("Collector entry label is required.");
            if (m_EndUtcUnixSeconds <= m_StartUtcUnixSeconds)
                throw new InvalidOperationException("Collector end time must be after start time.");
            if (m_UnlockLevel < 0)
                throw new InvalidOperationException("Collector unlock level cannot be negative.");
            if (m_TargetCount <= 0)
                throw new InvalidOperationException("Collector target count must be positive.");
            if (m_CollectionPerWin <= 0)
                throw new InvalidOperationException("Collector collection per win must be positive.");
            if (m_Tasks == null || m_Tasks.Length == 0)
                throw new InvalidOperationException("Collector needs at least one task reward.");

            int previousTier = 0;
            foreach (CollectorTaskDefinition task in m_Tasks)
            {
                if (task == null) throw new InvalidOperationException("Collector contains a null task definition.");
                // RequiredCount is the amount for this task only. It is intentionally allowed
                // to repeat (for example 50/50/50/50/50) because progress resets when a tier
                // is completed instead of carrying into the next tier.
                task.Validate(previousTier);
                previousTier = task.Tier;
            }

            if (m_Tasks[0].RequiredCount != m_TargetCount)
                throw new InvalidOperationException("Collector target count must equal the first task requirement.");
        }

#if UNITY_EDITOR
        /// <summary>
        /// 将新增字段写回已有配置资产。安装器会用当前值调用一次，避免旧资产因未序列化新字段而在重载后丢失默认值。
        /// </summary>
        public void SetCollectionPerWin(int collectionPerWin)
        {
            if (collectionPerWin <= 0)
                throw new ArgumentOutOfRangeException(nameof(collectionPerWin));
            m_CollectionPerWin = collectionPerWin;
        }

        public void SetInstallationData(string collectorId, string displayName, string entryLabel,
            long startUtcUnixSeconds, long endUtcUnixSeconds, int unlockLevel, bool editorAlwaysUnlocked,
            int targetCount, int collectionPerWin, Sprite collectibleIcon, CollectorTaskDefinition[] tasks)
        {
            m_CollectorId = collectorId;
            m_DisplayName = displayName;
            m_EntryLabel = entryLabel;
            m_StartUtcUnixSeconds = startUtcUnixSeconds;
            m_EndUtcUnixSeconds = endUtcUnixSeconds;
            m_UnlockLevel = unlockLevel;
            m_EditorAlwaysUnlocked = editorAlwaysUnlocked;
            m_TargetCount = targetCount;
            m_CollectionPerWin = collectionPerWin;
            m_CollectibleIcon = collectibleIcon;
            m_Tasks = tasks ?? Array.Empty<CollectorTaskDefinition>();
        }
#endif
    }
}
