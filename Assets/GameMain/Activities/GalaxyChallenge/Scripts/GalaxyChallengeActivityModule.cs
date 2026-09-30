using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lokas.Activities.GalaxyChallenge
{
    public static class GalaxyChallengePageKeys
    {
        public const string Start = "start";
        public const string Main = "main";
        public const string Details = "details";
        public const string End = "end";
        public const string Interval = "interval";
    }

    public enum GalaxyChallengeState
    {
        Inactive,
        Locked,
        Ready,
        Running,
        Interval,
        Finished
    }

    public sealed class GalaxyChallengeSnapshot
    {
        public string DisplayName { get; }
        public GalaxyChallengeState State { get; }
        public bool IsActive { get; }
        public bool IsUnlocked { get; }
        public bool IsJoined { get; }
        public bool IsFailed { get; }
        public bool CanReceiveReward => !IsFailed && CurrentProgress >= StepCount && !HasPendingProgress;
        public int EventId { get; }
        public int CurrentProgress { get; }
        public int BestProgress { get; }
        public int StepCount { get; }
        public int JoinedUserCount { get; }
        public int CurrentUserCount { get; }
        public int LastPresentedProgress { get; }
        public int CrowdSeed { get; }
        public bool HasPendingProgress => LastPresentedProgress < CurrentProgress;
        public int RewardCoinPool { get; }
        public int RewardCoinShare => Mathf.Max(1, CurrentUserCount > 0 ? RewardCoinPool / CurrentUserCount : RewardCoinPool);
        public int IntervalSeconds { get; }
        public DateTimeOffset EndUtc { get; }
        public DateTimeOffset? IntervalEndUtc { get; }
        public IReadOnlyList<int> UserCountsInStep { get; }

        internal GalaxyChallengeSnapshot(GalaxyChallengeActivityConfig config,
            GalaxyChallengeEventDefinition definition, GalaxyChallengeState state, bool active, bool unlocked,
            GalaxyChallengePersistedState persisted, DateTimeOffset endUtc, DateTimeOffset? intervalEndUtc)
        {
            DisplayName = config.DisplayName;
            State = state;
            IsActive = active;
            IsUnlocked = unlocked;
            IsJoined = persisted.joined;
            IsFailed = persisted.isFailed;
            EventId = definition.EventId;
            CurrentProgress = Mathf.Clamp(persisted.currentProgress, 0, definition.StepCount);
            BestProgress = Mathf.Clamp(persisted.bestProgress, 0, definition.StepCount);
            StepCount = definition.StepCount;
            JoinedUserCount = definition.JoinedUserCount;
            int[] counts = definition.IsValidUserCounts(persisted.userCountsInStep)
                ? persisted.userCountsInStep.ToArray()
                : definition.CreateUserCounts(persisted.crowdSeed);
            UserCountsInStep = Array.AsReadOnly(counts);
            CurrentUserCount = counts[CurrentProgress];
            LastPresentedProgress = Mathf.Clamp(persisted.lastPresentedProgress, 0, CurrentProgress);
            CrowdSeed = persisted.crowdSeed;
            RewardCoinPool = definition.RewardCoinPool;
            IntervalSeconds = definition.IntervalSeconds;
            EndUtc = endUtc;
            IntervalEndUtc = intervalEndUtc;
        }
    }

    [Serializable]
    internal sealed class GalaxyChallengePersistedState
    {
        public int eventId;
        public int currentProgress;
        public int bestProgress;
        public bool joined;
        public bool isFailed;
        public long intervalEndUtcUnixSeconds;
        public int crowdSeed;
        public int lastPresentedProgress;
        public List<int> userCountsInStep = new List<int>();
        public List<string> processedFactIds = new List<string>();
    }

    /// <summary>
    /// Galaxy Challenge 本地活动模块。页面只读取快照；所有可见对象、布局和适配均由 Prefab 持有。
    /// </summary>
    public sealed class GalaxyChallengeActivityModule : IActivityModule, IActivityEntryProvider
    {
        public const string Id = "galaxy_challenge";
        public const string EntryId = "galaxy-challenge";
        private const string StateKey = "state";
        private const int StateSchemaVersion = 3;
        private bool m_IsClaiming;

        private readonly GalaxyChallengeActivityConfig m_Config;
        private readonly IReadOnlyList<string> m_GameIds;
        private readonly HashSet<string> m_ProcessedFactIds = new HashSet<string>(StringComparer.Ordinal);
        private IActivityContext m_Context;
        private IDisposable m_LevelCompletedSubscription;
        private GalaxyChallengePersistedState m_State;
        private bool m_IsInitialized;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GalaxyChallengeTestModeModule m_TestModeModule;
#endif

        public string ModuleId => Id;
        public GalaxyChallengeActivityConfig Config => m_Config;
        public event Action EntriesChanged;
        public event Action StateChanged;

        public GalaxyChallengeActivityModule(GalaxyChallengeActivityConfig config,
            IReadOnlyList<string> gameIds = null)
        {
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_GameIds = gameIds == null ? Array.Empty<string>() : gameIds.ToArray();
        }

        public async UniTask InitializeAsync(IActivityContext context, CancellationToken cancellationToken)
        {
            if (m_IsInitialized) throw new InvalidOperationException("Galaxy Challenge is already initialized.");
            m_Config.ValidateConfiguration();
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            await LoadStateAsync(cancellationToken);
            m_LevelCompletedSubscription = m_Context.GameFacts.Subscribe<ActivityLevelCompletedFact>(OnLevelCompleted);
            m_IsInitialized = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterTestModeModule();
#endif
            NotifyChanged();
        }

        public async UniTask ShutdownAsync()
        {
            m_LevelCompletedSubscription?.Dispose();
            m_LevelCompletedSubscription = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnregisterTestModeModule();
#endif
            if (m_Context != null && m_State != null) await SaveStateAsync(CancellationToken.None);
            m_IsInitialized = false;
            m_Context = null;
        }

        public IReadOnlyList<ActivityEntryInfo> GetEntries()
        {
            if (!m_IsInitialized) return Array.Empty<ActivityEntryInfo>();
            GalaxyChallengeSnapshot snapshot = GetSnapshot();
            return new[]
            {
                new ActivityEntryInfo(EntryId, m_Config.EntryLabel, "chase_icon_all",
                    m_Config.ChallengeId, snapshot.IsActive, snapshot.IsUnlocked,
                    snapshot.IsUnlocked ? null : $"达到第 {m_Config.UnlockLevel} 关后开启",
                    snapshot.State == GalaxyChallengeState.Finished ? 1 : 0, 330, snapshot.EndUtc)
            };
        }

        public async UniTask OpenEntryAsync(string entryId, CancellationToken cancellationToken)
        {
            if (!string.Equals(entryId, EntryId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Unknown Galaxy Challenge entry '{entryId}'.");
            GalaxyChallengeSnapshot snapshot = GetSnapshot();
            if (!snapshot.IsActive) throw new InvalidOperationException("Galaxy Challenge is not active.");
            if (!snapshot.IsUnlocked)
                throw new InvalidOperationException($"Galaxy Challenge unlocks at level {m_Config.UnlockLevel}.");

            string pageKey = snapshot.HasPendingProgress
                ? GalaxyChallengePageKeys.Main
                : snapshot.State switch
            {
                GalaxyChallengeState.Interval => GalaxyChallengePageKeys.Interval,
                GalaxyChallengeState.Ready => GalaxyChallengePageKeys.Start,
                GalaxyChallengeState.Finished => GalaxyChallengePageKeys.End,
                _ => GalaxyChallengePageKeys.Main
            };
            await OpenPageAsync(pageKey, cancellationToken);
        }

        public GalaxyChallengeSnapshot GetSnapshot()
        {
            EnsureInitialized();
            DateTimeOffset now = m_Context.Clock.UtcNow;
            GalaxyChallengeEventDefinition definition = ResolveEvent();
            EnsureCrowdState(definition, false);
            DateTimeOffset? intervalEnd = m_State.intervalEndUtcUnixSeconds > 0
                ? DateTimeOffset.FromUnixTimeSeconds(m_State.intervalEndUtcUnixSeconds)
                : null;
            bool active = m_Config.IsOpenAt(now);
            bool unlocked = IsUnlocked();
            GalaxyChallengeState state;
            if (!active) state = GalaxyChallengeState.Inactive;
            else if (!unlocked) state = GalaxyChallengeState.Locked;
            else if (intervalEnd.HasValue && intervalEnd.Value > now) state = GalaxyChallengeState.Interval;
            else if (m_State.currentProgress >= definition.StepCount && !m_State.isFailed)
                state = GalaxyChallengeState.Finished;
            else state = m_State.joined ? GalaxyChallengeState.Running : GalaxyChallengeState.Ready;

            return new GalaxyChallengeSnapshot(m_Config, definition, state, active, unlocked, m_State,
                m_Config.GetWindowEndUtc(now), intervalEnd);
        }

        public async UniTask JoinAsync(CancellationToken cancellationToken = default)
        {
            GalaxyChallengeSnapshot snapshot = GetSnapshot();
            if (!snapshot.IsActive || !snapshot.IsUnlocked)
                throw new InvalidOperationException("Galaxy Challenge cannot be joined right now.");
            if (snapshot.IntervalEndUtc.HasValue && snapshot.IntervalEndUtc.Value > m_Context.Clock.UtcNow)
                throw new InvalidOperationException("Galaxy Challenge is in its retry interval.");
            GalaxyChallengeEventDefinition definition = ResolveEvent();
            if (!m_State.joined)
            {
                m_State.currentProgress = 0;
                m_State.lastPresentedProgress = 0;
                m_State.isFailed = false;
                GenerateCrowdState(definition);
            }
            m_State.joined = true;
            m_State.isFailed = false;
            m_State.intervalEndUtcUnixSeconds = 0;
            await SaveStateAsync(cancellationToken);
            NotifyChanged();
        }

        public UniTask OpenMainAsync(CancellationToken cancellationToken = default) =>
            OpenPageAsync(GalaxyChallengePageKeys.Main, cancellationToken);

        public UniTask OpenRulesAsync(CancellationToken cancellationToken = default) =>
            OpenPageAsync(GalaxyChallengePageKeys.Details, cancellationToken);

        public UniTask OpenEndAsync(CancellationToken cancellationToken = default) =>
            OpenPageAsync(GalaxyChallengePageKeys.End, cancellationToken);

        /// <summary>显式关闭当前轮次后进入 30 分钟间隔。</summary>
        public async UniTask EndRoundAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            GalaxyChallengeEventDefinition definition = ResolveEvent();
            if (!m_State.joined || m_State.currentProgress >= definition.StepCount) return;
            BeginInterval(definition, resetProgress: false);
            await SaveStateAsync(cancellationToken);
            NotifyChanged();
        }

        /// <summary>领取通关者平分的金币，并为下一轮建立 30 分钟等待。</summary>
        public async UniTask<ActivityRewardReceipt> ClaimRewardAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            GalaxyChallengeSnapshot snapshot = GetSnapshot();
            string grantId = BuildRewardGrantId(snapshot);
            if (snapshot.IsFailed || snapshot.CurrentProgress < snapshot.StepCount)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Failed,
                    reason: "Galaxy Challenge has not been completed.");
            if (m_IsClaiming)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Pending,
                    reason: "Galaxy Challenge reward is already being claimed.");
            if (!m_Context.Rewards.CanGrant("currency.money", "profile"))
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Unsupported,
                    reason: "Currency rewards are not mapped by this game.");

            m_IsClaiming = true;
            try
            {
                ActivityRewardReceipt receipt = await m_Context.Rewards.GrantAsync(
                    new ActivityRewardRequest(grantId, m_Config.ChallengeId, "profile",
                        new[] { new ActivityRewardItem("currency.money", "count", snapshot.RewardCoinShare) }),
                    cancellationToken);
                if (receipt.Status == ActivityRewardStatus.Granted ||
                    receipt.Status == ActivityRewardStatus.AlreadyGranted)
                {
                    BeginInterval(ResolveEvent(), resetProgress: true);
                    await SaveStateAsync(cancellationToken);
                    NotifyChanged();
                }
                return receipt;
            }
            finally
            {
                m_IsClaiming = false;
            }
        }

        /// <summary>主页面完成跳跃与淘汰演出后确认进度，避免再次打开时重复播放。</summary>
        public void MarkProgressPresented(int progress)
        {
            EnsureInitialized();
            int presented = Mathf.Clamp(progress, 0, m_State.currentProgress);
            if (m_State.lastPresentedProgress == presented) return;
            m_State.lastPresentedProgress = presented;
            SaveStateAsync(m_Context.LifetimeToken).Forget(Debug.LogException);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal string GetTestModeGameId()
        {
            foreach (string gameId in m_GameIds)
                if (!string.IsNullOrWhiteSpace(gameId)) return gameId;
            return null;
        }

        internal async UniTask SetTestEventAsync(int eventId)
        {
            if (!m_IsInitialized) return;
            GalaxyChallengeEventDefinition definition = m_Config.GetEvent(eventId);
            if (definition == null)
                throw new ArgumentOutOfRangeException(nameof(eventId), eventId,
                    "Galaxy Challenge test event is not configured.");
            ResetTestState(definition);
            await SaveStateAsync(CancellationToken.None);
            NotifyChanged();
        }

        internal async UniTask JoinTestAsync()
        {
            if (!m_IsInitialized) return;
            GalaxyChallengeEventDefinition definition = ResolveEvent();
            if (m_State.currentProgress >= definition.StepCount) ResetTestState(definition);
            if (!m_State.joined) GenerateCrowdState(definition);
            m_State.joined = true;
            m_State.isFailed = false;
            m_State.intervalEndUtcUnixSeconds = 0;
            await SaveStateAsync(CancellationToken.None);
            NotifyChanged();
        }

        internal async UniTask ResetTestStateAsync()
        {
            if (!m_IsInitialized) return;
            ResetTestState(ResolveEvent());
            await SaveStateAsync(CancellationToken.None);
            NotifyChanged();
        }

        internal void ProcessTestFacts(IReadOnlyList<ActivityLevelCompletedFact> facts)
        {
            if (!m_IsInitialized || facts == null || facts.Count == 0) return;
            bool changed = false;
            foreach (ActivityLevelCompletedFact fact in facts)
                changed |= ApplyLevelCompletedFact(fact, ignoreSchedule: true);
            if (changed) PersistStateAndNotify();
        }

        private void ResetTestState(GalaxyChallengeEventDefinition definition)
        {
            m_ProcessedFactIds.Clear();
            m_State = new GalaxyChallengePersistedState { eventId = definition.EventId };
            EnsureCrowdState(definition, true);
        }
#endif

        private void OnLevelCompleted(ActivityLevelCompletedFact fact)
        {
            if (ApplyLevelCompletedFact(fact, ignoreSchedule: false)) PersistStateAndNotify();
        }

        private bool ApplyLevelCompletedFact(ActivityLevelCompletedFact fact, bool ignoreSchedule)
        {
            if (!m_IsInitialized || fact == null || !m_State.joined ||
                (!ignoreSchedule && !m_Config.IsOpenAt(m_Context.Clock.UtcNow)) ||
                !IsGameSource(fact.GameId) || !m_ProcessedFactIds.Add(fact.FactId)) return false;

            GalaxyChallengeEventDefinition definition = ResolveEvent();
            if (fact.Won)
            {
                m_State.currentProgress = Mathf.Min(definition.StepCount, m_State.currentProgress + 1);
                m_State.bestProgress = Mathf.Max(m_State.bestProgress, m_State.currentProgress);
                m_State.isFailed = false;
                if (m_State.currentProgress >= definition.StepCount) m_State.joined = false;
            }
            else
            {
                // 失败也先推进到下一格，再在同一段演出中淘汰玩家头像。
                m_State.currentProgress = Mathf.Min(definition.StepCount, m_State.currentProgress + 1);
                m_State.joined = false;
                m_State.isFailed = true;
                BeginInterval(definition, resetProgress: false);
            }
            return true;
        }

        private GalaxyChallengeEventDefinition ResolveEvent()
        {
            GalaxyChallengeEventDefinition definition = m_Config.GetEvent(m_State.eventId) ?? m_Config.GetCurrentEvent();
            if (definition == null) throw new InvalidOperationException("Galaxy Challenge has no active event definition.");
            if (m_State.eventId != definition.EventId)
            {
                m_State.eventId = definition.EventId;
                m_State.currentProgress = 0;
                m_State.bestProgress = 0;
                m_State.joined = false;
                m_State.isFailed = false;
                m_State.intervalEndUtcUnixSeconds = 0;
                m_State.crowdSeed = 0;
                m_State.lastPresentedProgress = 0;
                m_State.userCountsInStep = new List<int>();
            }
            return definition;
        }

        private bool IsUnlocked()
        {
#if UNITY_EDITOR
            if (m_Config.EditorAlwaysOpen) return true;
#endif
            if (m_Config.UnlockLevel <= 0) return true;
            GameMode primaryMode = GameEntry.GameManager?.PrimaryGameMode ?? GameMode.None;
            return GameEntry.SubGames?.Get(primaryMode)?.CurrentLevel >= m_Config.UnlockLevel;
        }

        private bool IsGameSource(string gameId)
        {
            foreach (string registered in m_GameIds)
                if (string.Equals(registered, gameId, StringComparison.Ordinal)) return true;
            return false;
        }

        private async UniTask LoadStateAsync(CancellationToken cancellationToken)
        {
            m_ProcessedFactIds.Clear();
            ActivityStorageReadResult read = await m_Context.Storage.ReadAsync(StateKey, cancellationToken);
            if (read.Status == ActivityStorageReadStatus.Corrupt)
                throw new InvalidOperationException("Galaxy Challenge save data is corrupt: " + read.Error);
            if (read.Status == ActivityStorageReadStatus.Missing)
            {
                ResetState();
                return;
            }

            m_State = JsonUtility.FromJson<GalaxyChallengePersistedState>(read.Payload);
            if (m_State == null || read.SchemaVersion > StateSchemaVersion)
            {
                ResetState();
                return;
            }
            bool migrated = read.SchemaVersion < StateSchemaVersion;
            m_State.processedFactIds ??= new List<string>();
            foreach (string factId in m_State.processedFactIds)
                if (!string.IsNullOrWhiteSpace(factId)) m_ProcessedFactIds.Add(factId);
            GalaxyChallengeEventDefinition definition = ResolveEvent();
            EnsureCrowdState(definition, migrated);
            if (migrated) await SaveStateAsync(cancellationToken);
        }

        private void ResetState()
        {
            m_ProcessedFactIds.Clear();
            GalaxyChallengeEventDefinition definition = m_Config.GetCurrentEvent();
            m_State = new GalaxyChallengePersistedState { eventId = definition?.EventId ?? 0 };
            if (definition != null) EnsureCrowdState(definition, true);
        }

        private void GenerateCrowdState(GalaxyChallengeEventDefinition definition)
        {
            long timestamp = m_Context?.Clock.UtcNow.ToUnixTimeSeconds() ?? 0L;
            m_State.crowdSeed = unchecked((int)(timestamp ^ ((long)definition.EventId << 24) ^
                ((long)m_State.bestProgress << 8)));
            m_State.userCountsInStep = definition.CreateUserCounts(m_State.crowdSeed).ToList();
            m_State.lastPresentedProgress = Mathf.Clamp(m_State.currentProgress, 0, definition.StepCount);
        }

        private void EnsureCrowdState(GalaxyChallengeEventDefinition definition, bool alignPresentedProgress)
        {
            m_State.userCountsInStep ??= new List<int>();
            if (!definition.IsValidUserCounts(m_State.userCountsInStep))
                m_State.userCountsInStep = definition.CreateUserCounts(m_State.crowdSeed).ToList();
            m_State.currentProgress = Mathf.Clamp(m_State.currentProgress, 0, definition.StepCount);
            m_State.bestProgress = Mathf.Clamp(Mathf.Max(m_State.bestProgress, m_State.currentProgress), 0,
                definition.StepCount);
            if (alignPresentedProgress) m_State.lastPresentedProgress = m_State.currentProgress;
            m_State.lastPresentedProgress = Mathf.Clamp(m_State.lastPresentedProgress, 0,
                m_State.currentProgress);
        }

        private async UniTask SaveStateAsync(CancellationToken cancellationToken)
        {
            if (m_Context == null || m_State == null) return;
            m_State.processedFactIds = m_ProcessedFactIds.OrderBy(id => id, StringComparer.Ordinal).ToList();
            await m_Context.Storage.WriteAsync(StateKey, JsonUtility.ToJson(m_State), StateSchemaVersion,
                cancellationToken);
            await m_Context.Storage.FlushAsync(cancellationToken);
        }

        private void BeginInterval(GalaxyChallengeEventDefinition definition, bool resetProgress)
        {
            m_State.joined = false;
            m_State.intervalEndUtcUnixSeconds = m_Context.Clock.UtcNow
                .AddSeconds(definition.IntervalSeconds).ToUnixTimeSeconds();
            if (!resetProgress) return;
            m_State.currentProgress = 0;
            m_State.lastPresentedProgress = 0;
            m_State.bestProgress = 0;
            m_State.isFailed = false;
            GenerateCrowdState(definition);
        }

        private string BuildRewardGrantId(GalaxyChallengeSnapshot snapshot) =>
            $"{m_Config.ChallengeId}:{snapshot.EventId}:{snapshot.CrowdSeed}";

        private async UniTask OpenPageAsync(string pageKey, CancellationToken cancellationToken)
        {
            EnsureInitialized();
            ActivityUiOpenResult result = await m_Context.UI.OpenAsync(
                new ActivityPageRequest(pageKey, arguments: this), cancellationToken);
            if (result.Status != ActivityUiOpenStatus.Opened && result.Status != ActivityUiOpenStatus.Reused)
                throw new InvalidOperationException(result.Reason ?? "The Galaxy Challenge page could not be opened.");
        }

        private void PersistStateAndNotify()
        {
            SaveStateAsync(m_Context.LifetimeToken).Forget(Debug.LogException);
            NotifyChanged();
        }

        private void EnsureInitialized()
        {
            if (!m_IsInitialized || m_Context == null || m_State == null)
                throw new InvalidOperationException("Galaxy Challenge is not initialized.");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void RegisterTestModeModule()
        {
            if (GameEntry.TestMode == null || m_TestModeModule != null) return;
            var module = new GalaxyChallengeTestModeModule(this);
            if (GameEntry.TestMode.RegisterModule(module)) m_TestModeModule = module;
        }

        private void UnregisterTestModeModule()
        {
            if (m_TestModeModule == null) return;
            GameEntry.TestMode?.UnregisterModule(m_TestModeModule);
            m_TestModeModule = null;
        }
#endif

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
            EntriesChanged?.Invoke();
        }
    }
}
