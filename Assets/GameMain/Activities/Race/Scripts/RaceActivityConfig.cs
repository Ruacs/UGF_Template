using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.Race
{
    /// <summary>报名入口保留 APK 的三种语义；本地 MVP 仅默认启用 Free，不假定广告或体力的真实分支。</summary>
    public enum RaceStartType
    {
        Free,
        Ad,
        InfiniteHeart
    }

    [Serializable]
    public sealed class RaceRankRewardDefinition
    {
        [SerializeField] private string m_Label;
        [SerializeField] private int m_MinRank = 1;
        [SerializeField] private int m_MaxRank = 1;
        [SerializeField] private RaceRewardDefinition m_Reward;

        public string Label => m_Label;
        public int MinRank => m_MinRank;
        public int MaxRank => m_MaxRank;
        public RaceRewardDefinition Reward => m_Reward;

        public RaceRankRewardDefinition(string label, int minRank, int maxRank, RaceRewardDefinition reward)
        {
            m_Label = label;
            m_MinRank = minRank;
            m_MaxRank = maxRank;
            m_Reward = reward;
        }

        public bool ContainsRank(int rank) => rank >= m_MinRank && rank <= m_MaxRank;

        internal void Validate(int racerCount)
        {
            if (string.IsNullOrWhiteSpace(m_Label))
                throw new InvalidOperationException("Race rank reward needs a display label.");
            if (m_MinRank <= 0 || m_MaxRank < m_MinRank || m_MaxRank > racerCount)
                throw new InvalidOperationException($"Race reward '{m_Label}' has an invalid rank range.");
            if (m_Reward == null)
                throw new InvalidOperationException($"Race reward '{m_Label}' has no reward bundle.");
            m_Reward.ValidateEntries();
        }
    }

    /// <summary>一场本地 Race 的固定数值。活动运行状态和 NPC 随机结果不保存在此资产中。</summary>
    [CreateAssetMenu(fileName = "RaceActivityConfig", menuName = "GF/Activity/Race/Config")]
    public sealed class RaceActivityConfig : ScriptableObject
    {
        [SerializeField] private string m_RaceId = "hexa_race_local";
        [SerializeField] private string m_DisplayName = "HEXA RACE";
        [SerializeField] private string m_EntryLabel = "RACE";
        [SerializeField] private long m_StartUtcUnixSeconds;
        [SerializeField] private long m_EndUtcUnixSeconds;
        [SerializeField] private int m_UnlockLevel = 1;
        [SerializeField] private bool m_EditorAlwaysUnlocked = true;
        [SerializeField] private float m_TermHours = 0.5f;
        [SerializeField] private float m_InfiniteHeartHours = 0.25f;
        [SerializeField] private int m_TargetStageDifference = 10;
        [SerializeField] private int m_NpcCount = 4;
        [SerializeField] private float m_NpcMinGoalRate = 1.05f;
        [SerializeField] private float m_NpcMaxGoalRate = 1.35f;
        [SerializeField] private float m_PlayerMinGoalRate = 0.75f;
        [SerializeField] private float m_PlayerMaxGoalRate = 1.25f;
        [SerializeField] private float m_NpcInitialProgress;
        [SerializeField] private RaceStartType m_DefaultStartType = RaceStartType.Free;
        [SerializeField] private RaceRankRewardDefinition[] m_RankRewards = Array.Empty<RaceRankRewardDefinition>();

        public string RaceId => m_RaceId;
        public string DisplayName => m_DisplayName;
        public string EntryLabel => m_EntryLabel;
        public int UnlockLevel => m_UnlockLevel;
        public bool EditorAlwaysUnlocked => m_EditorAlwaysUnlocked;
        public float TermHours => m_TermHours;
        public float InfiniteHeartHours => m_InfiniteHeartHours;
        public int TargetStageDifference => m_TargetStageDifference;
        public int NpcCount => m_NpcCount;
        public int RacerCount => m_NpcCount + 1;
        public float NpcMinGoalRate => m_NpcMinGoalRate;
        public float NpcMaxGoalRate => m_NpcMaxGoalRate;
        public float PlayerMinGoalRate => m_PlayerMinGoalRate;
        public float PlayerMaxGoalRate => m_PlayerMaxGoalRate;
        public float NpcInitialProgress => m_NpcInitialProgress;
        public RaceStartType DefaultStartType => m_DefaultStartType;
        public IReadOnlyList<RaceRankRewardDefinition> RankRewards => m_RankRewards;
        public DateTimeOffset StartUtc => DateTimeOffset.FromUnixTimeSeconds(m_StartUtcUnixSeconds);
        public DateTimeOffset EndUtc => DateTimeOffset.FromUnixTimeSeconds(m_EndUtcUnixSeconds);

        public bool IsActiveAt(DateTimeOffset utcNow)
        {
            DateTimeOffset now = utcNow.ToUniversalTime();
            return now >= StartUtc && now < EndUtc;
        }

        public DateTimeOffset GetRaceEndUtc(DateTimeOffset joinedAtUtc)
        {
            DateTimeOffset termEnd = joinedAtUtc.ToUniversalTime().AddHours(m_TermHours);
            return termEnd < EndUtc ? termEnd : EndUtc;
        }

        public RaceRankRewardDefinition GetRewardForRank(int rank)
        {
            foreach (RaceRankRewardDefinition reward in m_RankRewards)
                if (reward != null && reward.ContainsRank(rank)) return reward;
            return null;
        }

        public void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(m_RaceId)) throw new InvalidOperationException("Race needs a stable race ID.");
            if (string.IsNullOrWhiteSpace(m_DisplayName) || string.IsNullOrWhiteSpace(m_EntryLabel))
                throw new InvalidOperationException("Race needs display and entry labels.");
            if (m_EndUtcUnixSeconds <= m_StartUtcUnixSeconds)
                throw new InvalidOperationException("Race end time must be later than start time.");
            if (m_UnlockLevel < 0) throw new InvalidOperationException("Race unlock level cannot be negative.");
            if (m_TermHours <= 0f) throw new InvalidOperationException("Race term hours must be positive.");
            if (m_InfiniteHeartHours <= 0f) throw new InvalidOperationException("Race infinite heart hours must be positive.");
            if (m_TargetStageDifference <= 0) throw new InvalidOperationException("Race target stage difference must be positive.");
            if (m_NpcCount != 4) throw new InvalidOperationException("The Hexa Race presentation requires exactly four NPC racers.");
            if (m_NpcMinGoalRate <= 0f || m_NpcMaxGoalRate < m_NpcMinGoalRate)
                throw new InvalidOperationException("Race NPC goal rates are invalid.");
            if (m_PlayerMinGoalRate <= 0f || m_PlayerMaxGoalRate < m_PlayerMinGoalRate)
                throw new InvalidOperationException("Race player goal rates are invalid.");
            if (m_NpcInitialProgress < 0f || m_NpcInitialProgress > 0.2f)
                throw new InvalidOperationException("Race NPC initial progress must be between 0 and 0.2.");
            if (!Enum.IsDefined(typeof(RaceStartType), m_DefaultStartType))
                throw new InvalidOperationException("Race default start type is invalid.");
            if (m_RankRewards == null || m_RankRewards.Length == 0)
                throw new InvalidOperationException("Race needs rank rewards.");

            var coveredRanks = new HashSet<int>();
            foreach (RaceRankRewardDefinition reward in m_RankRewards)
            {
                if (reward == null) throw new InvalidOperationException("Race has a null rank reward definition.");
                reward.Validate(RacerCount);
                for (int rank = reward.MinRank; rank <= reward.MaxRank; rank++)
                    if (!coveredRanks.Add(rank)) throw new InvalidOperationException($"Race rank {rank} has overlapping rewards.");
            }

            for (int rank = 1; rank <= RacerCount; rank++)
                if (!coveredRanks.Contains(rank)) throw new InvalidOperationException($"Race rank {rank} has no reward.");
        }

#if UNITY_EDITOR
        public void SetInstallationData(string raceId, string displayName, string entryLabel,
            long startUtcUnixSeconds, long endUtcUnixSeconds, int unlockLevel, bool editorAlwaysUnlocked,
            float termHours, int targetStageDifference, RaceStartType defaultStartType,
            RaceRankRewardDefinition[] rankRewards)
        {
            m_RaceId = raceId;
            m_DisplayName = displayName;
            m_EntryLabel = entryLabel;
            m_StartUtcUnixSeconds = startUtcUnixSeconds;
            m_EndUtcUnixSeconds = endUtcUnixSeconds;
            m_UnlockLevel = unlockLevel;
            m_EditorAlwaysUnlocked = editorAlwaysUnlocked;
            m_TermHours = termHours;
            m_InfiniteHeartHours = 0.25f;
            m_TargetStageDifference = targetStageDifference;
            m_DefaultStartType = defaultStartType;
            m_NpcCount = 4;
            m_NpcMinGoalRate = 1.05f;
            m_NpcMaxGoalRate = 1.35f;
            m_PlayerMinGoalRate = 0.75f;
            m_PlayerMaxGoalRate = 1.25f;
            m_NpcInitialProgress = 0f;
            m_RankRewards = rankRewards ?? Array.Empty<RaceRankRewardDefinition>();
        }
#endif
    }
}
