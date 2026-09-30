using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lokas.Activities.Mining;
using Lokas.Activities.Mining.UI;
using NUnit.Framework;
using UnityEditor;

namespace Lokas.Editor.Tests.Activity
{
    public sealed class MiningActivityModuleTests
    {
        private const string ConfigPath =
            "Assets/GameMain/Activities/Mining/ScriptableObjects/MiningActivityConfig.asset";

        [Test]
        public void ReopeningEntry_RestoresLatestSnapshotFromMemoryAndStorage()
        {
            MiningActivityConfig config = LoadConfig();
            var storage = new Storage();
            var firstUi = new UI();
            var firstModule = new MiningActivityModule(config);
            firstModule.InitializeAsync(new Context(storage, firstUi), default).GetAwaiter().GetResult();
            firstModule.OpenEntryAsync("main", default).GetAwaiter().GetResult();

            MiningBoardOpenArgs initialArgs = GetOpenArgs(firstUi);
            var model = new MiningBoardModel(config, config.GetStage(1, 3), 41);
            IReadOnlyList<int> gemCells = model.GetCellsForGem(model.Stage.Gems[0].GemId);
            model.Dig(gemCells[0]);
            MiningBoardSnapshot expected = model.CreateSnapshot();
            initialArgs.StateChanged(expected);

            firstModule.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            AssertOpenArgs(GetOpenArgs(firstUi), expected);
            Assert.That(storage.WriteCount, Is.GreaterThan(0));
            Assert.That(storage.SchemaVersion, Is.EqualTo(2));

            firstModule.ShutdownAsync().GetAwaiter().GetResult();

            var restoredUi = new UI();
            var restoredModule = new MiningActivityModule(config);
            restoredModule.InitializeAsync(new Context(storage, restoredUi), default).GetAwaiter().GetResult();
            restoredModule.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            AssertOpenArgs(GetOpenArgs(restoredUi), expected);
            restoredModule.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void CompletedNonFinalStage_RestoresAtFollowingStage()
        {
            MiningActivityConfig config = LoadConfig();
            var storage = new Storage();
            var ui = new UI();
            var module = new MiningActivityModule(config);
            module.InitializeAsync(new Context(storage, ui), default).GetAwaiter().GetResult();
            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();

            var model = new MiningBoardModel(config, config.GetStage(1, 2), 99);
            int cellCount = model.Stage.CellCount * model.Stage.CellCount;
            for (int cellId = 0; cellId < cellCount && !model.IsStageComplete; cellId++)
                model.Dig(cellId);
            Assert.That(model.IsStageComplete, Is.True);

            GetOpenArgs(ui).StateChanged(model.CreateSnapshot());
            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            MiningBoardOpenArgs restored = GetOpenArgs(ui);
            Assert.That(restored.EventId, Is.EqualTo(1));
            Assert.That(restored.StepId, Is.EqualTo(3));
            Assert.That(restored.PickaxeCount, Is.EqualTo(model.PickaxeCount));
            Assert.That(restored.OpenCellIds, Is.Empty);
            Assert.That(restored.CollectedGemIds, Is.Empty);
            module.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void CompletedStage_ClaimPersistsAndCannotGrantTwice()
        {
            MiningActivityConfig config = LoadConfig();
            var storage = new Storage();
            var ui = new UI();
            var rewards = new Rewards();
            var module = new MiningActivityModule(config);
            module.InitializeAsync(new Context(storage, ui, rewards: rewards), default)
                .GetAwaiter().GetResult();
            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();

            var model = new MiningBoardModel(config, config.GetStage(1, 1), 99);
            int cellCount = model.Stage.CellCount * model.Stage.CellCount;
            for (int cellId = 0; cellId < cellCount && !model.IsStageComplete; cellId++)
                model.Dig(cellId);
            Assert.That(model.IsStageComplete, Is.True);
            GetOpenArgs(ui).StateChanged(model.CreateSnapshot());

            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            MiningBoardOpenArgs claimArgs = GetOpenArgs(ui);
            Assert.That(claimArgs.StepId, Is.EqualTo(2));
            Assert.That(claimArgs.ClaimedRewardStepIds, Is.Empty);
            Assert.That(claimArgs.ClaimRewardAsync, Is.Not.Null);

            ActivityRewardReceipt first = claimArgs.ClaimRewardAsync(1).GetAwaiter().GetResult();
            ActivityRewardReceipt repeated = claimArgs.ClaimRewardAsync(1).GetAwaiter().GetResult();
            Assert.That(first.Status, Is.EqualTo(ActivityRewardStatus.Granted));
            Assert.That(repeated.Status, Is.EqualTo(ActivityRewardStatus.AlreadyGranted));
            Assert.That(rewards.Grants, Is.EqualTo(1));
            Assert.That(storage.SchemaVersion, Is.EqualTo(2));

            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            Assert.That(GetOpenArgs(ui).ClaimedRewardStepIds, Does.Contain(1));
            module.ShutdownAsync().GetAwaiter().GetResult();

            var restoredUi = new UI();
            var restoredModule = new MiningActivityModule(config);
            restoredModule.InitializeAsync(new Context(storage, restoredUi, rewards: rewards), default)
                .GetAwaiter().GetResult();
            restoredModule.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            MiningBoardOpenArgs restoredArgs = GetOpenArgs(restoredUi);
            Assert.That(restoredArgs.ClaimedRewardStepIds, Does.Contain(1));
            Assert.That(restoredArgs.ClaimRewardAsync(1).GetAwaiter().GetResult().Status,
                Is.EqualTo(ActivityRewardStatus.AlreadyGranted));
            Assert.That(rewards.Grants, Is.EqualTo(1));
            restoredModule.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void EntryAndPageArgs_UseConfiguredScheduleAndActivityClock()
        {
            MiningActivityConfig config = LoadConfig();
            var storage = new Storage();
            var ui = new UI();
            var clock = new Clock { UtcNow = config.ScheduleConfig.StartUtc.AddHours(6) };
            var module = new MiningActivityModule(config);
            module.InitializeAsync(new Context(storage, ui, clock), default).GetAwaiter().GetResult();

            IReadOnlyList<ActivityEntryInfo> entries = module.GetEntries();
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].CountdownEndUtc, Is.EqualTo(config.ScheduleConfig.EndUtc));

            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            MiningBoardOpenArgs args = GetOpenArgs(ui);
            Assert.That(args.EventId, Is.EqualTo(config.ScheduleConfig.EventId));
            Assert.That(args.CountdownEndUtc, Is.EqualTo(config.ScheduleConfig.EndUtc));
            Assert.That(args.UtcNow, Is.Not.Null);
            Assert.That(args.UtcNow(), Is.EqualTo(clock.UtcNow));
            module.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void SupplementalPages_OpenThroughTheirRegisteredKeys()
        {
            MiningActivityConfig config = LoadConfig();
            var ui = new UI();
            var module = new MiningActivityModule(config);
            module.InitializeAsync(new Context(new Storage(), ui), default).GetAwaiter().GetResult();

            module.OpenStartAsync().GetAwaiter().GetResult();
            Assert.That(ui.LastRequest.PageKey, Is.EqualTo(MiningPageKeys.Start));
            Assert.That(ui.LastRequest.Arguments, Is.SameAs(module));
            module.OpenDetailsAsync().GetAwaiter().GetResult();
            Assert.That(ui.LastRequest.PageKey, Is.EqualTo(MiningPageKeys.Details));
            Assert.That(ui.LastRequest.Arguments, Is.SameAs(module));
            module.OpenEndAsync().GetAwaiter().GetResult();
            Assert.That(ui.LastRequest.PageKey, Is.EqualTo(MiningPageKeys.End));
            Assert.That(ui.LastRequest.Arguments, Is.SameAs(module));
            module.OpenMainAsync().GetAwaiter().GetResult();
            Assert.That(ui.LastRequest.PageKey, Is.EqualTo(MiningPageKeys.Main));
            Assert.That(ui.LastRequest.Arguments, Is.TypeOf<MiningBoardOpenArgs>());

            module.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void SnapshotFromAnotherEvent_ResetsToScheduledEvent()
        {
            MiningActivityConfig config = LoadConfig();
            var storage = new Storage();
            storage.Seed("{\"eventId\":2,\"stepId\":4,\"pickaxeCount\":12,\"openCellIds\":[]," +
                "\"collectedGemIds\":[]}");
            var ui = new UI();
            var module = new MiningActivityModule(config);
            module.InitializeAsync(new Context(storage, ui), default).GetAwaiter().GetResult();
            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();

            MiningBoardOpenArgs args = GetOpenArgs(ui);
            Assert.That(args.EventId, Is.EqualTo(config.ScheduleConfig.EventId));
            Assert.That(args.StepId, Is.EqualTo(1));
            Assert.That(args.PickaxeCount, Is.EqualTo(99));
            Assert.That(args.OpenCellIds, Is.Empty);
            Assert.That(args.CollectedGemIds, Is.Empty);
            module.ShutdownAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void TestMode_PickaxesApplyIndependently_AndStageSelectionPreservesThem()
        {
            MiningActivityConfig config = LoadConfig();
            var storage = new Storage();
            var ui = new UI();
            var module = new MiningActivityModule(config);
            var testMode = new MiningTestModeModule(module);
            Assert.That(testMode.OwnerId, Is.EqualTo(MiningActivityModule.Id));
            Assert.That(testMode.PageName, Is.EqualTo("Mining"));
            Assert.That(testMode.ModuleName, Is.EqualTo("Progress"));

            module.InitializeAsync(new Context(storage, ui), default).GetAwaiter().GetResult();
            InvokeTestVoid(module, "SetTestPickaxeCount", 321);
            Assert.That(ui.CloseAllCount, Is.Zero,
                "Changing pickaxes must not require opening or closing a Mining stage.");
            InvokeTestUniTask(module, "OpenTestStageAsync", 2, 4, CancellationToken.None);

            Assert.That(ui.CloseAllCount, Is.EqualTo(1));
            MiningBoardOpenArgs selected = GetOpenArgs(ui);
            Assert.That(selected.EventId, Is.EqualTo(2));
            Assert.That(selected.StepId, Is.EqualTo(4));
            Assert.That(selected.PickaxeCount, Is.EqualTo(321));
            Assert.That(selected.OpenCellIds, Is.Empty);
            Assert.That(selected.CollectedGemIds, Is.Empty);

            InvokeTestVoid(module, "SetTestPickaxeCount", 456);
            Assert.That(ui.CloseAllCount, Is.EqualTo(1));
            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            Assert.That(GetOpenArgs(ui).PickaxeCount, Is.EqualTo(456));

            InvokeTestUniTask(module, "ResetTestStateAsync", CancellationToken.None);
            Assert.That(ui.CloseAllCount, Is.EqualTo(2));
            module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
            MiningBoardOpenArgs reset = GetOpenArgs(ui);
            Assert.That(reset.EventId, Is.EqualTo(config.ScheduleConfig.EventId));
            Assert.That(reset.StepId, Is.EqualTo(1));
            Assert.That(reset.PickaxeCount, Is.EqualTo(99));
            module.ShutdownAsync().GetAwaiter().GetResult();
        }

        private static MiningBoardOpenArgs GetOpenArgs(UI ui)
        {
            Assert.That(ui.LastRequest, Is.Not.Null);
            Assert.That(ui.LastRequest.Arguments, Is.TypeOf<MiningBoardOpenArgs>());
            return (MiningBoardOpenArgs)ui.LastRequest.Arguments;
        }

        private static void AssertOpenArgs(MiningBoardOpenArgs actual, MiningBoardSnapshot expected)
        {
            Assert.That(actual.EventId, Is.EqualTo(expected.EventId));
            Assert.That(actual.StepId, Is.EqualTo(expected.StepId));
            Assert.That(actual.PickaxeCount, Is.EqualTo(expected.PickaxeCount));
            Assert.That(actual.OpenCellIds, Is.EquivalentTo(expected.OpenCellIds));
            Assert.That(actual.CollectedGemIds, Is.EquivalentTo(expected.CollectedGemIds));
        }

        private static void InvokeTestUniTask(MiningActivityModule module, string methodName,
            params object[] arguments)
        {
            MethodInfo method = typeof(MiningActivityModule).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var task = (UniTask)method.Invoke(module, arguments);
            task.GetAwaiter().GetResult();
        }

        private static void InvokeTestVoid(MiningActivityModule module, string methodName,
            params object[] arguments)
        {
            MethodInfo method = typeof(MiningActivityModule).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(module, arguments);
        }

        private static MiningActivityConfig LoadConfig()
        {
            MiningActivityConfig config = AssetDatabase.LoadAssetAtPath<MiningActivityConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null);
            return config;
        }

        private sealed class Storage : IActivityStorage
        {
            public string Payload { get; private set; }
            public int SchemaVersion { get; private set; }
            public int WriteCount { get; private set; }

            public void Seed(string payload, int schemaVersion = 1)
            {
                Payload = payload;
                SchemaVersion = schemaVersion;
            }

            public UniTask<ActivityStorageReadResult> ReadAsync(string key,
                CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(string.IsNullOrEmpty(Payload)
                    ? new ActivityStorageReadResult(ActivityStorageReadStatus.Missing)
                    : new ActivityStorageReadResult(ActivityStorageReadStatus.Found, Payload, SchemaVersion));
            }

            public UniTask WriteAsync(string key, string payload, int schemaVersion,
                CancellationToken cancellationToken = default)
            {
                Payload = payload;
                SchemaVersion = schemaVersion;
                WriteCount++;
                return UniTask.CompletedTask;
            }

            public UniTask FlushAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        }

        private sealed class UI : IActivityUI
        {
            private int m_SerialId;
            public ActivityPageRequest LastRequest { get; private set; }
            public int CloseAllCount { get; private set; }

            public UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request,
                CancellationToken cancellationToken = default)
            {
                LastRequest = request;
                return UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Opened,
                    new ActivityPageHandle(MiningActivityModule.Id, 1, ++m_SerialId)));
            }

            public UniTask CloseAsync(ActivityPageHandle handle) => UniTask.CompletedTask;
            public UniTask CloseAllAsync()
            {
                CloseAllCount++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class Rewards : IActivityRewardGateway
        {
            public int Grants { get; private set; }

            public bool CanGrant(string resourceKey, string target)
            {
                return !string.IsNullOrWhiteSpace(resourceKey) && target == "profile";
            }

            public UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request,
                CancellationToken cancellationToken = default)
            {
                Grants++;
                return UniTask.FromResult(new ActivityRewardReceipt(request.GrantId,
                    ActivityRewardStatus.Granted, "mining-test-receipt"));
            }

            public UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId,
                CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(new ActivityRewardReceipt(grantId,
                    ActivityRewardStatus.NotFound));
            }
        }

        private sealed class Context : IActivityContext
        {
            public string ModuleId => MiningActivityModule.Id;
            public string ProfileId => "mining-test-profile";
            public long Generation => 1;
            public CancellationToken LifetimeToken => default;
            public IActivityClock Clock { get; }
            public IActivityStorage Storage { get; }
            public IActivityGameFacts GameFacts => null;
            public IActivityRewardGateway Rewards { get; }
            public IActivityUI UI { get; }

            public Context(Storage storage, UI ui, IActivityClock clock = null,
                IActivityRewardGateway rewards = null)
            {
                Storage = storage;
                UI = ui;
                Clock = clock;
                Rewards = rewards;
            }
        }

        private sealed class Clock : IActivityClock
        {
            public DateTimeOffset UtcNow { get; set; }
            public double MonotonicSeconds => 0d;
            public ActivityClockSource Source => ActivityClockSource.ServerSynchronized;
        }
    }
}
