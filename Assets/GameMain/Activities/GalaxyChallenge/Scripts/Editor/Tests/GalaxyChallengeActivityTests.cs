using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Lokas.Activities.GalaxyChallenge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lokas.Activities.GalaxyChallenge.Editor.Tests
{
    public sealed class GalaxyChallengeActivityTests
    {
        private const string Root = "Assets/GameMain/Activities/GalaxyChallenge/";
        private const string CatalogPath =
            "Assets/GameMain/ScriptableObjects/ActivitySystem/ActivityModuleCatalog.asset";

        [Test]
        public void InstalledDefinitionRegistersEveryAuthoredPage()
        {
            GalaxyChallengeActivityConfig config = AssetDatabase.LoadAssetAtPath<GalaxyChallengeActivityConfig>(
                Root + "ScriptableObjects/Config/HexaGalaxyChallenge202609.asset");
            GalaxyChallengeActivityDefinition definition =
                AssetDatabase.LoadAssetAtPath<GalaxyChallengeActivityDefinition>(
                    Root + "ScriptableObjects/Registry/GalaxyChallengeActivityDefinition.asset");
            ActivityModuleCatalogConfig catalog =
                AssetDatabase.LoadAssetAtPath<ActivityModuleCatalogConfig>(CatalogPath);

            Assert.That(config, Is.Not.Null);
            Assert.That(definition, Is.Not.Null);
            Assert.That(catalog, Is.Not.Null);
            Assert.DoesNotThrow(config.ValidateConfiguration);
            Assert.That(definition.Config, Is.SameAs(config));
            Assert.That(definition.ModuleId, Is.EqualTo(GalaxyChallengeActivityModule.Id));
            Assert.That(definition.GameIds, Is.EqualTo(new[] { "Game" }));
            Assert.That(catalog.Modules, Does.Contain(definition));

            string[] keys = definition.Pages.Select(page => page.PageKey).ToArray();
            int[] ids = definition.Pages.Select(page => page.UIFormId).ToArray();
            Assert.That(keys, Is.EqualTo(new[] { "start", "main", "details", "end", "interval" }));
            Assert.That(ids, Is.EqualTo(new[]
            {
                UIFormIdRanges.GalaxyChallengeStartPage,
                UIFormIdRanges.GalaxyChallengeMain,
                UIFormIdRanges.GalaxyChallengeDetails,
                UIFormIdRanges.GalaxyChallengeEndPage,
                UIFormIdRanges.GalaxyChallengeInterval
            }));
            foreach (ActivityPageDefinition page in definition.Pages)
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(page.PrefabAssetPath), Is.Not.Null,
                    page.PrefabAssetPath);
        }

        [Test]
        public void ConfigCreatesStrictlyDecreasingFiveAndSevenStepCrowds()
        {
            GalaxyChallengeActivityConfig config = AssetDatabase.LoadAssetAtPath<GalaxyChallengeActivityConfig>(
                Root + "ScriptableObjects/Config/HexaGalaxyChallenge202609.asset");
            Assert.That(config, Is.Not.Null);

            GalaxyChallengeEventDefinition sevenStep = config.GetEvent(1);
            GalaxyChallengeEventDefinition fiveStep = config.GetEvent(2);
            AssertCrowdSequence(sevenStep, sevenStep.CreateUserCounts(1001), 4, 6);
            int[] fiveCounts = fiveStep.CreateUserCounts(2002);
            AssertCrowdSequence(fiveStep, fiveCounts, 6, 7);
            Assert.That(fiveCounts.Take(5), Is.EqualTo(new[] { 100, 40, 18, 12, 8 }),
                "The authored five-step mode keeps the complete distribution recovered from the export.");
        }

        [Test]
        public void TestModeSwitchesToSevenStepsAdvancesAndResetsTheSelectedMode()
        {
            GalaxyChallengeActivityConfig config = AssetDatabase.LoadAssetAtPath<GalaxyChallengeActivityConfig>(
                Root + "ScriptableObjects/Config/HexaGalaxyChallenge202609.asset");
            var storage = new Storage();
            var facts = new Facts();
            var module = new GalaxyChallengeActivityModule(config, new[] { "Game" });
            var testMode = new GalaxyChallengeTestModeModule(module);

            Assert.That(testMode.OwnerId, Is.EqualTo(GalaxyChallengeActivityModule.Id));
            Assert.That(testMode.PageName, Is.EqualTo("Galaxy Challenge"));
            Assert.That(testMode.ModuleName, Is.EqualTo("Progress"));

            try
            {
                module.InitializeAsync(new Context(storage, facts), default).GetAwaiter().GetResult();
                ((UniTask)InvokePrivate(module, "SetTestEventAsync", 1)).GetAwaiter().GetResult();
                GalaxyChallengeSnapshot selected = module.GetSnapshot();
                Assert.That(selected.EventId, Is.EqualTo(1));
                Assert.That(selected.StepCount, Is.EqualTo(7));
                Assert.That(selected.CurrentProgress, Is.Zero);

                ((UniTask)InvokePrivate(module, "JoinTestAsync")).GetAwaiter().GetResult();
                var simulatedWins = new List<ActivityLevelCompletedFact>
                {
                    new ActivityLevelCompletedFact("galaxy-test-mode-1", "Game", "galaxy-test-mode", 1,
                        DateTimeOffset.FromUnixTimeSeconds(1789430400), "1", true),
                    new ActivityLevelCompletedFact("galaxy-test-mode-2", "Game", "galaxy-test-mode", 2,
                        DateTimeOffset.FromUnixTimeSeconds(1789430401), "2", true)
                };
                InvokePrivate(module, "ProcessTestFacts", simulatedWins);

                GalaxyChallengeSnapshot advanced = module.GetSnapshot();
                Assert.That(advanced.CurrentProgress, Is.EqualTo(2));
                Assert.That(advanced.HasPendingProgress, Is.True);
                Assert.That(advanced.CurrentUserCount, Is.LessThan(advanced.JoinedUserCount));

                ((UniTask)InvokePrivate(module, "ResetTestStateAsync")).GetAwaiter().GetResult();
                GalaxyChallengeSnapshot reset = module.GetSnapshot();
                Assert.That(reset.EventId, Is.EqualTo(1));
                Assert.That(reset.StepCount, Is.EqualTo(7));
                Assert.That(reset.CurrentProgress, Is.Zero);
                Assert.That(reset.IsJoined, Is.False);
                Assert.That(reset.HasPendingProgress, Is.False);
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void WonProgressRemainsPendingUntilTheAvatarAnimationAcknowledgesIt()
        {
            GalaxyChallengeActivityConfig config = AssetDatabase.LoadAssetAtPath<GalaxyChallengeActivityConfig>(
                Root + "ScriptableObjects/Config/HexaGalaxyChallenge202609.asset");
            var storage = new Storage();
            var facts = new Facts();
            var module = new GalaxyChallengeActivityModule(config, new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(storage, facts), default).GetAwaiter().GetResult();
                module.JoinAsync().GetAwaiter().GetResult();
                facts.Publish(new ActivityLevelCompletedFact("galaxy-win-1", "Game", "galaxy-session", 1,
                    DateTimeOffset.FromUnixTimeSeconds(1789430400), "1", true));

                GalaxyChallengeSnapshot pending = module.GetSnapshot();
                Assert.That(pending.CurrentProgress, Is.EqualTo(1));
                Assert.That(pending.LastPresentedProgress, Is.Zero);
                Assert.That(pending.HasPendingProgress, Is.True);
                Assert.That(pending.UserCountsInStep.Count, Is.EqualTo(pending.StepCount + 1));

                module.MarkProgressPresented(1);
                GalaxyChallengeSnapshot presented = module.GetSnapshot();
                Assert.That(presented.LastPresentedProgress, Is.EqualTo(1));
                Assert.That(presented.HasPendingProgress, Is.False);
                Assert.That(storage.SchemaVersion, Is.EqualTo(3));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void FailedLevelAdvancesToNextStepAndIncludesPlayerInElimination()
        {
            GalaxyChallengeActivityConfig config = AssetDatabase.LoadAssetAtPath<GalaxyChallengeActivityConfig>(
                Root + "ScriptableObjects/Config/HexaGalaxyChallenge202609.asset");
            var storage = new Storage();
            var facts = new Facts();
            var module = new GalaxyChallengeActivityModule(config, new[] { "Game" });
            Scene previewScene = default;
            GameObject instance = null;
            Sequence sequence = null;
            try
            {
                module.InitializeAsync(new Context(storage, facts), default).GetAwaiter().GetResult();
                module.JoinAsync().GetAwaiter().GetResult();
                facts.Publish(new ActivityLevelCompletedFact("galaxy-fail-1", "Game", "galaxy-session", 1,
                    DateTimeOffset.FromUnixTimeSeconds(1789430400), "1", false));
                GalaxyChallengeSnapshot snapshot = module.GetSnapshot();
                Assert.That(snapshot.CurrentProgress, Is.EqualTo(1));
                Assert.That(snapshot.IsFailed, Is.True);
                Assert.That(snapshot.State, Is.EqualTo(GalaxyChallengeState.Interval));
                Assert.That(snapshot.HasPendingProgress, Is.True);
                Assert.That(snapshot.IntervalEndUtc, Is.Not.Null);

                previewScene = EditorSceneManager.NewPreviewScene();
                instance = UnityEngine.Object.Instantiate(LoadPrefab("GalaxyChallengeMainUIPanel"));
                SceneManager.MoveGameObjectToScene(instance, previewScene);
                GalaxyChallengeMainUIPanel panel = instance.GetComponent<GalaxyChallengeMainUIPanel>();
                SetPrivateField(panel, "m_Module", module);
                InvokePrivate(panel, "CacheAuthoredAvatarLayout");
                InvokePrivate(panel, "ResolveActiveContainer", snapshot.StepCount);
                InvokePrivate(panel, "ApplyStableState", snapshot, 0);
                sequence = InvokePrivate(panel, "BuildStepTransition", snapshot, 0, 1) as Sequence;
                sequence.Play();
                sequence.Complete(true);

                SerializedProperty avatars = new SerializedObject(panel).FindProperty("m_Avatars");
                GalaxyChallengeAvatarCellView player = avatars.GetArrayElementAtIndex(0).objectReferenceValue
                    as GalaxyChallengeAvatarCellView;
                Assert.That(player, Is.Not.Null);
                Assert.That(player.gameObject.activeSelf, Is.False);
            }
            finally
            {
                sequence?.Kill(false);
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void AuthoredAvatarPoolCompletesOneJumpAndEliminationTransition()
        {
            GalaxyChallengeActivityConfig config = AssetDatabase.LoadAssetAtPath<GalaxyChallengeActivityConfig>(
                Root + "ScriptableObjects/Config/HexaGalaxyChallenge202609.asset");
            var storage = new Storage();
            var facts = new Facts();
            var module = new GalaxyChallengeActivityModule(config, new[] { "Game" });
            Scene previewScene = default;
            GameObject instance = null;
            Sequence sequence = null;
            try
            {
                module.InitializeAsync(new Context(storage, facts), default).GetAwaiter().GetResult();
                module.JoinAsync().GetAwaiter().GetResult();
                facts.Publish(new ActivityLevelCompletedFact("galaxy-animation-1", "Game", "galaxy-session", 1,
                    DateTimeOffset.FromUnixTimeSeconds(1789430400), "1", true));
                GalaxyChallengeSnapshot snapshot = module.GetSnapshot();

                previewScene = EditorSceneManager.NewPreviewScene();
                instance = UnityEngine.Object.Instantiate(LoadPrefab("GalaxyChallengeMainUIPanel"));
                SceneManager.MoveGameObjectToScene(instance, previewScene);
                GalaxyChallengeMainUIPanel panel = instance.GetComponent<GalaxyChallengeMainUIPanel>();
                SetPrivateField(panel, "m_Module", module);
                InvokePrivate(panel, "CacheAuthoredAvatarLayout");
                InvokePrivate(panel, "ResolveActiveContainer", snapshot.StepCount);
                InvokePrivate(panel, "ApplyStableState", snapshot, 0);

                sequence = InvokePrivate(panel, "BuildStepTransition", snapshot, 0, 1) as Sequence;
                Assert.That(sequence, Is.Not.Null);
                sequence.Play();
                sequence.Complete(true);

                GalaxyChallengeSnapshot presented = module.GetSnapshot();
                Assert.That(presented.LastPresentedProgress, Is.EqualTo(1));
                Assert.That(presented.HasPendingProgress, Is.False);

                SerializedProperty avatars = new SerializedObject(panel).FindProperty("m_Avatars");
                int visibleCount = 0;
                for (int index = 0; index < avatars.arraySize; index++)
                {
                    GalaxyChallengeAvatarCellView avatar = avatars.GetArrayElementAtIndex(index)
                        .objectReferenceValue as GalaxyChallengeAvatarCellView;
                    if (avatar != null && avatar.gameObject.activeSelf) visibleCount++;
                }
                int finalVisible = Mathf.Clamp(snapshot.UserCountsInStep[snapshot.StepCount], 1, 24);
                int expectedVisible = Mathf.RoundToInt(Mathf.Lerp(24f, finalVisible, 1f / snapshot.StepCount));
                Assert.That(visibleCount, Is.EqualTo(expectedVisible));
                Assert.That(24 - visibleCount, Is.GreaterThanOrEqualTo(3),
                    "The enlarged visual pool should show several avatars falling per step.");
                GalaxyChallengeAvatarCellView player = avatars.GetArrayElementAtIndex(0).objectReferenceValue
                    as GalaxyChallengeAvatarCellView;
                Assert.That(player, Is.Not.Null);
                Assert.That(player.gameObject.activeSelf, Is.True, "The current player is never eliminated.");
            }
            finally
            {
                sequence?.Kill(false);
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void PrefabsKeepAllInspectorReferencesInsideTheAuthoredPages()
        {
            AssertReferences<GalaxyChallengeStartUIPanel>("GalaxyChallengeStartUIPanel",
                "m_Countdown", "m_Description", "m_StartButton", "m_CloseButton");
            AssertReferences<GalaxyChallengeDetailUIPanel>("GalaxyChallengeDetailUIPanel",
                "m_CloseButton", "m_ContinueButton");
            AssertReferences<GalaxyChallengeEndUIPanel>("GalaxyChallengeEndUIPanel",
                "m_BestProgress", "m_PrizePool", "m_CloseButton");
            AssertReferences<GalaxyChallengeIntervalUIPanel>("GalaxyChallengeIntervalUIPanel",
                "m_Countdown", "m_ContinueButton", "m_CloseButton");

            GameObject prefab = LoadPrefab("GalaxyChallengeMainUIPanel");
            GalaxyChallengeMainUIPanel panel = prefab.GetComponent<GalaxyChallengeMainUIPanel>();
            Assert.That(panel, Is.Not.Null);
            var serialized = new SerializedObject(panel);
            foreach (string propertyName in new[]
                     {
                         "m_Countdown", "m_Description", "m_LevelCount", "m_PlayerCount", "m_PrizePool",
                         "m_DetailButton", "m_CloseButton", "m_Level5Container", "m_Level7Container",
                         "m_AvatarLayer", "m_AvatarLayoutOrigin", "m_GoalReadyState", "m_FailedState",
                         "m_ReadyOverlay", "m_TapToContinueButton", "m_VictoryOverlay", "m_VictoryReward",
                         "m_VictoryClaimButton", "m_VictoryPlayerAvatar"
                     })
                Assert.That(serialized.FindProperty(propertyName).objectReferenceValue, Is.Not.Null, propertyName);

            AssertStepContainer(serialized.FindProperty("m_Level5Container").objectReferenceValue as
                GalaxyChallengeStepContainerView, 6);
            AssertStepContainer(serialized.FindProperty("m_Level7Container").objectReferenceValue as
                GalaxyChallengeStepContainerView, 8);

            SerializedProperty avatars = serialized.FindProperty("m_Avatars");
            Assert.That(avatars.arraySize, Is.EqualTo(24));
            HashSet<int> authoredRows = new HashSet<int>();
            for (int index = 0; index < avatars.arraySize; index++)
            {
                GalaxyChallengeAvatarCellView avatar = avatars.GetArrayElementAtIndex(index).objectReferenceValue
                    as GalaxyChallengeAvatarCellView;
                Assert.That(avatar, Is.Not.Null, $"Avatar {index}");
                var avatarSerialized = new SerializedObject(avatar);
                Assert.That(avatarSerialized.FindProperty("m_Avatar").objectReferenceValue, Is.Not.Null,
                    $"Avatar {index}: m_Avatar");
                CanvasGroup canvasGroup = avatarSerialized.FindProperty("m_CanvasGroup").objectReferenceValue
                    as CanvasGroup;
                Assert.That(canvasGroup, Is.Not.Null, $"Avatar {index}: m_CanvasGroup");
                Assert.That(canvasGroup.blocksRaycasts, Is.False);
                Assert.That(canvasGroup.interactable, Is.False);
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(avatar.gameObject);
                Assert.That(AssetDatabase.GetAssetPath(source),
                    Is.EqualTo("Assets/GameMain/UI/UIPrefabs/Avatar/AvatarBox.prefab"));
                RectTransform rect = avatar.RectTransform;
                authoredRows.Add(Mathf.RoundToInt(rect.anchoredPosition.y));
            }

            Assert.That(authoredRows.Count, Is.GreaterThanOrEqualTo(3),
                "Avatar cells should be authored in front, middle and back rows.");
            GalaxyChallengeAvatarCellView playerAvatar = avatars.GetArrayElementAtIndex(0).objectReferenceValue
                as GalaxyChallengeAvatarCellView;
            Assert.That(playerAvatar.RectTransform.anchoredPosition, Is.EqualTo(new Vector2(0f, -20f)));
            Assert.That(playerAvatar.transform.GetSiblingIndex(), Is.GreaterThan(0),
                "The player avatar is authored above the other crowd layers.");

            SerializedProperty winnerAvatars = serialized.FindProperty("m_VictoryWinnerAvatars");
            Assert.That(winnerAvatars.arraySize, Is.EqualTo(6));
            for (int index = 0; index < winnerAvatars.arraySize; index++)
                Assert.That(winnerAvatars.GetArrayElementAtIndex(index).objectReferenceValue, Is.Not.Null);

            RectTransform avatarLayer = serialized.FindProperty("m_AvatarLayer").objectReferenceValue
                as RectTransform;
            GameObject readyOverlay = serialized.FindProperty("m_ReadyOverlay").objectReferenceValue
                as GameObject;
            Assert.That(readyOverlay.transform.GetSiblingIndex(), Is.GreaterThan(avatarLayer.GetSiblingIndex()),
                "The authored ready overlay must render above the board and avatar layer.");
        }

        private static void AssertStepContainer(GalaxyChallengeStepContainerView container, int expectedCount)
        {
            Assert.That(container, Is.Not.Null);
            var serialized = new SerializedObject(container);
            SerializedProperty steps = serialized.FindProperty("m_Steps");
            Assert.That(steps.arraySize, Is.EqualTo(expectedCount));
            for (int index = 0; index < steps.arraySize; index++)
            {
                GalaxyChallengeStepView step = steps.GetArrayElementAtIndex(index).objectReferenceValue
                    as GalaxyChallengeStepView;
                Assert.That(step, Is.Not.Null, $"Step slot {index}");
                Assert.That(new SerializedObject(step).FindProperty("m_PeopleRoot").objectReferenceValue,
                    Is.Not.Null, $"Step slot {index}: m_PeopleRoot");
            }
        }

        private static void AssertCrowdSequence(GalaxyChallengeEventDefinition definition, int[] counts,
            int minFinal, int maxFinal)
        {
            Assert.That(counts.Length, Is.EqualTo(definition.StepCount + 1));
            Assert.That(counts[0], Is.EqualTo(definition.JoinedUserCount));
            Assert.That(counts[counts.Length - 1], Is.InRange(minFinal, maxFinal));
            for (int index = 1; index < counts.Length; index++)
                Assert.That(counts[index], Is.LessThan(counts[index - 1]), $"Step {index}");
            Assert.That(definition.IsValidUserCounts(counts), Is.True);
        }

        private static void AssertReferences<T>(string prefabName, params string[] properties)
            where T : Component
        {
            GameObject prefab = LoadPrefab(prefabName);
            T component = prefab.GetComponent<T>();
            Assert.That(component, Is.Not.Null, prefabName);
            var serialized = new SerializedObject(component);
            foreach (string propertyName in properties)
                Assert.That(serialized.FindProperty(propertyName).objectReferenceValue, Is.Not.Null,
                    $"{prefabName}: {propertyName}");
        }

        private static GameObject LoadPrefab(string prefabName)
        {
            string path = Root + "UI/" + prefabName + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(prefab.GetComponentsInChildren<Component>(true), Has.None.Null, path);
            return prefab;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(target, arguments);
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
            public int SchemaVersion;

            public UniTask<ActivityStorageReadResult> ReadAsync(string key,
                CancellationToken cancellationToken = default) =>
                UniTask.FromResult(string.IsNullOrEmpty(Payload)
                    ? new ActivityStorageReadResult(ActivityStorageReadStatus.Missing)
                    : new ActivityStorageReadResult(ActivityStorageReadStatus.Found, Payload, SchemaVersion));

            public UniTask WriteAsync(string key, string payload, int schemaVersion,
                CancellationToken cancellationToken = default)
            {
                Payload = payload;
                SchemaVersion = schemaVersion;
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
            public bool CanGrant(string resourceKey, string target) => false;
            public UniTask<ActivityRewardReceipt> GrantAsync(ActivityRewardRequest request,
                CancellationToken cancellationToken = default) =>
                UniTask.FromResult(new ActivityRewardReceipt(request.GrantId, ActivityRewardStatus.Unsupported));
            public UniTask<ActivityRewardReceipt> GetReceiptAsync(string grantId,
                CancellationToken cancellationToken = default) =>
                UniTask.FromResult(new ActivityRewardReceipt(grantId, ActivityRewardStatus.NotFound));
        }

        private sealed class UI : IActivityUI
        {
            public UniTask<ActivityUiOpenResult> OpenAsync(ActivityPageRequest request,
                CancellationToken cancellationToken = default) =>
                UniTask.FromResult(new ActivityUiOpenResult(ActivityUiOpenStatus.Opened,
                    new ActivityPageHandle(GalaxyChallengeActivityModule.Id, 1, 1)));
            public UniTask CloseAsync(ActivityPageHandle handle) => UniTask.CompletedTask;
            public UniTask CloseAllAsync() => UniTask.CompletedTask;
        }

        private sealed class Context : IActivityContext
        {
            public string ModuleId => GalaxyChallengeActivityModule.Id;
            public string ProfileId => "galaxy-challenge-test";
            public long Generation => 1;
            public CancellationToken LifetimeToken => default;
            public IActivityClock Clock { get; } = new Clock();
            public IActivityStorage Storage { get; }
            public IActivityGameFacts GameFacts { get; }
            public IActivityRewardGateway Rewards { get; } = new Rewards();
            public IActivityUI UI { get; } = new UI();

            public Context(Storage storage, Facts facts)
            {
                Storage = storage;
                GameFacts = facts;
            }
        }
    }
}
