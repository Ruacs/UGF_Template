using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    public enum ActivityClockSource { LocalEstimated, ServerSynchronized }

    public interface IActivityClock
    {
        DateTimeOffset UtcNow { get; }
        double MonotonicSeconds { get; }
        ActivityClockSource Source { get; }
    }

    public enum ActivityStorageReadStatus { Missing, Found, Corrupt }

    public sealed class ActivityStorageReadResult
    {
        public ActivityStorageReadStatus Status { get; }
        public string Payload { get; }
        public int SchemaVersion { get; }
        public string Error { get; }

        public ActivityStorageReadResult(ActivityStorageReadStatus status, string payload = null,
            int schemaVersion = 0, string error = null)
        {
            Status = status;
            Payload = payload;
            SchemaVersion = schemaVersion;
            Error = error;
        }
    }

    /// <summary>key 是模块内的键；异常表示操作失败。模块负责数据格式、迁移和写入顺序。</summary>
    public interface IActivityStorage
    {
        UniTask<ActivityStorageReadResult> ReadAsync(string key, CancellationToken cancellationToken = default);
        UniTask WriteAsync(string key, string payload, int schemaVersion, CancellationToken cancellationToken = default);
        UniTask FlushAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>不可变事实。FactId 在同一事实的重试中保持不变，不能在订阅适配器中重新生成。</summary>
    public interface IActivityGameFact
    {
        string FactId { get; }
        string GameId { get; }
        string SessionId { get; }
        long Sequence { get; }
        DateTimeOffset OccurredAtUtc { get; }
    }

    public interface IActivityGameFacts
    {
        IDisposable Subscribe<TFact>(Action<TFact> handler) where TFact : class, IActivityGameFact;
    }

    public sealed class ActivityLevelCompletedFact : IActivityGameFact
    {
        public string FactId { get; }
        public string GameId { get; }
        public string SessionId { get; }
        public long Sequence { get; }
        public DateTimeOffset OccurredAtUtc { get; }
        public string LevelId { get; }
        public bool Won { get; }

        public ActivityLevelCompletedFact(string factId, string gameId, string sessionId, long sequence,
            DateTimeOffset occurredAtUtc, string levelId, bool won)
        {
            FactId = ActivityContract.RequireId(factId, nameof(factId));
            GameId = ActivityContract.RequireId(gameId, nameof(gameId));
            SessionId = ActivityContract.RequireId(sessionId, nameof(sessionId));
            if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
            OccurredAtUtc = occurredAtUtc.ToUniversalTime();
            LevelId = ActivityContract.RequireId(levelId, nameof(levelId));
            Won = won;
        }
    }
}
