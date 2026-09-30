using System;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    /// <summary>Mining 当前活动期次与开放时间；关卡和奖励数据继续由各自配置维护。</summary>
    [CreateAssetMenu(fileName = "MiningScheduleConfig", menuName = "GF/Activity/Mining/Schedule Config")]
    public sealed class MiningScheduleConfig : ScriptableObject
    {
        [SerializeField, Range(1, 2)] private int m_EventId = 1;
        [SerializeField] private long m_StartUtcUnixSeconds = 1788393600;
        [SerializeField] private long m_EndUtcUnixSeconds = 1790812800;

        public int EventId => m_EventId;
        public DateTimeOffset StartUtc => DateTimeOffset.FromUnixTimeSeconds(m_StartUtcUnixSeconds);
        public DateTimeOffset EndUtc => DateTimeOffset.FromUnixTimeSeconds(m_EndUtcUnixSeconds);

        public bool IsActiveAt(DateTimeOffset utcNow)
        {
            DateTimeOffset now = utcNow.ToUniversalTime();
            return now >= StartUtc && now < EndUtc;
        }

        public void ValidateConfiguration()
        {
            if (m_EventId < 1 || m_EventId > 2)
                throw new InvalidOperationException($"Mining schedule EventId={m_EventId} is invalid.");
            if (m_EndUtcUnixSeconds <= m_StartUtcUnixSeconds)
                throw new InvalidOperationException("Mining schedule end time must be after start time.");
        }
    }
}
