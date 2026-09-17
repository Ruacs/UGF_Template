using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    public sealed class LocalActivityClock : IActivityClock
    {
        private readonly Stopwatch m_Watch = Stopwatch.StartNew();
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public double MonotonicSeconds => m_Watch.Elapsed.TotalSeconds;
        public ActivityClockSource Source => ActivityClockSource.LocalEstimated;
    }

    /// <summary>客户端装配入口；自带 GF 本地奖励和页面适配器，项目可按需要替换为服务端实现。</summary>
    public sealed class LocalActivityServicesFactory : IActivityServicesFactory
    {
        private readonly IActivityStorageBackend m_Storage;
        private readonly IActivityGameFacts m_Facts;
        private readonly IActivityClock m_Clock;
        private readonly Func<ActivityScope, IActivityRewardGateway> m_Rewards;
        private readonly Func<ActivityScope, IActivityRewardReceiver> m_RewardReceiver;
        private readonly Func<ActivityScope, IActivityUI> m_UI;

        public LocalActivityServicesFactory(IActivityStorageBackend storage, IActivityGameFacts facts,
            Func<ActivityScope, IActivityRewardGateway> rewards = null, Func<ActivityScope, IActivityUI> ui = null,
            IActivityClock clock = null, Func<ActivityScope, IActivityRewardReceiver> rewardReceiver = null)
        {
            m_Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
            m_Rewards = rewards;
            m_RewardReceiver = rewardReceiver;
            m_UI = ui;
            m_Clock = clock ?? new LocalActivityClock();
        }

        public ActivityServices CreateServices(ActivityScope scope)
        {
            var storage = new ActivityStorage(m_Storage, scope.ProfileId, scope.ModuleId);
            return new ActivityServices(m_Clock, storage, m_Facts,
                m_Rewards == null ? new GameFrameworkActivityRewardGateway(storage, m_RewardReceiver?.Invoke(scope)) : m_Rewards(scope),
                m_UI == null ? new GameFrameworkActivityUI(scope) : m_UI(scope));
        }
    }

    public sealed class UnavailableActivityRewards : IActivityRewardGateway
    {
        public bool CanGrant(string resourceKey, string target) => false;
        public UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request == null) throw new ArgumentNullException(nameof(request));
            return UniTask.FromResult(new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Unsupported, reason: "No reward adapter is installed."));
        }
        public UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(new ActivityRewardReceipt(grantId, ActivityRewardStatus.Unsupported, reason: "No receipt backend is installed."));
        }
    }

    public sealed class UnavailableActivityUI : IActivityUI
    {
        public UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request == null) throw new ArgumentNullException(nameof(request));
            return UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Unavailable, reason: "No activity page router is installed."));
        }
        public UniTask CloseAsync(ActivityPageHandle handle) => UniTask.CompletedTask;
        public UniTask CloseAllAsync() => UniTask.CompletedTask;
    }
}
