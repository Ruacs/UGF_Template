using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lokas.Activities.Mining.UI;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    public static class MiningPageKeys
    {
        public const string Start = "start";
        public const string Main = "main";
        public const string Details = "details";
        public const string End = "end";
    }

    [Serializable]
    internal sealed class MiningPersistedState
    {
        public int eventId;
        public int stepId;
        public int pickaxeCount;
        public List<int> openCellIds = new List<int>();
        public List<int> collectedGemIds = new List<int>();
        public List<int> claimedRewardStepIds = new List<int>();
    }

    /// <summary>
    /// Mining 当前开发阶段的首页测试入口。模块持有并保存最近一次棋盘快照，
    /// 页面只负责展示和持续回传 MiningBoardModel 的状态。
    /// </summary>
    public sealed class MiningActivityModule : IActivityModule, IActivityEntryProvider
    {
        public const string Id = "mining";
        private const string MainEntryId = "main";
        private const string StateKey = "state";
        private const int StateSchemaVersion = 2;
        private const int DefaultStepId = 1;
        private const int DefaultPickaxeCount = 99;

        private readonly MiningActivityConfig m_Config;
        private readonly HashSet<int> m_ClaimedRewardStepIds = new HashSet<int>();
        private IActivityContext m_Context;
        private MiningPersistedState m_State;
        private bool m_IsInitialized;
        private bool m_IsClaimingReward;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private MiningTestModeModule m_TestModeModule;
        private readonly MiningBoardTestControls m_TestControls = new MiningBoardTestControls();
        private bool m_IgnoreBoardStateChanges;
#endif

        public string ModuleId => Id;
        public MiningActivityConfig Config => m_Config;

        // 测试入口在一次模块生命周期内不会动态变化，因此不需要保存订阅者。
        public event Action EntriesChanged
        {
            add { }
            remove { }
        }

        public MiningActivityModule(MiningActivityConfig config)
        {
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async UniTask InitializeAsync(IActivityContext context, CancellationToken cancellationToken)
        {
            if (m_IsInitialized) throw new InvalidOperationException("Mining is already initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            m_Config.ValidateConfiguration();
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            await LoadStateAsync(cancellationToken);
            m_IsInitialized = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterTestModeModule();
#endif
        }

        public async UniTask ShutdownAsync()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnregisterTestModeModule();
            m_TestControls.ClearListeners();
#endif
            if (m_Context != null && m_State != null)
                await SaveStateAsync(CancellationToken.None);
            m_IsInitialized = false;
            m_Context = null;
        }

        public IReadOnlyList<ActivityEntryInfo> GetEntries()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (m_IsInitialized)
            {
                return new[]
                {
                    new ActivityEntryInfo(MainEntryId, "Mining 测试", "mining", order: 70,
                        countdownEndUtc: m_Config.ScheduleConfig.EndUtc)
                };
            }
#endif
            return Array.Empty<ActivityEntryInfo>();
        }

        public async UniTask OpenEntryAsync(string entryId, CancellationToken cancellationToken)
        {
            if (!string.Equals(entryId, MainEntryId, StringComparison.Ordinal))
                throw new ArgumentException("Unknown Mining entry.", nameof(entryId));
            if (!m_IsInitialized || m_Context == null)
                throw new InvalidOperationException("Mining is not initialized.");

            await OpenMainAsync(cancellationToken);
        }

        public UniTask OpenStartAsync(CancellationToken cancellationToken = default)
        {
            return OpenModulePageAsync(MiningPageKeys.Start, this, cancellationToken);
        }

        public UniTask OpenMainAsync(CancellationToken cancellationToken = default)
        {
            return OpenModulePageAsync(MiningPageKeys.Main, CreateOpenArgs(), cancellationToken);
        }

        public UniTask OpenDetailsAsync(CancellationToken cancellationToken = default)
        {
            return OpenModulePageAsync(MiningPageKeys.Details, this, cancellationToken);
        }

        public UniTask OpenEndAsync(CancellationToken cancellationToken = default)
        {
            return OpenModulePageAsync(MiningPageKeys.End, this, cancellationToken);
        }

        private MiningBoardOpenArgs CreateOpenArgs()
        {
            if (m_State == null) ResetState();
            var openArgs = new MiningBoardOpenArgs(m_State.eventId, m_State.stepId, m_State.pickaxeCount,
                new List<int>(m_State.openCellIds), new List<int>(m_State.collectedGemIds),
                HandleBoardStateChanged, m_Config.ScheduleConfig.EndUtc, GetUtcNow,
                new List<int>(m_ClaimedRewardStepIds), stepId => ClaimRewardAsync(stepId));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_TestControls.SynchronizePickaxeCount(m_State.pickaxeCount);
            openArgs.AttachTestControls(m_TestControls);
#endif
            return openArgs;
        }

        private async UniTask OpenModulePageAsync(string pageKey, object arguments,
            CancellationToken cancellationToken)
        {
            if (!m_IsInitialized || m_Context == null)
                throw new InvalidOperationException("Mining is not initialized.");
            cancellationToken.ThrowIfCancellationRequested();
            ActivityUiOpenResult result = await m_Context.UI.OpenAsync(
                new ActivityPageRequest(pageKey, arguments: arguments), cancellationToken);
            if (result.Status != ActivityUiOpenStatus.Opened && result.Status != ActivityUiOpenStatus.Reused)
                throw new InvalidOperationException(result.Reason ?? $"Mining page '{pageKey}' could not be opened.");
        }

        public async UniTask<ActivityRewardReceipt> ClaimRewardAsync(int stepId,
            CancellationToken cancellationToken = default)
        {
            if (!m_IsInitialized || m_Context == null)
                throw new InvalidOperationException("Mining is not initialized.");
            if (stepId < 1 || stepId > MiningRewardConfig.RewardCount)
                throw new ArgumentOutOfRangeException(nameof(stepId));

            string grantId = BuildRewardGrantId(stepId);
            if (m_ClaimedRewardStepIds.Contains(stepId))
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.AlreadyGranted);
            if (stepId > GetCompletedStepCount())
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Failed,
                    reason: "The Mining stage has not been completed.");
            if (m_IsClaimingReward)
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Pending,
                    reason: "Another Mining reward claim is in progress.");

            MiningRewardDefinition reward = m_Config.GetStageReward(m_State.eventId, stepId);
            if (!CanGrantAll(reward.Entries))
                return new ActivityRewardReceipt(grantId, ActivityRewardStatus.Unsupported,
                    reason: "One or more Mining reward types are not mapped by this game.");

            var items = new List<ActivityRewardItem>(reward.Entries.Count);
            foreach (RewardEntry entry in reward.Entries)
                items.Add(new ActivityRewardItem(entry.ResourceKey, entry.Unit, entry.RequestAmount));

            m_IsClaimingReward = true;
            try
            {
                ActivityRewardReceipt receipt = await m_Context.Rewards.GrantAsync(
                    new ActivityRewardRequest(grantId, $"event-{m_State.eventId}", "profile", items),
                    cancellationToken);
                if (receipt.Status == ActivityRewardStatus.Granted ||
                    receipt.Status == ActivityRewardStatus.AlreadyGranted)
                {
                    m_ClaimedRewardStepIds.Add(stepId);
                    await SaveStateAsync(cancellationToken);
                }
                return receipt;
            }
            finally
            {
                m_IsClaimingReward = false;
            }
        }

        private void HandleBoardStateChanged(MiningBoardSnapshot snapshot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (m_IgnoreBoardStateChanges) return;
#endif
            if (!m_IsInitialized || m_Context == null || snapshot == null) return;
            if (!m_Config.TryGetStage(snapshot.EventId, snapshot.StepId, out _))
                throw new InvalidOperationException(
                    $"Mining snapshot references unknown Event={snapshot.EventId}, Step={snapshot.StepId}.");

            m_State = CreatePersistedState(snapshot);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_TestControls.SynchronizePickaxeCount(m_State.pickaxeCount);
#endif
            // ActivityStorage currently commits synchronously, while the interface remains async for
            // future backends. Keep the in-memory snapshot authoritative and retry it during shutdown
            // if an individual write reports a failure.
            SaveStateAsync(CancellationToken.None).Forget(Debug.LogException);
        }

        private async UniTask LoadStateAsync(CancellationToken cancellationToken)
        {
            ActivityStorageReadResult read = await m_Context.Storage.ReadAsync(StateKey, cancellationToken);
            if (read.Status == ActivityStorageReadStatus.Corrupt)
                throw new InvalidOperationException("Mining save data is corrupt: " + read.Error);
            if (read.Status == ActivityStorageReadStatus.Missing)
            {
                ResetState();
                return;
            }

            if (read.SchemaVersion < 1)
            {
                ResetState();
                await SaveStateAsync(cancellationToken);
                return;
            }

            MiningPersistedState loaded = JsonUtility.FromJson<MiningPersistedState>(read.Payload);
            if (loaded == null) throw new InvalidOperationException("Mining save data is invalid.");

            string loadedJson = JsonUtility.ToJson(loaded);
            m_State = NormalizeState(loaded);
            m_ClaimedRewardStepIds.Clear();
            foreach (int stepId in m_State.claimedRewardStepIds)
                m_ClaimedRewardStepIds.Add(stepId);
            if (read.SchemaVersion < StateSchemaVersion ||
                !string.Equals(loadedJson, JsonUtility.ToJson(m_State), StringComparison.Ordinal))
                await SaveStateAsync(cancellationToken);
        }

        private MiningPersistedState NormalizeState(MiningPersistedState state)
        {
            if (state == null || state.eventId != m_Config.ScheduleConfig.EventId ||
                !m_Config.TryGetStage(state.eventId, state.stepId,
                    out MiningStageDefinition stage))
                return CreateDefaultState();

            var model = new MiningBoardModel(m_Config, stage, state.pickaxeCount,
                state.openCellIds, state.collectedGemIds);
            return CreatePersistedState(model.CreateSnapshot(), state.claimedRewardStepIds);
        }

        private MiningPersistedState CreatePersistedState(MiningBoardSnapshot snapshot,
            IEnumerable<int> claimedRewardStepIds = null)
        {
            int completedStepCount = Mathf.Clamp(snapshot.StepId - 1 + (snapshot.IsStageComplete ? 1 : 0),
                0, MiningRewardConfig.RewardCount);
            var normalizedClaimedSteps = new List<int>();
            var seenClaimedSteps = new HashSet<int>();
            IEnumerable<int> claimedSteps = claimedRewardStepIds ?? m_ClaimedRewardStepIds;
            if (claimedSteps != null)
            {
                foreach (int claimedStepId in claimedSteps)
                {
                    if (claimedStepId < 1 || claimedStepId > completedStepCount ||
                        !seenClaimedSteps.Add(claimedStepId))
                        continue;
                    normalizedClaimedSteps.Add(claimedStepId);
                }
            }
            normalizedClaimedSteps.Sort();

            if (snapshot.IsStageComplete && snapshot.StepId < 5)
            {
                return new MiningPersistedState
                {
                    eventId = snapshot.EventId,
                    stepId = snapshot.StepId + 1,
                    pickaxeCount = Math.Max(0, snapshot.PickaxeCount),
                    claimedRewardStepIds = normalizedClaimedSteps
                };
            }

            return new MiningPersistedState
            {
                eventId = snapshot.EventId,
                stepId = snapshot.StepId,
                pickaxeCount = Math.Max(0, snapshot.PickaxeCount),
                openCellIds = snapshot.OpenCellIds != null
                    ? new List<int>(snapshot.OpenCellIds)
                    : new List<int>(),
                collectedGemIds = snapshot.CollectedGemIds != null
                    ? new List<int>(snapshot.CollectedGemIds)
                    : new List<int>(),
                claimedRewardStepIds = normalizedClaimedSteps
            };
        }

        private void ResetState()
        {
            m_ClaimedRewardStepIds.Clear();
            m_State = CreateDefaultState();
        }

        private MiningPersistedState CreateDefaultState()
        {
            return new MiningPersistedState
            {
                eventId = m_Config.ScheduleConfig.EventId,
                stepId = DefaultStepId,
                pickaxeCount = DefaultPickaxeCount
            };
        }

        private DateTimeOffset GetUtcNow()
        {
            return m_Context?.Clock?.UtcNow ?? DateTimeOffset.UtcNow;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal MiningBoardSnapshot GetTestSnapshot()
        {
            if (!m_IsInitialized) return null;
            if (m_State == null) ResetState();
            MiningStageDefinition stage = m_Config.GetStage(m_State.eventId, m_State.stepId);
            var model = new MiningBoardModel(m_Config, stage, m_State.pickaxeCount,
                m_State.openCellIds, m_State.collectedGemIds);
            return model.CreateSnapshot();
        }

        internal async UniTask OpenTestStageAsync(int eventId, int stepId,
            CancellationToken cancellationToken)
        {
            if (!m_IsInitialized || m_Context == null)
                throw new InvalidOperationException("Mining is not initialized.");
            m_Config.GetStage(eventId, stepId);
            cancellationToken.ThrowIfCancellationRequested();

            await ClosePagesForTestAsync();
            cancellationToken.ThrowIfCancellationRequested();
            int pickaxeCount = m_State != null ? m_State.pickaxeCount : DefaultPickaxeCount;
            m_ClaimedRewardStepIds.Clear();
            m_State = new MiningPersistedState
            {
                eventId = eventId,
                stepId = stepId,
                pickaxeCount = Math.Max(0, pickaxeCount)
            };
            await SaveStateAsync(cancellationToken);
            await OpenEntryAsync(MainEntryId, cancellationToken);
        }

        internal void SetTestPickaxeCount(int pickaxeCount)
        {
            if (!m_IsInitialized || m_Context == null) return;
            if (m_State == null) ResetState();
            int clampedCount = Math.Max(0, pickaxeCount);
            m_State.pickaxeCount = clampedCount;
            m_TestControls.SetPickaxeCount(clampedCount);
            SaveStateAsync(CancellationToken.None).Forget(Debug.LogException);
        }

        internal async UniTask ResetTestStateAsync(CancellationToken cancellationToken)
        {
            if (!m_IsInitialized || m_Context == null) return;
            cancellationToken.ThrowIfCancellationRequested();
            await ClosePagesForTestAsync();
            cancellationToken.ThrowIfCancellationRequested();
            ResetState();
            m_TestControls.SetPickaxeCount(m_State.pickaxeCount);
            await SaveStateAsync(cancellationToken);
        }

        private async UniTask ClosePagesForTestAsync()
        {
            m_IgnoreBoardStateChanges = true;
            try
            {
                await m_Context.UI.CloseAllAsync();
            }
            finally
            {
                m_IgnoreBoardStateChanges = false;
            }
        }

        private void RegisterTestModeModule()
        {
            if (GameEntry.TestMode == null || m_TestModeModule != null) return;
            var module = new MiningTestModeModule(this);
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
            m_State.claimedRewardStepIds = new List<int>(m_ClaimedRewardStepIds);
            m_State.claimedRewardStepIds.Sort();
            string payload = JsonUtility.ToJson(m_State);
            await m_Context.Storage.WriteAsync(StateKey, payload, StateSchemaVersion, cancellationToken);
            await m_Context.Storage.FlushAsync(cancellationToken);
        }

        private int GetCompletedStepCount()
        {
            if (m_State == null) return 0;
            MiningStageDefinition stage = m_Config.GetStage(m_State.eventId, m_State.stepId);
            var model = new MiningBoardModel(m_Config, stage, m_State.pickaxeCount,
                m_State.openCellIds, m_State.collectedGemIds);
            return Mathf.Clamp(m_State.stepId - 1 + (model.IsStageComplete ? 1 : 0),
                0, MiningRewardConfig.RewardCount);
        }

        private bool CanGrantAll(IReadOnlyList<RewardEntry> rewards)
        {
            if (m_Context?.Rewards == null || rewards == null || rewards.Count == 0) return false;
            foreach (RewardEntry reward in rewards)
            {
                if (reward == null || reward.RequestAmount <= 0) return false;
                try { reward.Validate(); }
                catch (InvalidOperationException) { return false; }
                if (!m_Context.Rewards.CanGrant(reward.ResourceKey, "profile")) return false;
            }
            return true;
        }

        private string BuildRewardGrantId(int stepId)
        {
            return $"{Id}/{m_State.eventId}/reward/{stepId}";
        }
    }
}
