using System;
using NUnit.Framework;

namespace Lokas.Editor.Tests
{
    public sealed class TestModeModuleRegistryTests
    {
        private sealed class Module : ITestModeModule, ITestModeModuleLifecycle
        {
            public Module(string owner, string name = "Main") { OwnerId = owner; ModuleName = name; }
            public string OwnerId { get; }
            public string ModuleName { get; }
            public string PageName => "Test";
            public int Order => 0;
            public int Registered;
            public int Unregistered;
            public bool FailRegistration;
            public bool FailCleanup;
            public TestModeModuleContext Context;
            public void Build(TestModePage page, TestModeModuleContext context) { }
            public void OnRegistered(TestModeModuleContext context)
            {
                Registered++;
                Context = context;
                if (FailRegistration) throw new InvalidOperationException("registration");
            }
            public void OnUnregistered()
            {
                Unregistered++;
                if (FailCleanup) throw new InvalidOperationException("cleanup");
            }
        }

        [Test]
        public void EmptyTemplateAndUnknownRemovalAreSafe()
        {
            using (var registry = new TestModeModuleRegistry())
            {
                Assert.That(registry.Modules, Is.Empty);
                Assert.That(registry.Unregister(null), Is.False);
                registry.UnregisterOwner("Absent");
                Assert.That(registry.GetContext(new Module("Absent")), Is.Null);
            }
        }

        [Test]
        public void RegistrationIsIdempotentByOwnerAndModuleName()
        {
            using (var registry = new TestModeModuleRegistry())
            {
                var first = new Module("One");
                var duplicate = new Module("One");
                Assert.That(registry.Register(first), Is.True);
                Assert.That(registry.Register(first), Is.False);
                Assert.That(registry.Register(duplicate), Is.False);
                Assert.That(first.Registered, Is.EqualTo(1));
                Assert.That(duplicate.Registered, Is.Zero);
                Assert.That(registry.Modules.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void EqualModuleNamesInDifferentGamesHaveIndependentContexts()
        {
            using (var registry = new TestModeModuleRegistry())
            {
                var one = new Module("One");
                var two = new Module("Two");
                registry.Register(one);
                registry.Register(two);
                Assert.That(one.Context, Is.Not.SameAs(two.Context));
                Assert.That(one.Context.OwnerId, Is.EqualTo("One"));
                Assert.That(two.Context.OwnerId, Is.EqualTo("Two"));
            }
        }

        [Test]
        public void RemovingOneGamePreservesCommonAndOtherGameModules()
        {
            using (var registry = new TestModeModuleRegistry())
            {
                var common = new Module(TestModeModuleRegistry.CommonOwnerId);
                var one = new Module("One");
                var secondPage = new Module("One", "Extra");
                var two = new Module("Two");
                registry.Register(common);
                registry.Register(one);
                registry.Register(secondPage);
                registry.Register(two);
                registry.UnregisterOwner("One");
                Assert.That(registry.Modules, Is.EquivalentTo(new[] { common, two }));
                Assert.That(one.Context.IsActive, Is.False);
                Assert.That(one.Context.CancellationToken.IsCancellationRequested, Is.True);
                Assert.That(secondPage.Unregistered, Is.EqualTo(1));
                Assert.That(two.Context.IsActive, Is.True);
            }
        }

        [Test]
        public void StaleInstanceCannotUnregisterANewSession()
        {
            using (var registry = new TestModeModuleRegistry())
            {
                var old = new Module("One");
                registry.Register(old);
                registry.Unregister(old);
                var current = new Module("One");
                registry.Register(current);
                Assert.That(registry.Unregister(old), Is.False);
                Assert.That(current.Context.IsActive, Is.True);
                Assert.That(current.Context, Is.Not.SameAs(old.Context));
            }
        }

        [Test]
        public void LateRefreshAndRewardCallbacksAreDiscardedAfterExit()
        {
            int rewards = 0;
            using (var registry = new TestModeModuleRegistry(_ => rewards++))
            {
                int refreshes = 0;
                registry.Changed += () => refreshes++;
                var module = new Module("One");
                registry.Register(module);
                var context = module.Context;
                context.SimulateRankReward(1);
                registry.Unregister(module);
                int afterExit = refreshes;
                context.RequestRefresh();
                context.SimulateRankReward(1);
                Assert.That(refreshes, Is.EqualTo(afterExit));
                Assert.That(rewards, Is.EqualTo(1));
            }
        }

        [Test]
        public void FailedRegistrationRollsBackSubscriptionsAndContext()
        {
            using (var registry = new TestModeModuleRegistry())
            {
                var module = new Module("One") { FailRegistration = true };
                Assert.Throws<InvalidOperationException>(() => registry.Register(module));
                Assert.That(registry.Modules, Is.Empty);
                Assert.That(module.Unregistered, Is.EqualTo(1));
                Assert.That(module.Context.IsActive, Is.False);
            }
        }

        [Test]
        public void CleanupFailureDoesNotLeaveOtherModulesRegistered()
        {
            var registry = new TestModeModuleRegistry();
            var broken = new Module("One") { FailCleanup = true };
            var other = new Module("Two");
            registry.Register(broken);
            registry.Register(other);
            Assert.Throws<AggregateException>(() => registry.Dispose());
            Assert.That(registry.Modules, Is.Empty);
            Assert.That(other.Unregistered, Is.EqualTo(1));
            Assert.That(other.Context.IsActive, Is.False);
            Assert.DoesNotThrow(() => registry.Dispose());
        }

        [Test]
        public void MissingOwnerIsRejectedBeforeRegistration()
        {
            using (var registry = new TestModeModuleRegistry())
            {
                Assert.Throws<ArgumentException>(() => registry.Register(new Module("")));
                Assert.That(registry.Modules, Is.Empty);
            }
        }
    }
}
