using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Lokas.Editor.Tests
{
    public sealed class ActivityModuleHostTests
    {
        private sealed class Module : IActivityModule, IActivityModuleFactory, IActivityEntryProvider
        {
            public string ModuleId { get; }
            public IActivityContext Context;
            public int Initializes;
            public int Shutdowns;
            public int Opens;
            public int Facts;
            public bool FailInit;
            public bool FailShutdown;
            public UniTaskCompletionSource InitGate;
            public Func<UniTask> OnShutdown;
            public IReadOnlyList<ActivityEntryInfo> Entries = new[] { new ActivityEntryInfo("main", "Activity.Title") };
            public event Action EntriesChanged;
            public Module(string id) { ModuleId = id; }
            public IActivityModule CreateModule() => this;
            public async UniTask InitializeAsync(IActivityContext context, CancellationToken cancellationToken)
            {
                Initializes++;
                Context = context;
                context.GameFacts.Subscribe<ActivityLevelCompletedFact>(_ => Facts++);
                if (InitGate != null) await InitGate.Task; // 模拟忽略取消的第三方回调。
                if (FailInit) throw new InvalidOperationException("init");
            }
            public async UniTask ShutdownAsync()
            {
                Shutdowns++;
                if (FailShutdown) throw new InvalidOperationException("shutdown");
                if (OnShutdown != null) await OnShutdown();
            }
            public IReadOnlyList<ActivityEntryInfo> GetEntries() => Entries;
            public void Refresh() => EntriesChanged?.Invoke();
            public Action CaptureRefresh() => EntriesChanged;
            public UniTask OpenEntryAsync(string entryId, CancellationToken cancellationToken)
            {
                Opens++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class MemoryBackend : IActivityStorageBackend
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public bool FailFlush;
            public string Read(string key) => Values.TryGetValue(key, out string value) ? value : null;
            public void Write(string key, string value) { Values[key] = value; }
            public bool Flush() => !FailFlush;
        }

        private sealed class Facts : IActivityGameFacts
        {
            public readonly List<Action<IActivityGameFact>> Handlers = new List<Action<IActivityGameFact>>();
            public IDisposable Subscribe<TFact>(Action<TFact> handler) where TFact : class, IActivityGameFact
            {
                Action<IActivityGameFact> action = fact => { if (fact is TFact typed) handler(typed); };
                Handlers.Add(action);
                return new Subscription(() => Handlers.Remove(action));
            }
            public void Publish(string gameId)
            {
                var fact = new ActivityLevelCompletedFact("fact-1", gameId, "session-1", 1, DateTimeOffset.UtcNow, "level-1", true);
                foreach (var handler in Handlers.ToArray()) handler(fact);
            }
            private sealed class Subscription : IDisposable
            {
                private readonly Action m_Dispose;
                public Subscription(Action dispose) { m_Dispose = dispose; }
                public void Dispose() => m_Dispose();
            }
        }

        private sealed class UI : IActivityUI
        {
            public readonly ActivityScope Scope;
            public int Closes;
            public int CloseAllCalls;
            public UniTaskCompletionSource<ActivityUiOpenResult> OpenGate;
            public UI(ActivityScope scope) { Scope = scope; }
            public UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request, CancellationToken cancellationToken)
                => OpenGate == null ? UniTask.FromResult(Result()) : OpenGate.Task;
            public ActivityUiOpenResult Result() => new ActivityUiOpenResult(ActivityUiOpenStatus.Opened,
                new ActivityPageHandle(Scope.ModuleId, Scope.Generation, 1));
            public UniTask CloseAsync(ActivityPageHandle handle) { Closes++; return UniTask.CompletedTask; }
            public UniTask CloseAllAsync() { CloseAllCalls++; return UniTask.CompletedTask; }
        }

        private sealed class Rewards : IActivityRewardGateway
        {
            public UniTaskCompletionSource<ActivityRewardReceipt> Gate;
            public bool CanGrant(string resourceKey, string target) => true;
            public UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request, CancellationToken cancellationToken) => Gate.Task;
            public UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId, CancellationToken cancellationToken)
                => UniTask.FromResult(new ActivityRewardReceipt(grantId, ActivityRewardStatus.Pending));
        }

        private MemoryBackend m_Backend;
        private Facts m_Facts;
        private Rewards m_Rewards;
        private List<UI> m_UIs;
        private ActivityModuleHost m_Host;
        private List<Exception> m_Errors;

        [SetUp]
        public void SetUp()
        {
            m_Backend = new MemoryBackend();
            m_Facts = new Facts();
            m_Rewards = new Rewards();
            m_UIs = new List<UI>();
            m_Errors = new List<Exception>();
            var services = new LocalActivityServicesFactory(m_Backend, m_Facts, _ => m_Rewards, scope =>
            {
                var ui = new UI(scope);
                m_UIs.Add(ui);
                return ui;
            });
            m_Host = new ActivityModuleHost("profile", services, m_Errors.Add);
        }

        private static async UniTask<Exception> Failure(UniTask task)
        {
            try { await task; } catch (Exception error) { return error; }
            Assert.Fail("Expected the operation to fail.");
            return null;
        }

        [UnityTest]
        public IEnumerator EmptyHostAndRepeatedShutdownAreSafe() => UniTask.ToCoroutine(async () =>
        {
            Assert.That(m_Host.GetModules(), Is.Empty);
            await m_Host.UnregisterAsync("absent", 1);
            await m_Host.ShutdownAsync();
            await m_Host.ShutdownAsync();
            Assert.That(await Failure(m_Host.RegisterAsync(new Module("one"))), Is.TypeOf<InvalidOperationException>());
        });

        [UnityTest]
        public IEnumerator DuplicateRegistrationIsRejectedWithoutInitializingAgain() => UniTask.ToCoroutine(async () =>
        {
            var module = new Module("one");
            await m_Host.RegisterAsync(module);
            Assert.That(await Failure(m_Host.RegisterAsync(module)), Is.TypeOf<InvalidOperationException>());
            var duplicate = new Module("one");
            await Failure(m_Host.RegisterAsync(duplicate));
            Assert.That(module.Initializes, Is.EqualTo(1));
            Assert.That(duplicate.Initializes, Is.Zero);
            Assert.That(m_Host.GetModules().Count, Is.EqualTo(1));
            await m_Host.ShutdownAsync();
        });

        [UnityTest]
        public IEnumerator FailedInitializationCleansSubscriptionsAndCanBeRetried() => UniTask.ToCoroutine(async () =>
        {
            var module = new Module("one") { FailInit = true };
            Assert.That(await Failure(m_Host.RegisterAsync(module, new[] { "GameA" })), Is.TypeOf<InvalidOperationException>());
            Assert.That(m_Host.GetModules(), Is.Empty);
            Assert.That(m_Host.GetEntries(), Is.Empty);
            Assert.That(m_Facts.Handlers, Is.Empty);
            Assert.That(module.Context.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(module.Shutdowns, Is.EqualTo(1));
            await m_Host.RegisterAsync(new Module("one"));
            await m_Host.ShutdownAsync();
        });

        [UnityTest]
        public IEnumerator StopDuringInitializationWaitsAndNeverPublishesEntries() => UniTask.ToCoroutine(async () =>
        {
            var module = new Module("one") { InitGate = new UniTaskCompletionSource() };
            UniTask start = m_Host.RegisterAsync(module);
            var duplicate = new Module("one");
            Assert.That(await Failure(m_Host.RegisterAsync(duplicate)), Is.TypeOf<InvalidOperationException>());
            Assert.That(duplicate.Initializes, Is.Zero);
            UniTask stop = m_Host.UnregisterAsync("one", module.Context.Generation);
            Assert.That(module.Context.LifetimeToken.IsCancellationRequested, Is.True);
            Assert.That(m_Host.GetEntries(), Is.Empty);
            Assert.That(stop.Status, Is.EqualTo(UniTaskStatus.Pending));
            module.InitGate.TrySetResult();
            Assert.That(await Failure(start), Is.InstanceOf<OperationCanceledException>());
            await stop;
            Assert.That(module.Shutdowns, Is.EqualTo(1));
            Assert.That(m_Host.GetModules(), Is.Empty);
        });

        [UnityTest]
        public IEnumerator OldProfileEntryCannotBeUsedByAnotherHost() => UniTask.ToCoroutine(async () =>
        {
            var old = new Module("one");
            await m_Host.RegisterAsync(old);
            ActivityEntrySnapshot entry = m_Host.GetEntries()[0];
            await m_Host.ShutdownAsync();
            var nextHost = new ActivityModuleHost("another-profile", new LocalActivityServicesFactory(m_Backend, m_Facts));
            var current = new Module("one");
            await nextHost.RegisterAsync(current);
            Assert.That(current.Context.Generation, Is.GreaterThan(entry.Generation));
            Assert.That(await Failure(nextHost.OpenEntryAsync(entry)), Is.TypeOf<InvalidOperationException>());
            Assert.That(current.Opens, Is.Zero);
            await nextHost.ShutdownAsync();
        });

        [UnityTest]
        public IEnumerator FailedStopDuringInitializationIsNotImplicitlyRetried() => UniTask.ToCoroutine(async () =>
        {
            var module = new Module("one") { InitGate = new UniTaskCompletionSource(), FailShutdown = true };
            UniTask start = m_Host.RegisterAsync(module);
            UniTask stop = m_Host.UnregisterAsync("one", module.Context.Generation);
            module.InitGate.TrySetResult();
            Assert.That(await Failure(start), Is.TypeOf<AggregateException>());
            Assert.That(await Failure(stop), Is.TypeOf<AggregateException>());
            Assert.That(module.Shutdowns, Is.EqualTo(1));
            Assert.That(m_Host.GetModules()[0].State, Is.EqualTo(ActivityModuleState.Failed));
            module.FailShutdown = false;
            await m_Host.UnregisterAsync("one", module.Context.Generation);
            Assert.That(module.Shutdowns, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator FactsAreFilteredAndStaleCallbacksCannotReachReinstalledModule() => UniTask.ToCoroutine(async () =>
        {
            var old = new Module("one");
            await m_Host.RegisterAsync(old, new[] { "GameA" });
            var oldEntry = m_Host.GetEntries()[0];
            Action refresh = old.CaptureRefresh();
            Action<IActivityGameFact> lateFact = m_Facts.Handlers[0];
            m_Facts.Publish("GameB");
            m_Facts.Publish("GameA");
            Assert.That(old.Facts, Is.EqualTo(1));
            await m_Host.UnregisterAsync("one", old.Context.Generation);
            var current = new Module("one");
            await m_Host.RegisterAsync(current, new[] { "GameA" });
            refresh?.Invoke();
            lateFact(new ActivityLevelCompletedFact("fact-2", "GameA", "s", 2, DateTimeOffset.UtcNow, "l", true));
            await m_Host.UnregisterAsync("one", old.Context.Generation);
            Assert.That(await Failure(m_Host.OpenEntryAsync(oldEntry)), Is.TypeOf<InvalidOperationException>());
            Assert.That(old.Facts, Is.EqualTo(1));
            Assert.That(current.Facts, Is.Zero);
            Assert.That(current.Context.Generation, Is.GreaterThan(old.Context.Generation));
            Assert.That(m_Host.GetModules().Count, Is.EqualTo(1));
            Assert.That(await Failure(old.Context.Storage.WriteAsync("old", "value", 1)), Is.TypeOf<ObjectDisposedException>());
            await m_Host.ShutdownAsync();
        });

        [UnityTest]
        public IEnumerator EntriesRefreshInOrderAndClicksUseCurrentAvailability() => UniTask.ToCoroutine(async () =>
        {
            var one = new Module("one");
            var two = new Module("two") { Entries = new[] { new ActivityEntryInfo("main", "Two", order: -1) } };
            await m_Host.RegisterAsync(one);
            await m_Host.RegisterAsync(two);
            Assert.That(m_Host.GetEntries()[0].ModuleId, Is.EqualTo("two"));
            var entry = m_Host.GetEntries()[1];
            await m_Host.OpenEntryAsync(entry);
            Assert.That(one.Opens, Is.EqualTo(1));
            one.Entries = new[] { new ActivityEntryInfo("main", "One", interactable: false, badgeCount: 3) };
            one.Refresh();
            Assert.That(m_Host.GetEntries()[1].Entry.BadgeCount, Is.EqualTo(3));
            Assert.That(await Failure(m_Host.OpenEntryAsync(entry)), Is.TypeOf<InvalidOperationException>());
            await m_Host.UnregisterAsync("two", two.Context.Generation);
            Assert.That(m_Host.GetEntries().Count, Is.EqualTo(1));
            await m_Host.ShutdownAsync();
        });

        [UnityTest]
        public IEnumerator InvalidEntryRefreshIsDiagnosedAndWithdrawn() => UniTask.ToCoroutine(async () =>
        {
            var module = new Module("one");
            await m_Host.RegisterAsync(module);
            module.Entries = new[] { new ActivityEntryInfo("same", "One"), new ActivityEntryInfo("same", "Two") };
            module.Refresh();
            Assert.That(m_Host.GetEntries(), Is.Empty);
            Assert.That(m_Errors.Count, Is.EqualTo(1));
            Assert.That(m_Host.GetModules()[0].LastError, Is.Not.Null);
            await m_Host.ShutdownAsync();
        });

        [UnityTest]
        public IEnumerator CleanupFailureKeepsRecoveryStateAndDoesNotBlockOtherModules() => UniTask.ToCoroutine(async () =>
        {
            var broken = new Module("one") { FailShutdown = true };
            var other = new Module("two");
            await m_Host.RegisterAsync(broken);
            await m_Host.RegisterAsync(other);
            Assert.That(await Failure(m_Host.ShutdownAsync()), Is.TypeOf<AggregateException>());
            Assert.That(m_Host.GetEntries(), Is.Empty);
            Assert.That(m_Host.GetModules().Count, Is.EqualTo(1));
            Assert.That(m_Host.GetModules()[0].State, Is.EqualTo(ActivityModuleState.Failed));
            Assert.That(other.Shutdowns, Is.EqualTo(1));
            broken.FailShutdown = false;
            await m_Host.ShutdownAsync();
            Assert.That(m_Host.GetModules(), Is.Empty);
            Assert.That(broken.Shutdowns, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator ShutdownCanSaveAndFlushFailureCanBeRetried() => UniTask.ToCoroutine(async () =>
        {
            var module = new Module("one");
            await m_Host.RegisterAsync(module);
            module.OnShutdown = () => module.Context.Storage.WriteAsync("recovery", "pending", 1);
            m_Backend.FailFlush = true;
            await Failure(m_Host.UnregisterAsync("one", module.Context.Generation));
            Assert.That(m_Host.GetModules()[0].State, Is.EqualTo(ActivityModuleState.Failed));
            m_Backend.FailFlush = false;
            await m_Host.UnregisterAsync("one", module.Context.Generation);
            Assert.That(m_Backend.Values.Count, Is.EqualTo(1));
            Assert.That(m_Host.GetModules(), Is.Empty);
        });

        [UnityTest]
        public IEnumerator ShutdownWaitsForAcceptedGrantAndRejectsNewGrants() => UniTask.ToCoroutine(async () =>
        {
            var module = new Module("one");
            await m_Host.RegisterAsync(module);
            m_Rewards.Gate = new UniTaskCompletionSource<ActivityRewardReceipt>();
            var request = new ActivityRewardRequest("grant-1", "round-1", "GameA", new[] { new ActivityRewardItem("coin", "count", 1) });
            UniTask<ActivityRewardReceipt> grant = module.Context.Rewards.GrantAsync(request);
            UniTask stop = m_Host.UnregisterAsync("one", module.Context.Generation);
            Assert.That(stop.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(module.Context.Rewards.CanGrant("coin", "GameA"), Is.False);
            Assert.That(await Failure(module.Context.Rewards.GrantAsync(request).AsUniTask()), Is.InstanceOf<OperationCanceledException>());
            Assert.That((await module.Context.Rewards.GetReceiptAsync("grant-1")).Status, Is.EqualTo(ActivityRewardStatus.Pending));
            m_Rewards.Gate.TrySetResult(new ActivityRewardReceipt("grant-1", ActivityRewardStatus.Granted));
            Assert.That((await grant).Status, Is.EqualTo(ActivityRewardStatus.Granted));
            await stop;
        });

        [UnityTest]
        public IEnumerator LatePageOpenIsClosedAndOldHandlesCannotCloseNewPages() => UniTask.ToCoroutine(async () =>
        {
            var module = new Module("one");
            await m_Host.RegisterAsync(module);
            UI oldUI = m_UIs[0];
            oldUI.OpenGate = new UniTaskCompletionSource<ActivityUiOpenResult>();
            UniTask<ActivityUiOpenResult> open = module.Context.UI.OpenAsync(new ActivityPageRequest("rules"));
            UniTask stop = m_Host.UnregisterAsync("one", module.Context.Generation);
            Assert.That(stop.Status, Is.EqualTo(UniTaskStatus.Pending));
            oldUI.OpenGate.TrySetResult(oldUI.Result());
            Assert.That(await Failure(open.AsUniTask()), Is.InstanceOf<OperationCanceledException>());
            await stop;
            Assert.That(oldUI.Closes, Is.EqualTo(1));
            var current = new Module("one");
            await m_Host.RegisterAsync(current);
            await current.Context.UI.CloseAsync(oldUI.Result().Handle);
            await module.Context.UI.CloseAllAsync();
            Assert.That(m_UIs[1].Closes, Is.Zero);
            Assert.That(m_UIs[1].CloseAllCalls, Is.Zero);
            await m_Host.ShutdownAsync();
        });
    }
}
