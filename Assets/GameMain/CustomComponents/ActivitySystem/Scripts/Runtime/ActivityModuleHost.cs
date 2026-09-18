using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Lokas
{
    /// <summary>显式装配活动；仅管理加载状态，不定义任何活动业务流程。主线程调用。</summary>
    public sealed class ActivityModuleHost
    {
        private sealed class Registration
        {
            public IActivityModule Module;
            public ActivityContext Context;
            public long Generation;
            public ActivityModuleState State;
            public Exception LastError;
            public IActivityEntryProvider Provider;
            public Action OnEntriesChanged;
            public IReadOnlyList<ActivityEntrySnapshot> Entries = Array.Empty<ActivityEntrySnapshot>();
            public readonly UniTaskCompletionSource Initialized = new UniTaskCompletionSource();
            public UniTaskCompletionSource Stopped;
            public int EntryCalls;
            public UniTaskCompletionSource EntriesDrained;
        }

        private readonly Dictionary<string, Registration> m_Modules = new Dictionary<string, Registration>(StringComparer.Ordinal);
        private readonly IActivityServicesFactory m_Services;
        private readonly Action<Exception> m_ReportError;
        private static long s_Generation;
        private bool m_ShuttingDown;
        private UniTaskCompletionSource m_Shutdown;
        public string ProfileId { get; }
        public event Action EntriesChanged;

        public ActivityModuleHost(string profileId, IActivityServicesFactory services, Action<Exception> reportError = null)
        {
            ProfileId = ActivityContract.RequireId(profileId, nameof(profileId));
            m_Services = services ?? throw new ArgumentNullException(nameof(services));
            m_ReportError = reportError;
        }

        public IReadOnlyList<ActivityModuleSnapshot> GetModules()
        {
            var result = new List<ActivityModuleSnapshot>();
            foreach (var pair in m_Modules)
                result.Add(new ActivityModuleSnapshot(pair.Key, pair.Value.Generation, pair.Value.State, pair.Value.LastError));
            result.Sort((a, b) => string.CompareOrdinal(a.ModuleId, b.ModuleId));
            return result.AsReadOnly();
        }

        /// <summary>
        /// Returns a ready module instance for an activity-specific presenter.
        /// The host still owns the lifetime; callers must not initialize or shut down the returned module.
        /// </summary>
        public bool TryGetModule<TModule>(string moduleId, out TModule module)
            where TModule : class, IActivityModule
        {
            module = null;
            if (string.IsNullOrWhiteSpace(moduleId) ||
                !m_Modules.TryGetValue(moduleId, out Registration registration) ||
                registration.State != ActivityModuleState.Ready)
                return false;

            module = registration.Module as TModule;
            return module != null;
        }

        public IReadOnlyList<ActivityEntrySnapshot> GetEntries()
        {
            var result = new List<ActivityEntrySnapshot>();
            foreach (Registration registration in m_Modules.Values)
                if (registration.State == ActivityModuleState.Ready) result.AddRange(registration.Entries);
            result.Sort((a, b) =>
            {
                int order = a.Entry.Order.CompareTo(b.Entry.Order);
                if (order != 0) return order;
                order = string.CompareOrdinal(a.ModuleId, b.ModuleId);
                return order != 0 ? order : string.CompareOrdinal(a.Entry.EntryId, b.Entry.EntryId);
            });
            return result.AsReadOnly();
        }

        public async UniTask RegisterAsync(IActivityModuleFactory factory, IEnumerable<string> gameIds = null,
            CancellationToken cancellationToken = default)
        {
            if (m_ShuttingDown) throw new InvalidOperationException("The activity host is shutting down.");
            cancellationToken.ThrowIfCancellationRequested();
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            IActivityModule module = factory.CreateModule() ?? throw new InvalidOperationException("The factory returned no module.");
            string id = ActivityContract.RequireId(module.ModuleId, nameof(module.ModuleId));
            if (m_Modules.ContainsKey(id)) throw new InvalidOperationException($"Activity module '{id}' is already registered.");
            var scope = new ActivityScope(ProfileId, id, Interlocked.Increment(ref s_Generation), gameIds);
            var registration = new Registration { Module = module, Generation = scope.Generation, State = ActivityModuleState.Initializing };
            m_Modules.Add(id, registration);
            Exception failure = null;
            UniTaskCompletionSource existingStop = null;
            try
            {
                registration.Context = new ActivityContext(scope, m_Services.CreateServices(scope));
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, registration.Context.LifetimeToken))
                {
                    await module.InitializeAsync(registration.Context, linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                }
                registration.Provider = module as IActivityEntryProvider;
                if (registration.Provider != null)
                {
                    registration.OnEntriesChanged = () => RefreshEntries(id, registration);
                    registration.Provider.EntriesChanged += registration.OnEntriesChanged;
                    ReadEntries(id, registration); // 首次失败视为初始化失败，撤回模块。
                }
                registration.State = ActivityModuleState.Ready;
            }
            catch (Exception error) { failure = error; registration.LastError = error; }
            finally
            {
                // 完成信号可同步恢复正在等待的停用；先保留其结果，避免失败后被初始化路径隐式重试。
                existingStop = registration.Stopped;
                registration.Initialized.TrySetResult();
            }

            if (failure != null)
            {
                try { await (existingStop != null ? existingStop.Task : StopRegistrationAsync(id, registration)); }
                catch (Exception cleanupError) { throw new AggregateException(failure, cleanupError); }
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
            NotifyEntriesChanged();
        }

        public async UniTask OpenEntryAsync(ActivityEntrySnapshot entry, CancellationToken cancellationToken = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (!m_Modules.TryGetValue(entry.ModuleId, out Registration registration) ||
                registration.Generation != entry.Generation || registration.State != ActivityModuleState.Ready || registration.Provider == null)
                throw new InvalidOperationException("The activity entry belongs to an inactive module generation.");
            // 重新读取最新摘要；展示后可用性可能已改变，业务层仍须再次校验。
            ReadEntries(entry.ModuleId, registration);
            bool available = false;
            foreach (ActivityEntrySnapshot current in registration.Entries)
                if (current.Entry.EntryId == entry.Entry.EntryId) available = current.Entry.Visible && current.Entry.Interactable;
            if (!available) throw new InvalidOperationException("The activity entry is unavailable.");
            if (registration.EntryCalls++ == 0) registration.EntriesDrained = new UniTaskCompletionSource();
            try
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, registration.Context.LifetimeToken))
                {
                    linked.Token.ThrowIfCancellationRequested();
                    await registration.Provider.OpenEntryAsync(entry.Entry.EntryId, linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                if (--registration.EntryCalls == 0) registration.EntriesDrained.TrySetResult();
            }
        }

        // generation 防止旧页面或迟到回调移除同名新模块。
        public UniTask UnregisterAsync(string moduleId, long generation)
        {
            return m_Modules.TryGetValue(moduleId, out Registration registration) && registration.Generation == generation
                ? StopRegistrationAsync(moduleId, registration) : UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync()
        {
            if (m_Shutdown != null) return m_Shutdown.Task;
            m_ShuttingDown = true;
            var completion = new UniTaskCompletionSource();
            m_Shutdown = completion;
            CompleteShutdownAsync(completion).Forget();
            return completion.Task;
        }

        private async UniTask CompleteShutdownAsync(UniTaskCompletionSource completion)
        {
            var errors = new List<Exception>();
            var pending = new List<UniTask>();
            // 先让所有模块同步失效，再逐个等待清理。
            foreach (var pair in new List<KeyValuePair<string, Registration>>(m_Modules))
                pending.Add(StopRegistrationAsync(pair.Key, pair.Value));
            foreach (UniTask task in pending)
                try { await task; } catch (Exception error) { errors.Add(error); }
            if (errors.Count == 0) completion.TrySetResult();
            else
            {
                m_Shutdown = null; // 保留失败模块，允许显式重试清理。
                completion.TrySetException(new AggregateException(errors));
            }
        }

        private UniTask StopRegistrationAsync(string id, Registration registration)
        {
            if (registration.Stopped != null) return registration.Stopped.Task;
            if (!m_Modules.TryGetValue(id, out Registration current) || current != registration) return UniTask.CompletedTask;
            var completion = new UniTaskCompletionSource();
            registration.Stopped = completion;
            registration.State = ActivityModuleState.Stopping;
            registration.Entries = Array.Empty<ActivityEntrySnapshot>();
            var errors = new List<Exception>();
            try
            {
                if (registration.Provider != null && registration.OnEntriesChanged != null)
                    registration.Provider.EntriesChanged -= registration.OnEntriesChanged;
            }
            catch (Exception error) { errors.Add(error); }
            try { registration.Context?.StopAccepting(); } catch (Exception error) { errors.Add(error); }
            NotifyEntriesChanged();
            CompleteStopAsync(id, registration, completion, errors).Forget();
            return completion.Task;
        }

        private async UniTask CompleteStopAsync(string id, Registration registration, UniTaskCompletionSource completion, List<Exception> errors)
        {
            try
            {
                if (registration.Context != null)
                    try { await registration.Context.UI.CloseAllAsync(); } catch (Exception error) { errors.Add(error); }
                await registration.Initialized.Task;
                if (registration.EntryCalls > 0) await registration.EntriesDrained.Task;
                try { await registration.Module.ShutdownAsync(); } catch (Exception error) { errors.Add(error); }
                if (registration.Context != null)
                {
                    await registration.Context.DrainAsync();
                    // 再清理一次已结束的异步开页，覆盖取消后才返回窗口的适配器。
                    try { await registration.Context.UI.CloseAllAsync(); } catch (Exception error) { errors.Add(error); }
                    try { await registration.Context.Storage.FlushAsync(); } catch (Exception error) { errors.Add(error); }
                }
                if (errors.Count > 0) throw new AggregateException($"Activity module '{id}' cleanup failed; retry before removing its assets.", errors);
                registration.Context?.Release();
                m_Modules.Remove(id);
                completion.TrySetResult();
            }
            catch (Exception error)
            {
                registration.LastError = error;
                registration.State = ActivityModuleState.Failed;
                registration.Stopped = null;
                completion.TrySetException(error);
            }
        }

        private void ReadEntries(string id, Registration registration)
        {
            var result = new List<ActivityEntrySnapshot>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<ActivityEntryInfo> entries = registration.Provider.GetEntries();
            if (entries == null) throw new InvalidOperationException($"Activity '{id}' returned no entry list.");
            foreach (ActivityEntryInfo entry in entries)
            {
                if (entry == null || !keys.Add(entry.EntryId)) throw new InvalidOperationException($"Activity '{id}' has null or duplicate entries.");
                result.Add(new ActivityEntrySnapshot(id, registration.Generation, entry));
            }
            registration.Entries = result.AsReadOnly();
        }

        private void RefreshEntries(string id, Registration registration)
        {
            if (registration.State != ActivityModuleState.Ready) return;
            try { ReadEntries(id, registration); }
            catch (Exception error)
            {
                registration.Entries = Array.Empty<ActivityEntrySnapshot>();
                registration.LastError = error;
                ReportError(error);
            }
            NotifyEntriesChanged();
        }

        private void NotifyEntriesChanged()
        {
            if (EntriesChanged == null) return;
            foreach (Action subscriber in EntriesChanged.GetInvocationList())
                try { subscriber(); } catch (Exception error) { ReportError(error); }
        }

        private void ReportError(Exception error)
        {
            try
            {
                if (m_ReportError != null) m_ReportError(error);
                else UnityEngine.Debug.LogException(error);
            }
            catch (Exception reporterError) { UnityEngine.Debug.LogException(new AggregateException(error, reporterError)); }
        }
    }
}
