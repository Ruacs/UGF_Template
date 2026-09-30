using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.WinStreak
{
    public enum WinStreakChestTier
    {
        Blue = 0,
        Pink = 1,
        Gold = 2,
        Orange = 3,
    }

    /// <summary>一个连胜检查点及其奖励。Checkpoint 是累加后的目标值，而不是本档增量。</summary>
    [Serializable]
    public sealed class WinStreakCheckpointDefinition
    {
        [SerializeField] private int m_Checkpoint;
        [SerializeField] private int m_RewardId;
        [SerializeField] private WinStreakChestTier m_ChestTier;
        [SerializeField] private WinStreakRewardDefinition m_Reward;

        public int Checkpoint => m_Checkpoint;
        public int RewardId => m_RewardId;
        public WinStreakChestTier ChestTier => m_ChestTier;
        public WinStreakRewardDefinition Reward => m_Reward;
        public IReadOnlyList<RewardEntry> Rewards => m_Reward != null ? m_Reward.Entries : Array.Empty<RewardEntry>();

        public WinStreakCheckpointDefinition(int checkpoint, int rewardId, WinStreakChestTier chestTier,
            WinStreakRewardDefinition reward)
        {
            m_Checkpoint = checkpoint;
            m_RewardId = rewardId;
            m_ChestTier = chestTier;
            m_Reward = reward;
        }

        internal void Validate(int previousCheckpoint)
        {
            if (m_Checkpoint <= previousCheckpoint)
                throw new InvalidOperationException(
                    $"Win Streak checkpoints must be strictly ascending. Expected a value after {previousCheckpoint}, got {m_Checkpoint}.");
            if (m_RewardId <= 0)
                throw new InvalidOperationException($"Win Streak checkpoint {m_Checkpoint} needs a positive reward ID.");
            if (!Enum.IsDefined(typeof(WinStreakChestTier), m_ChestTier))
                throw new InvalidOperationException($"Win Streak checkpoint {m_Checkpoint} has an invalid chest tier.");
            if (m_Reward == null)
                throw new InvalidOperationException($"Win Streak checkpoint {m_Checkpoint} has no reward bundle.");
            m_Reward.ValidateEntries();
        }
    }

    /// <summary>
    /// Win Streak 的静态活动配置。当前默认数据对应分析文档中的 Event 2，进度按胜场累加。
    /// </summary>
    [CreateAssetMenu(fileName = "WinStreakActivityConfig", menuName = "GF/Activity/Win Streak/Config")]
    public sealed class WinStreakActivityConfig : ScriptableObject
    {
        [SerializeField] private int m_EventId = 2;
        [SerializeField] private string m_StreakId = "hexa_win_streak_event_2_202609";
        [SerializeField] private string m_DisplayName = "HEXA STREAK";
        [SerializeField] private string m_EntryLabel = "STREAK";
        [SerializeField] private long m_StartUtcUnixSeconds;
        [SerializeField] private long m_EndUtcUnixSeconds;
        [SerializeField] private int m_UnlockLevel = 40;
        [SerializeField, Min(1)] private int m_ProgressPerWin = 1;
        [SerializeField] private bool m_ResetOnLoss = true;
        [SerializeField] private bool m_EditorAlwaysUnlocked = true;
        [SerializeField] private WinStreakCheckpointDefinition[] m_Checkpoints = Array.Empty<WinStreakCheckpointDefinition>();

        public int EventId => m_EventId;
        public string StreakId => m_StreakId;
        public string DisplayName => m_DisplayName;
        public string EntryLabel => m_EntryLabel;
        public int UnlockLevel => m_UnlockLevel;
        public int ProgressPerWin => m_ProgressPerWin;
        public bool ResetOnLoss => m_ResetOnLoss;
        public bool EditorAlwaysUnlocked => m_EditorAlwaysUnlocked;
        public IReadOnlyList<WinStreakCheckpointDefinition> Checkpoints => m_Checkpoints;
        public int MaxProgress => m_Checkpoints == null || m_Checkpoints.Length == 0
            ? 0
            : m_Checkpoints[m_Checkpoints.Length - 1].Checkpoint;
        public DateTimeOffset StartUtc => DateTimeOffset.FromUnixTimeSeconds(m_StartUtcUnixSeconds);
        public DateTimeOffset EndUtc => DateTimeOffset.FromUnixTimeSeconds(m_EndUtcUnixSeconds);

        public bool IsActiveAt(DateTimeOffset utcNow)
        {
            DateTimeOffset now = utcNow.ToUniversalTime();
            return now >= StartUtc && now < EndUtc;
        }

        public WinStreakCheckpointDefinition GetCheckpoint(int checkpoint)
        {
            if (m_Checkpoints == null) return null;
            foreach (WinStreakCheckpointDefinition definition in m_Checkpoints)
                if (definition != null && definition.Checkpoint == checkpoint) return definition;
            return null;
        }

        public WinStreakCheckpointDefinition GetCheckpointAtIndex(int index)
        {
            if (m_Checkpoints == null || index < 0 || index >= m_Checkpoints.Length) return null;
            return m_Checkpoints[index];
        }

        public int GetHighestReachedIndex(int progress)
        {
            int result = -1;
            if (m_Checkpoints == null) return result;
            for (int index = 0; index < m_Checkpoints.Length; index++)
            {
                WinStreakCheckpointDefinition definition = m_Checkpoints[index];
                if (definition == null || definition.Checkpoint > progress) break;
                result = index;
            }
            return result;
        }

        public WinStreakCheckpointDefinition GetNextCheckpoint(int progress)
        {
            if (m_Checkpoints == null) return null;
            foreach (WinStreakCheckpointDefinition definition in m_Checkpoints)
                if (definition != null && definition.Checkpoint > progress) return definition;
            return null;
        }

        public void ValidateConfiguration()
        {
            ActivityContract.RequireId(m_StreakId, nameof(m_StreakId));
            if (m_EventId <= 0) throw new InvalidOperationException("Win Streak event ID must be positive.");
            if (string.IsNullOrWhiteSpace(m_DisplayName))
                throw new InvalidOperationException("Win Streak display name is required.");
            if (string.IsNullOrWhiteSpace(m_EntryLabel))
                throw new InvalidOperationException("Win Streak entry label is required.");
            if (m_EndUtcUnixSeconds <= m_StartUtcUnixSeconds)
                throw new InvalidOperationException("Win Streak end time must be after start time.");
            if (m_UnlockLevel < 0) throw new InvalidOperationException("Win Streak unlock level cannot be negative.");
            if (m_ProgressPerWin <= 0) throw new InvalidOperationException("Win Streak progress per win must be positive.");
            if (m_Checkpoints == null || m_Checkpoints.Length == 0)
                throw new InvalidOperationException("Win Streak needs at least one checkpoint reward.");

            int previousCheckpoint = 0;
            var rewardIds = new HashSet<int>();
            foreach (WinStreakCheckpointDefinition checkpoint in m_Checkpoints)
            {
                if (checkpoint == null) throw new InvalidOperationException("Win Streak contains a null checkpoint.");
                checkpoint.Validate(previousCheckpoint);
                if (!rewardIds.Add(checkpoint.RewardId))
                    throw new InvalidOperationException($"Win Streak reward ID {checkpoint.RewardId} is repeated.");
                previousCheckpoint = checkpoint.Checkpoint;
            }
        }

#if UNITY_EDITOR
        public void SetInstallationData(int eventId, string streakId, string displayName, string entryLabel,
            long startUtcUnixSeconds, long endUtcUnixSeconds, int unlockLevel, int progressPerWin,
            bool resetOnLoss, bool editorAlwaysUnlocked, WinStreakCheckpointDefinition[] checkpoints)
        {
            m_EventId = eventId;
            m_StreakId = streakId;
            m_DisplayName = displayName;
            m_EntryLabel = entryLabel;
            m_StartUtcUnixSeconds = startUtcUnixSeconds;
            m_EndUtcUnixSeconds = endUtcUnixSeconds;
            m_UnlockLevel = unlockLevel;
            m_ProgressPerWin = progressPerWin;
            m_ResetOnLoss = resetOnLoss;
            m_EditorAlwaysUnlocked = editorAlwaysUnlocked;
            m_Checkpoints = checkpoints ?? Array.Empty<WinStreakCheckpointDefinition>();
        }
#endif
    }
}
