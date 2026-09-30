using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lokas.Activities.SeasonPass
{
    public static class SeasonPassPageKeys
    {
        public const string Main = "main";
        public const string Rules = "rules";
        public const string GoldPassPurchase = "gold_pass_purchase";
    }

    public enum SeasonPassTrack { Free, Premium }

    public sealed class SeasonPassTierSnapshot
    {
        public int Tier { get; }
        public int RequiredCharge { get; }
        public SeasonPassRewardDefinition FreeReward { get; }
        public SeasonPassRewardDefinition PremiumReward { get; }
        public IReadOnlyList<RewardEntry> FreeRewards { get; }
        public IReadOnlyList<RewardEntry> PremiumRewards { get; }
        public bool FreeClaimed { get; }
        public bool PremiumClaimed { get; }
        public bool CanClaimFree { get; }
        public bool CanClaimPremium { get; }

        internal SeasonPassTierSnapshot(SeasonPassTierDefinition definition, bool freeClaimed, bool premiumClaimed,
            bool canClaimFree, bool canClaimPremium = false)
        {
            Tier = definition.Tier;
            RequiredCharge = definition.RequiredCharge;
            FreeReward = definition.FreeReward;
            PremiumReward = definition.PremiumReward;
            FreeRewards = definition.FreeRewards;
            PremiumRewards = definition.PremiumRewards;
            FreeClaimed = freeClaimed;
            PremiumClaimed = premiumClaimed;
            CanClaimFree = canClaimFree;
            CanClaimPremium = canClaimPremium;
        }
    }

    /// <summary>通行证顶部当前档位的局部充能进度。</summary>
    public sealed class SeasonPassTierProgress
    {
        public int TargetTier { get; }
        public int CurrentCharge { get; }
        public int RequiredCharge { get; }
        public float FillAmount { get; }

        private SeasonPassTierProgress(int targetTier, int currentCharge, int requiredCharge, float fillAmount)
        {
            TargetTier = targetTier;
            CurrentCharge = currentCharge;
            RequiredCharge = requiredCharge;
            FillAmount = fillAmount;
        }

        internal static SeasonPassTierProgress Create(int totalCharge, IReadOnlyList<SeasonPassTierSnapshot> tiers)
        {
            if (tiers == null || tiers.Count == 0)
                return new SeasonPassTierProgress(0, 0, 0, 0f);

            int charge = Math.Max(0, totalCharge);
            int chargeBeforeTier = 0;
            for (int i = 0; i < tiers.Count; i++)
            {
                int requiredCharge = Math.Max(0, tiers[i].RequiredCharge);
                int completionCharge = checked(chargeBeforeTier + requiredCharge);
                if (charge < completionCharge)
                {
                    int currentCharge = Mathf.Clamp(charge - chargeBeforeTier, 0, requiredCharge);
                    float fillAmount = requiredCharge > 0 ? currentCharge / (float)requiredCharge : 1f;
                    return new SeasonPassTierProgress(tiers[i].Tier, currentCharge, requiredCharge, fillAmount);
                }
                chargeBeforeTier = completionCharge;
            }

            SeasonPassTierSnapshot lastTier = tiers[tiers.Count - 1];
            int lastRequiredCharge = Math.Max(0, lastTier.RequiredCharge);
            return new SeasonPassTierProgress(lastTier.Tier, lastRequiredCharge, lastRequiredCharge, 1f);
        }
    }

    public sealed class SeasonPassSnapshot
    {
        public const int BonusBankRequiredProgress = 10;

        public string PassId { get; }
        public string DisplayName { get; }
        public int Charge { get; }
        public int ChargePerCompletedLevel { get; }
        public int UnlockLevel { get; }
        public bool IsActive { get; }
        public bool IsUnlocked { get; }
        public bool IsPremiumActivated { get; }
        public DateTimeOffset EndUtc { get; }
        public IReadOnlyList<SeasonPassTierSnapshot> Tiers { get; }
        public bool IsBasePassCompleted { get; }
        public int BonusBankTier { get; }
        public int BonusBankProgress { get; }
        public bool IsBonusBankUnlocked => IsPremiumActivated;
        /// <summary>供顶部进度条显示的当前目标档位局部充能。</summary>
        public SeasonPassTierProgress CurrentTierProgress { get; }
        public int CurrentTier => CurrentTierProgress.TargetTier;

        internal SeasonPassSnapshot(SeasonPassActivityConfig config, int charge, int bonusBankTier,
            int bonusBankProgress, bool isActive, bool isUnlocked, bool isPremiumActivated,
            IReadOnlyList<SeasonPassTierSnapshot> tiers)
        {
            PassId = config.PassId;
            DisplayName = config.DisplayName;
            Charge = charge;
            ChargePerCompletedLevel = config.ChargePerCompletedLevel;
            UnlockLevel = config.UnlockLevel;
            IsActive = isActive;
            IsUnlocked = isUnlocked;
            IsPremiumActivated = isPremiumActivated;
            EndUtc = config.EndUtc;
            Tiers = tiers;
            int totalRequiredCharge = 0;
            foreach (SeasonPassTierSnapshot tier in tiers)
                totalRequiredCharge = checked(totalRequiredCharge + Math.Max(0, tier.RequiredCharge));
            IsBasePassCompleted = charge >= totalRequiredCharge;
            BonusBankTier = IsBasePassCompleted ? Math.Max(0, bonusBankTier) : 0;
            BonusBankProgress = IsBasePassCompleted
                ? Mathf.Clamp(bonusBankProgress, 0, BonusBankRequiredProgress - 1)
                : 0;
            CurrentTierProgress = SeasonPassTierProgress.Create(charge, tiers);
        }

        /// <summary>单个里程碑只显示自身的状态：已达成满格、当前目标显示局部进度、后续档位为空。</summary>
        public float GetTierFillAmount(SeasonPassTierSnapshot tier)
        {
            if (tier == null) throw new ArgumentNullException(nameof(tier));
            int chargeBeforeTier = 0;
            foreach (SeasonPassTierSnapshot candidate in Tiers)
            {
                int requiredCharge = Math.Max(0, candidate.RequiredCharge);
                int completionCharge = checked(chargeBeforeTier + requiredCharge);
                if (candidate.Tier == tier.Tier)
                {
                    if (Charge >= completionCharge) return 1f;
                    return requiredCharge > 0
                        ? Mathf.Clamp01((Charge - chargeBeforeTier) / (float)requiredCharge)
                        : 1f;
                }
                chargeBeforeTier = completionCharge;
            }
            throw new ArgumentException("The tier does not belong to this snapshot.", nameof(tier));
        }
    }

    public sealed class SeasonPassClaimPresentation
    {
        public SeasonPassTrack Track { get; }
        public int Tier { get; }
        public IReadOnlyList<RewardEntry> Rewards { get; }
        public ActivityRewardReceipt Receipt { get; }

        public SeasonPassClaimPresentation(SeasonPassTrack track, int tier, IReadOnlyList<RewardEntry> rewards,
            ActivityRewardReceipt receipt)
        {
            Track = track;
            Tier = tier;
            Rewards = rewards ?? Array.Empty<RewardEntry>();
            Receipt = receipt;
        }
    }

    [Serializable]
    internal sealed class SeasonPassPersistedState
    {
        public string passId;
        public int charge;
        public int bonusBankTier;
        public int bonusBankProgress;
        public bool premiumActivated;
        public List<int> claimedFreeTiers = new List<int>();
        public List<int> claimedPremiumTiers = new List<int>();
        public List<string> processedFactIds = new List<string>();
    }

    /// <summary>通行证业务模块。它只依赖活动契约；具体页面、存档和奖励映射由宿主提供。</summary>
    public sealed class SeasonPassActivityModule : IActivityModule, IActivityEntryProvider
    {
        public const string Id = "season_pass";
        private const string StateKey = "state";
        private const int StateSchemaVersion = 3;

        private readonly SeasonPassActivityConfig m_Config;
        private readonly IReadOnlyList<string> m_GameIds;
        private readonly HashSet<int> m_ClaimedFreeTiers = new HashSet<int>();
        private readonly HashSet<int> m_ClaimedPremiumTiers = new HashSet<int>();
        private readonly HashSet<string> m_ProcessedFactIds = new HashSet<string>(StringComparer.Ordinal);
        private IActivityContext m_Context;
        private IDisposable m_LevelCompletedSubscription;
        private SeasonPassPersistedState m_State;
        private bool m_IsInitialized;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private SeasonPassTestModeModule m_TestModeModule;
#endif

        public string ModuleId => Id;
        public SeasonPassActivityConfig Config => m_Config;
        public event Action EntriesChanged;
        public event Action StateChanged;

        public SeasonPassActivityModule(SeasonPassActivityConfig config, IReadOnlyList<string> gameIds = null)
        {
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_GameIds = gameIds == null ? Array.Empty<string>() : gameIds.ToArray();
        }

        public async UniTask InitializeAsync(IActivityContext context, CancellationToken cancellationToken)
        {
            if (m_IsInitialized) throw new InvalidOperationException("Season pass has already been initialized.");
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
        }

        public IReadOnlyList<ActivityEntryInfo> GetEntries()
        {
            if (!m_IsInitialized) return Array.Empty<ActivityEntryInfo>();
            DateTimeOffset now = m_Context.Clock.UtcNow;
            bool active = m_Config.IsActiveAt(now);
            bool unlocked = IsUnlocked();
            string unavailableReason = !active ? "活动未开启或已结束" : !unlocked ? $"达到第 {m_Config.UnlockLevel} 关后开启" : null;
            return new[]
            {
                new ActivityEntryInfo("main", m_Config.EntryLabel, "season-pass", visible: true,
                    interactable: active && unlocked, unavailableReasonKey: unavailableReason,
                    badgeCount: GetAvailableFreeClaimCount(active, unlocked), order: 100, countdownEndUtc: m_Config.EndUtc)
            };
        }

        public async UniTask OpenEntryAsync(string entryId, CancellationToken cancellationToken)
        {
            if (!string.Equals(entryId, "main", StringComparison.Ordinal))
                throw new ArgumentException("Unknown season pass entry.", nameof(entryId));
            EnsureCanOpen();
            await OpenPageAsync(SeasonPassPageKeys.Main, this, cancellationToken);
        }

        public SeasonPassSnapshot GetSnapshot()
        {
            DateTimeOffset now = m_Context?.Clock.UtcNow ?? DateTimeOffset.UtcNow;
            bool active = m_Config.IsActiveAt(now);
            bool unlocked = IsUnlocked();
            var tiers = new List<SeasonPassTierSnapshot>(m_Config.Tiers.Count);
            foreach (SeasonPassTierDefinition tier in m_Config.Tiers)
            {
                bool freeClaimed = m_ClaimedFreeTiers.Contains(tier.Tier);
                bool premiumClaimed = m_ClaimedPremiumTiers.Contains(tier.Tier);
                bool reached = HasReachedTier(tier.Tier);
                bool canClaimFree = active && unlocked && !freeClaimed && reached && CanGrantAll(tier.FreeRewards);
                bool canClaimPremium = active && unlocked && m_State != null && m_State.premiumActivated
                    && !premiumClaimed && reached && CanGrantAll(tier.PremiumRewards);
                tiers.Add(new SeasonPassTierSnapshot(tier, freeClaimed, premiumClaimed, canClaimFree, canClaimPremium));
            }
            return new SeasonPassSnapshot(m_Config, m_State?.charge ?? 0, m_State?.bonusBankTier ?? 0,
                m_State?.bonusBankProgress ?? 0, active, unlocked,
                m_State != null && m_State.premiumActivated, tiers.AsReadOnly());
        }

        public async UniTask<ActivityRewardReceipt> ClaimFreeAsync(int tier, CancellationToken cancellationToken = default)
        {
            EnsureCanOpen();
            SeasonPassTierDefinition definition = m_Config.GetTier(tier);
            if (definition == null) throw new ArgumentOutOfRangeException(nameof(tier));
            if (m_ClaimedFreeTiers.Contains(tier))
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Free, tier), ActivityRewardStatus.AlreadyGranted);
            if (!HasReachedTier(tier))
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Free, tier), ActivityRewardStatus.Failed, reason: "Insufficient pass charge.");
            if (definition.FreeRewards.Count == 0)
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Free, tier), ActivityRewardStatus.Unsupported, reason: "This tier has no free reward.");
            if (!CanGrantAll(definition.FreeRewards))
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Free, tier), ActivityRewardStatus.Unsupported,
                    reason: "One or more reward types are not mapped by this game.");

            var request = new ActivityRewardRequest(BuildGrantId(SeasonPassTrack.Free, tier), m_Config.PassId, "profile",
                definition.FreeRewards.Select(reward => new ActivityRewardItem(reward.ResourceKey, reward.Unit, reward.RequestAmount)));
            ActivityRewardReceipt receipt = await m_Context.Rewards.GrantAsync(request, cancellationToken);
            if (receipt.Status == ActivityRewardStatus.Granted || receipt.Status == ActivityRewardStatus.AlreadyGranted)
            {
                m_ClaimedFreeTiers.Add(tier);
                await SaveStateAsync(cancellationToken);
                NotifyChanged();
            }
            return receipt;
        }

        public async UniTask<ActivityRewardReceipt> ClaimPremiumAsync(int tier, CancellationToken cancellationToken = default)
        {
            EnsureCanOpen();
            SeasonPassTierDefinition definition = m_Config.GetTier(tier);
            if (definition == null) throw new ArgumentOutOfRangeException(nameof(tier));
            if (!m_State.premiumActivated)
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Premium, tier), ActivityRewardStatus.Failed,
                    reason: "Gold Pass is not activated.");
            if (m_ClaimedPremiumTiers.Contains(tier))
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Premium, tier), ActivityRewardStatus.AlreadyGranted);
            if (!HasReachedTier(tier))
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Premium, tier), ActivityRewardStatus.Failed,
                    reason: "Insufficient pass charge.");
            if (definition.PremiumRewards.Count == 0)
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Premium, tier), ActivityRewardStatus.Unsupported,
                    reason: "This tier has no Gold Pass reward.");
            if (!CanGrantAll(definition.PremiumRewards))
                return new ActivityRewardReceipt(BuildGrantId(SeasonPassTrack.Premium, tier), ActivityRewardStatus.Unsupported,
                    reason: "One or more reward types are not mapped by this game.");

            var request = new ActivityRewardRequest(BuildGrantId(SeasonPassTrack.Premium, tier), m_Config.PassId, "profile",
                definition.PremiumRewards.Select(reward => new ActivityRewardItem(reward.ResourceKey, reward.Unit, reward.RequestAmount)));
            ActivityRewardReceipt receipt = await m_Context.Rewards.GrantAsync(request, cancellationToken);
            if (receipt.Status == ActivityRewardStatus.Granted || receipt.Status == ActivityRewardStatus.AlreadyGranted)
            {
                m_ClaimedPremiumTiers.Add(tier);
                await SaveStateAsync(cancellationToken);
                NotifyChanged();
            }
            return receipt;
        }

        public UniTask OpenRulesAsync(CancellationToken cancellationToken = default)
        {
            return OpenPageAsync(SeasonPassPageKeys.Rules, this, cancellationToken);
        }

        public UniTask OpenGoldPassPurchaseAsync(CancellationToken cancellationToken = default)
        {
            EnsureCanOpen();
            return OpenPageAsync(SeasonPassPageKeys.GoldPassPurchase, this, cancellationToken);
        }

        /// <summary>Temporary purchase seam. Replace the successful branch with a store receipt validation later.</summary>
        public async UniTask<bool> ActivatePremiumAsync(CancellationToken cancellationToken = default)
        {
            EnsureCanOpen();
            if (m_State.premiumActivated) return true;

            // TODO: Replace this mock success with the game's store purchase and receipt validation.
            m_State.premiumActivated = true;
            await SaveStateAsync(cancellationToken);
            NotifyChanged();
            Debug.Log($"[SeasonPass] Gold Pass activation simulated successfully: {m_Config.PassId}");
            return true;
        }

        private void OnLevelCompleted(ActivityLevelCompletedFact fact)
        {
            if (!m_IsInitialized || fact == null || !fact.Won || !m_Config.IsActiveAt(m_Context.Clock.UtcNow)) return;
            if (!m_ProcessedFactIds.Add(fact.FactId)) return;

            int totalRequiredCharge = GetTotalRequiredCharge();
            if (m_State.charge < totalRequiredCharge)
            {
                long updatedCharge = (long)m_State.charge + m_Config.ChargePerCompletedLevel;
                m_State.charge = (int)Math.Min(totalRequiredCharge, updatedCharge);
            }
            else
            {
                m_State.bonusBankProgress++;
                if (m_State.bonusBankProgress >= SeasonPassSnapshot.BonusBankRequiredProgress)
                {
                    m_State.bonusBankProgress = 0;
                    if (m_State.bonusBankTier < int.MaxValue) m_State.bonusBankTier++;
                }
            }
            SaveStateAsync(m_Context.LifetimeToken).Forget(Debug.LogException);
            NotifyChanged();
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
            var module = new SeasonPassTestModeModule(this);
            if (GameEntry.TestMode.RegisterModule(module)) m_TestModeModule = module;
        }

        private void UnregisterTestModeModule()
        {
            if (m_TestModeModule == null) return;
            GameEntry.TestMode?.UnregisterModule(m_TestModeModule);
            m_TestModeModule = null;
        }
#endif

        private async UniTask LoadStateAsync(CancellationToken cancellationToken)
        {
            ActivityStorageReadResult read = await m_Context.Storage.ReadAsync(StateKey, cancellationToken);
            if (read.Status == ActivityStorageReadStatus.Corrupt)
                throw new InvalidOperationException("Season pass save data is corrupt: " + read.Error);
            if (read.Status == ActivityStorageReadStatus.Missing)
            {
                ResetState();
                return;
            }

            m_State = JsonUtility.FromJson<SeasonPassPersistedState>(read.Payload);
            if (m_State == null) throw new InvalidOperationException("Season pass save data is invalid.");
            if (!string.Equals(m_State.passId, m_Config.PassId, StringComparison.Ordinal))
            {
                ResetState();
                await SaveStateAsync(cancellationToken);
                return;
            }

            m_State.charge = Math.Max(0, m_State.charge);
            int totalRequiredCharge = GetTotalRequiredCharge();
            bool migrateLegacyState = read.SchemaVersion < StateSchemaVersion;
            if (read.SchemaVersion < 2)
            {
                long overflowCharge = Math.Max(0L, (long)m_State.charge - totalRequiredCharge);
                m_State.charge = Math.Min(m_State.charge, totalRequiredCharge);
                long overflowWins = overflowCharge / m_Config.ChargePerCompletedLevel;
                SetBonusBankProgressFromWins(overflowWins);
            }
            else
            {
                m_State.charge = Math.Min(m_State.charge, totalRequiredCharge);
                if (read.SchemaVersion < StateSchemaVersion)
                {
                    // Schema 2 stored charge units (capped at 10), not won-level count or bank tier.
                    int legacyProgress = Mathf.Clamp(m_State.bonusBankProgress, 0,
                        SeasonPassSnapshot.BonusBankRequiredProgress);
                    long legacyWins = (legacyProgress + (long)m_Config.ChargePerCompletedLevel - 1)
                        / m_Config.ChargePerCompletedLevel;
                    SetBonusBankProgressFromWins(legacyWins);
                }
                else
                {
                    m_State.bonusBankTier = Math.Max(0, m_State.bonusBankTier);
                    m_State.bonusBankProgress = Mathf.Clamp(m_State.bonusBankProgress, 0,
                        SeasonPassSnapshot.BonusBankRequiredProgress - 1);
                }
            }
            if (m_State.charge < totalRequiredCharge)
            {
                m_State.bonusBankTier = 0;
                m_State.bonusBankProgress = 0;
            }
            m_State.claimedFreeTiers ??= new List<int>();
            m_State.claimedPremiumTiers ??= new List<int>();
            m_State.processedFactIds ??= new List<string>();
            foreach (int tier in m_State.claimedFreeTiers.Where(IsKnownTier)) m_ClaimedFreeTiers.Add(tier);
            foreach (int tier in m_State.claimedPremiumTiers.Where(IsKnownTier)) m_ClaimedPremiumTiers.Add(tier);
            foreach (string factId in m_State.processedFactIds.Where(id => !string.IsNullOrWhiteSpace(id))) m_ProcessedFactIds.Add(factId);
            if (migrateLegacyState) await SaveStateAsync(cancellationToken);
        }

        private void ResetState()
        {
            m_ClaimedFreeTiers.Clear();
            m_ClaimedPremiumTiers.Clear();
            m_ProcessedFactIds.Clear();
            m_State = new SeasonPassPersistedState { passId = m_Config.PassId, charge = 0 };
        }

        private async UniTask SaveStateAsync(CancellationToken cancellationToken)
        {
            if (m_Context == null || m_State == null) return;
            m_State.passId = m_Config.PassId;
            m_State.claimedFreeTiers = m_ClaimedFreeTiers.OrderBy(tier => tier).ToList();
            m_State.claimedPremiumTiers = m_ClaimedPremiumTiers.OrderBy(tier => tier).ToList();
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
            if (rewards == null || rewards.Count == 0) return false;
            foreach (RewardEntry reward in rewards)
            {
                if (reward == null || reward.RequestAmount <= 0) return false;
                try { reward.Validate(); }
                catch (InvalidOperationException) { return false; }
                if (!m_Context.Rewards.CanGrant(reward.ResourceKey, "profile")) return false;
            }
            return true;
        }

        private int GetAvailableFreeClaimCount(bool active, bool unlocked)
        {
            if (!active || !unlocked) return 0;
            int result = 0;
            foreach (SeasonPassTierDefinition tier in m_Config.Tiers)
            {
                if (!m_ClaimedFreeTiers.Contains(tier.Tier) && HasReachedTier(tier.Tier) && CanGrantAll(tier.FreeRewards))
                    result++;
            }
            return result;
        }

        private bool HasReachedTier(int tier)
        {
            int completionCharge = 0;
            foreach (SeasonPassTierDefinition definition in m_Config.Tiers)
            {
                completionCharge = checked(completionCharge + Math.Max(0, definition.RequiredCharge));
                if (definition.Tier == tier) return m_State.charge >= completionCharge;
            }
            return false;
        }

        private int GetTotalRequiredCharge()
        {
            int totalRequiredCharge = 0;
            foreach (SeasonPassTierDefinition definition in m_Config.Tiers)
                totalRequiredCharge = checked(totalRequiredCharge + Math.Max(0, definition.RequiredCharge));
            return totalRequiredCharge;
        }

        private void SetBonusBankProgressFromWins(long bonusBankWins)
        {
            long requiredProgress = SeasonPassSnapshot.BonusBankRequiredProgress;
            long tier = Math.Max(0L, bonusBankWins) / requiredProgress;
            m_State.bonusBankTier = (int)Math.Min(int.MaxValue, tier);
            m_State.bonusBankProgress = (int)(Math.Max(0L, bonusBankWins) % requiredProgress);
        }

        private async UniTask OpenPageAsync(string pageKey, object userData, CancellationToken cancellationToken)
        {
            ActivityUiOpenResult result = await m_Context.UI.OpenAsync(new ActivityPageRequest(pageKey, arguments: userData), cancellationToken);
            if (result.Status != ActivityUiOpenStatus.Opened && result.Status != ActivityUiOpenStatus.Reused)
                throw new InvalidOperationException(result.Reason ?? "The season pass page could not be opened.");
        }

        private void EnsureCanOpen()
        {
            if (!m_IsInitialized) throw new InvalidOperationException("Season pass is not initialized.");
            if (!m_Config.IsActiveAt(m_Context.Clock.UtcNow)) throw new InvalidOperationException("Season pass is not active.");
            if (!IsUnlocked()) throw new InvalidOperationException($"Season pass unlocks at level {m_Config.UnlockLevel}.");
        }

        private bool IsKnownTier(int tier) => m_Config.GetTier(tier) != null;

        private string BuildGrantId(SeasonPassTrack track, int tier)
        {
            return $"{Id}/{m_Config.PassId}/{track.ToString().ToLowerInvariant()}/{tier}";
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
            EntriesChanged?.Invoke();
        }
    }
}
