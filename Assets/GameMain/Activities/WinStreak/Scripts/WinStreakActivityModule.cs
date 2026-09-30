using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lokas.Activities.WinStreak
{
    public static class WinStreakPageKeys
    {
        public const string Start = "start";
        public const string Main = "main";
        public const string Details = "details";
        public const string End = "end";
    }

    public enum WinStreakActivityState
    {
        Inactive,
        Locked,
        ReadyToJoin,
        Active,
        Completed
    }

    public sealed class WinStreakCheckpointSnapshot
    {
        public int Checkpoint { get; }
        public int RewardId { get; }
        public WinStreakChestTier ChestTier { get; }
        public WinStreakRewardDefinition Reward { get; }
        public IReadOnlyList<RewardEntry> Rewards { get; }
        public bool IsReached { get; }
        public bool IsClaimed { get; }
        public bool CanClaim { get; }

        internal WinStreakCheckpointSnapshot(WinStreakCheckpointDefinition definition, bool reached,
            bool claimed, bool canClaim)
        {
            Checkpoint = definition.Checkpoint;
            RewardId = definition.RewardId;
            ChestTier = definition.ChestTier;
            Reward = definition.Reward;
            Rewards = definition.Rewards;
            IsReached = reached;
            IsClaimed = claimed;
            CanClaim = canClaim;
        }
    }

    /// <summary>
    /// 连胜页使用的不可变摘要。CurrentProgress 是当前连续胜场，BestProgress 是本活动期间
    /// 达到过的最高连续胜场；检查点本身不会把进度清零，因此进度按目标值累加。
    /// </summary>
    public sealed class WinStreakSnapshot
    {
        public string StreakId { get; }
        public int EventId { get; }
        public string DisplayName { get; }
        public WinStreakActivityState State { get; }
        public bool IsActive { get; }
        public bool IsUnlocked { get; }
        public bool IsJoined { get; }
        public bool IsComplete { get; }
        public int CurrentProgress { get; }
        public int BestProgress { get; }
        public int MaxProgress { get; }
        public int MaxCheckpointIndex { get; }
        public int RewardReceiveProgress { get; }
        public int LastCheckedProgress { get; }
        public bool IsTutorialComplete { get; }
        public float Progress01 { get; }
        public DateTimeOffset EndUtc { get; }
        public IReadOnlyList<WinStreakCheckpointSnapshot> Checkpoints { get; }
        public WinStreakCheckpointSnapshot NextCheckpoint { get; }

        // Aliases make the DTO convenient for the test panel and designer scripts.
        public int Progress => CurrentProgress;
        public int CurrentStreak => CurrentProgress;
        public int HighestCheckpointIndex => MaxCheckpointIndex;

        internal WinStreakSnapshot(WinStreakActivityConfig config, WinStreakActivityState state,
            bool active, bool unlocked, WinStreakPersistedState persisted,
            IReadOnlyList<WinStreakCheckpointSnapshot> checkpoints,
            WinStreakCheckpointSnapshot nextCheckpoint, float progress01)
        {
            StreakId = config.StreakId;
            EventId = config.EventId;
            DisplayName = config.DisplayName;
            State = state;
            IsActive = active;
            IsUnlocked = unlocked;
            IsJoined = persisted != null && persisted.joined;
            IsComplete = persisted != null && config.Checkpoints.Count > 0 &&
                persisted.maxCheckPointIndex >= config.Checkpoints.Count - 1;
            CurrentProgress = Mathf.Clamp(persisted?.currentProgress ?? 0, 0, config.MaxProgress);
            BestProgress = Mathf.Clamp(persisted?.bestProgress ?? 0, 0, config.MaxProgress);
            MaxProgress = config.MaxProgress;
            MaxCheckpointIndex = persisted?.maxCheckPointIndex ?? -1;
            RewardReceiveProgress = persisted?.rewardReceiveProgress ?? -1;
            LastCheckedProgress = Mathf.Clamp(persisted?.lastCheckedProgress ?? 0, 0, config.MaxProgress);
            IsTutorialComplete = persisted != null && persisted.isCompleteTutorial;
            Progress01 = Mathf.Clamp01(progress01);
            EndUtc = config.EndUtc;
            Checkpoints = checkpoints ?? Array.Empty<WinStreakCheckpointSnapshot>();
            NextCheckpoint = nextCheckpoint;
        }
    }

    [Serializable]
    internal sealed class WinStreakPersistedState
    {
        // Names mirror the upstream activity DTO so a later server/profile adapter can map
        // this local MVP without changing the page contract.
        public string streakId;
        public int eventId;
        public int currentProgress;
        public int maxCheckPointIndex = -1;
        public int bestProgress;
        public int rewardReceiveProgress = -1;
        public int joinCount;
        public int lastCheckedProgress;
        public bool isReserveReset;
        public bool isShowStart = true;
        public bool isCompleteTutorial;

        public bool joined;
        public long joinedUtcUnixSeconds;
        public List<int> claimedRewardIds = new List<int>();
        public List<string> processedFactIds = new List<string>();
    }

    /// <summary>
    /// Win Streak 活动模块。通关事实只在报名并处于活动时间内处理；胜利会把当前连续进度
    /// 增加配置的步长，失败只重置当前连续进度，已领取奖励和历史最高进度保持不变。
    /// </summary>
    public sealed class WinStreakActivityModule : IActivityModule, IActivityEntryProvider
    {
        public const string Id = "win_streak";
        private const string StateKey = "state";
        private const int StateSchemaVersion = 1;

        private readonly WinStreakActivityConfig m_Config;
        private readonly IReadOnlyList<string> m_GameIds;
        private readonly HashSet<int> m_ClaimedRewardIds = new HashSet<int>();
        private readonly HashSet<string> m_ProcessedFactIds = new HashSet<string>(StringComparer.Ordinal);
        private IActivityContext m_Context;
        private IDisposable m_LevelCompletedSubscription;
        private WinStreakPersistedState m_State;
        private bool m_IsInitialized;
        private bool m_IsClaiming;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private WinStreakTestModeModule m_TestModeModule;
#endif

        public string ModuleId => Id;
        public WinStreakActivityConfig Config => m_Config;
        public event Action EntriesChanged;
        public event Action StateChanged;
        public event Action RulesPageClosed;

        public WinStreakActivityModule(WinStreakActivityConfig config, IReadOnlyList<string> gameIds = null)
        {
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_GameIds = gameIds == null ? Array.Empty<string>() : new List<string>(gameIds).AsReadOnly();
        }

        public async UniTask InitializeAsync(IActivityContext context, CancellationToken cancellationToken)
        {
            if (m_IsInitialized) throw new InvalidOperationException("Win Streak is already initialized.");
            m_Config.ValidateConfiguration();
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            cancellationToken.ThrowIfCancellationRequested();
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
            if (m_Context != null && m_State != null)
                await SaveStateAsync(CancellationToken.None);
            m_IsClaiming = false;
            m_IsInitialized = false;
            m_Context = null;
        }

        public IReadOnlyList<ActivityEntryInfo> GetEntries()
        {
            if (!m_IsInitialized) return Array.Empty<ActivityEntryInfo>();
            WinStreakSnapshot snapshot = GetSnapshot();
            bool visible = snapshot.IsActive || (snapshot.IsJoined && !snapshot.IsComplete);
            if (!visible) return Array.Empty<ActivityEntryInfo>();
            string reason = snapshot.State switch
            {
                WinStreakActivityState.Locked => $"达到第 {m_Config.UnlockLevel} 关后开启",
                WinStreakActivityState.Inactive => "活动已结束",
                _ => null
            };
            bool interactable = snapshot.State == WinStreakActivityState.ReadyToJoin ||
                snapshot.State == WinStreakActivityState.Active || snapshot.State == WinStreakActivityState.Completed;
            return new[]
            {
                new ActivityEntryInfo("main", m_Config.EntryLabel, "win_streak", visible: true,
                    interactable: interactable, unavailableReasonKey: reason,
                    badgeCount: GetAvailableClaimCount(snapshot), order: 85, countdownEndUtc: snapshot.EndUtc)
            };
        }

        public async UniTask OpenEntryAsync(string entryId, CancellationToken cancellationToken)
        {
            if (!string.Equals(entryId, "main", StringComparison.Ordinal))
                throw new ArgumentException("Unknown Win Streak entry.", nameof(entryId));
            EnsureInitialized();
            WinStreakSnapshot snapshot = GetSnapshot();
            if (snapshot.State == WinStreakActivityState.Locked)
                throw new InvalidOperationException($"Win Streak unlocks at level {m_Config.UnlockLevel}.");
            if (!snapshot.IsActive)
            {
                await OpenPageAsync(WinStreakPageKeys.End, cancellationToken);
                return;
            }
            await OpenPageAsync(snapshot.IsJoined ? WinStreakPageKeys.Main : WinStreakPageKeys.Start,
                cancellationToken);
        }

        public WinStreakSnapshot GetSnapshot()
        {
            DateTimeOffset now = m_Context?.Clock.UtcNow ?? DateTimeOffset.UtcNow;
            bool active = m_Config.IsActiveAt(now);
            bool unlocked = IsUnlocked();
            int current = Mathf.Clamp(m_State?.currentProgress ?? 0, 0, m_Config.MaxProgress);
            var checkpoints = new List<WinStreakCheckpointSnapshot>(m_Config.Checkpoints.Count);
            WinStreakCheckpointSnapshot next = null;
            for (int index = 0; index < m_Config.Checkpoints.Count; index++)
            {
                WinStreakCheckpointDefinition definition = m_Config.Checkpoints[index];
                if (definition == null) continue;
                bool reached = m_State != null && (index <= m_State.maxCheckPointIndex ||
                    m_State.bestProgress >= definition.Checkpoint);
                bool claimed = m_ClaimedRewardIds.Contains(definition.RewardId);
                bool canClaim = active && unlocked && m_State != null && m_State.joined && reached && !claimed &&
                    CanGrantAll(definition.Rewards);
                var checkpoint = new WinStreakCheckpointSnapshot(definition, reached, claimed, canClaim);
                checkpoints.Add(checkpoint);
                if (next == null && !reached) next = checkpoint;
            }
            int maxIndex = m_State?.maxCheckPointIndex ?? -1;
            if (m_Config.Checkpoints.Count > 0)
                maxIndex = Mathf.Clamp(Mathf.Max(maxIndex, m_Config.GetHighestReachedIndex(current)), -1,
                    m_Config.Checkpoints.Count - 1);
            WinStreakActivityState state = ResolveState(active, unlocked);
            float progress = m_Config.MaxProgress > 0 ? current / (float)m_Config.MaxProgress : 0f;
            return new WinStreakSnapshot(m_Config, state, active, unlocked, m_State, checkpoints.AsReadOnly(), next,
                progress);
        }

        /// <summary>报名并开始记录当前活动的连胜事实。</summary>
        public async UniTask<bool> JoinAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            WinStreakSnapshot snapshot = GetSnapshot();
            if (!snapshot.IsActive || !snapshot.IsUnlocked)
                throw new InvalidOperationException("Win Streak cannot be joined in its current state.");
            if (m_State != null && m_State.joined) return false;
            cancellationToken.ThrowIfCancellationRequested();
            m_State ??= CreateInitialState();
            m_State.joined = true;
            m_State.joinCount = Mathf.Max(0, m_State.joinCount) + 1;
            m_State.joinedUtcUnixSeconds = m_Context.Clock.UtcNow.ToUnixTimeSeconds();
            m_State.isShowStart = false;
            m_State.isReserveReset = false;
            await SaveStateAsync(cancellationToken);
            NotifyChanged();
            return true;
        }

        public UniTask<bool> StartAsync(CancellationToken cancellationToken = default) => JoinAsync(cancellationToken);

        public UniTask OpenMainAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            WinStreakSnapshot snapshot = GetSnapshot();
            if (!snapshot.IsJoined) throw new InvalidOperationException("Join Win Streak before opening its page.");
            return OpenPageAsync(WinStreakPageKeys.Main, cancellationToken);
        }

        public UniTask OpenRulesAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return OpenPageAsync(WinStreakPageKeys.Details, cancellationToken);
        }

        public UniTask OpenEndAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return OpenPageAsync(WinStreakPageKeys.End, cancellationToken);
        }

        /// <summary>领取一个已达成的累计检查点奖励。</summary>
        public async UniTask<ActivityRewardReceipt> ClaimAsync(int checkpoint,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            WinStreakCheckpointDefinition definition = m_Config.GetCheckpoint(checkpoint);
            string grantId = BuildGrantId(checkpoint);
            if (definition == null)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.NotFound,
                    reason: "Unknown Win Streak checkpoint.");
            if (m_ClaimedRewardIds.Contains(definition.RewardId))
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.AlreadyGranted);
            if (m_State == null || !m_State.joined || m_State.bestProgress < checkpoint)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Failed,
                    reason: "Insufficient Win Streak progress.");
            if (!CanGrantAll(definition.Rewards))
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Unsupported,
                    reason: "One or more Win Streak rewards are not mapped by this game.");
            if (m_IsClaiming)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Pending,
                    reason: "Win Streak reward is already being claimed.");

            m_IsClaiming = true;
            try
            {
                var items = new List<ActivityRewardItem>(definition.Rewards.Count);
                foreach (RewardEntry reward in definition.Rewards)
                    items.Add(new ActivityRewardItem(reward.ResourceKey, reward.Unit, reward.RequestAmount));
                ActivityRewardReceipt receipt = await m_Context.Rewards.GrantAsync(
                    new ActivityRewardRequest(grantId, m_Config.StreakId, "profile", items), cancellationToken);
                if (receipt.Status == ActivityRewardStatus.Granted ||
                    receipt.Status == ActivityRewardStatus.AlreadyGranted)
                {
                    m_ClaimedRewardIds.Add(definition.RewardId);
                    m_State.rewardReceiveProgress = Mathf.Max(m_State.rewardReceiveProgress, checkpoint);
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

        public UniTask<ActivityRewardReceipt> ClaimRewardAsync(int checkpoint,
            CancellationToken cancellationToken = default) => ClaimAsync(checkpoint, cancellationToken);

        public UniTask<ActivityRewardReceipt> TryReceiveReward(int checkpoint,
            CancellationToken cancellationToken = default) => ClaimAsync(checkpoint, cancellationToken);

        /// <summary>给测试面板和离线演示使用的直接增量入口；正常运行由通关事实驱动。</summary>
        public void AddProgress(int amount = 1)
        {
            EnsureInitialized();
            if (m_State == null || !m_State.joined || !m_Config.IsActiveAt(m_Context.Clock.UtcNow)) return;
            if (amount <= 0) return;
            m_State.currentProgress = Mathf.Clamp(m_State.currentProgress + amount, 0, m_Config.MaxProgress);
            m_State.bestProgress = Mathf.Max(m_State.bestProgress, m_State.currentProgress);
            m_State.maxCheckPointIndex = Mathf.Max(m_State.maxCheckPointIndex,
                m_Config.GetHighestReachedIndex(m_State.currentProgress));
            PersistStateAndNotify();
        }

        /// <summary>进度展示动画完成后由主页面确认，避免下次打开重复播放同一段动画。</summary>
        public void MarkProgressChecked(int progress)
        {
            EnsureInitialized();
            if (m_State == null) return;
            int checkedProgress = Mathf.Clamp(progress, 0, m_State.currentProgress);
            if (m_State.lastCheckedProgress == checkedProgress) return;
            m_State.lastCheckedProgress = checkedProgress;
            SaveStateAsync(m_Context?.LifetimeToken ?? CancellationToken.None).Forget(Debug.LogException);
        }

        /// <summary>首次规则及奖励浏览动画完整结束后记录，后续进入不再重复播放。</summary>
        public void MarkTutorialComplete()
        {
            EnsureInitialized();
            if (m_State == null || m_State.isCompleteTutorial) return;
            m_State.isCompleteTutorial = true;
            SaveStateAsync(m_Context?.LifetimeToken ?? CancellationToken.None).Forget(Debug.LogException);
        }

        /// <summary>由规则页在关闭时通知等待中的主页面。</summary>
        public void NotifyRulesPageClosed()
        {
            RulesPageClosed?.Invoke();
        }

        public void ResetProgress()
        {
            EnsureInitialized();
            if (m_State == null) return;
            ResetCurrentProgress();
            PersistStateAndNotify();
        }

        public void ReserveResetProgress()
        {
            EnsureInitialized();
            if (m_State == null) return;
            m_State.isReserveReset = true;
            PersistStateAndNotify();
        }

        public void ResetProgressIfReserve()
        {
            EnsureInitialized();
            if (m_State == null || !m_State.isReserveReset) return;
            ResetCurrentProgress();
            m_State.isReserveReset = false;
            PersistStateAndNotify();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal string GetTestModeGameId()
        {
            foreach (string gameId in m_GameIds)
                if (!string.IsNullOrWhiteSpace(gameId)) return gameId;
            return null;
        }

        internal async UniTask JoinTestAsync()
        {
            if (!m_IsInitialized) return;
            await JoinAsync(CancellationToken.None);
        }

        internal async UniTask ResetTestStateAsync()
        {
            if (!m_IsInitialized) return;
            ResetState();
            await SaveStateAsync(CancellationToken.None);
            NotifyChanged();
        }
#endif

        internal void ProcessTestFacts(IReadOnlyList<ActivityLevelCompletedFact> facts)
        {
            if (!m_IsInitialized || facts == null || facts.Count == 0) return;
            bool changed = false;
            foreach (ActivityLevelCompletedFact fact in facts)
                changed |= ApplyLevelCompletedFact(fact);
            if (changed) PersistStateAndNotify();
        }

        private void OnLevelCompleted(ActivityLevelCompletedFact fact)
        {
            if (ApplyLevelCompletedFact(fact)) PersistStateAndNotify();
        }

        private bool ApplyLevelCompletedFact(ActivityLevelCompletedFact fact)
        {
            if (!m_IsInitialized || m_State == null || fact == null || !m_State.joined ||
                !IsGameSource(fact.GameId) || !m_Config.IsActiveAt(m_Context.Clock.UtcNow)) return false;
            if (!m_ProcessedFactIds.Add(fact.FactId)) return false;
            if (m_Config.Checkpoints.Count > 0 &&
                m_State.maxCheckPointIndex >= m_Config.Checkpoints.Count - 1) return true;
            if (!fact.Won)
            {
                if (!m_Config.ResetOnLoss) return true;
                ResetCurrentProgress();
                return true;
            }
            if (m_State.currentProgress >= m_Config.MaxProgress) return true;
            int amount = Mathf.Max(1, m_Config.ProgressPerWin);
            m_State.currentProgress = Mathf.Clamp(m_State.currentProgress + amount, 0, m_Config.MaxProgress);
            m_State.bestProgress = Mathf.Max(m_State.bestProgress, m_State.currentProgress);
            m_State.maxCheckPointIndex = Mathf.Max(m_State.maxCheckPointIndex,
                m_Config.GetHighestReachedIndex(m_State.currentProgress));
            return true;
        }

        private void ResetCurrentProgress()
        {
            if (m_State == null) return;
            m_State.lastCheckedProgress = m_State.currentProgress;
            m_State.currentProgress = 0;
            m_State.isReserveReset = false;
        }

        private bool IsGameSource(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId)) return false;
            foreach (string registered in m_GameIds)
                if (string.Equals(registered, gameId, StringComparison.Ordinal)) return true;
            return false;
        }

        private async UniTask LoadStateAsync(CancellationToken cancellationToken)
        {
            m_ClaimedRewardIds.Clear();
            m_ProcessedFactIds.Clear();
            ActivityStorageReadResult read = await m_Context.Storage.ReadAsync(StateKey, cancellationToken);
            if (read.Status == ActivityStorageReadStatus.Corrupt)
                throw new InvalidOperationException("Win Streak save data is corrupt: " + read.Error);
            if (read.Status == ActivityStorageReadStatus.Missing || read.SchemaVersion < StateSchemaVersion)
            {
                ResetState();
                if (read.Status == ActivityStorageReadStatus.Found)
                    await SaveStateAsync(cancellationToken);
                return;
            }
            m_State = JsonUtility.FromJson<WinStreakPersistedState>(read.Payload);
            if (m_State == null) throw new InvalidOperationException("Win Streak save data is invalid.");
            if (!string.Equals(m_State.streakId, m_Config.StreakId, StringComparison.Ordinal) ||
                m_State.eventId != m_Config.EventId)
            {
                ResetState();
                await SaveStateAsync(cancellationToken);
                return;
            }
            m_State.claimedRewardIds ??= new List<int>();
            m_State.processedFactIds ??= new List<string>();
            m_State.currentProgress = Mathf.Clamp(m_State.currentProgress, 0, m_Config.MaxProgress);
            m_State.lastCheckedProgress = Mathf.Clamp(m_State.lastCheckedProgress, 0, m_State.currentProgress);
            m_State.bestProgress = Mathf.Clamp(Mathf.Max(m_State.bestProgress, m_State.currentProgress), 0,
                m_Config.MaxProgress);
            m_State.maxCheckPointIndex = Mathf.Clamp(Mathf.Max(m_State.maxCheckPointIndex,
                m_Config.GetHighestReachedIndex(m_State.bestProgress)), -1, m_Config.Checkpoints.Count - 1);
            foreach (int rewardId in m_State.claimedRewardIds)
                if (m_Config.Checkpoints.Any(checkpoint => checkpoint != null && checkpoint.RewardId == rewardId))
                    m_ClaimedRewardIds.Add(rewardId);
            foreach (string factId in m_State.processedFactIds)
                if (!string.IsNullOrWhiteSpace(factId)) m_ProcessedFactIds.Add(factId);
        }

        private void ResetState()
        {
            m_ClaimedRewardIds.Clear();
            m_ProcessedFactIds.Clear();
            m_State = new WinStreakPersistedState
            {
                streakId = m_Config.StreakId,
                eventId = m_Config.EventId,
                maxCheckPointIndex = -1,
                rewardReceiveProgress = -1,
                isShowStart = true
            };
        }

        private WinStreakPersistedState CreateInitialState()
        {
            return new WinStreakPersistedState
            {
                streakId = m_Config.StreakId,
                eventId = m_Config.EventId,
                maxCheckPointIndex = -1,
                rewardReceiveProgress = -1,
                isShowStart = true
            };
        }

        private async UniTask SaveStateAsync(CancellationToken cancellationToken)
        {
            if (m_Context == null || m_State == null) return;
            m_State.streakId = m_Config.StreakId;
            m_State.eventId = m_Config.EventId;
            m_State.claimedRewardIds = m_ClaimedRewardIds.OrderBy(value => value).ToList();
            m_State.processedFactIds = m_ProcessedFactIds.OrderBy(value => value, StringComparer.Ordinal).ToList();
            await m_Context.Storage.WriteAsync(StateKey, JsonUtility.ToJson(m_State), StateSchemaVersion,
                cancellationToken);
            await m_Context.Storage.FlushAsync(cancellationToken);
        }

        private WinStreakActivityState ResolveState(bool active, bool unlocked)
        {
            if (!active) return WinStreakActivityState.Inactive;
            if (!unlocked) return WinStreakActivityState.Locked;
            if (m_State == null || !m_State.joined) return WinStreakActivityState.ReadyToJoin;
            return m_State != null && m_Config.Checkpoints.Count > 0 &&
                m_State.maxCheckPointIndex >= m_Config.Checkpoints.Count - 1
                ? WinStreakActivityState.Completed
                : WinStreakActivityState.Active;
        }

        private bool IsUnlocked()
        {
#if UNITY_EDITOR
            if (m_Config.EditorAlwaysUnlocked) return true;
#endif
            if (m_Config.UnlockLevel <= 0) return true;
            GameMode primaryMode = GameEntry.GameManager?.PrimaryGameMode ?? GameMode.None;
            return GameEntry.SubGames?.Get(primaryMode)?.CurrentLevel >= m_Config.UnlockLevel;
        }

        private bool CanGrantAll(IReadOnlyList<RewardEntry> rewards)
        {
            if (m_Context == null || rewards == null || rewards.Count == 0) return false;
            foreach (RewardEntry reward in rewards)
            {
                if (reward == null || reward.RequestAmount <= 0) return false;
                try { reward.Validate(); }
                catch (InvalidOperationException) { return false; }
                if (!m_Context.Rewards.CanGrant(reward.ResourceKey, "profile")) return false;
            }
            return true;
        }

        private int GetAvailableClaimCount(WinStreakSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsActive || !snapshot.IsUnlocked || !snapshot.IsJoined) return 0;
            int count = 0;
            foreach (WinStreakCheckpointSnapshot checkpoint in snapshot.Checkpoints)
                if (checkpoint != null && checkpoint.CanClaim) count++;
            return count;
        }

        private void PersistStateAndNotify()
        {
            SaveStateAsync(m_Context?.LifetimeToken ?? CancellationToken.None).Forget(Debug.LogException);
            NotifyChanged();
        }

        private async UniTask OpenPageAsync(string pageKey, CancellationToken cancellationToken)
        {
            ActivityUiOpenResult result = await m_Context.UI.OpenAsync(
                new ActivityPageRequest(pageKey, arguments: this), cancellationToken);
            if (result.Status != ActivityUiOpenStatus.Opened && result.Status != ActivityUiOpenStatus.Reused)
                throw new InvalidOperationException(result.Reason ?? "The Win Streak page could not be opened.");
        }

        private void EnsureInitialized()
        {
            if (!m_IsInitialized) throw new InvalidOperationException("Win Streak is not initialized.");
        }

        private string BuildGrantId(int checkpoint) => string.Concat(Id, "/", m_Config.StreakId, "/checkpoint/",
            checkpoint.ToString(CultureInfo.InvariantCulture));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void RegisterTestModeModule()
        {
            if (GameEntry.TestMode == null || m_TestModeModule != null) return;
            var module = new WinStreakTestModeModule(this);
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
