using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Lokas;
using Lokas.Activities.Collector.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lokas.Activities.Collector.Editor.Tests
{
    /// <summary>
    /// Collector 的业务和页面回归测试：通关事实必须驱动进度，页面打开后必须按配置生成任务行。
    /// </summary>
    public sealed class CollectorActivityTests
    {
        private const string MainPanelPrefabPath = "Assets/GameMain/Activities/Collector/UI/CollectorMainPanel.prefab";
        private readonly List<UnityEngine.Object> m_TestAssets = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object asset in m_TestAssets)
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            m_TestAssets.Clear();
        }

        [Test]
        public void CompletedFactsAdvanceProgressAndIgnoreDuplicatesOrOtherGames()
        {
            var clock = new Clock();
            var storage = new Storage();
            var facts = new Facts();
            var rewards = new Rewards();
            var module = new CollectorActivityModule(CreateConfig(collectionPerWin: 1), new[] { "Game" });
            int stateChanges = 0;

            try
            {
                module.StateChanged += () => stateChanges++;
                module.InitializeAsync(new Context(clock, storage, facts, rewards), default)
                    .GetAwaiter().GetResult();

                CollectorSnapshot initial = module.GetSnapshot();
                Assert.That(initial.CollectedCount, Is.Zero);
                Assert.That(initial.CurrentTask.Tier, Is.EqualTo(1));
                Assert.That(initial.Progress01, Is.Zero);

                facts.Publish(Fact("other-game", "Other", 1, clock.UtcNow, won: true));
                facts.Publish(Fact("failed", "Game", 2, clock.UtcNow, won: false));
                Assert.That(module.GetSnapshot().CollectedCount, Is.Zero);

                facts.Publish(Fact("win-1", "Game", 3, clock.UtcNow, won: true));
                facts.Publish(Fact("win-1", "Game", 3, clock.UtcNow, won: true));
                CollectorSnapshot partial = module.GetSnapshot();
                Assert.That(partial.CollectedCount, Is.EqualTo(1));
                Assert.That(partial.CurrentTask.Tier, Is.EqualTo(1));
                Assert.That(partial.Progress01, Is.EqualTo(0.5f));

                facts.Publish(Fact("win-2", "Game", 4, clock.UtcNow, won: true));
                CollectorSnapshot reached = module.GetSnapshot();
                // Completing tier 1 advances to tier 2, whose progress starts at zero.
                Assert.That(reached.CollectedCount, Is.Zero);
                Assert.That(reached.CurrentTask.Tier, Is.EqualTo(2));
                Assert.That(reached.Tasks[0].IsReached, Is.True);
                Assert.That(reached.Tasks[0].CanClaim, Is.True);
                Assert.That(stateChanges, Is.GreaterThanOrEqualTo(3));

                facts.Publish(Fact("win-3", "Game", 5, clock.UtcNow, won: true));
                CollectorSnapshot nextPartial = module.GetSnapshot();
                Assert.That(nextPartial.CollectedCount, Is.EqualTo(1));
                Assert.That(nextPartial.TargetCount, Is.EqualTo(2));
                Assert.That(nextPartial.CurrentTask.Tier, Is.EqualTo(2));

                Assert.That(module.ClaimAsync(1).GetAwaiter().GetResult().Status,
                    Is.EqualTo(ActivityRewardStatus.Granted));
                Assert.That(module.ClaimAsync(1).GetAwaiter().GetResult().Status,
                    Is.EqualTo(ActivityRewardStatus.AlreadyGranted));
                Assert.That(rewards.Grants, Is.EqualTo(1));
                Assert.That(storage.Payload, Does.Contain("win-1"));
                Assert.That(storage.Payload, Does.Contain("win-2"));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void BatchedTestFactsResetProgressAtEachTierAndNotifyOnce()
        {
            var clock = new Clock();
            var storage = new Storage();
            var facts = new Facts();
            var rewards = new Rewards();
            var module = new CollectorActivityModule(CreateConfig(), new[] { "Game" });
            int stateChanges = 0;

            try
            {
                module.StateChanged += () => stateChanges++;
                module.InitializeAsync(new Context(clock, storage, facts, rewards), default)
                    .GetAwaiter().GetResult();

                var batch = new List<ActivityLevelCompletedFact>
                {
                    Fact("batch-1", "Game", 1, clock.UtcNow, won: true),
                    Fact("batch-2", "Game", 2, clock.UtcNow, won: true),
                    Fact("batch-3", "Game", 3, clock.UtcNow, won: true)
                };
                MethodInfo process = typeof(CollectorActivityModule).GetMethod("ProcessTestFacts",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(process, Is.Not.Null);
                process.Invoke(module, new object[] { batch });

                CollectorSnapshot snapshot = module.GetSnapshot();
                Assert.That(snapshot.CurrentTask.Tier, Is.EqualTo(2));
                Assert.That(snapshot.CollectedCount, Is.EqualTo(1));
                Assert.That(snapshot.TargetCount, Is.EqualTo(2));
                Assert.That(snapshot.Tasks[0].IsReached, Is.True);
                Assert.That(stateChanges, Is.EqualTo(2),
                    "Initialize and one batched StateChanged; intermediate wins must not refresh the UI.");
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void MainPanelRefreshCreatesOneActiveRowPerConfiguredTask()
        {
            var module = new CollectorActivityModule(CreateConfig(), new[] { "Game" });
            GameObject instance = null;
            try
            {
                module.InitializeAsync(new Context(new Clock(), new Storage(), new Facts(), new Rewards()), default)
                    .GetAwaiter().GetResult();

                instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(MainPanelPrefabPath));
                Assert.That(instance, Is.Not.Null);
                CollectorMainPanel panel = instance.GetComponent<CollectorMainPanel>();
                Assert.That(panel, Is.Not.Null);

                FieldInfo moduleField = typeof(CollectorMainPanel).GetField("m_Module",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo contentField = typeof(CollectorMainPanel).GetField("m_TaskContent",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo templateField = typeof(CollectorMainPanel).GetField("m_TaskRowTemplate",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo refresh = typeof(CollectorMainPanel).GetMethod("Refresh",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(moduleField, Is.Not.Null);
                Assert.That(contentField, Is.Not.Null);
                Assert.That(templateField, Is.Not.Null);
                Assert.That(refresh, Is.Not.Null);

                moduleField.SetValue(panel, module);
                Transform content = (Transform)contentField.GetValue(panel);
                CollectorTaskRowView template = (CollectorTaskRowView)templateField.GetValue(panel);
                Assert.That(content, Is.Not.Null);
                Assert.That(template, Is.Not.Null);
                template.gameObject.SetActive(false);

                refresh.Invoke(panel, null);

                int activeRows = 0;
                var activeTiers = new List<int>();
                for (int index = 0; index < content.childCount; index++)
                {
                    Transform child = content.GetChild(index);
                    CollectorTaskRowView row = child.GetComponent<CollectorTaskRowView>();
                    if (row == null || !child.gameObject.activeSelf) continue;
                    activeRows++;
                    activeTiers.Add(ReadTier(row));
                }

                Assert.That(activeRows, Is.EqualTo(module.Config.Tasks.Count));
                Assert.That(activeTiers, Is.EqualTo(new[] { 2, 1 }));
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void EntryIsVisibleAndOpensConfiguredMainPageWhileActive()
        {
            var ui = new UI();
            var clock = new Clock();
            var module = new CollectorActivityModule(CreateConfig(), new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), new Facts(), new Rewards(), ui), default)
                    .GetAwaiter().GetResult();

                ActivityEntryInfo entry = module.GetEntries()[0];
                Assert.That(entry.Visible, Is.True);
                Assert.That(entry.Interactable, Is.True);
                Assert.That(entry.CountdownEndUtc, Is.EqualTo(module.Config.EndUtc));

                module.OpenEntryAsync(CollectorPageKeys.Main, default).GetAwaiter().GetResult();
                Assert.That(ui.LastPage, Is.EqualTo(CollectorPageKeys.Main));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        private CollectorActivityConfig CreateConfig(int collectionPerWin = 1)
        {
            RewardDefinitionSO resource = ScriptableObject.CreateInstance<RewardDefinitionSO>();
            resource.Configure("common", "currency.money", "Coins", null, RewardResourceKind.Currency);

            CollectorRewardDefinition firstReward = CreateReward(resource, 10);
            CollectorRewardDefinition secondReward = CreateReward(resource, 20);
            var config = ScriptableObject.CreateInstance<CollectorActivityConfig>();
            config.SetInstallationData("collector-test", "Collector Test", "COLLECTOR",
                1789430000, 1789433600, unlockLevel: 0, editorAlwaysUnlocked: false,
                targetCount: 2, collectionPerWin: collectionPerWin, collectibleIcon: null,
                 tasks: new[]
                 {
                     new CollectorTaskDefinition(1, 2, firstReward),
                     new CollectorTaskDefinition(2, 2, secondReward)
                 });
            m_TestAssets.Add(resource);
            m_TestAssets.Add(firstReward);
            m_TestAssets.Add(secondReward);
            m_TestAssets.Add(config);
            return config;
        }

        private CollectorRewardDefinition CreateReward(RewardDefinitionSO resource, long amount)
        {
            var reward = ScriptableObject.CreateInstance<CollectorRewardDefinition>();
            reward.ConfigureEntries(new[] { new RewardEntry(resource, RewardGrantMode.AddQuantity, amount) });
            return reward;
        }

        private static int ReadTier(CollectorTaskRowView row)
        {
            var serialized = new SerializedObject(row);
            TMP_Text text = serialized.FindProperty("m_TaskNumber").objectReferenceValue as TMP_Text;
            return int.Parse(text.text);
        }

        private static ActivityLevelCompletedFact Fact(string id, string gameId, long sequence,
            DateTimeOffset occurredAt, bool won)
        {
            return new ActivityLevelCompletedFact(id, gameId, "collector-test-session", sequence,
                occurredAt, sequence.ToString(), won);
        }

        private sealed class Clock : IActivityClock
        {
            public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1789430400);
            public double MonotonicSeconds => 1d;
            public ActivityClockSource Source => ActivityClockSource.LocalEstimated;
        }

        private sealed class Storage : IActivityStorage
        {
            public string Payload;

            public UniTask<ActivityStorageReadResult> ReadAsync(string key,
                CancellationToken cancellationToken = default) =>
                UniTask.FromResult(string.IsNullOrEmpty(Payload)
                    ? new ActivityStorageReadResult(ActivityStorageReadStatus.Missing)
                    : new ActivityStorageReadResult(ActivityStorageReadStatus.Found, Payload, 1));

            public UniTask WriteAsync(string key, string payload, int schemaVersion,
                CancellationToken cancellationToken = default)
            {
                Payload = payload;
                return UniTask.CompletedTask;
            }

            public UniTask FlushAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        }

        private sealed class Facts : IActivityGameFacts
        {
            private readonly List<Action<IActivityGameFact>> m_Handlers = new List<Action<IActivityGameFact>>();

            public IDisposable Subscribe<TFact>(Action<TFact> handler) where TFact : class, IActivityGameFact
            {
                Action<IActivityGameFact> wrapper = fact =>
                {
                    if (fact is TFact typed) handler(typed);
                };
                m_Handlers.Add(wrapper);
                return new Subscription(() => m_Handlers.Remove(wrapper));
            }

            public void Publish(IActivityGameFact fact)
            {
                foreach (Action<IActivityGameFact> handler in m_Handlers.ToArray()) handler(fact);
            }

            private sealed class Subscription : IDisposable
            {
                private readonly Action m_Dispose;
                public Subscription(Action dispose) => m_Dispose = dispose;
                public void Dispose() => m_Dispose();
            }
        }

        private sealed class Rewards : IActivityRewardGateway
        {
            public int Grants { get; private set; }

            public bool CanGrant(string resourceKey, string target) =>
                resourceKey == "currency.money" && target == "profile";

            public UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request,
                CancellationToken cancellationToken = default)
            {
                Grants++;
                return UniTask.FromResult(new ActivityRewardReceipt(request.GrantId,
                    ActivityRewardStatus.Granted, "test"));
            }

            public UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId,
                CancellationToken cancellationToken = default) =>
                UniTask.FromResult(new ActivityRewardReceipt(grantId, ActivityRewardStatus.NotFound));
        }

        private sealed class UI : IActivityUI
        {
            public string LastPage { get; private set; }

            public UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request,
                CancellationToken cancellationToken = default)
            {
                LastPage = request.PageKey;
                return UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Opened,
                    new ActivityPageHandle(CollectorActivityModule.Id, 1, 1)));
            }

            public UniTask CloseAsync(ActivityPageHandle handle) => UniTask.CompletedTask;
            public UniTask CloseAllAsync() => UniTask.CompletedTask;
        }

        private sealed class Context : IActivityContext
        {
            public string ModuleId => CollectorActivityModule.Id;
            public string ProfileId => "collector-test-profile";
            public long Generation => 1;
            public CancellationToken LifetimeToken => default;
            public IActivityClock Clock { get; }
            public IActivityStorage Storage { get; }
            public IActivityGameFacts GameFacts { get; }
            public IActivityRewardGateway Rewards { get; }
            public IActivityUI UI { get; }

            public Context(Clock clock, Storage storage, Facts facts, Rewards rewards, UI ui = null)
            {
                Clock = clock;
                Storage = storage;
                GameFacts = facts;
                Rewards = rewards;
                UI = ui ?? new UI();
            }
        }
    }
}
