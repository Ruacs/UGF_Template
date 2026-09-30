using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.GalaxyChallenge
{
    [Serializable]
    public sealed class GalaxyChallengeEventDefinition
    {
        [SerializeField] private int m_EventId = 2;
        [SerializeField, Min(1)] private int m_StepCount = 5;
        [SerializeField, Min(1)] private int m_IntervalSeconds = 1800;
        [SerializeField, Min(1)] private int m_RewardCoinPool = 10000;
        [SerializeField, Min(1)] private int m_JoinedUserCount = 100;
        [SerializeField, Range(0f, 1f)] private float m_AllStepClearMinRate = 0.06f;
        [SerializeField, Range(0f, 1f)] private float m_AllStepClearMaxRate = 0.07f;
        [SerializeField, Min(0.01f)] private float m_CrowdDecay = 1.05f;

        // The five-step event export contains one complete server-generated sample. Keep it as the
        // local baseline while the seven-step event remains driven by the configured survivor range.
        private static readonly int[] s_FiveStepReference = { 100, 40, 18, 12, 8, 7 };

        public int EventId => m_EventId;
        public int StepCount => m_StepCount;
        public int IntervalSeconds => m_IntervalSeconds;
        public int RewardCoinPool => m_RewardCoinPool;
        public int JoinedUserCount => m_JoinedUserCount;
        public float AllStepClearMinRate => m_AllStepClearMinRate;
        public float AllStepClearMaxRate => m_AllStepClearMaxRate;

        public GalaxyChallengeEventDefinition(int eventId, int stepCount, int intervalSeconds,
            int rewardCoinPool, int joinedUserCount, float minRate, float maxRate)
        {
            m_EventId = eventId;
            m_StepCount = stepCount;
            m_IntervalSeconds = intervalSeconds;
            m_RewardCoinPool = rewardCoinPool;
            m_JoinedUserCount = joinedUserCount;
            m_AllStepClearMinRate = minRate;
            m_AllStepClearMaxRate = maxRate;
        }

        public int EstimateUsersAtStep(int step)
        {
            int[] counts = CreateUserCounts(m_EventId);
            return counts[Mathf.Clamp(step, 0, counts.Length - 1)];
        }

        /// <summary>
        /// Creates the logical survivor count for every authored platform, including the starting
        /// platform at index zero. The seed only selects the final count inside the configured range;
        /// intermediate counts are deterministic and strictly decrease.
        /// </summary>
        public int[] CreateUserCounts(int seed)
        {
            int stepCount = Mathf.Max(1, m_StepCount);
            int joinedCount = Mathf.Max(1, m_JoinedUserCount);
            int finalCount = SelectFinalUserCount(seed, joinedCount);
            var result = new int[stepCount + 1];
            result[0] = joinedCount;
            result[stepCount] = finalCount;

            bool useFiveStepReference = stepCount == 5 && joinedCount == 100 &&
                s_FiveStepReference.Length == result.Length;
            float decay = m_CrowdDecay > 0f ? m_CrowdDecay : 1.05f;
            int previous = joinedCount;
            for (int step = 1; step < stepCount; step++)
            {
                int estimate = useFiveStepReference
                    ? s_FiveStepReference[step]
                    : Mathf.RoundToInt(finalCount + (joinedCount - finalCount) * Mathf.Exp(-decay * step));
                int minimum = Mathf.Min(previous - 1, finalCount + stepCount - step);
                int maximum = Mathf.Max(finalCount, previous - 1);
                result[step] = Mathf.Clamp(estimate, minimum, maximum);
                previous = result[step];
            }

            return result;
        }

        public bool IsValidUserCounts(IReadOnlyList<int> counts)
        {
            if (counts == null || counts.Count != m_StepCount + 1 || counts[0] != m_JoinedUserCount)
                return false;
            int minFinal = GetMinimumFinalUserCount(m_JoinedUserCount);
            int maxFinal = GetMaximumFinalUserCount(m_JoinedUserCount, minFinal);
            if (counts[counts.Count - 1] < minFinal || counts[counts.Count - 1] > maxFinal) return false;
            for (int index = 1; index < counts.Count; index++)
                if (counts[index] <= 0 || counts[index] >= counts[index - 1]) return false;
            return true;
        }

        private int SelectFinalUserCount(int seed, int joinedCount)
        {
            int minFinal = GetMinimumFinalUserCount(joinedCount);
            int maxFinal = GetMaximumFinalUserCount(joinedCount, minFinal);
            uint hash = Mix(unchecked((uint)seed) ^ unchecked((uint)(m_EventId * 397)));
            return minFinal + (int)(hash % (uint)(maxFinal - minFinal + 1));
        }

        private int GetMinimumFinalUserCount(int joinedCount) => Mathf.Clamp(
            Mathf.RoundToInt(joinedCount * m_AllStepClearMinRate), 1, joinedCount);

        private int GetMaximumFinalUserCount(int joinedCount, int minimum) => Mathf.Clamp(
            Mathf.RoundToInt(joinedCount * m_AllStepClearMaxRate), minimum, joinedCount);

        private static uint Mix(uint value)
        {
            value += 0x9e3779b9u;
            value = (value ^ (value >> 16)) * 0x85ebca6bu;
            value = (value ^ (value >> 13)) * 0xc2b2ae35u;
            return value ^ (value >> 16);
        }

        internal void Validate()
        {
            if (m_EventId <= 0) throw new InvalidOperationException("Galaxy Challenge event ID must be positive.");
            if (m_StepCount <= 0) throw new InvalidOperationException("Galaxy Challenge step count must be positive.");
            if (m_IntervalSeconds <= 0) throw new InvalidOperationException("Galaxy Challenge interval must be positive.");
            if (m_RewardCoinPool <= 0) throw new InvalidOperationException("Galaxy Challenge prize pool must be positive.");
            if (m_JoinedUserCount <= 0) throw new InvalidOperationException("Galaxy Challenge player count must be positive.");
            if (m_AllStepClearMinRate < 0f || m_AllStepClearMaxRate > 1f ||
                m_AllStepClearMinRate > m_AllStepClearMaxRate)
                throw new InvalidOperationException("Galaxy Challenge clear-rate range is invalid.");
        }
    }

    /// <summary>Galaxy Challenge 的静态 UI 与规则配置；运行进度保存在活动模块存档中。</summary>
    [CreateAssetMenu(fileName = "GalaxyChallengeActivityConfig",
        menuName = "GF/Activity/Galaxy Challenge/Config")]
    public sealed class GalaxyChallengeActivityConfig : ScriptableObject
    {
        [SerializeField] private string m_ChallengeId = "hexa_galaxy_challenge_202609";
        [SerializeField] private string m_DisplayName = "GALAXY CHALLENGE";
        [SerializeField] private string m_EntryLabel = "GALAXY";
        [SerializeField, Min(0)] private int m_UnlockLevel = 40;
        [SerializeField] private bool m_EditorAlwaysOpen = true;
        [SerializeField, Min(1)] private int m_ActivityWindowSeconds = 36000;
        [SerializeField] private int m_DefaultEventId = 2;
        [SerializeField] private Sprite m_EntryIcon;
        [SerializeField] private GalaxyChallengeEventDefinition[] m_Events =
        {
            new GalaxyChallengeEventDefinition(1, 7, 1800, 10000, 100, 0.04f, 0.06f),
            new GalaxyChallengeEventDefinition(2, 5, 1800, 10000, 100, 0.06f, 0.07f)
        };

        public string ChallengeId => m_ChallengeId;
        public string DisplayName => m_DisplayName;
        public string EntryLabel => m_EntryLabel;
        public int UnlockLevel => m_UnlockLevel;
        public bool EditorAlwaysOpen => m_EditorAlwaysOpen;
        public int ActivityWindowSeconds => m_ActivityWindowSeconds;
        public Sprite EntryIcon => m_EntryIcon;

        public GalaxyChallengeEventDefinition GetCurrentEvent()
        {
            GalaxyChallengeEventDefinition configured = GetEvent(m_DefaultEventId);
            if (configured != null) return configured;
            return m_Events != null && m_Events.Length > 0 ? m_Events[0] : null;
        }

        public GalaxyChallengeEventDefinition GetEvent(int eventId)
        {
            if (m_Events == null) return null;
            foreach (GalaxyChallengeEventDefinition definition in m_Events)
                if (definition != null && definition.EventId == eventId) return definition;
            return null;
        }

        public bool IsOpenAt(DateTimeOffset utcNow)
        {
#if UNITY_EDITOR
            if (m_EditorAlwaysOpen) return true;
#endif
            DayOfWeek day = utcNow.ToUniversalTime().DayOfWeek;
            return day == DayOfWeek.Friday || day == DayOfWeek.Saturday || day == DayOfWeek.Sunday;
        }

        public DateTimeOffset GetWindowEndUtc(DateTimeOffset utcNow)
        {
            DateTimeOffset now = utcNow.ToUniversalTime();
            if (m_EditorAlwaysOpen)
            {
                long editorWindowSeconds = Math.Max(1, m_ActivityWindowSeconds);
                long nextBoundary = ((now.ToUnixTimeSeconds() / editorWindowSeconds) + 1L) * editorWindowSeconds;
                return DateTimeOffset.FromUnixTimeSeconds(nextBoundary);
            }
            DateTimeOffset scheduleStart = GetWeeklyWindowStartUtc(now);
            DateTimeOffset scheduleEnd = GetWeeklyWindowEndUtc(now);
            if (now < scheduleStart)
                return scheduleStart.AddSeconds(Mathf.Min(m_ActivityWindowSeconds,
                    (int)(scheduleEnd - scheduleStart).TotalSeconds));

            long elapsedSeconds = Math.Max(0L, (long)(now - scheduleStart).TotalSeconds);
            long windowSeconds = Math.Max(1, m_ActivityWindowSeconds);
            long windowIndex = elapsedSeconds / windowSeconds;
            DateTimeOffset nextWindow = scheduleStart.AddSeconds((windowIndex + 1L) * windowSeconds);
            return nextWindow < scheduleEnd ? nextWindow : scheduleEnd;
        }

        private static DateTimeOffset GetWeeklyWindowStartUtc(DateTimeOffset now)
        {
            int daysSinceFriday = ((int)now.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
            return new DateTimeOffset(now.Date.AddDays(-daysSinceFriday), TimeSpan.Zero);
        }

        private static DateTimeOffset GetWeeklyWindowEndUtc(DateTimeOffset now)
        {
            DateTimeOffset scheduleStart = GetWeeklyWindowStartUtc(now);
            DateTimeOffset end = scheduleStart.AddDays(3);
            return end > now ? end : end.AddDays(7);
        }

        public void ValidateConfiguration()
        {
            ActivityContract.RequireId(m_ChallengeId, nameof(m_ChallengeId));
            if (string.IsNullOrWhiteSpace(m_DisplayName))
                throw new InvalidOperationException("Galaxy Challenge display name is required.");
            if (string.IsNullOrWhiteSpace(m_EntryLabel))
                throw new InvalidOperationException("Galaxy Challenge entry label is required.");
            if (m_UnlockLevel < 0) throw new InvalidOperationException("Unlock level cannot be negative.");
            if (m_ActivityWindowSeconds <= 0) throw new InvalidOperationException("Activity window must be positive.");
            if (m_Events == null || m_Events.Length == 0)
                throw new InvalidOperationException("Galaxy Challenge needs at least one event definition.");

            foreach (GalaxyChallengeEventDefinition definition in m_Events)
            {
                if (definition == null) throw new InvalidOperationException("Galaxy Challenge contains a null event.");
                definition.Validate();
            }
            if (GetCurrentEvent() == null)
                throw new InvalidOperationException("Galaxy Challenge default event is not configured.");
        }
    }
}
