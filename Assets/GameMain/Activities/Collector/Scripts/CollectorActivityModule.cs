using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lokas.Activities.Collector
{
    public static class CollectorPageKeys
    {
        public const string Main = "main";
    }

    public sealed class CollectorTaskSnapshot
    {
        public int Tier { get; }
        public int RequiredCount { get; }
        public CollectorRewardDefinition Reward { get; }
        public IReadOnlyList<RewardEntry> Rewards { get; }
        public bool IsReached { get; }
        public bool IsClaimed { get; }
        public bool CanClaim { get; }

        internal CollectorTaskSnapshot(CollectorTaskDefinition definition, bool reached, bool claimed,
            bool canClaim)
        {
            Tier = definition.Tier;
            RequiredCount = definition.RequiredCount;
            Reward = definition.Reward;
            Rewards = definition.Rewards;
            IsReached = reached;
            IsClaimed = claimed;
            CanClaim = canClaim;
        }
    }

    /// <summary>Collector 页面和首页使用的只读摘要；不暴露活动存档对象。</summary>
    public sealed class CollectorSnapshot
    {
        public string CollectorId { get; }
        public string DisplayName { get; }
        public bool IsActive { get; }
        public bool IsUnlocked { get; }
        /// <summary>当前档已收集数量；完成一档后会回到 0。</summary>
        public int CollectedCount { get; }
        /// <summary>当前档目标数量。</summary>
        public int TargetCount { get; }
        public int CurrentTaskProgress => CollectedCount;
        public float Progress01 { get; }
        public DateTimeOffset EndUtc { get; }
        public IReadOnlyList<CollectorTaskSnapshot> Tasks { get; }
        public CollectorTaskSnapshot CurrentTask { get; }
        public CollectorTaskSnapshot NextTask { get; }

        internal CollectorSnapshot(CollectorActivityConfig config, bool active, bool unlocked, int collectedCount,
            IReadOnlyList<CollectorTaskSnapshot> tasks, CollectorTaskSnapshot currentTask,
            CollectorTaskSnapshot nextTask, float progress01)
        {
            CollectorId = config.CollectorId;
            DisplayName = config.DisplayName;
            IsActive = active;
            IsUnlocked = unlocked;
            CollectedCount = collectedCount;
            TargetCount = currentTask != null ? currentTask.RequiredCount : config.TargetCount;
            Progress01 = Mathf.Clamp01(progress01);
            EndUtc = config.EndUtc;
            Tasks = tasks ?? Array.Empty<CollectorTaskSnapshot>();
            CurrentTask = currentTask;
            NextTask = nextTask;
        }
    }

    [Serializable]
    internal sealed class CollectorPersistedState
    {
        public string collectorId;
        // Progress belongs to the active task only. Completing a task advances the index
        // and resets collectedCount instead of carrying overflow into the next task.
        public int currentTaskIndex;
        public int collectedCount;
        public List<int> claimedTiers = new List<int>();
        public List<string> processedFactIds = new List<string>();
    }

    /// <summary>
    /// Collector 活动模块：从目标游戏的通关事实收集当前档数量，提供首页摘要和主页面数据。
    /// 规则状态、去重记录和领取状态均由本模块存储，公共宿主只管理生命周期。
    /// </summary>
    public sealed class CollectorActivityModule : IActivityModule, IActivityEntryProvider
    {
        public const string Id = "collector";
        private const string StateKey = "state";
        private const int StateSchemaVersion = 2;

        private readonly CollectorActivityConfig m_Config;
        private readonly IReadOnlyList<string> m_GameIds;
        private readonly HashSet<int> m_ClaimedTiers = new HashSet<int>();
        private readonly HashSet<string> m_ProcessedFactIds = new HashSet<string>(StringComparer.Ordinal);
        private IActivityContext m_Context;
        private IDisposable m_LevelCompletedSubscription;
        private CollectorPersistedState m_State;
        private bool m_IsInitialized;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private CollectorTestModeModule m_TestModeModule;
#endif

        public string ModuleId => Id;
        public CollectorActivityConfig Config => m_Config;
        public event Action EntriesChanged;
        public event Action StateChanged;

        public CollectorActivityModule(CollectorActivityConfig config, IReadOnlyList<string> gameIds = null)
        {
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_GameIds = gameIds == null ? Array.Empty<string>() : new List<string>(gameIds).AsReadOnly();
        }

        public async UniTask InitializeAsync(IActivityContext context, CancellationToken cancellationToken)
        {
            if (m_IsInitialized) throw new InvalidOperationException("Collector is already initialized.");
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
            m_IsInitialized = false;
            m_Context = null;
        }

        public IReadOnlyList<ActivityEntryInfo> GetEntries()
        {
            if (!m_IsInitialized) return Array.Empty<ActivityEntryInfo>();

            CollectorSnapshot snapshot = GetSnapshot();
            string unavailableReason = !snapshot.IsActive
                ? "活动未开启或已结束"
                : !snapshot.IsUnlocked
                    ? $"达到第 {m_Config.UnlockLevel} 关后开启"
                    : null;
            return new[]
            {
                new ActivityEntryInfo(
                    "main",
                    m_Config.EntryLabel,
                    "collector",
                    visible: true,
                    interactable: snapshot.IsActive && snapshot.IsUnlocked,
                    unavailableReasonKey: unavailableReason,
                    badgeCount: GetAvailableClaimCount(snapshot),
                    order: 80,
                    countdownEndUtc: snapshot.EndUtc)
            };
        }

        public async UniTask OpenEntryAsync(string entryId, CancellationToken cancellationToken)
        {
            if (!string.Equals(entryId, "main", StringComparison.Ordinal))
                throw new ArgumentException("Unknown collector entry.", nameof(entryId));
            EnsureCanOpen();
            await OpenPageAsync(CollectorPageKeys.Main, cancellationToken);
        }

        public CollectorSnapshot GetSnapshot()
        {
            DateTimeOffset now = m_Context?.Clock.UtcNow ?? DateTimeOffset.UtcNow;
            bool active = m_Config.IsActiveAt(now);
            bool unlocked = IsUnlocked();
            int taskCount = m_Config.Tasks?.Count ?? 0;
            int activeTaskIndex = Mathf.Clamp(m_State?.currentTaskIndex ?? 0, 0, taskCount);
            bool allTasksCompleted = activeTaskIndex >= taskCount;
            int collected = GetCurrentCollectedCount(activeTaskIndex);

            var tasks = new List<CollectorTaskSnapshot>(taskCount);
            CollectorTaskSnapshot current = null;
            CollectorTaskSnapshot next = null;
            for (int index = 0; index < taskCount; index++)
            {
                CollectorTaskDefinition definition = m_Config.Tasks[index];
                if (definition == null) continue;
                bool claimed = m_ClaimedTiers.Contains(definition.Tier);
                bool reached = index < activeTaskIndex;
                bool canClaim = active && unlocked && reached && !claimed && CanGrantAll(definition.Rewards);
                var task = new CollectorTaskSnapshot(definition, reached, claimed, canClaim);
                tasks.Add(task);

                if (index == activeTaskIndex && !allTasksCompleted)
                    current = task;
                else if (index == activeTaskIndex + 1 && !allTasksCompleted)
                    next = task;
            }

            // 全部档位完成后仍展示最后一档奖励，便于首页保持稳定的完成态。
            if (current == null && tasks.Count > 0)
                current = tasks[tasks.Count - 1];

            int required = current != null ? current.RequiredCount : m_Config.TargetCount;
            int displayedCollected = allTasksCompleted && current != null ? required : collected;
            float progress = allTasksCompleted ? 1f : required > 0
                ? Mathf.Clamp01(collected / (float)required)
                : 0f;
            return new CollectorSnapshot(m_Config, active, unlocked, displayedCollected, tasks.AsReadOnly(), current, next,
                progress);
        }

        /// <summary>领取一个已达成的里程碑；具体经济入账由活动奖励网关负责。</summary>
        public async UniTask<ActivityRewardReceipt> ClaimAsync(int tier,
            CancellationToken cancellationToken = default)
        {
            EnsureCanOpen();
            CollectorTaskDefinition definition = m_Config.GetTask(tier);
            if (definition == null) throw new ArgumentOutOfRangeException(nameof(tier));
            string grantId = BuildGrantId(tier);
            if (m_ClaimedTiers.Contains(tier))
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.AlreadyGranted);

            CollectorSnapshot snapshot = GetSnapshot();
            CollectorTaskSnapshot taskSnapshot = null;
            foreach (CollectorTaskSnapshot task in snapshot.Tasks)
            {
                if (task != null && task.Tier == tier)
                {
                    taskSnapshot = task;
                    break;
                }
            }
            if (taskSnapshot == null || !taskSnapshot.IsReached)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Failed,
                    reason: "Insufficient collector progress.");
            if (!CanGrantAll(definition.Rewards))
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Unsupported,
                    reason: "One or more reward types are not mapped by this game.");

            var items = new List<ActivityRewardItem>(definition.Rewards.Count);
            foreach (RewardEntry reward in definition.Rewards)
                items.Add(new ActivityRewardItem(reward.ResourceKey, reward.Unit, reward.RequestAmount));

            ActivityRewardReceipt receipt = await m_Context.Rewards.GrantAsync(
                new ActivityRewardRequest(grantId, m_Config.CollectorId, "profile", items), cancellationToken);
            if (receipt.Status == ActivityRewardStatus.Granted || receipt.Status == ActivityRewardStatus.AlreadyGranted)
            {
                m_ClaimedTiers.Add(tier);
                await SaveStateAsync(cancellationToken);
                NotifyChanged();
            }
            return receipt;
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

#endif

        /// <summary>
        /// 测试面板批量注入通关事实时只保存和通知一次。一次模拟多局不会让首页
        /// 每个事实都重启进度/奖励动画，也不会在同一帧创建几十条 DOTween 序列。
        /// </summary>
        internal void ProcessTestFacts(IReadOnlyList<ActivityLevelCompletedFact> facts)
        {
            if (!m_IsInitialized || facts == null || facts.Count == 0) return;
            bool accepted = false;
            foreach (ActivityLevelCompletedFact fact in facts)
                accepted |= ApplyLevelCompletedFact(fact);
            if (accepted) PersistStateAndNotify();
        }

        private void OnLevelCompleted(ActivityLevelCompletedFact fact)
        {
            if (ApplyLevelCompletedFact(fact)) PersistStateAndNotify();
        }

        private bool ApplyLevelCompletedFact(ActivityLevelCompletedFact fact)
        {
            if (!m_IsInitialized || m_State == null || fact == null || !fact.Won ||
                !IsGameSource(fact.GameId) || !m_Config.IsActiveAt(m_Context.Clock.UtcNow)) return false;
            int taskCount = m_Config.Tasks?.Count ?? 0;
            if (m_State.currentTaskIndex >= taskCount) return false;
            if (!m_ProcessedFactIds.Add(fact.FactId)) return false;

            CollectorTaskDefinition currentTask = m_Config.Tasks[m_State.currentTaskIndex];
            int amount = Mathf.Max(1, m_Config.CollectionPerWin);
            // A win can finish the current task, but overflow is intentionally discarded.
            // The next task always starts from 0 as required by the activity rules.
            m_State.collectedCount = Mathf.Min(currentTask.RequiredCount,
                m_State.collectedCount + amount);
            if (m_State.collectedCount >= currentTask.RequiredCount)
            {
                m_State.currentTaskIndex++;
                m_State.collectedCount = 0;
            }
            return true;
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
            // A module instance can be initialized again after a clean shutdown in tests or
            // an editor session; never carry the previous in-memory sets into the new load.
            m_ClaimedTiers.Clear();
            m_ProcessedFactIds.Clear();
            ActivityStorageReadResult read = await m_Context.Storage.ReadAsync(StateKey, cancellationToken);
            if (read.Status == ActivityStorageReadStatus.Corrupt)
                throw new InvalidOperationException("Collector save data is corrupt: " + read.Error);
            if (read.Status == ActivityStorageReadStatus.Missing)
            {
                ResetState();
                return;
            }

            // Schema 1 stored one cumulative counter. It cannot be losslessly interpreted
            // after switching to per-task progress, so start the activity with a clean state.
            if (read.SchemaVersion < StateSchemaVersion)
            {
                ResetState();
                await SaveStateAsync(cancellationToken);
                return;
            }

            m_State = JsonUtility.FromJson<CollectorPersistedState>(read.Payload);
            if (m_State == null) throw new InvalidOperationException("Collector save data is invalid.");
            if (!string.Equals(m_State.collectorId, m_Config.CollectorId, StringComparison.Ordinal))
            {
                ResetState();
                await SaveStateAsync(cancellationToken);
                return;
            }

            int taskCount = m_Config.Tasks?.Count ?? 0;
            m_State.currentTaskIndex = Mathf.Clamp(m_State.currentTaskIndex, 0, taskCount);
            int required = GetRequiredCount(m_State.currentTaskIndex);
            m_State.collectedCount = required > 0
                ? Mathf.Clamp(m_State.collectedCount, 0, required)
                : 0;
            m_State.claimedTiers ??= new List<int>();
            m_State.processedFactIds ??= new List<string>();
            foreach (int tier in m_State.claimedTiers)
                if (m_Config.GetTask(tier) != null) m_ClaimedTiers.Add(tier);
            foreach (string factId in m_State.processedFactIds)
                if (!string.IsNullOrWhiteSpace(factId)) m_ProcessedFactIds.Add(factId);
        }

        private void ResetState()
        {
            m_ClaimedTiers.Clear();
            m_ProcessedFactIds.Clear();
            m_State = new CollectorPersistedState
            {
                collectorId = m_Config.CollectorId,
                currentTaskIndex = 0,
                collectedCount = 0
            };
        }

        private async UniTask SaveStateAsync(CancellationToken cancellationToken)
        {
            if (m_Context == null || m_State == null) return;
            m_State.collectorId = m_Config.CollectorId;
            m_State.claimedTiers = new List<int>(m_ClaimedTiers);
            m_State.claimedTiers.Sort();
            m_State.processedFactIds = new List<string>(m_ProcessedFactIds);
            m_State.processedFactIds.Sort(StringComparer.Ordinal);
            await m_Context.Storage.WriteAsync(StateKey, JsonUtility.ToJson(m_State), StateSchemaVersion,
                cancellationToken);
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

        private int GetAvailableClaimCount(CollectorSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsActive || !snapshot.IsUnlocked) return 0;
            int count = 0;
            foreach (CollectorTaskSnapshot task in snapshot.Tasks)
                if (task != null && task.CanClaim) count++;
            return count;
        }

        private int GetCurrentCollectedCount(int activeTaskIndex)
        {
            int required = GetRequiredCount(activeTaskIndex);
            if (required <= 0 || m_State == null) return 0;
            return Mathf.Clamp(m_State.collectedCount, 0, required);
        }

        private int GetRequiredCount(int taskIndex)
        {
            if (taskIndex < 0 || m_Config.Tasks == null || taskIndex >= m_Config.Tasks.Count) return 0;
            CollectorTaskDefinition task = m_Config.Tasks[taskIndex];
            return task == null ? 0 : task.RequiredCount;
        }

        private void PersistStateAndNotify()
        {
            SaveStateAsync(m_Context.LifetimeToken).Forget(Debug.LogException);
            NotifyChanged();
        }

        private async UniTask OpenPageAsync(string pageKey, CancellationToken cancellationToken)
        {
            ActivityUiOpenResult result = await m_Context.UI.OpenAsync(
                new ActivityPageRequest(pageKey, arguments: this), cancellationToken);
            if (result.Status != ActivityUiOpenStatus.Opened && result.Status != ActivityUiOpenStatus.Reused)
                throw new InvalidOperationException(result.Reason ?? "The collector page could not be opened.");
        }

        private void EnsureCanOpen()
        {
            if (!m_IsInitialized) throw new InvalidOperationException("Collector is not initialized.");
            if (!m_Config.IsActiveAt(m_Context.Clock.UtcNow)) throw new InvalidOperationException("Collector is not active.");
            if (!IsUnlocked()) throw new InvalidOperationException($"Collector unlocks at level {m_Config.UnlockLevel}.");
        }

        private string BuildGrantId(int tier) =>
            string.Concat(Id, "/", m_Config.CollectorId, "/tier/", tier.ToString(CultureInfo.InvariantCulture));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void RegisterTestModeModule()
        {
            if (GameEntry.TestMode == null || m_TestModeModule != null) return;
            var module = new CollectorTestModeModule(this);
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
