using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lokas.Activities.Race
{
    public static class RacePageKeys
    {
        public const string Start = "start";
        public const string Main = "main";
        public const string Details = "details";
    }

    public enum RaceActivityState
    {
        Inactive,
        Locked,
        ReadyToJoin,
        Racing,
        Finished,
        RewardClaimed
    }

    /// <summary>赛道上一辆车的只读展示数据。进度显示被限制在 0–1，内部排名仍可识别先于玩家冲线的 NPC。</summary>
    public sealed class RaceRacerSnapshot
    {
        public string RacerId { get; }
        public string DisplayName { get; }
        public bool IsPlayer { get; }
        public int Rank { get; internal set; }
        public int CompletedStages { get; }
        public int TargetStages { get; }
        public float Progress01 { get; }
        internal float SortProgress { get; }

        internal RaceRacerSnapshot(string racerId, string displayName, bool isPlayer, int completedStages,
            int targetStages, float progress01, float sortProgress)
        {
            RacerId = racerId;
            DisplayName = displayName;
            IsPlayer = isPlayer;
            CompletedStages = Mathf.Clamp(completedStages, 0, targetStages);
            TargetStages = targetStages;
            SortProgress = Math.Max(0f, sortProgress);
            Progress01 = Mathf.Clamp01(progress01);
        }
    }

    public sealed class RaceRankRewardSnapshot
    {
        public string Label { get; }
        public int MinRank { get; }
        public int MaxRank { get; }
        public IReadOnlyList<RewardEntry> Rewards { get; }

        internal RaceRankRewardSnapshot(RaceRankRewardDefinition definition)
        {
            Label = definition.Label;
            MinRank = definition.MinRank;
            MaxRank = definition.MaxRank;
            Rewards = definition.Reward != null ? definition.Reward.Entries : Array.Empty<RewardEntry>();
        }
    }

    /// <summary>页面只使用此快照，不读取 Race 存档或直接推断 NPC 结果。</summary>
    public sealed class RaceSnapshot
    {
        public string RaceId { get; }
        public string DisplayName { get; }
        public RaceActivityState State { get; }
        public bool IsActive { get; }
        public bool IsUnlocked { get; }
        public bool IsJoined { get; }
        public bool IsFinished { get; }
        public bool IsRewardClaimed { get; }
        public bool CanJoin { get; }
        public bool CanClaim { get; }
        public int StartLevel { get; }
        public int TargetLevel { get; }
        public int CompletedStages { get; }
        public int TargetStages { get; }
        public int PlayerRank { get; }
        public DateTimeOffset EndUtc { get; }
        public IReadOnlyList<RaceRacerSnapshot> Racers { get; }
        public IReadOnlyList<RaceRankRewardSnapshot> RankRewards { get; }
        public RaceRankRewardSnapshot EarnedReward { get; }

        internal RaceSnapshot(RaceActivityConfig config, RaceActivityState state, bool isActive, bool isUnlocked,
            RacePersistedState persisted, int playerRank, IReadOnlyList<RaceRacerSnapshot> racers,
            IReadOnlyList<RaceRankRewardSnapshot> rankRewards, RaceRankRewardSnapshot earnedReward, bool canClaim,
            DateTimeOffset endUtc)
        {
            RaceId = config.RaceId;
            DisplayName = config.DisplayName;
            State = state;
            IsActive = isActive;
            IsUnlocked = isUnlocked;
            IsJoined = persisted != null && persisted.joined;
            IsFinished = persisted != null && persisted.finished;
            IsRewardClaimed = persisted != null && persisted.rewardClaimed;
            CanJoin = state == RaceActivityState.ReadyToJoin;
            CanClaim = canClaim;
            StartLevel = persisted?.startLevel ?? 0;
            TargetLevel = persisted?.targetLevel ?? 0;
            CompletedStages = persisted?.playerCompletedStages ?? 0;
            TargetStages = config.TargetStageDifference;
            PlayerRank = playerRank;
            EndUtc = endUtc;
            Racers = racers;
            RankRewards = rankRewards;
            EarnedReward = earnedReward;
        }
    }

    [Serializable]
    internal sealed class RacePersistedState
    {
        public string raceId;
        public bool joined;
        public bool finished;
        public bool rewardClaimed;
        public int startLevel;
        public int targetLevel;
        public int playerCompletedStages;
        public int finalPlayerRank;
        public int startType;
        public float playerGoalRate;
        public long joinedUtcUnixSeconds;
        public long finishedUtcUnixSeconds;
        public float[] npcGoalRates;
        public List<string> npcNames = new List<string>();
        public List<string> processedFactIds = new List<string>();
    }

    /// <summary>
    /// 本地 Race MVP。规则、存档、NPC 模拟和奖励均属于本模块；公共宿主只提供生命周期、事实、存储和页面路由。
    /// </summary>
    public sealed class RaceActivityModule : IActivityModule, IActivityEntryProvider
    {
        public const string Id = "race";
        private const string StateKey = "state";
        private const int StateSchemaVersion = 1;

        private readonly RaceActivityConfig m_Config;
        private readonly IReadOnlyList<string> m_GameIds;
        private readonly HashSet<string> m_ProcessedFactIds = new HashSet<string>(StringComparer.Ordinal);
        private IActivityContext m_Context;
        private IDisposable m_LevelCompletedSubscription;
        private RacePersistedState m_State;
        private bool m_IsInitialized;
        private bool m_IsStarting;
        private bool m_IsClaiming;
        private NameProvider m_NameProvider;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private RaceTestModeModule m_TestModeModule;
#endif

        public string ModuleId => Id;
        public RaceActivityConfig Config => m_Config;
        public event Action EntriesChanged;
        public event Action StateChanged;

        public RaceActivityModule(RaceActivityConfig config, IReadOnlyList<string> gameIds = null)
        {
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_GameIds = gameIds == null ? Array.Empty<string>() : gameIds.ToArray();
        }

        public async UniTask InitializeAsync(IActivityContext context, CancellationToken cancellationToken)
        {
            if (m_IsInitialized) throw new InvalidOperationException("Race is already initialized.");
            m_Config.ValidateConfiguration();
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            cancellationToken.ThrowIfCancellationRequested();
            await LoadStateAsync(cancellationToken);
            if (m_State != null && m_State.joined)
            {
                EnsureNpcNames();
                await SaveStateAsync(cancellationToken);
            }
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
            m_IsStarting = false;
            m_IsClaiming = false;
            m_IsInitialized = false;
        }

        public IReadOnlyList<ActivityEntryInfo> GetEntries()
        {
            if (!m_IsInitialized) return Array.Empty<ActivityEntryInfo>();
            RaceSnapshot snapshot = GetSnapshot();
            bool visible = snapshot.IsActive || (snapshot.IsJoined && !snapshot.IsRewardClaimed);
            if (!visible) return Array.Empty<ActivityEntryInfo>();

            string unavailableReason = snapshot.State switch
            {
                RaceActivityState.Locked => $"达到第 {m_Config.UnlockLevel} 关后开启",
                RaceActivityState.Inactive => "活动已结束",
                RaceActivityState.RewardClaimed => "奖励已领取",
                _ => null
            };
            bool interactable = snapshot.State == RaceActivityState.ReadyToJoin ||
                snapshot.State == RaceActivityState.Racing || snapshot.State == RaceActivityState.Finished;
            return new[]
            {
                new ActivityEntryInfo("main", m_Config.EntryLabel, "race", visible: true, interactable: interactable,
                    unavailableReasonKey: unavailableReason, badgeCount: snapshot.CanClaim ? 1 : 0,
                    order: 90, countdownEndUtc: snapshot.EndUtc)
            };
        }

        public async UniTask OpenEntryAsync(string entryId, CancellationToken cancellationToken)
        {
            if (!string.Equals(entryId, "main", StringComparison.Ordinal))
                throw new ArgumentException("Unknown race entry.", nameof(entryId));
            EnsureInitialized();
            RaceSnapshot snapshot = GetSnapshot();
            if (snapshot.State == RaceActivityState.ReadyToJoin)
            {
                await OpenPageAsync(RacePageKeys.Start, cancellationToken);
                return;
            }
            if (snapshot.State == RaceActivityState.Racing || snapshot.State == RaceActivityState.Finished)
            {
                await OpenPageAsync(RacePageKeys.Main, cancellationToken);
                return;
            }
            throw new InvalidOperationException(snapshot.State == RaceActivityState.Locked
                ? $"Race unlocks at level {m_Config.UnlockLevel}."
                : "Race is not available.");
        }

        public RaceSnapshot GetSnapshot()
        {
            DateTimeOffset now = m_Context?.Clock.UtcNow ?? DateTimeOffset.UtcNow;
            if (m_IsInitialized && FinalizeIfDue(now))
            {
                SaveStateAsync(m_Context.LifetimeToken).Forget(Debug.LogException);
                NotifyChanged();
            }

            bool active = m_Config.IsActiveAt(now);
            bool unlocked = IsUnlocked();
            IReadOnlyList<RaceRacerSnapshot> racers = m_State != null && m_State.joined
                ? BuildRacers(now).AsReadOnly()
                : Array.Empty<RaceRacerSnapshot>();
            int playerRank = m_State != null && m_State.finished
                ? m_State.finalPlayerRank
                : FindPlayerRank(racers);
            RaceRankRewardDefinition earnedDefinition = playerRank > 0 ? m_Config.GetRewardForRank(playerRank) : null;
            RaceRankRewardSnapshot earnedReward = earnedDefinition == null ? null : new RaceRankRewardSnapshot(earnedDefinition);
            bool canClaim = m_State != null && m_State.finished && !m_State.rewardClaimed && earnedDefinition != null &&
                CanGrantAll(earnedDefinition.Reward?.Entries);
            var rewards = new List<RaceRankRewardSnapshot>(m_Config.RankRewards.Count);
            foreach (RaceRankRewardDefinition reward in m_Config.RankRewards)
                if (reward != null) rewards.Add(new RaceRankRewardSnapshot(reward));
            DateTimeOffset endUtc = m_State != null && m_State.joined
                ? m_Config.GetRaceEndUtc(FromUnixTime(m_State.joinedUtcUnixSeconds))
                : m_Config.EndUtc;
            return new RaceSnapshot(m_Config, ResolveState(active, unlocked), active, unlocked, m_State, playerRank,
                racers, rewards.AsReadOnly(), earnedReward, canClaim, endUtc);
        }

        /// <summary>本地 MVP 只允许配置的默认报名方式；广告/无限体力需要真实产品分支接入后再开放。</summary>
        public async UniTask<bool> StartRaceAsync(RaceStartType startType, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            if (m_IsStarting || m_State.joined) return false;
            if (startType != m_Config.DefaultStartType)
                throw new NotSupportedException($"Race start type '{startType}' is not enabled by this local configuration.");
            RaceSnapshot snapshot = GetSnapshot();
            if (!snapshot.CanJoin) throw new InvalidOperationException("Race cannot be joined in its current state.");

            m_IsStarting = true;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                DateTimeOffset now = m_Context.Clock.UtcNow;
                int startLevel = GetCurrentLevel();
                m_State = new RacePersistedState
                {
                    raceId = m_Config.RaceId,
                    joined = true,
                    startLevel = startLevel,
                    targetLevel = checked(startLevel + m_Config.TargetStageDifference),
                    playerCompletedStages = 0,
                    startType = (int)startType,
                    playerGoalRate = CreatePlayerGoalRate(now),
                    joinedUtcUnixSeconds = now.ToUnixTimeSeconds(),
                    npcGoalRates = CreateNpcGoalRates(now),
                    npcNames = CreateNpcNames(),
                    processedFactIds = new List<string>()
                };
                m_ProcessedFactIds.Clear();
                await SaveStateAsync(cancellationToken);
                NotifyChanged();
                return true;
            }
            finally
            {
                m_IsStarting = false;
            }
        }

        public UniTask<bool> StartRaceAsync(CancellationToken cancellationToken = default) =>
            StartRaceAsync(m_Config.DefaultStartType, cancellationToken);

        public UniTask OpenMainAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            RaceSnapshot snapshot = GetSnapshot();
            if (!snapshot.IsJoined) throw new InvalidOperationException("Join the race before opening its track.");
            if (snapshot.State == RaceActivityState.RewardClaimed || snapshot.State == RaceActivityState.Inactive)
                throw new InvalidOperationException("This race is no longer available.");
            return OpenPageAsync(RacePageKeys.Main, cancellationToken);
        }

        public UniTask OpenRulesAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return OpenPageAsync(RacePageKeys.Details, cancellationToken);
        }

        public async UniTask<ActivityRewardReceipt> ClaimRewardAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            RaceSnapshot snapshot = GetSnapshot();
            if (!snapshot.IsFinished)
                return new ActivityRewardReceipt(BuildGrantId(), ActivityRewardStatus.Failed, reason: "Race has not finished.");
            if (m_State.rewardClaimed)
                return new ActivityRewardReceipt(BuildGrantId(), ActivityRewardStatus.AlreadyGranted);
            if (m_IsClaiming)
                return new ActivityRewardReceipt(BuildGrantId(), ActivityRewardStatus.Pending, reason: "Race reward is already being claimed.");

            RaceRankRewardDefinition reward = m_Config.GetRewardForRank(m_State.finalPlayerRank);
            if (reward?.Reward == null || reward.Reward.Entries.Count == 0)
                return new ActivityRewardReceipt(BuildGrantId(), ActivityRewardStatus.Unsupported, reason: "No reward is configured for this rank.");
            if (!CanGrantAll(reward.Reward.Entries))
                return new ActivityRewardReceipt(BuildGrantId(), ActivityRewardStatus.Unsupported,
                    reason: "One or more Race rewards are not mapped by this game.");

            m_IsClaiming = true;
            try
            {
                var request = new ActivityRewardRequest(BuildGrantId(), m_Config.RaceId, "profile",
                    reward.Reward.Entries.Select(entry => new ActivityRewardItem(entry.ResourceKey, entry.Unit, entry.RequestAmount)));
                ActivityRewardReceipt receipt = await m_Context.Rewards.GrantAsync(request, cancellationToken);
                if (receipt.Status == ActivityRewardStatus.Granted || receipt.Status == ActivityRewardStatus.AlreadyGranted)
                {
                    m_State.rewardClaimed = true;
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

        private void OnLevelCompleted(ActivityLevelCompletedFact fact)
        {
            if (!m_IsInitialized || fact == null || !fact.Won || !m_State.joined || m_State.finished) return;
            if (m_GameIds.Count == 0 || !m_GameIds.Contains(fact.GameId)) return;
            if (!m_ProcessedFactIds.Add(fact.FactId)) return;
            if (!int.TryParse(fact.LevelId, out int completedLevel) || completedLevel < m_State.startLevel) return;

            int inferredProgress = checked(completedLevel - m_State.startLevel + 1);
            m_State.playerCompletedStages = Mathf.Clamp(Math.Max(m_State.playerCompletedStages, inferredProgress),
                0, m_Config.TargetStageDifference);
            DateTimeOffset now = m_Context.Clock.UtcNow;
            FinalizeIfDue(now);
            SaveStateAsync(m_Context.LifetimeToken).Forget(Debug.LogException);
            NotifyChanged();
        }

        private RaceActivityState ResolveState(bool active, bool unlocked)
        {
            if (m_State == null || !m_State.joined)
            {
                if (!active) return RaceActivityState.Inactive;
                return unlocked ? RaceActivityState.ReadyToJoin : RaceActivityState.Locked;
            }
            if (m_State.rewardClaimed) return RaceActivityState.RewardClaimed;
            if (m_State.finished) return RaceActivityState.Finished;
            return RaceActivityState.Racing;
        }

        private bool FinalizeIfDue(DateTimeOffset now)
        {
            if (m_State == null || !m_State.joined || m_State.finished) return false;
            DateTimeOffset raceEnd = m_Config.GetRaceEndUtc(FromUnixTime(m_State.joinedUtcUnixSeconds));
            bool reachedGoal = m_State.playerCompletedStages >= m_Config.TargetStageDifference;
            if (!reachedGoal && now < raceEnd) return false;

            List<RaceRacerSnapshot> racers = BuildRacers(now);
            m_State.finished = true;
            m_State.finishedUtcUnixSeconds = now.ToUnixTimeSeconds();
            m_State.finalPlayerRank = FindPlayerRank(racers);
            if (m_State.finalPlayerRank <= 0) m_State.finalPlayerRank = m_Config.RacerCount;
            return true;
        }

        private List<RaceRacerSnapshot> BuildRacers(DateTimeOffset now)
        {
            var racers = new List<RaceRacerSnapshot>(m_Config.RacerCount);
            int targetStages = m_Config.TargetStageDifference;
            float playerProgress = m_State == null ? 0f : m_State.playerCompletedStages / (float)targetStages;
            float playerGoalRate = m_State == null || !m_State.joined ? 1f : EnsurePlayerGoalRate();
            racers.Add(new RaceRacerSnapshot("player", "You", true, m_State?.playerCompletedStages ?? 0,
                targetStages, playerProgress, playerProgress * playerGoalRate));
            if (m_State == null || !m_State.joined) return racers;

            float elapsed01 = GetElapsed01(now);
            float[] rates = EnsureNpcGoalRates();
            for (int index = 0; index < m_Config.NpcCount; index++)
            {
                float rate = rates[index];
                float initialProgress = m_Config.NpcInitialProgress * (1f + index * 0.35f);
                float progress = Mathf.Max(initialProgress, elapsed01 * rate, playerProgress * rate);
                int completedStages = Mathf.FloorToInt(Mathf.Clamp01(progress) * targetStages);
                racers.Add(new RaceRacerSnapshot($"npc-{index + 1}", GetNpcName(index), false,
                    completedStages, targetStages, progress, progress));
            }

            racers.Sort((left, right) =>
            {
                int result = right.SortProgress.CompareTo(left.SortProgress);
                if (result != 0) return result;
                if (left.IsPlayer != right.IsPlayer)
                {
                    bool bothAtStart = left.SortProgress <= Mathf.Epsilon && right.SortProgress <= Mathf.Epsilon;
                    if (bothAtStart) return left.IsPlayer ? 1 : -1;
                    return left.IsPlayer ? -1 : 1;
                }
                return string.CompareOrdinal(left.RacerId, right.RacerId);
            });
            for (int index = 0; index < racers.Count; index++) racers[index].Rank = index + 1;
            return racers;
        }

        private float GetElapsed01(DateTimeOffset now)
        {
            if (m_State == null || !m_State.joined || m_State.joinedUtcUnixSeconds <= 0) return 0f;
            DateTimeOffset joined = FromUnixTime(m_State.joinedUtcUnixSeconds);
            DateTimeOffset end = m_Config.GetRaceEndUtc(joined);
            double durationSeconds = Math.Max(1d, (end - joined).TotalSeconds);
            return Mathf.Clamp01((float)((now.ToUniversalTime() - joined).TotalSeconds / durationSeconds));
        }

        private float[] EnsureNpcGoalRates()
        {
            if (m_State.npcGoalRates == null || m_State.npcGoalRates.Length != m_Config.NpcCount)
                m_State.npcGoalRates = CreateNpcGoalRates(FromUnixTime(m_State.joinedUtcUnixSeconds));
            return m_State.npcGoalRates;
        }

        private float EnsurePlayerGoalRate()
        {
            if (m_State.playerGoalRate <= 0f)
                m_State.playerGoalRate = CreatePlayerGoalRate(FromUnixTime(m_State.joinedUtcUnixSeconds));
            return m_State.playerGoalRate;
        }

        private string GetNpcName(int index)
        {
            EnsureNpcNames();
            return m_State.npcNames[index];
        }

        private void EnsureNpcNames()
        {
            if (m_State == null) return;
            if (m_State.npcNames == null) m_State.npcNames = new List<string>();
            if (m_State.npcNames.Count == m_Config.NpcCount) return;
            m_State.npcNames = CreateNpcNames();
        }

        private List<string> CreateNpcNames()
        {
            m_NameProvider ??= new NameProvider();
            var names = new List<string>(m_Config.NpcCount);
            for (int index = 0; index < m_Config.NpcCount; index++)
                names.Add(m_NameProvider.GetRandomName());
            return names;
        }

        private float[] CreateNpcGoalRates(DateTimeOffset seedTime)
        {
            var values = new float[m_Config.NpcCount];
            uint state = StableHash($"{m_Config.RaceId}|{m_Context?.ProfileId}|{seedTime.ToUnixTimeSeconds()}");
            for (int index = 0; index < values.Length; index++)
            {
                state = unchecked(state * 1664525u + 1013904223u);
                float unit = (state & 0x00ffffffu) / 16777215f;
                values[index] = Mathf.Lerp(m_Config.NpcMinGoalRate, m_Config.NpcMaxGoalRate, unit);
            }
            return values;
        }

        private float CreatePlayerGoalRate(DateTimeOffset seedTime)
        {
            uint state = StableHash($"{m_Config.RaceId}|{m_Context?.ProfileId}|{seedTime.ToUnixTimeSeconds()}|player");
            state = unchecked(state * 1664525u + 1013904223u);
            float unit = (state & 0x00ffffffu) / 16777215f;
            return Mathf.Lerp(m_Config.PlayerMinGoalRate, m_Config.PlayerMaxGoalRate, unit);
        }

        private async UniTask LoadStateAsync(CancellationToken cancellationToken)
        {
            ActivityStorageReadResult read = await m_Context.Storage.ReadAsync(StateKey, cancellationToken);
            if (read.Status == ActivityStorageReadStatus.Corrupt)
                throw new InvalidOperationException("Race save data is corrupt: " + read.Error);
            if (read.Status == ActivityStorageReadStatus.Missing)
            {
                ResetState();
                return;
            }

            m_State = JsonUtility.FromJson<RacePersistedState>(read.Payload);
            if (m_State == null) throw new InvalidOperationException("Race save data is invalid.");
            if (!string.Equals(m_State.raceId, m_Config.RaceId, StringComparison.Ordinal))
            {
                ResetState();
                await SaveStateAsync(cancellationToken);
                return;
            }

            m_State.processedFactIds ??= new List<string>();
            m_State.playerCompletedStages = Mathf.Clamp(m_State.playerCompletedStages, 0, m_Config.TargetStageDifference);
            if (m_State.joined)
            {
                if (m_State.joinedUtcUnixSeconds <= 0) throw new InvalidOperationException("Race save is missing its join time.");
                EnsureNpcGoalRates();
                EnsurePlayerGoalRate();
            }
            foreach (string factId in m_State.processedFactIds)
                if (!string.IsNullOrWhiteSpace(factId)) m_ProcessedFactIds.Add(factId);
        }

        private void ResetState()
        {
            m_ProcessedFactIds.Clear();
            m_State = new RacePersistedState { raceId = m_Config.RaceId };
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal string GetTestModeGameId()
        {
            foreach (string gameId in m_GameIds)
                if (!string.IsNullOrWhiteSpace(gameId)) return gameId;
            return null;
        }

        internal async UniTask ResetTestStateAsync()
        {
            if (!m_IsInitialized) return;
            ResetState();
            await SaveStateAsync(CancellationToken.None);
            NotifyChanged();
        }

        private void RegisterTestModeModule()
        {
            if (GameEntry.TestMode == null || m_TestModeModule != null) return;
            var module = new RaceTestModeModule(this);
            if (GameEntry.TestMode.RegisterModule(module)) m_TestModeModule = module;
        }

        private void UnregisterTestModeModule()
        {
            if (m_TestModeModule == null) return;
            GameEntry.TestMode?.UnregisterModule(m_TestModeModule);
            m_TestModeModule = null;
        }
#endif

        private async UniTask SaveStateAsync(CancellationToken cancellationToken)
        {
            if (m_Context == null || m_State == null) return;
            m_State.raceId = m_Config.RaceId;
            m_State.processedFactIds = m_ProcessedFactIds.OrderBy(id => id, StringComparer.Ordinal).ToList();
            await m_Context.Storage.WriteAsync(StateKey, JsonUtility.ToJson(m_State), StateSchemaVersion, cancellationToken);
            await m_Context.Storage.FlushAsync(cancellationToken);
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
            if (rewards == null || rewards.Count == 0 || m_Context == null) return false;
            foreach (RewardEntry reward in rewards)
            {
                if (reward == null || reward.RequestAmount <= 0) return false;
                try { reward.Validate(); }
                catch (InvalidOperationException) { return false; }
                if (!m_Context.Rewards.CanGrant(reward.ResourceKey, "profile")) return false;
            }
            return true;
        }

        private int GetCurrentLevel()
        {
            GameMode primaryMode = GameEntry.GameManager?.PrimaryGameMode ?? GameMode.None;
            int? current = GameEntry.SubGames?.Get(primaryMode)?.CurrentLevel;
            return Math.Max(1, current ?? 1);
        }

        private async UniTask OpenPageAsync(string pageKey, CancellationToken cancellationToken)
        {
            ActivityUiOpenResult result = await m_Context.UI.OpenAsync(new ActivityPageRequest(pageKey, arguments: this), cancellationToken);
            if (result.Status != ActivityUiOpenStatus.Opened && result.Status != ActivityUiOpenStatus.Reused)
                throw new InvalidOperationException(result.Reason ?? "The Race page could not be opened.");
        }

        private string BuildGrantId()
        {
            long joinedAt = m_State?.joinedUtcUnixSeconds ?? 0;
            int rank = m_State?.finalPlayerRank ?? 0;
            return $"{Id}/{m_Config.RaceId}/{joinedAt}/rank-{rank}";
        }

        private static int FindPlayerRank(IReadOnlyList<RaceRacerSnapshot> racers)
        {
            if (racers == null) return 0;
            foreach (RaceRacerSnapshot racer in racers)
                if (racer.IsPlayer) return racer.Rank;
            return 0;
        }

        private static DateTimeOffset FromUnixTime(long seconds) => DateTimeOffset.FromUnixTimeSeconds(Math.Max(0, seconds));

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        private void EnsureInitialized()
        {
            if (!m_IsInitialized) throw new InvalidOperationException("Race is not initialized.");
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
            EntriesChanged?.Invoke();
        }
    }
}
