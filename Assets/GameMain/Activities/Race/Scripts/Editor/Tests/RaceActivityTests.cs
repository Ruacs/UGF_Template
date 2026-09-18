using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lokas.Activities.Race.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lokas.Activities.Race.Editor.Tests
{
    public sealed class RaceActivityTests
    {
        private readonly List<UnityEngine.Object> m_Assets = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object asset in m_Assets)
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            m_Assets.Clear();
        }

        [Test]
        public void RacePagesUseTheCorrectTemplatesAndSharedAvatarPrefab()
        {
            const string uiPath = "Assets/GameMain/Activities/Race/UI/";
            const string avatarPath = "Assets/GameMain/UI/UIPrefabs/Avatar/AvatarBox.prefab";
            GameObject start = AssetDatabase.LoadAssetAtPath<GameObject>(uiPath + "RaceStartUIPanel.prefab");
            GameObject main = AssetDatabase.LoadAssetAtPath<GameObject>(uiPath + "RaceMainUIPanel.prefab");
            GameObject detail = AssetDatabase.LoadAssetAtPath<GameObject>(uiPath + "RaceDetailUIPanel.prefab");
            Assert.That(start, Is.Not.Null);
            Assert.That(main, Is.Not.Null);
            Assert.That(detail, Is.Not.Null);
            Assert.That(start.transform.Find("Root/Content/RaceArt"), Is.Not.Null);
            Assert.That(detail.transform.Find("Root/Content/Content_01"), Is.Not.Null);
            Assert.That(main.transform.Find("Root"), Is.Null);
            Assert.That(main.transform.Find("SafeArea/Cars"), Is.Not.Null);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(main);
            try
            {
                for (int index = 0; index < 5; index++)
                {
                    string name = index == 0 ? "RaceCars" : "RaceCars_" + index;
                    Transform lane = instance.transform.Find("SafeArea/Cars/" + name);
                    Assert.That(lane, Is.Not.Null);
                  
                    Transform avatar = lane.Find("Infos/PlayerFrame/AvatarBox");
                    Assert.That(avatar, Is.Not.Null);
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(avatar.gameObject),
                        Is.EqualTo(avatarPath));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CarProgressMovesTheImageWithoutChangingItsSize()
        {
            const string lanePath = "Assets/GameMain/Activities/Race/UI/RaceCars.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(lanePath);
            Assert.That(prefab, Is.Not.Null);

            var clock = new Clock();
            var module = new RaceActivityModule(CreateConfig(), new[] { "Game" });
            GameObject instance = null;
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), new Facts(), new Rewards(), new UI()), default)
                    .GetAwaiter().GetResult();
                module.StartRaceAsync().GetAwaiter().GetResult();
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Transform laneTransform = instance.transform;
                RaceLaneView lane = laneTransform.GetComponent<RaceLaneView>();
                RectTransform car = laneTransform.Find("Handle/RaceCars/RaceCar") as RectTransform;
                Assert.That(lane, Is.Not.Null);
                Assert.That(car, Is.Not.Null);

                float height = car.rect.height;
                lane.Bind(module.GetSnapshot().Racers[1]);
                float initialY = car.anchoredPosition.y;
                clock.UtcNow = clock.UtcNow.AddMinutes(15);
                lane.Bind(module.GetSnapshot().Racers[1]);

                Assert.That(car.anchorMin.y, Is.EqualTo(car.anchorMax.y));
                Assert.That(car.rect.height, Is.EqualTo(height).Within(0.01f));
                Assert.That(car.anchoredPosition.y, Is.GreaterThan(initialY));
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void JoinConsumesOnlyConfiguredGameFactsAndClaimsOnceAfterTheGoal()
        {
            var clock = new Clock();
            var storage = new Storage();
            var facts = new Facts();
            var rewards = new Rewards();
            var ui = new UI();
            var module = new RaceActivityModule(CreateConfig(), new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(clock, storage, facts, rewards, ui), default).GetAwaiter().GetResult();
                module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
                Assert.That(ui.LastPage, Is.EqualTo(RacePageKeys.Start));

                Assert.That(module.StartRaceAsync().GetAwaiter().GetResult(), Is.True);
                RaceSnapshot started = module.GetSnapshot();
                Assert.That(started.State, Is.EqualTo(RaceActivityState.Racing));
                Assert.That(started.Racers.Count, Is.EqualTo(5));
                Assert.That(started.CompletedStages, Is.Zero);
                Assert.That(started.Racers, Has.All.Property(nameof(RaceRacerSnapshot.Progress01)).EqualTo(0f));

                facts.Publish(Fact("other-game", "Other", 1, clock.UtcNow));
                Assert.That(module.GetSnapshot().CompletedStages, Is.Zero);
                facts.Publish(Fact("race-1", "Game", 1, clock.UtcNow));
                facts.Publish(Fact("race-1", "Game", 1, clock.UtcNow));
                Assert.That(module.GetSnapshot().CompletedStages, Is.EqualTo(1));
                facts.Publish(Fact("race-2", "Game", 2, clock.UtcNow));

                RaceSnapshot finished = module.GetSnapshot();
                Assert.That(finished.IsFinished, Is.True);
                Assert.That(finished.PlayerRank, Is.InRange(1, 5));
                Assert.That(finished.CanClaim, Is.True);
                Assert.That(module.ClaimRewardAsync().GetAwaiter().GetResult().Status, Is.EqualTo(ActivityRewardStatus.Granted));
                Assert.That(module.ClaimRewardAsync().GetAwaiter().GetResult().Status, Is.EqualTo(ActivityRewardStatus.AlreadyGranted));
                Assert.That(rewards.Grants, Is.EqualTo(1));
                Assert.That(module.GetSnapshot().State, Is.EqualTo(RaceActivityState.RewardClaimed));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void RaceEndsAtItsOwnDeadlineWhenThePlayerHasNotReachedTheGoal()
        {
            var clock = new Clock();
            var module = new RaceActivityModule(CreateConfig(), new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), new Facts(), new Rewards(), new UI()), default).GetAwaiter().GetResult();
                module.StartRaceAsync().GetAwaiter().GetResult();
                clock.UtcNow = clock.UtcNow.AddMinutes(31);

                RaceSnapshot snapshot = module.GetSnapshot();
                Assert.That(snapshot.IsFinished, Is.True);
                Assert.That(snapshot.CompletedStages, Is.Zero);
                Assert.That(snapshot.PlayerRank, Is.InRange(1, 5));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void UntracedStartModesAreNotEnabledByTheLocalMvp()
        {
            var module = new RaceActivityModule(CreateConfig(), new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(new Clock(), new Storage(), new Facts(), new Rewards(), new UI()), default)
                    .GetAwaiter().GetResult();
                Assert.Throws<NotSupportedException>(() => module.StartRaceAsync(RaceStartType.Ad).GetAwaiter().GetResult());
                Assert.That(module.GetSnapshot().IsJoined, Is.False);
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void RaceUsesThirtyMinuteTermAndFifteenMinuteInfiniteHeartWindow()
        {
            RaceActivityConfig config = CreateConfig();
            Assert.That(config.TermHours, Is.EqualTo(0.5f));
            Assert.That(config.InfiniteHeartHours, Is.EqualTo(0.25f));
            Assert.That(config.PlayerMinGoalRate, Is.LessThan(config.PlayerMaxGoalRate));
            Assert.That(config.PlayerMaxGoalRate, Is.GreaterThan(config.NpcMinGoalRate));
        }

        [Test]
        public void NpcsAdvanceWithElapsedTimeAndWinInitialProgressTies()
        {
            var clock = new Clock();
            var module = new RaceActivityModule(CreateConfig(), new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), new Facts(), new Rewards(), new UI()), default)
                    .GetAwaiter().GetResult();
                module.StartRaceAsync().GetAwaiter().GetResult();
                Assert.That(module.GetSnapshot().PlayerRank, Is.EqualTo(5));

                clock.UtcNow = clock.UtcNow.AddMinutes(15);
                RaceSnapshot halfway = module.GetSnapshot();
                RaceRacerSnapshot player = null;
                foreach (RaceRacerSnapshot racer in halfway.Racers)
                    if (racer.IsPlayer) player = racer;
                Assert.That(player, Is.Not.Null);
                Assert.That(player.Progress01, Is.Zero);
                Assert.That(halfway.Racers[0].Progress01, Is.GreaterThan(0f));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        private RaceActivityConfig CreateConfig()
        {
            var resource = ScriptableObject.CreateInstance<RewardDefinitionSO>();
            resource.Configure("common", "currency.money", "金币", null, RewardResourceKind.Currency);
            var first = Bundle(resource, 500);
            var second = Bundle(resource, 300);
            var third = Bundle(resource, 150);
            var config = ScriptableObject.CreateInstance<RaceActivityConfig>();
            config.SetInstallationData("race-test", "Race Test", "RACE", 1788220800, 1798761599,
                unlockLevel: 0, editorAlwaysUnlocked: false, termHours: 0.5f, targetStageDifference: 2,
                defaultStartType: RaceStartType.Free,
                rankRewards: new[]
                {
                    new RaceRankRewardDefinition("First", 1, 1, first),
                    new RaceRankRewardDefinition("Second", 2, 2, second),
                    new RaceRankRewardDefinition("Finish", 3, 5, third)
                });
            m_Assets.Add(resource);
            m_Assets.Add(config);
            return config;
        }

        private RaceRewardDefinition Bundle(RewardDefinitionSO resource, long amount)
        {
            var bundle = ScriptableObject.CreateInstance<RaceRewardDefinition>();
            bundle.ConfigureEntries(new[] { new RewardEntry(resource, RewardGrantMode.AddQuantity, amount) });
            m_Assets.Add(bundle);
            return bundle;
        }

        private static ActivityLevelCompletedFact Fact(string id, string gameId, int level, DateTimeOffset time) =>
            new ActivityLevelCompletedFact(id, gameId, "session", level, time, level.ToString(), true);

        private sealed class Clock : IActivityClock
        {
            public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1789430400);
            public double MonotonicSeconds => 1d;
            public ActivityClockSource Source => ActivityClockSource.LocalEstimated;
        }

        private sealed class Storage : IActivityStorage
        {
            public string Payload;
            public UniTask<ActivityStorageReadResult> ReadAsync(string key, CancellationToken cancellationToken = default) =>
                UniTask.FromResult(string.IsNullOrEmpty(Payload)
                    ? new ActivityStorageReadResult(ActivityStorageReadStatus.Missing)
                    : new ActivityStorageReadResult(ActivityStorageReadStatus.Found, Payload, 1));
            public UniTask WriteAsync(string key, string payload, int schemaVersion, CancellationToken cancellationToken = default)
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
                Action<IActivityGameFact> wrapped = fact => { if (fact is TFact typed) handler(typed); };
                m_Handlers.Add(wrapped);
                return new Subscription(() => m_Handlers.Remove(wrapped));
            }
            public void Publish(IActivityGameFact fact)
            {
                foreach (Action<IActivityGameFact> handler in m_Handlers.ToArray()) handler(fact);
            }
            private sealed class Subscription : IDisposable
            {
                private readonly Action m_Dispose;
                public Subscription(Action dispose) { m_Dispose = dispose; }
                public void Dispose() => m_Dispose();
            }
        }

        private sealed class Rewards : IActivityRewardGateway
        {
            public int Grants { get; private set; }
            public bool CanGrant(string resourceKey, string target) => resourceKey == "currency.money" && target == "profile";
            public UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request, CancellationToken cancellationToken = default)
            {
                Grants++;
                return UniTask.FromResult(new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Granted, "test"));
            }
            public UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId, CancellationToken cancellationToken = default) =>
                UniTask.FromResult(new ActivityRewardReceipt(grantId, ActivityRewardStatus.NotFound));
        }

        private sealed class UI : IActivityUI
        {
            public string LastPage { get; private set; }
            public UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request, CancellationToken cancellationToken = default)
            {
                LastPage = request.PageKey;
                return UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Opened,
                    new ActivityPageHandle(RaceActivityModule.Id, 1, 1)));
            }
            public UniTask CloseAsync(ActivityPageHandle handle) => UniTask.CompletedTask;
            public UniTask CloseAllAsync() => UniTask.CompletedTask;
        }

        private sealed class Context : IActivityContext
        {
            public string ModuleId => RaceActivityModule.Id;
            public string ProfileId => "test";
            public long Generation => 1;
            public CancellationToken LifetimeToken => default;
            public IActivityClock Clock { get; }
            public IActivityStorage Storage { get; }
            public IActivityGameFacts GameFacts { get; }
            public IActivityRewardGateway Rewards { get; }
            public IActivityUI UI { get; }

            public Context(Clock clock, Storage storage, Facts facts, Rewards rewards, UI ui)
            {
                Clock = clock;
                Storage = storage;
                GameFacts = facts;
                Rewards = rewards;
                UI = ui;
            }
        }
    }
}
