using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    /// <summary>只负责宿主服务的生命周期隔离，不持有活动业务状态。所有调用均在 Unity 主线程。</summary>
    internal sealed class ActivityContext : IActivityContext, IActivityStorage, IActivityGameFacts, IActivityRewardGateway, IActivityUI
    {
        private readonly ActivityScope m_Scope;
        private readonly ActivityServices m_Services;
        private readonly CancellationTokenSource m_Lifetime = new CancellationTokenSource();
        private readonly List<Subscription> m_Subscriptions = new List<Subscription>();
        private readonly HashSet<string> m_GameIds;
        private bool m_Active = true;
        private bool m_Released;
        private int m_PendingOperations;
        private UniTaskCompletionSource m_Drained;

        public string ModuleId => m_Scope.ModuleId;
        public string ProfileId => m_Scope.ProfileId;
        public long Generation => m_Scope.Generation;
        public CancellationToken LifetimeToken { get; }
        public IActivityClock Clock => m_Services.Clock;
        public IActivityStorage Storage => this;
        public IActivityGameFacts GameFacts => this;
        public IActivityRewardGateway Rewards => this;
        public IActivityUI UI => this;

        public ActivityContext(ActivityScope scope, ActivityServices services)
        {
            m_Scope = scope;
            m_Services = services ?? throw new ArgumentNullException(nameof(services));
            m_GameIds = new HashSet<string>(scope.GameIds, StringComparer.Ordinal);
            LifetimeToken = m_Lifetime.Token;
        }

        public void StopAccepting()
        {
            bool wasActive = m_Active;
            m_Active = false; // 先失效，再触发同步取消回调。
            var errors = new List<Exception>();
            if (wasActive)
                try { m_Lifetime.Cancel(); } catch (Exception error) { errors.Add(error); }
            foreach (Subscription subscription in m_Subscriptions.ToArray())
                try { subscription.Dispose(); } catch (Exception error) { errors.Add(error); }
            if (errors.Count > 0) throw new AggregateException(errors);
        }

        public UniTask DrainAsync() => m_PendingOperations == 0 ? UniTask.CompletedTask : m_Drained.Task;

        public void Release()
        {
            m_Released = true;
            m_Lifetime.Dispose();
        }

        private void RequireActive()
        {
            if (!m_Active) throw new OperationCanceledException(LifetimeToken);
        }

        private void BeginOperation()
        {
            if (m_Released) throw new ObjectDisposedException(nameof(ActivityContext));
            if (m_PendingOperations++ == 0) m_Drained = new UniTaskCompletionSource();
        }

        private void EndOperation()
        {
            if (--m_PendingOperations == 0) m_Drained.TrySetResult();
        }

        public async UniTask<ActivityStorageReadResult> ReadAsync(string key, CancellationToken cancellationToken = default)
        {
            BeginOperation();
            try { return await m_Services.Storage.ReadAsync(key, cancellationToken); }
            finally { EndOperation(); }
        }

        public async UniTask WriteAsync(string key, string payload, int schemaVersion, CancellationToken cancellationToken = default)
        {
            // 清理阶段仍可保存恢复记录；不隐式关联 LifetimeToken，避免丢弃已接受的写入。
            BeginOperation();
            try { await m_Services.Storage.WriteAsync(key, payload, schemaVersion, cancellationToken); }
            finally { EndOperation(); }
        }

        public async UniTask FlushAsync(CancellationToken cancellationToken = default)
        {
            BeginOperation();
            try { await m_Services.Storage.FlushAsync(cancellationToken); }
            finally { EndOperation(); }
        }

        public IDisposable Subscribe<TFact>(Action<TFact> handler) where TFact : class, IActivityGameFact
        {
            RequireActive();
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var subscription = new Subscription(m_Subscriptions);
            subscription.Inner = m_Services.GameFacts.Subscribe<TFact>(fact =>
            {
                if (m_Active && !subscription.IsDisposed && fact != null && m_GameIds.Contains(fact.GameId)) handler(fact);
            });
            m_Subscriptions.Add(subscription);
            return subscription;
        }

        public bool CanGrant(string resourceKey, string target) => m_Active && m_Services.Rewards.CanGrant(resourceKey, target);

        public async UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request, CancellationToken cancellationToken = default)
        {
            RequireActive();
            BeginOperation();
            try { return await m_Services.Rewards.GrantAsync(request, cancellationToken); }
            finally { EndOperation(); }
        }

        public async UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId, CancellationToken cancellationToken = default)
        {
            BeginOperation();
            try { return await m_Services.Rewards.GetReceiptAsync(grantId, cancellationToken); }
            finally { EndOperation(); }
        }

        public async UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request, CancellationToken cancellationToken = default)
        {
            RequireActive();
            if (request == null) throw new ArgumentNullException(nameof(request));
            BeginOperation();
            try
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(LifetimeToken, cancellationToken))
                {
                    linked.Token.ThrowIfCancellationRequested();
                    ActivityUiOpenResult result = await m_Services.UI.OpenAsync(request, linked.Token);
                    if (result == null) throw new InvalidOperationException("The UI adapter returned no result.");
                    if (result.Handle.IsValid && !Owns(result.Handle))
                        throw new InvalidOperationException("The UI adapter returned a handle owned by another scope.");
                    if (linked.IsCancellationRequested)
                    {
                        if (result.Handle.IsValid) await m_Services.UI.CloseAsync(result.Handle);
                        linked.Token.ThrowIfCancellationRequested();
                    }
                    return result;
                }
            }
            finally { EndOperation(); }
        }

        public UniTask CloseAsync(ActivityPageHandle handle)
        {
            if (m_Released || !Owns(handle)) return UniTask.CompletedTask;
            return m_Services.UI.CloseAsync(handle);
        }

        public UniTask CloseAllAsync() => m_Released ? UniTask.CompletedTask : m_Services.UI.CloseAllAsync();

        private bool Owns(ActivityPageHandle handle) => handle.IsValid && handle.ModuleId == ModuleId && handle.Generation == Generation;

        private sealed class Subscription : IDisposable
        {
            private readonly List<Subscription> m_Owner;
            public IDisposable Inner;
            public bool IsDisposed { get; private set; }
            public Subscription(List<Subscription> owner) { m_Owner = owner; }
            public void Dispose()
            {
                if (IsDisposed && Inner == null) return;
                IsDisposed = true;
                Inner?.Dispose();
                Inner = null;
                m_Owner.Remove(this);
            }
        }
    }
}
