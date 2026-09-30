using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Lokas.Activities.WinStreak.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.WinStreak.Editor.Tests
{
    public sealed class WinStreakActivityTests
    {
        private const string RootPath = "Assets/GameMain/Activities/WinStreak";
        private const string ConfigPath = RootPath + "/ScriptableObjects/Config/HexaWinStreak202609.asset";
        private const string DefinitionPath = RootPath + "/ScriptableObjects/Registry/WinStreakActivityDefinition.asset";
        private const string MainPrefabPath = RootPath + "/UI/WinStreakMainUIPanel.prefab";
        private const string TowerPrefabPath = RootPath + "/UI/TowerView.prefab";
        private readonly List<UnityEngine.Object> m_TestAssets = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object asset in m_TestAssets)
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            m_TestAssets.Clear();
        }

        [Test]
        public void WinsAccumulateWithoutCheckpointResetAndLossKeepsReachedRewards()
        {
            Clock clock = new Clock();
            Storage storage = new Storage();
            Facts facts = new Facts();
            Rewards rewards = new Rewards();
            WinStreakActivityModule module = new WinStreakActivityModule(CreateConfig(resetOnLoss: true),
                new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(clock, storage, facts, rewards), default).GetAwaiter().GetResult();
                Assert.That(module.GetSnapshot().State, Is.EqualTo(WinStreakActivityState.ReadyToJoin));
                Assert.That(module.JoinAsync().GetAwaiter().GetResult(), Is.True);

                facts.Publish(Fact("other", "Other", 1, clock.UtcNow, won: true));
                facts.Publish(Fact("win-1", "Game", 2, clock.UtcNow, won: true));
                facts.Publish(Fact("win-1", "Game", 2, clock.UtcNow, won: true));
                Assert.That(module.GetSnapshot().CurrentProgress, Is.EqualTo(1));

                facts.Publish(Fact("win-2", "Game", 3, clock.UtcNow, won: true));
                facts.Publish(Fact("win-3", "Game", 4, clock.UtcNow, won: true));
                WinStreakSnapshot accumulated = module.GetSnapshot();
                Assert.That(accumulated.CurrentProgress, Is.EqualTo(3),
                    "Crossing checkpoint 2 must not reset the current cumulative streak.");
                Assert.That(accumulated.BestProgress, Is.EqualTo(3));
                Assert.That(accumulated.MaxCheckpointIndex, Is.EqualTo(0));
                Assert.That(accumulated.Checkpoints[0].IsReached, Is.True);

                facts.Publish(Fact("loss", "Game", 5, clock.UtcNow, won: false));
                WinStreakSnapshot afterLoss = module.GetSnapshot();
                Assert.That(afterLoss.CurrentProgress, Is.Zero);
                Assert.That(afterLoss.BestProgress, Is.EqualTo(3));
                Assert.That(afterLoss.Checkpoints[0].IsReached, Is.True,
                    "A loss resets the current streak but must not revoke an earned checkpoint.");

                facts.Publish(Fact("win-4", "Game", 6, clock.UtcNow, won: true));
                facts.Publish(Fact("win-5", "Game", 7, clock.UtcNow, won: true));
                Assert.That(module.GetSnapshot().CurrentProgress, Is.EqualTo(2));
                Assert.That(module.ClaimAsync(2).GetAwaiter().GetResult().Status,
                    Is.EqualTo(ActivityRewardStatus.Granted));
                Assert.That(module.ClaimAsync(2).GetAwaiter().GetResult().Status,
                    Is.EqualTo(ActivityRewardStatus.AlreadyGranted));
                Assert.That(module.GetSnapshot().RewardReceiveProgress, Is.EqualTo(2));
                Assert.That(rewards.Grants, Is.EqualTo(1));
                Assert.That(storage.Payload, Does.Contain("win-5"));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void StateRoundTripPreservesProgressBestReachedAndClaimedCheckpoint()
        {
            WinStreakActivityConfig config = CreateConfig(resetOnLoss: true);
            Clock clock = new Clock();
            Storage storage = new Storage();
            Rewards rewards = new Rewards();
            Facts firstFacts = new Facts();
            WinStreakActivityModule first = new WinStreakActivityModule(config, new[] { "Game" });
            first.InitializeAsync(new Context(clock, storage, firstFacts, rewards), default).GetAwaiter().GetResult();
            first.JoinAsync().GetAwaiter().GetResult();
            firstFacts.Publish(Fact("persist-1", "Game", 1, clock.UtcNow, true));
            firstFacts.Publish(Fact("persist-2", "Game", 2, clock.UtcNow, true));
            first.ClaimAsync(2).GetAwaiter().GetResult();
            Assert.That(first.GetSnapshot().IsTutorialComplete, Is.False);
            first.MarkTutorialComplete();
            Assert.That(first.GetSnapshot().IsTutorialComplete, Is.True);
            first.ShutdownAsync().GetAwaiter().GetResult();

            WinStreakActivityModule restored = new WinStreakActivityModule(config, new[] { "Game" });
            try
            {
                restored.InitializeAsync(new Context(clock, storage, new Facts(), rewards), default)
                    .GetAwaiter().GetResult();
                WinStreakSnapshot snapshot = restored.GetSnapshot();
                Assert.That(snapshot.IsJoined, Is.True);
                Assert.That(snapshot.CurrentProgress, Is.EqualTo(2));
                Assert.That(snapshot.BestProgress, Is.EqualTo(2));
                Assert.That(snapshot.Checkpoints[0].IsReached, Is.True);
                Assert.That(snapshot.Checkpoints[0].IsClaimed, Is.True);
                Assert.That(snapshot.RewardReceiveProgress, Is.EqualTo(2));
                Assert.That(snapshot.IsTutorialComplete, Is.True,
                    "The first-entry rules and reward tour must only run once per activity state.");
            }
            finally
            {
                restored.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void ProgressRemainsUncheckedUntilTheUiAcknowledgesItsAnimation()
        {
            Clock clock = new Clock();
            Storage storage = new Storage();
            Facts facts = new Facts();
            WinStreakActivityConfig config = CreateConfig();
            WinStreakActivityModule module = new WinStreakActivityModule(config, new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(clock, storage, facts, new Rewards()), default)
                    .GetAwaiter().GetResult();
                module.JoinAsync().GetAwaiter().GetResult();
                facts.Publish(Fact("unchecked-1", "Game", 1, clock.UtcNow, true));

                Assert.That(module.GetSnapshot().CurrentProgress, Is.EqualTo(1));
                Assert.That(module.GetSnapshot().LastCheckedProgress, Is.Zero,
                    "Winning records the new progress but must leave it pending for the next UI animation.");

                module.MarkProgressChecked(1);
                Assert.That(module.GetSnapshot().LastCheckedProgress, Is.EqualTo(1));

                facts.Publish(Fact("unchecked-2", "Game", 2, clock.UtcNow, true));
                Assert.That(module.GetSnapshot().CurrentProgress, Is.EqualTo(2));
                Assert.That(module.GetSnapshot().LastCheckedProgress, Is.EqualTo(1));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void EntryRoutesToStartMainAndEndPages()
        {
            Clock clock = new Clock();
            UI ui = new UI();
            WinStreakActivityConfig config = CreateConfig();
            WinStreakActivityModule module = new WinStreakActivityModule(config, new[] { "Game" });
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), new Facts(), new Rewards(), ui), default)
                    .GetAwaiter().GetResult();
                module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
                Assert.That(ui.LastPage, Is.EqualTo(WinStreakPageKeys.Start));
                module.JoinAsync().GetAwaiter().GetResult();
                module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
                Assert.That(ui.LastPage, Is.EqualTo(WinStreakPageKeys.Main));
                clock.UtcNow = config.EndUtc.AddMinutes(1);
                module.OpenEntryAsync("main", default).GetAwaiter().GetResult();
                Assert.That(ui.LastPage, Is.EqualTo(WinStreakPageKeys.End));
            }
            finally
            {
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void InstalledEvent2MatchesAnalyzedCheckpointsAndRewards()
        {
            WinStreakActivityConfig config = AssetDatabase.LoadAssetAtPath<WinStreakActivityConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null);
            Assert.DoesNotThrow(config.ValidateConfiguration);
            Assert.That(config.EventId, Is.EqualTo(2));
            Assert.That(config.UnlockLevel, Is.EqualTo(40));
            Assert.That(config.ProgressPerWin, Is.EqualTo(1));
            Assert.That(config.ResetOnLoss, Is.True);
            Assert.That(config.Checkpoints.Select(item => item.Checkpoint),
                Is.EqualTo(new[] { 2, 5, 8, 16, 24, 36, 48, 70 }));
            Assert.That(config.Checkpoints.Select(item => item.RewardId),
                Is.EqualTo(new[] { 5001, 5002, 5003, 5005, 5006, 5007, 5010, 5012 }));
            Assert.That(config.Checkpoints[0].Rewards[0].RequestAmount, Is.EqualTo(300));
            Assert.That(config.Checkpoints[1].Rewards[0].GrantMode, Is.EqualTo(RewardGrantMode.UnlimitedUse));
            Assert.That(config.Checkpoints[1].Rewards[0].RequestAmount, Is.EqualTo(900));
            Assert.That(config.Checkpoints[7].Rewards.Count, Is.EqualTo(5));
            Assert.That(config.Checkpoints[7].Rewards.Last().RequestAmount, Is.EqualTo(3600));
        }

        [Test]
        public void RegistrationAndPrefabUseReusableTowerRowsAndBackgroundLayer()
        {
            WinStreakActivityDefinition definition =
                AssetDatabase.LoadAssetAtPath<WinStreakActivityDefinition>(DefinitionPath);
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.ModuleId, Is.EqualTo(WinStreakActivityModule.Id));
            Assert.That(definition.Pages.Select(page => page.UIFormId), Is.EqualTo(new[]
            {
                UIFormIdRanges.WinStreakStartPage,
                UIFormIdRanges.WinStreakMain,
                UIFormIdRanges.WinStreakDetails,
                UIFormIdRanges.WinStreakEndPage
            }));
            Assert.That(definition.Pages.All(page => page.AssetName.EndsWith("UIPanel", StringComparison.Ordinal)),
                Is.True);

            AssertPanel<WinStreakStartUIPanel>(RootPath + "/UI/WinStreakStartUIPanel.prefab");
            AssertPanel<WinStreakMainUIPanel>(MainPrefabPath);
            AssertPanel<WinStreakDetailUIPanel>(RootPath + "/UI/WinStreakDetailUIPanel.prefab");
            AssertPanel<WinStreakEndUIPanel>(RootPath + "/UI/WinStreakEndUIPanel.prefab");

            GameObject main = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            WinStreakMainUIPanel panel = main.GetComponent<WinStreakMainUIPanel>();
            SerializedObject serialized = new SerializedObject(panel);
            Transform content = serialized.FindProperty("m_CheckpointContent").objectReferenceValue as Transform;
            Assert.That(content, Is.Not.Null);
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(content.GetComponent<ContentSizeFitter>(), Is.Not.Null);
            Assert.That(layout.spacing, Is.Zero,
                "Tower segments meet at their edges; content-wide spacing caused cross-row overlap.");
            WinStreakCheckpointRowView rowTemplate = serialized.FindProperty("m_CheckpointRowTemplate")
                .objectReferenceValue as WinStreakCheckpointRowView;
            Assert.That(rowTemplate, Is.Not.Null);
            Assert.That(rowTemplate.Type, Is.EqualTo(TowerType.Main));
            Assert.That(rowTemplate.GetComponent<LayoutElement>(), Is.Not.Null);
            SerializedObject rowData = new SerializedObject(rowTemplate);
            Assert.That(rowData.FindProperty("m_TowerTypeDatas").arraySize, Is.EqualTo(3),
                "One TowerView must be able to present Top, Main, and Bottom visual types.");
            Assert.That(rowData.FindProperty("m_StageBoxRT").objectReferenceValue, Is.Not.Null);
            Assert.That(rowData.FindProperty("m_CheckpointBadgeRT").objectReferenceValue, Is.Not.Null);
            Assert.That(rowData.FindProperty("m_ProgressTrackRT").objectReferenceValue, Is.Not.Null);
            Assert.That(rowData.FindProperty("m_ProgressFill").objectReferenceValue, Is.TypeOf<Image>());
            Assert.That(rowData.FindProperty("m_Chest").objectReferenceValue, Is.TypeOf<Image>());
            Assert.That(rowData.FindProperty("m_ClaimedState").objectReferenceValue, Is.TypeOf<GameObject>());
            Assert.That(rowData.FindProperty("m_RewardTooltip").objectReferenceValue, Is.TypeOf<RectTransform>());
            ScrollRect checkpointScroll = serialized.FindProperty("m_CheckpointScroll").objectReferenceValue as ScrollRect;
            Assert.That(checkpointScroll, Is.Not.Null);
            Assert.That(checkpointScroll.viewport.Find("BackgroundLayer"), Is.Not.Null,
                "BackgroundLayer is kept as a sibling of the scrolling content for parallax movement.");
            Image viewportGraphic = checkpointScroll.viewport.GetComponent<Image>();
            Mask viewportMask = checkpointScroll.viewport.GetComponent<Mask>();
            Assert.That(viewportGraphic, Is.Not.Null);
            Assert.That(viewportMask, Is.Not.Null);
            Assert.That(viewportMask.showMaskGraphic, Is.False);

            string uiForm = File.ReadAllText("Assets/GameMain/DataTables/UIForm.txt");
            Assert.That(uiForm, Does.Contain($"{UIFormIdRanges.WinStreakStartPage}\tWinStreak报名页面\tWinStreakStartUIPanel"));
            Assert.That(uiForm, Does.Contain($"{UIFormIdRanges.WinStreakEndPage}\tWinStreak结束页面\tWinStreakEndUIPanel"));
        }

        [Test]
        public void CheckpointRowPopulatesRewardTooltipFromConfiguredRewards()
        {
            WinStreakActivityModule module = new WinStreakActivityModule(CreateConfig(), new[] { "Game" });
            GameObject instance = null;
            try
            {
                module.InitializeAsync(new Context(new Clock(), new Storage(), new Facts(), new Rewards()), default)
                    .GetAwaiter().GetResult();
                WinStreakCheckpointSnapshot checkpoint = module.GetSnapshot().Checkpoints[0];
                instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(TowerPrefabPath));
                WinStreakCheckpointRowView row = instance.GetComponent<WinStreakCheckpointRowView>();

                row.Bind(checkpoint, currentProgress: 0, segmentStart: 1, segmentEnd: 2, claimAction: null);

                RewardTooltipView tooltip = row.GetComponentInChildren<RewardTooltipView>(true);
                Assert.That(tooltip, Is.Not.Null);
                Assert.That(tooltip.IsVisible, Is.True);
                Assert.That(tooltip.GetComponentsInChildren<RewardItemView>(true)
                        .Count(item => item.gameObject.activeSelf),
                    Is.EqualTo(checkpoint.Rewards.Count));
                Assert.That(tooltip.GetComponentsInChildren<TMP_Text>(true)
                        .Any(label => label.gameObject.activeInHierarchy && label.text.Contains("×10")),
                    Is.True, "The reward amount must be bound into the visible tooltip item.");

                tooltip.HideImmediate();
                tooltip.Show(checkpoint.Rewards);
                Assert.That(tooltip.IsVisible, Is.True,
                    "The existing transient tooltip API must remain available for reward chests.");
                tooltip.Hide();
                Assert.That(tooltip.IsVisible, Is.False);
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        [Test]
        public void MainPanelBuildsConfiguredRowsAndUpdatesEachLocalSegmentProgress()
        {
            Clock clock = new Clock();
            Facts facts = new Facts();
            UI ui = new UI();
            WinStreakActivityModule module = new WinStreakActivityModule(CreateConfig(), new[] { "Game" });
            GameObject instance = null;
            try
            {
                module.InitializeAsync(new Context(clock, new Storage(), facts, new Rewards(), ui), default)
                    .GetAwaiter().GetResult();
                module.JoinAsync().GetAwaiter().GetResult();
                facts.Publish(Fact("panel-1", "Game", 1, clock.UtcNow, true));
                facts.Publish(Fact("panel-2", "Game", 2, clock.UtcNow, true));
                facts.Publish(Fact("panel-3", "Game", 3, clock.UtcNow, true));
                facts.Publish(Fact("panel-4", "Game", 4, clock.UtcNow, true));

                instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath));
                WinStreakMainUIPanel panel = instance.GetComponent<WinStreakMainUIPanel>();
                FieldInfo moduleField = typeof(WinStreakMainUIPanel).GetField("m_Module",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo refresh = typeof(WinStreakMainUIPanel).GetMethod("Refresh",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo resolveBindings = typeof(WinStreakMainUIPanel).GetMethod("ResolveBindings",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo prepareContent = typeof(WinStreakMainUIPanel).GetMethod("PrepareContent",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo cacheBackground = typeof(WinStreakMainUIPanel).GetMethod("CacheBackgroundPosition",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applyParallax = typeof(WinStreakMainUIPanel).GetMethod("ApplyBackgroundParallax",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applyAnimatedProgress = typeof(WinStreakMainUIPanel).GetMethod("ApplyAnimatedProgress",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo setPageInteraction = typeof(WinStreakMainUIPanel).GetMethod("SetPageInteraction",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo startProgressAnimation = typeof(WinStreakMainUIPanel).GetMethod("StartProgressAnimation",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo beginFirstEntryTutorial = typeof(WinStreakMainUIPanel).GetMethod(
                    "BeginFirstEntryTutorial", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo characterField = typeof(WinStreakMainUIPanel).GetField("m_Character",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo characterOffsetField = typeof(WinStreakMainUIPanel).GetField("m_CharacterPositionOffset",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo progressTweenField = typeof(WinStreakMainUIPanel).GetField("m_ProgressTween",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo tutorialTweenField = typeof(WinStreakMainUIPanel).GetField("m_TutorialTween",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo isAnimatingField = typeof(WinStreakMainUIPanel).GetField("m_IsAnimatingProgress",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(moduleField, Is.Not.Null);
                Assert.That(refresh, Is.Not.Null);
                Assert.That(resolveBindings, Is.Not.Null);
                Assert.That(prepareContent, Is.Not.Null);
                Assert.That(cacheBackground, Is.Not.Null);
                Assert.That(applyParallax, Is.Not.Null);
                Assert.That(applyAnimatedProgress, Is.Not.Null);
                Assert.That(setPageInteraction, Is.Not.Null);
                Assert.That(startProgressAnimation, Is.Not.Null);
                Assert.That(beginFirstEntryTutorial, Is.Not.Null);
                Assert.That(characterField, Is.Not.Null);
                Assert.That(characterOffsetField, Is.Not.Null);
                Assert.That(progressTweenField, Is.Not.Null);
                Assert.That(tutorialTweenField, Is.Not.Null);
                Assert.That(isAnimatingField, Is.Not.Null);
                resolveBindings.Invoke(panel, null);
                prepareContent.Invoke(panel, null);
                cacheBackground.Invoke(panel, null);
                moduleField.SetValue(panel, module);
                refresh.Invoke(panel, null);

                SerializedObject panelData = new SerializedObject(panel);
                Transform content = panelData.FindProperty("m_CheckpointContent").objectReferenceValue as Transform;
                Assert.That(content, Is.Not.Null);
                Assert.That(content.GetComponent<VerticalLayoutGroup>().childControlHeight, Is.True,
                    "Runtime setup must apply each cloned row's preferred height without editing the prefab.");
                WinStreakCheckpointRowView[] rows = content.GetComponentsInChildren<WinStreakCheckpointRowView>(false);
                Assert.That(rows.Length, Is.EqualTo(3));
                WinStreakCheckpointRowView top = content.Find("TowerTop").GetComponent<WinStreakCheckpointRowView>();
                WinStreakCheckpointRowView rowTwo = content.Find("TowerMain_2")
                    .GetComponent<WinStreakCheckpointRowView>();
                WinStreakCheckpointRowView bottom = content.Find("TowerBottom")
                    .GetComponent<WinStreakCheckpointRowView>();
                WinStreakCheckpointRowView topRow = content.Find("TowerTop")
                    .GetComponent<WinStreakCheckpointRowView>();
                Assert.That(top.Type, Is.EqualTo(TowerType.Top));
                Assert.That(rowTwo.Type, Is.EqualTo(TowerType.Main));
                Assert.That(bottom.Type, Is.EqualTo(TowerType.Bottom));

                Button topClaimButton = new SerializedObject(topRow)
                    .FindProperty("m_ClaimButton").objectReferenceValue as Button;
                Assert.That(topClaimButton, Is.Not.Null);
                Assert.That(topClaimButton.gameObject.activeSelf, Is.False,
                    "An unreached checkpoint must not show its Claim button.");

                RewardTooltipView topTooltip = top.GetComponentInChildren<RewardTooltipView>(true);
                Assert.That(topTooltip, Is.Not.Null);
                Assert.That(topTooltip.IsVisible, Is.True,
                    "RewardTooltipView must be populated and shown when a reward checkpoint is bound.");
                Assert.That(topTooltip.GetComponentsInChildren<RewardItemView>(true)
                        .Count(item => item.gameObject.activeSelf),
                    Is.EqualTo(module.GetSnapshot().Checkpoints.Last().Rewards.Count),
                    "The tooltip must create one visible reward item for every configured reward entry.");

                SerializedObject topData = new SerializedObject(top);
                RectTransform topTrack = topData.FindProperty("m_ProgressTrackRT").objectReferenceValue as RectTransform;
                Assert.That(topTrack.gameObject.activeSelf, Is.False,
                    "TowerTop is a terminal reward and must not expose an outgoing progress track.");

                SerializedObject bottomData = new SerializedObject(bottom);
                Image bottomFill = bottomData.FindProperty("m_ProgressFill").objectReferenceValue as Image;
                GameObject bottomClaimed = bottomData.FindProperty("m_ClaimedState").objectReferenceValue as GameObject;
                Assert.That(GetFillProgress(bottomFill), Is.EqualTo(1f).Within(0.001f),
                    "The base 1->2 segment is full once the cumulative count reaches 2.");
                Image rowFill = new SerializedObject(rowTwo).FindProperty("m_ProgressFill").objectReferenceValue as Image;
                Assert.That(bottomClaimed.activeSelf, Is.True,
                    "TowerBottom has no reward and its checkpoint marker is claimed by default.");
                Assert.That(GetFillProgress(rowFill), Is.EqualTo(2f / 3f).Within(0.001f),
                    "At 4 wins, the 2->5 segment is two of its three levels complete, not 4/5 global progress.");

                Button rowTwoClaimButton = new SerializedObject(rowTwo)
                    .FindProperty("m_ClaimButton").objectReferenceValue as Button;
                Assert.That(rowTwoClaimButton, Is.Not.Null);
                Assert.That(rowTwoClaimButton.gameObject.activeSelf, Is.True,
                    "A reached and claimable checkpoint must show its Claim button.");

                RectTransform character = characterField.GetValue(panel) as RectTransform;
                Assert.That(character, Is.Not.Null);
                applyAnimatedProgress.Invoke(panel, new object[] { 2f });
                float characterAtTwo = character.anchoredPosition.y;
                Assert.That(GetFillProgress(rowFill), Is.Zero.Within(0.001f));
                applyAnimatedProgress.Invoke(panel, new object[] { 3.5f });
                float characterAtThreePointFive = character.anchoredPosition.y;
                Assert.That(GetFillProgress(rowFill), Is.EqualTo(0.5f).Within(0.001f),
                    "Fill must interpolate continuously inside the current local segment.");
                applyAnimatedProgress.Invoke(panel, new object[] { 4f });
                Assert.That(characterAtThreePointFive, Is.GreaterThan(characterAtTwo));
                Assert.That(character.anchoredPosition.y, Is.GreaterThan(characterAtThreePointFive),
                    "Character must move from the same interpolated progress value as the fill.");

                characterOffsetField.SetValue(panel, Vector2.zero);
                applyAnimatedProgress.Invoke(panel, new object[] { 3f });
                Vector2 characterWithoutOffset = character.anchoredPosition;
                characterOffsetField.SetValue(panel, new Vector2(17f, 23f));
                applyAnimatedProgress.Invoke(panel, new object[] { 3f });
                Assert.That(character.anchoredPosition.x,
                    Is.EqualTo(characterWithoutOffset.x + 17f).Within(0.001f));
                Assert.That(character.anchoredPosition.y,
                    Is.EqualTo(characterWithoutOffset.y + 23f).Within(0.001f));
                characterOffsetField.SetValue(panel, Vector2.zero);

                applyAnimatedProgress.Invoke(panel, new object[] { 1.999f });
                float characterBeforeFirstCheckpoint = character.anchoredPosition.y;
                applyAnimatedProgress.Invoke(panel, new object[] { 2f });
                float characterAtFirstCheckpoint = character.anchoredPosition.y;
                applyAnimatedProgress.Invoke(panel, new object[] { 2.001f });
                float characterAfterFirstCheckpoint = character.anchoredPosition.y;
                Assert.That(Mathf.Abs(characterAtFirstCheckpoint - characterBeforeFirstCheckpoint),
                    Is.LessThan(2f), "Character must not skip the visual gap at the first checkpoint.");
                Assert.That(Mathf.Abs(characterAfterFirstCheckpoint - characterAtFirstCheckpoint),
                    Is.LessThan(2f), "Character must remain continuous after entering the next tower segment.");

                applyAnimatedProgress.Invoke(panel, new object[] { 4.999f });
                float characterBeforeTopCheckpoint = character.anchoredPosition.y;
                applyAnimatedProgress.Invoke(panel, new object[] { 5f });
                Assert.That(Mathf.Abs(character.anchoredPosition.y - characterBeforeTopCheckpoint),
                    Is.LessThan(2f), "Character must not jump when a main segment reaches TowerTop.");

                CanvasGroup pageGroup = panel.GetComponent<CanvasGroup>();
                setPageInteraction.Invoke(panel, new object[] { false });
                Assert.That(pageGroup.interactable, Is.False);
                Assert.That(pageGroup.blocksRaycasts, Is.True,
                    "The disabled page must still block input from reaching UI below it.");
                setPageInteraction.Invoke(panel, new object[] { true });
                Assert.That(pageGroup.interactable, Is.True);

                applyAnimatedProgress.Invoke(panel, new object[] { 2f });
                startProgressAnimation.Invoke(panel, new object[] { 2f, 4 });
                Assert.That(pageGroup.interactable, Is.False,
                    "Starting the Character/progress animation must lock page interaction.");
                Assert.That(isAnimatingField.GetValue(panel), Is.True);
                Tween progressTween = progressTweenField.GetValue(panel) as Tween;
                Assert.That(progressTween, Is.Not.Null);
                Assert.That(progressTween.Duration(includeLoops: false), Is.GreaterThan(0.45f),
                    "Scroll uses a longer duration than the 0.45-second Fill/Character tween.");
                progressTween.Complete(withCallbacks: true);
                Assert.That(pageGroup.interactable, Is.True);
                Assert.That(isAnimatingField.GetValue(panel), Is.False);
                Assert.That(GetFillProgress(rowFill), Is.EqualTo(2f / 3f).Within(0.001f));

                ScrollRect scroll = panelData.FindProperty("m_CheckpointScroll").objectReferenceValue as ScrollRect;
                Assert.That(scroll.verticalNormalizedPosition, Is.InRange(0f, 1f));
                Assert.That(scroll.verticalNormalizedPosition, Is.GreaterThan(0f),
                    "Opening the page must focus the current progress instead of always jumping to the bottom.");

                Assert.That(module.GetSnapshot().IsTutorialComplete, Is.False);
                beginFirstEntryTutorial.Invoke(panel, null);
                Assert.That(ui.LastPage, Is.EqualTo(WinStreakPageKeys.Details));
                Assert.That(pageGroup.interactable, Is.False,
                    "The main page stays locked while its first-entry rules are displayed.");
                module.NotifyRulesPageClosed();
                Tween tutorialTween = tutorialTweenField.GetValue(panel) as Tween;
                Assert.That(tutorialTween, Is.Not.Null);
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f),
                    "The reward tour must begin at the top of the tower.");
                tutorialTween.Goto(tutorialTween.Duration(includeLoops: false) * 0.5f, andPlay: false);
                Assert.That(scroll.verticalNormalizedPosition, Is.InRange(0.001f, 0.999f));
                tutorialTween.Complete(withCallbacks: true);
                Assert.That(module.GetSnapshot().IsTutorialComplete, Is.True);
                Assert.That(pageGroup.interactable, Is.True);

                RectTransform background = panelData.FindProperty("m_BackgroundLayer").objectReferenceValue
                    as RectTransform;
                Assert.That(background, Is.Not.Null);
                ((RectTransform)content).anchoredPosition = Vector2.zero;
                applyParallax.Invoke(panel, null);
                Vector2 basePosition = background.anchoredPosition;
                ((RectTransform)content).anchoredPosition = Vector2.up * 120f;
                applyParallax.Invoke(panel, null);
                Assert.That(background.anchoredPosition.y,
                    Is.EqualTo(basePosition.y + 42f).Within(0.001f));
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                module.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        private void AssertPanel<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(prefab.GetComponent<T>(), Is.Not.Null, path);
        }

        private static float GetFillProgress(Image fill)
        {
            Assert.That(fill, Is.Not.Null);
            return fill.type == Image.Type.Filled ? fill.fillAmount : fill.rectTransform.anchorMax.y;
        }

        private WinStreakActivityConfig CreateConfig(bool resetOnLoss = true)
        {
            RewardDefinitionSO resource = ScriptableObject.CreateInstance<RewardDefinitionSO>();
            resource.Configure("common", "currency.money", "Coins", null, RewardResourceKind.Currency);
            WinStreakRewardDefinition first = CreateReward(resource, 10);
            WinStreakRewardDefinition second = CreateReward(resource, 20);
            WinStreakActivityConfig config = ScriptableObject.CreateInstance<WinStreakActivityConfig>();
            config.SetInstallationData(2, "win-streak-test", "Win Streak Test", "STREAK",
                1789430000, 1789433600, unlockLevel: 0, progressPerWin: 1, resetOnLoss: resetOnLoss,
                editorAlwaysUnlocked: false,
                checkpoints: new[]
                {
                    new WinStreakCheckpointDefinition(2, 9001, WinStreakChestTier.Blue, first),
                    new WinStreakCheckpointDefinition(5, 9002, WinStreakChestTier.Pink, second)
                });
            m_TestAssets.Add(resource);
            m_TestAssets.Add(first);
            m_TestAssets.Add(second);
            m_TestAssets.Add(config);
            return config;
        }

        private WinStreakRewardDefinition CreateReward(RewardDefinitionSO resource, long amount)
        {
            WinStreakRewardDefinition reward = ScriptableObject.CreateInstance<WinStreakRewardDefinition>();
            reward.ConfigureEntries(new[] { new RewardEntry(resource, RewardGrantMode.AddQuantity, amount) });
            return reward;
        }

        private static ActivityLevelCompletedFact Fact(string id, string gameId, long sequence,
            DateTimeOffset occurredAt, bool won) =>
            new ActivityLevelCompletedFact(id, gameId, "win-streak-test-session", sequence, occurredAt,
                sequence.ToString(), won);

        private sealed class Clock : IActivityClock
        {
            public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1789430400);
            public double MonotonicSeconds => 1d;
            public ActivityClockSource Source => ActivityClockSource.LocalEstimated;
        }

        private sealed class Storage : IActivityStorage
        {
            public string Payload;
            public int SchemaVersion = 1;

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
            public int Grants { get; private set; }
            public bool CanGrant(string resourceKey, string target) =>
                !string.IsNullOrWhiteSpace(resourceKey) && target == "profile";

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
                    new ActivityPageHandle(WinStreakActivityModule.Id, 1, 1)));
            }

            public UniTask CloseAsync(ActivityPageHandle handle) => UniTask.CompletedTask;
            public UniTask CloseAllAsync() => UniTask.CompletedTask;
        }

        private sealed class Context : IActivityContext
        {
            public string ModuleId => WinStreakActivityModule.Id;
            public string ProfileId => "win-streak-test-profile";
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
