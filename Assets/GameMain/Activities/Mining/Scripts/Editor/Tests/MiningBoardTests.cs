using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Lokas.Activities.Mining;
using Lokas.Activities.Mining.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Editor.Tests.Activity
{
    public sealed class MiningBoardTests
    {
        private const string ConfigPath =
            "Assets/GameMain/Activities/Mining/ScriptableObjects/MiningActivityConfig.asset";
        private const string GemVisualConfigPath =
            "Assets/GameMain/Activities/Mining/ScriptableObjects/MiningGemVisualConfig.asset";
        private const string ScheduleConfigPath =
            "Assets/GameMain/Activities/Mining/ScriptableObjects/MiningScheduleConfig.asset";
        private const string GateThemeConfigPath =
            "Assets/GameMain/Activities/Mining/Textures/related/MiningGateThemeConfig.asset";
        private const string Event01ConfigPath =
            "Assets/GameMain/Activities/Mining/ScriptableObjects/MiningEvent01Config.asset";
        private const string Event02ConfigPath =
            "Assets/GameMain/Activities/Mining/ScriptableObjects/MiningEvent02Config.asset";
        private const string RewardConfigPath =
            "Assets/GameMain/Activities/Mining/ScriptableObjects/Rewards/MiningRewardConfig.asset";
        private const string DefinitionPath =
            "Assets/GameMain/Activities/Mining/ScriptableObjects/Registry/MiningActivityDefinition.asset";
        private const string CatalogPath =
            "Assets/GameMain/ScriptableObjects/ActivitySystem/ActivityModuleCatalog.asset";
        private const string UiFormBytesPath = "Assets/GameMain/DataTables/UIForm.bytes";
        private const string MainPrefabPath =
            "Assets/GameMain/Activities/Mining/UI/MiningMainUIPanel.prefab";
        private const string StartPrefabPath =
            "Assets/GameMain/Activities/Mining/UI/MiningStartUIPanel.prefab";
        private const string DetailPrefabPath =
            "Assets/GameMain/Activities/Mining/UI/MiningDetailUIPanel.prefab";
        private const string EndPrefabPath =
            "Assets/GameMain/Activities/Mining/UI/MiningEndUIPanel.prefab";
        private const string GridAreaPrefabPath =
            "Assets/GameMain/Activities/Mining/UI/GridArea.prefab";
        private const string GatePrefabPath =
            "Assets/GameMain/Activities/Mining/UI/Gate.prefab";

        [Test]
        public void SplitConfigAssets_ContainTenStagesSixtyThreeGemsAndFiveGateThemes()
        {
            MiningActivityConfig config = LoadConfig();
            Assert.DoesNotThrow(() => config.ValidateConfiguration(false));
            Assert.That(config.ScheduleConfig, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(config.ScheduleConfig), Is.EqualTo(ScheduleConfigPath));
            Assert.That(config.ScheduleConfig.EventId, Is.EqualTo(1));
            Assert.That(config.ScheduleConfig.EndUtc, Is.GreaterThan(config.ScheduleConfig.StartUtc));
            Assert.That(config.GetEventConfig(config.ScheduleConfig.EventId), Is.Not.Null);
            Assert.That(config.GemVisualConfig, Is.Not.Null);
            Assert.That(config.GemVisualConfig.GemVisuals.Count, Is.EqualTo(14));
            Assert.That(AssetDatabase.GetAssetPath(config.GemVisualConfig), Is.EqualTo(GemVisualConfigPath));
            Assert.That(config.GateThemeConfig, Is.Not.Null);
            Assert.That(config.GateThemeConfig.Themes.Count, Is.EqualTo(5));
            Assert.That(AssetDatabase.GetAssetPath(config.GateThemeConfig), Is.EqualTo(GateThemeConfigPath));
            for (int stepId = 1; stepId <= 5; stepId++)
            {
                MiningGateThemeDefinition theme = config.GetGateTheme(stepId);
                Assert.That(theme, Is.Not.Null);
                Assert.That(theme.BackSprite, Is.Not.Null);
                Assert.That(theme.SceneSprite, Is.Not.Null);
            }
            Assert.That(config.EventConfigs.Count, Is.EqualTo(2));
            Assert.That(AssetDatabase.GetAssetPath(config.GetEventConfig(1)), Is.EqualTo(Event01ConfigPath));
            Assert.That(AssetDatabase.GetAssetPath(config.GetEventConfig(2)), Is.EqualTo(Event02ConfigPath));

            int stageCount = 0;
            int gemCount = 0;
            for (int eventIndex = 0; eventIndex < config.EventConfigs.Count; eventIndex++)
            {
                MiningEventConfig eventConfig = config.EventConfigs[eventIndex];
                Assert.That(eventConfig.Stages.Count, Is.EqualTo(5));
                stageCount += eventConfig.Stages.Count;
                for (int stageIndex = 0; stageIndex < eventConfig.Stages.Count; stageIndex++)
                    gemCount += eventConfig.Stages[stageIndex].Gems.Count;
            }

            Assert.That(stageCount, Is.EqualTo(10));
            Assert.That(gemCount, Is.EqualTo(63));
        }

        [Test]
        public void RewardConfig_ContainsRuntimeExportBundlesAndBothEventsResolveThem()
        {
            MiningActivityConfig config = LoadConfig();
            Assert.That(config.RewardConfig, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(config.RewardConfig), Is.EqualTo(RewardConfigPath));
            Assert.That(config.RewardConfig.Rewards.Count, Is.EqualTo(5));

            AssertReward(config.GetReward(9001),
                new[] { "currency.money", "game.extra_move", "mining.pickaxe", "effect.league_boost" },
                new[] { RewardGrantMode.AddQuantity, RewardGrantMode.AddQuantity,
                    RewardGrantMode.AddQuantity, RewardGrantMode.UnlimitedUse },
                new long[] { 100, 1, 3, 600 });
            AssertReward(config.GetReward(9002),
                new[] { "currency.money", "game.hammer", "mining.pickaxe", "effect.league_boost" },
                new[] { RewardGrantMode.AddQuantity, RewardGrantMode.AddQuantity,
                    RewardGrantMode.AddQuantity, RewardGrantMode.UnlimitedUse },
                new long[] { 300, 1, 3, 600 });
            AssertReward(config.GetReward(9003),
                new[] { "currency.money", "game.drill", "game.hammer", "mining.pickaxe" },
                new[] { RewardGrantMode.AddQuantity, RewardGrantMode.AddQuantity,
                    RewardGrantMode.AddQuantity, RewardGrantMode.AddQuantity },
                new long[] { 300, 2, 1, 5 });
            AssertReward(config.GetReward(9004),
                new[] { "currency.money", "game.hammer", "game.extra_move", "effect.unlimited_life",
                    "mining.pickaxe" },
                new[] { RewardGrantMode.AddQuantity, RewardGrantMode.AddQuantity,
                    RewardGrantMode.AddQuantity, RewardGrantMode.UnlimitedUse, RewardGrantMode.AddQuantity },
                new long[] { 500, 1, 2, 1800, 5 });
            AssertReward(config.GetReward(9005),
                new[] { "currency.money", "game.bomb", "game.hammer", "game.drill", "game.extra_move" },
                new[] { RewardGrantMode.AddQuantity, RewardGrantMode.AddQuantity,
                    RewardGrantMode.AddQuantity, RewardGrantMode.AddQuantity, RewardGrantMode.AddQuantity },
                new long[] { 1000, 1, 1, 2, 2 });

            for (int eventId = 1; eventId <= 2; eventId++)
            {
                for (int stepId = 1; stepId <= 5; stepId++)
                {
                    MiningStageDefinition stage = config.GetStage(eventId, stepId);
                    Assert.That(config.GetStageReward(eventId, stepId).id, Is.EqualTo(stage.RewardId));
                }
            }
        }

        [Test]
        public void DiggingAllGemCells_CollectsGemOnlyOnLastCell()
        {
            MiningActivityConfig config = LoadConfig();
            MiningStageDefinition stage = config.GetStage(1, 1);
            var model = new MiningBoardModel(config, stage, 99);
            MiningGemPlacement gem = stage.Gems[0];
            IReadOnlyList<int> cells = model.GetCellsForGem(gem.GemId);
            Assert.That(cells.Count, Is.GreaterThan(1));

            for (int index = 0; index < cells.Count - 1; index++)
                Assert.That(model.Dig(cells[index]).CollectedGemIds, Is.Empty);
            MiningDigResult finalDig = model.Dig(cells[cells.Count - 1]);
            Assert.That(finalDig.CollectedGemIds, Is.EquivalentTo(new[] { gem.GemId }));
            Assert.That(model.IsGemCollected(gem.GemId), Is.True);
        }

        [Test]
        public void ActivityRoute_IsRegisteredForHomepageTestEntry()
        {
            MiningActivityConfig config = LoadConfig();
            MiningActivityDefinition definition =
                AssetDatabase.LoadAssetAtPath<MiningActivityDefinition>(DefinitionPath);
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Config, Is.SameAs(config));
            Assert.That(definition.ModuleId, Is.EqualTo(MiningActivityModule.Id));
            Assert.That(definition.GameIds, Is.EquivalentTo(new[] { "Game" }));
            Assert.That(definition.Pages.Count, Is.EqualTo(4));

            AssertPage(definition, MiningPageKeys.Start, UIFormIdRanges.MiningStartPage, "MiningStartUIPanel", StartPrefabPath);
            AssertPage(definition, MiningPageKeys.Main, UIFormIdRanges.MiningMain, "MiningMainUIPanel", MainPrefabPath);
            AssertPage(definition, MiningPageKeys.Details, UIFormIdRanges.MiningDetails, "MiningDetailUIPanel", DetailPrefabPath);
            AssertPage(definition, MiningPageKeys.End, UIFormIdRanges.MiningEndPage, "MiningEndUIPanel", EndPrefabPath);
            Assert.That((int)UIFormId.MiningMainUIPanel, Is.EqualTo(UIFormIdRanges.MiningMain));
            Assert.That((int)UIFormId.MiningStartUIPanel, Is.EqualTo(UIFormIdRanges.MiningStartPage));
            Assert.That((int)UIFormId.MiningDetailUIPanel, Is.EqualTo(UIFormIdRanges.MiningDetails));
            Assert.That((int)UIFormId.MiningEndUIPanel, Is.EqualTo(UIFormIdRanges.MiningEndPage));

            ActivityModuleCatalogConfig catalog =
                AssetDatabase.LoadAssetAtPath<ActivityModuleCatalogConfig>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Modules, Does.Contain(definition));
            Assert.That(definition.CreateModule(), Is.TypeOf<MiningActivityModule>());

            TextAsset uiFormBytes = AssetDatabase.LoadAssetAtPath<TextAsset>(UiFormBytesPath);
            Assert.That(uiFormBytes, Is.Not.Null);
            Assert.That(uiFormBytes.bytes.Length, Is.GreaterThan(0));
        }

        [Test]
        public void DuplicateCell_DoesNotConsumeSecondPickaxe()
        {
            MiningActivityConfig config = LoadConfig();
            var model = new MiningBoardModel(config, config.GetStage(1, 1), 2);
            MiningDigResult first = model.Dig(0);
            MiningDigResult duplicate = model.Dig(0);
            Assert.That(first.Status, Is.EqualTo(MiningDigStatus.Opened));
            Assert.That(duplicate.Status, Is.EqualTo(MiningDigStatus.AlreadyOpened));
            Assert.That(model.PickaxeCount, Is.EqualTo(1));
        }

        [Test]
        public void RestoredCollectedGem_RepairsItsOpenedCells()
        {
            MiningActivityConfig config = LoadConfig();
            MiningStageDefinition stage = config.GetStage(2, 1);
            MiningGemPlacement gem = stage.Gems[0];
            var model = new MiningBoardModel(config, stage, 5, null, new[] { gem.GemId });
            foreach (int cellId in model.GetCellsForGem(gem.GemId))
                Assert.That(model.IsCellOpen(cellId), Is.True);
        }

        [Test]
        public void ConfigAsset_AndPrefabs_AreFullyBound()
        {
            MiningActivityConfig config = AssetDatabase.LoadAssetAtPath<MiningActivityConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null);
            Assert.DoesNotThrow(() => config.ValidateConfiguration());

            GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            Assert.That(mainPrefab, Is.Not.Null);
            MiningMainUIPanel panel = mainPrefab.GetComponent<MiningMainUIPanel>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.Config, Is.SameAs(config));
            Object gridAreaPrefab = GetObjectReference(panel, "m_GridAreaPrefab");
            Assert.That(gridAreaPrefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(gridAreaPrefab), Is.EqualTo(GridAreaPrefabPath));
            Assert.That(GetObjectReference(panel, "m_GridPanel"), Is.Not.Null);
            Assert.That(GetObjectReference(panel, "m_GridLayout"), Is.Not.Null);
            Assert.That(GetObjectReference(panel, "m_PickaxeCountText"), Is.Not.Null);
            Assert.That(GetObjectReference(panel, "m_GateView"), Is.Not.Null);
            Assert.That(GetObjectReference(panel, "m_NextGateView"), Is.Not.Null);
            Assert.That(GetObjectReference(panel, "m_FlightLayer"), Is.Not.Null);
            Assert.That(GetObjectReference(panel, "m_CloseButton"), Is.Not.Null);
            Assert.That(mainPrefab.transform.Find("SafeArea/Top/DetailBtn")?.GetComponent<Button>(), Is.Not.Null);
            SerializedProperty gateEntryDelay = new SerializedObject(panel).FindProperty("m_GateEntryDelay");
            Assert.That(gateEntryDelay, Is.Not.Null);
            Assert.That(gateEntryDelay.floatValue, Is.GreaterThan(0f));
            GameObject completionPage = GetObjectReference(panel, "m_CompletionPage") as GameObject;
            Assert.That(completionPage, Is.Not.Null);
            Assert.That(mainPrefab.transform.Find("SafeArea/GameArea/GridArea/GridPanel"), Is.Not.Null);
            Transform gameArea = mainPrefab.transform.Find("SafeArea/GameArea");
            Assert.That(completionPage.transform.parent, Is.SameAs(gameArea));
            Assert.That(completionPage.transform.GetSiblingIndex(), Is.EqualTo(gameArea.childCount - 1));
            Assert.That(completionPage.activeSelf, Is.False);
            RectTransform completionRect = completionPage.GetComponent<RectTransform>();
            Assert.That(completionRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(completionRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(completionPage.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.True);
            Assert.That(mainPrefab.transform.Find("SafeArea/GateArea/Gate/Back/GemSlots"), Is.Not.Null);
            Transform currentGateTransform = mainPrefab.transform.Find("SafeArea/GateArea/Gate");
            Transform nextGateTransform = mainPrefab.transform.Find("SafeArea/GateArea/Gate_Next");
            Assert.That(nextGateTransform, Is.Not.Null);
            Assert.That(nextGateTransform.GetSiblingIndex(), Is.LessThan(currentGateTransform.GetSiblingIndex()));
            MiningGateView currentGate = currentGateTransform.GetComponent<MiningGateView>();
            MiningGateView nextGate = nextGateTransform.GetComponent<MiningGateView>();
            Assert.That(currentGate, Is.Not.Null);
            Assert.That(nextGate, Is.Not.Null);
            Assert.That(currentGate.GemSlots, Is.Not.Null);
            Assert.That(nextGate.GemSlots, Is.Not.Null);

            GameObject gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GatePrefabPath);
            Assert.That(gatePrefab, Is.Not.Null);
            MiningGateView gate = gatePrefab.GetComponent<MiningGateView>();
            Assert.That(gate, Is.Not.Null);
            Assert.That(gate.GemSlots, Is.Not.Null);
            Assert.That(gatePrefab.transform.Find("Back/GemSlots"), Is.Not.Null);
            Assert.That(gatePrefab.transform.Find("Back")?.GetComponent<UnityEngine.UI.Image>(), Is.Not.Null);
            Assert.That(gatePrefab.transform.Find("scene_BG")?.GetComponent<UnityEngine.UI.Image>(), Is.Not.Null);
            Assert.That(gatePrefab.GetComponent<Animator>(), Is.Not.Null);
        }

        [Test]
        public void PopupPrefabs_HaveTheirPageComponentsAndButtonBindings()
        {
            AssertPopupPrefab<MiningStartUIPanel>(StartPrefabPath, "m_StartButton", "m_CloseButton");
            AssertPopupPrefab<MiningDetailUIPanel>(DetailPrefabPath, "m_ContinueButton");
            AssertPopupPrefab<MiningEndUIPanel>(EndPrefabPath, "m_NextButton");
        }

        [Test]
        public void RewardProgress_BindsStageChestStylesAndScheduleCountdown()
        {
            MiningActivityConfig config = LoadConfig();
            GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            var canvasObject = new GameObject("MiningRewardProgressCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject panelObject = null;
            System.DateTimeOffset now = config.ScheduleConfig.EndUtc - System.TimeSpan.FromDays(9) -
                System.TimeSpan.FromHours(15);
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelObject = Object.Instantiate(mainPrefab, canvasObject.transform);
                MiningMainUIPanel panel = panelObject.GetComponent<MiningMainUIPanel>();
                int claimedStepId = 0;
                panel.InitializeBoard(new MiningBoardOpenArgs(1, 3, 99,
                    countdownEndUtc: config.ScheduleConfig.EndUtc, utcNow: () => now,
                    claimedRewardStepIds: new[] { 1 }, claimRewardAsync: stepId =>
                    {
                        claimedStepId = stepId;
                        return UniTask.FromResult(new ActivityRewardReceipt($"test/{stepId}",
                            ActivityRewardStatus.Granted));
                    }));

                Slider slider = panelObject.transform.Find("SafeArea/RewardArea/RewardSlider")
                    ?.GetComponent<Slider>();
                Assert.That(slider, Is.Not.Null);
                Assert.That(slider.interactable, Is.False);
                Assert.That(slider.normalizedValue, Is.EqualTo(0.5f).Within(0.001f));

                slider.SetValueWithoutNotify(slider.minValue);
                MethodInfo refreshProgress = typeof(MiningMainUIPanel).GetMethod("RefreshRewardProgress",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(refreshProgress, Is.Not.Null);
                refreshProgress.Invoke(panel, new object[] { config.GetStage(1, 3), true });
                Tween progressTween = (Tween)typeof(MiningMainUIPanel)
                    .GetField("m_RewardProgressTween", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(panel);
                Assert.That(progressTween, Is.Not.Null);
                Assert.That(progressTween.IsActive(), Is.True);
                Assert.That(slider.normalizedValue, Is.Zero.Within(0.001f));
                progressTween.Complete(true);
                Assert.That(slider.normalizedValue, Is.EqualTo(0.5f).Within(0.001f));

                for (int stepId = 1; stepId <= 5; stepId++)
                {
                    Transform chest = panelObject.transform.Find(
                        $"SafeArea/RewardArea/ChestArea/Chest_{stepId}");
                    Assert.That(chest, Is.Not.Null);
                    Image closedImage = chest.Find("Chest_Close")?.GetComponent<Image>();
                    Assert.That(closedImage, Is.Not.Null);
                    Assert.That(closedImage.sprite,
                        Is.SameAs(config.GetStageReward(1, stepId).ChestStyle.sprite));
                    Assert.That(chest.Find("Label_Step/Text (TMP)")?.GetComponent<TMPro.TMP_Text>()?.text,
                        Is.EqualTo($"Step{stepId}"));
                    Assert.That(chest.GetComponent<Button>(), Is.Not.Null);
                    Assert.That(chest.GetComponent<RewardChestView>(), Is.Not.Null);
                }

                Transform firstChest = panelObject.transform.Find(
                    "SafeArea/RewardArea/ChestArea/Chest_1");
                Assert.That(firstChest.Find("Chest_Close").gameObject.activeSelf, Is.False);
                Assert.That(firstChest.Find("Chest_Open").gameObject.activeSelf, Is.True);
                Assert.That(firstChest.Find("Label_Step").gameObject.activeSelf, Is.False);
                Assert.That(firstChest.Find("Btn_Claim").gameObject.activeSelf, Is.False);

                Transform secondChest = panelObject.transform.Find(
                    "SafeArea/RewardArea/ChestArea/Chest_2");
                Assert.That(secondChest.Find("Chest_Close").gameObject.activeSelf, Is.True);
                Assert.That(secondChest.Find("Chest_Open").gameObject.activeSelf, Is.False);
                Assert.That(secondChest.Find("Label_Step").gameObject.activeSelf, Is.False);
                Button secondClaimButton = secondChest.Find("Btn_Claim").GetComponent<Button>();
                Assert.That(secondClaimButton.gameObject.activeSelf, Is.True);
                Assert.That(secondClaimButton.interactable, Is.True);

                Transform thirdChest = panelObject.transform.Find(
                    "SafeArea/RewardArea/ChestArea/Chest_3");
                Assert.That(thirdChest.Find("Label_Step").gameObject.activeSelf, Is.True);
                Assert.That(thirdChest.Find("Btn_Claim").gameObject.activeSelf, Is.False);

                RewardTooltipView tooltip = firstChest.GetComponentInChildren<RewardTooltipView>(true);
                Assert.That(tooltip, Is.Not.Null);
                firstChest.GetComponent<Button>().onClick.Invoke();
                Assert.That(tooltip.IsVisible, Is.True);
                tooltip.HideImmediate();

                secondClaimButton.onClick.Invoke();
                Assert.That(claimedStepId, Is.EqualTo(2));
                Assert.That(secondChest.Find("Btn_Claim").gameObject.activeSelf, Is.False);
                Assert.That(secondChest.Find("Chest_Close").gameObject.activeSelf, Is.False);
                Assert.That(secondChest.Find("Chest_Open").gameObject.activeSelf, Is.True);

                Assert.That(panel.CountdownText, Is.Not.Null);
                Assert.That(panel.CountdownText.text, Is.EqualTo("9d 15h"));

                now = config.ScheduleConfig.EndUtc - new System.TimeSpan(0, 1, 2, 3);
                MethodInfo refreshCountdown = typeof(MiningMainUIPanel).GetMethod("RefreshCountdown",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(refreshCountdown, Is.Not.Null);
                refreshCountdown.Invoke(panel, null);
                Assert.That(panel.CountdownText.text, Is.EqualTo("01:02:03"));
            }
            finally
            {
                DOTween.KillAll();
                if (panelObject != null) Object.DestroyImmediate(panelObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void TestPickaxeControl_UpdatesOpenBoardImmediately()
        {
            GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            var canvasObject = new GameObject("MiningPickaxeControlCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject panelObject = null;
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelObject = Object.Instantiate(mainPrefab, canvasObject.transform);
                MiningMainUIPanel panel = panelObject.GetComponent<MiningMainUIPanel>();
                var args = new MiningBoardOpenArgs(1, 1, 3);

                System.Type controlsType = typeof(MiningBoardOpenArgs).Assembly.GetType(
                    "Lokas.Activities.Mining.UI.MiningBoardTestControls");
                Assert.That(controlsType, Is.Not.Null);
                object controls = System.Activator.CreateInstance(controlsType, true);
                MethodInfo synchronize = controlsType.GetMethod("SynchronizePickaxeCount",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo setPickaxes = controlsType.GetMethod("SetPickaxeCount",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo attach = typeof(MiningBoardOpenArgs).GetMethod("AttachTestControls",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(synchronize, Is.Not.Null);
                Assert.That(setPickaxes, Is.Not.Null);
                Assert.That(attach, Is.Not.Null);
                synchronize.Invoke(controls, new object[] { 3 });
                attach.Invoke(args, new[] { controls });

                panel.InitializeBoard(args);
                TMPro.TMP_Text pickaxeText = panelObject.transform.Find(
                    "SafeArea/GameArea/GridArea/Count_base_frame/frame/CountText")?.GetComponent<TMPro.TMP_Text>();
                Assert.That(pickaxeText, Is.Not.Null);
                Assert.That(panel.Model.PickaxeCount, Is.EqualTo(3));
                Assert.That(pickaxeText.text, Is.EqualTo("3"));

                setPickaxes.Invoke(controls, new object[] { 777 });
                Assert.That(panel.Model.PickaxeCount, Is.EqualTo(777));
                Assert.That(pickaxeText.text, Is.EqualTo("777"));

                setPickaxes.Invoke(controls, new object[] { 0 });
                Assert.That(panel.Model.PickaxeCount, Is.Zero);
                Assert.That(pickaxeText.text, Is.EqualTo("0"));
            }
            finally
            {
                DOTween.KillAll();
                if (panelObject != null) Object.DestroyImmediate(panelObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void InitializingBoard_ShowsAllFrames_AndCollectingFillsItsConfiguredSlot()
        {
            GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            var canvasObject = new GameObject("MiningTestCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject panelObject = null;
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelObject = Object.Instantiate(mainPrefab, canvasObject.transform);
                MiningMainUIPanel panel = panelObject.GetComponent<MiningMainUIPanel>();
                panel.InitializeBoard(new MiningBoardOpenArgs(1, 1, 99));

                MiningGemPlacement gem = panel.Model.Stage.Gems[0];
                Assert.That(panel.CurrentGateView.GemSlots.childCount,
                    Is.EqualTo(panel.Model.Stage.Gems.Count));
                for (int index = 0; index < panel.Model.Stage.Gems.Count; index++)
                {
                    MiningGemPlacement placement = panel.Model.Stage.Gems[index];
                    Transform emptySlot = panel.CurrentGateView.GemSlots.Find($"GemSlot_{placement.GemId}");
                    Assert.That(emptySlot, Is.Not.Null);
                    Assert.That(emptySlot.gameObject.activeSelf, Is.True);
                    Assert.That(emptySlot.Find("gem_frame")?.GetComponent<UnityEngine.UI.Image>()?.enabled,
                        Is.True);
                    Assert.That(emptySlot.Find("Gem")?.GetComponent<UnityEngine.UI.Image>()?.enabled,
                        Is.False);
                }

                MiningStageDefinition nextStage = panel.Config.GetStage(1, 2);
                Assert.That(panel.NextGateView.GemSlots.childCount, Is.EqualTo(nextStage.Gems.Count));
                for (int index = 0; index < nextStage.Gems.Count; index++)
                {
                    Transform nextSlot = panel.NextGateView.GemSlots.Find(
                        $"GemSlot_{nextStage.Gems[index].GemId}");
                    Assert.That(nextSlot?.Find("gem_frame")?.GetComponent<UnityEngine.UI.Image>()?.enabled,
                        Is.True);
                    Assert.That(nextSlot?.Find("Gem")?.GetComponent<UnityEngine.UI.Image>()?.enabled,
                        Is.False);
                }

                IReadOnlyList<int> cellIds = panel.Model.GetCellsForGem(gem.GemId);
                Transform gridPanel = panelObject.transform.Find("SafeArea/GameArea/GridArea/GridPanel");
                int collectedGemId = 0;
                panel.GemCollected += gemId => collectedGemId = gemId;

                for (int index = 0; index < cellIds.Count; index++)
                {
                    MiningCellView cell = gridPanel.GetChild(cellIds[index]).GetComponent<MiningCellView>();
                    Assert.That(cell, Is.Not.Null);
                    cell.OnPointerClick(null);
                    DOTween.CompleteAll(true);
                }
                DOTween.CompleteAll(true);

                Assert.That(panel.Model.IsGemCollected(gem.GemId), Is.True);
                Assert.That(collectedGemId, Is.EqualTo(gem.GemId));
                Transform slot = panelObject.transform.Find(
                    $"SafeArea/GateArea/Gate/Back/GemSlots/GemSlot_{gem.GemId}");
                Assert.That(slot, Is.Not.Null);
                Assert.That(slot.gameObject.activeSelf, Is.True);
                Assert.That(((RectTransform)slot).anchoredPosition, Is.EqualTo(gem.GatePosition));
                Assert.That(slot.Find("gem_frame")?.GetComponent<UnityEngine.UI.Image>()?.enabled, Is.True);
                Assert.That(slot.Find("Gem")?.GetComponent<UnityEngine.UI.Image>()?.enabled, Is.True);
            }
            finally
            {
                DOTween.KillAll();
                if (panelObject != null) Object.DestroyImmediate(panelObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void CompletingStage_PlaysCurrentExitAndNextEntry()
        {
            MiningActivityConfig config = LoadConfig();
            GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            var canvasObject = new GameObject("MiningGateTransitionCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject panelObject = null;
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelObject = Object.Instantiate(mainPrefab, canvasObject.transform);
                MiningMainUIPanel panel = panelObject.GetComponent<MiningMainUIPanel>();
                int completedCount = 0;
                panel.StageCompleted += () => completedCount++;
                panel.InitializeBoard(new MiningBoardOpenArgs(1, 1, 99));

                Assert.That(panel.CurrentGateView.ThemeStepId, Is.EqualTo(1));
                Assert.That(panel.NextGateView.ThemeStepId, Is.EqualTo(2));
                Assert.That(panel.CurrentGateView.BackImage.sprite,
                    Is.SameAs(config.GetGateTheme(1).BackSprite));
                Assert.That(panel.NextGateView.SceneImage.sprite,
                    Is.SameAs(config.GetGateTheme(2).SceneSprite));
                Assert.That(panel.NextGateView.transform.GetSiblingIndex(),
                    Is.LessThan(panel.CurrentGateView.transform.GetSiblingIndex()));

                RectTransform firstGrid = panel.CurrentGridArea;
                MiningGateView enteringGate = panel.NextGateView;
                CompleteCurrentStage(panel);

                Assert.That(completedCount, Is.EqualTo(1));
                Assert.That(panel.Model.Stage.StepId, Is.EqualTo(2));
                Assert.That(panel.CurrentGridArea, Is.Not.SameAs(firstGrid));
                Assert.That(firstGrid == null, Is.True);
                Assert.That(panel.CurrentGridArea.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(panel.CurrentGateView, Is.SameAs(enteringGate));
                Assert.That(panel.CurrentGateView.ThemeStepId, Is.EqualTo(2));
                Assert.That(panel.NextGateView.ThemeStepId, Is.EqualTo(3));
                Assert.That(panel.CurrentGateView.gameObject.activeSelf, Is.True);
                Assert.That(panel.NextGateView.gameObject.activeSelf, Is.True);
                Assert.That(panel.NextGateView.transform.GetSiblingIndex(),
                    Is.LessThan(panel.CurrentGateView.transform.GetSiblingIndex()));
                Animator enteringAnimator = panel.CurrentGateView.GetComponent<Animator>();
                Assert.That(enteringAnimator.GetCurrentAnimatorStateInfo(0)
                    .IsName("Base Layer.Gate_Enter"), Is.True);
                Assert.That(panel.CompletionPage.activeSelf, Is.False);
            }
            finally
            {
                if (panelObject != null) Object.DestroyImmediate(panelObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void GateSequence_OverlapsDelayedEntry_AndRecyclesHiddenGate()
        {
            MiningActivityConfig config = LoadConfig();
            GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            var canvasObject = new GameObject("MiningGateRecycleCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject panelObject = null;
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelObject = Object.Instantiate(mainPrefab, canvasObject.transform);
                MiningMainUIPanel panel = panelObject.GetComponent<MiningMainUIPanel>();
                panel.InitializeBoard(new MiningBoardOpenArgs(1, 1, 99));

                MiningGateView firstGate = panel.CurrentGateView;
                MiningGateView secondGate = panel.NextGateView;

                InvokeGateSequence(panel, config.GetStage(1, 2));
                Assert.That(firstGate.IsTransitioning, Is.True);
                Assert.That(secondGate.IsTransitioning, Is.False);
                CompleteGateEntryDelay(panel);
                Assert.That(firstGate.IsTransitioning, Is.True,
                    "Incoming Gate must start before the outgoing Gate finishes.");
                Assert.That(secondGate.IsTransitioning, Is.True);
                AdvanceEntryAnimation(secondGate);
                AdvanceExitAnimation(firstGate);
                CompleteTransition(firstGate);

                Assert.That(panel.CurrentGateView, Is.SameAs(secondGate));
                Assert.That(panel.CurrentGateView.ThemeStepId, Is.EqualTo(2));
                Assert.That(panel.NextGateView, Is.SameAs(firstGate));
                Assert.That(panel.NextGateView.gameObject.activeSelf, Is.True);
                Assert.That(panel.NextGateView.ThemeStepId, Is.EqualTo(3));
                Assert.That(panel.NextGateView.transform.GetSiblingIndex(),
                    Is.LessThan(panel.CurrentGateView.transform.GetSiblingIndex()));
                AssertGateVisible(panel.CurrentGateView);
                AssertGateVisible(panel.NextGateView);
                CompleteTransition(secondGate);
                AssertGateVisible(secondGate);

                InvokeGateSequence(panel, config.GetStage(1, 3));
                CompleteGateEntryDelay(panel);
                Assert.That(secondGate.IsTransitioning, Is.True);
                Assert.That(firstGate.IsTransitioning, Is.True);
                AdvanceEntryAnimation(firstGate);
                AdvanceExitAnimation(secondGate);
                CompleteTransition(secondGate);

                Assert.That(panel.CurrentGateView, Is.SameAs(firstGate));
                Assert.That(panel.CurrentGateView.gameObject.activeSelf, Is.True);
                Assert.That(panel.CurrentGateView.ThemeStepId, Is.EqualTo(3));
                Assert.That(panel.NextGateView, Is.SameAs(secondGate));
                Assert.That(panel.NextGateView.gameObject.activeSelf, Is.True);
                Assert.That(panel.NextGateView.ThemeStepId, Is.EqualTo(4));
                Assert.That(panel.NextGateView.transform.GetSiblingIndex(),
                    Is.LessThan(panel.CurrentGateView.transform.GetSiblingIndex()));
                AssertGateVisible(panel.CurrentGateView);
                AssertGateVisible(panel.NextGateView);
                CompleteTransition(firstGate);
                AssertGateVisible(firstGate);

                InvokeGateSequence(panel, config.GetStage(1, 4));
                CompleteGateEntryDelay(panel);
                Assert.That(firstGate.IsTransitioning, Is.True);
                Assert.That(secondGate.IsTransitioning, Is.True);
                AdvanceEntryAnimation(secondGate);
                AdvanceExitAnimation(firstGate);
                CompleteTransition(firstGate);

                Assert.That(panel.CurrentGateView, Is.SameAs(secondGate));
                Assert.That(panel.CurrentGateView.ThemeStepId, Is.EqualTo(4));
                Assert.That(panel.NextGateView, Is.SameAs(firstGate));
                Assert.That(panel.NextGateView.ThemeStepId, Is.EqualTo(5));
                AssertGateVisible(panel.CurrentGateView);
                AssertGateVisible(panel.NextGateView);
                CompleteTransition(secondGate);
                AssertGateVisible(secondGate);
            }
            finally
            {
                DOTween.KillAll();
                if (panelObject != null) Object.DestroyImmediate(panelObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void RestoringCompletedFinalStage_ShowsCompletionPage()
        {
            MiningActivityConfig config = LoadConfig();
            MiningStageDefinition stage = config.GetStage(1, 5);
            var restoredModel = new MiningBoardModel(config, stage, 999);
            int cellCount = stage.CellCount * stage.CellCount;
            for (int cellId = 0; cellId < cellCount && !restoredModel.IsStageComplete; cellId++)
                restoredModel.Dig(cellId);
            MiningBoardSnapshot snapshot = restoredModel.CreateSnapshot();
            Assert.That(snapshot.IsStageComplete, Is.True);

            GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            var canvasObject = new GameObject("MiningCompletedRestoreCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject panelObject = null;
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelObject = Object.Instantiate(mainPrefab, canvasObject.transform);
                MiningMainUIPanel panel = panelObject.GetComponent<MiningMainUIPanel>();
                panel.InitializeBoard(new MiningBoardOpenArgs(snapshot.EventId, snapshot.StepId,
                    snapshot.PickaxeCount, snapshot.OpenCellIds, snapshot.CollectedGemIds));

                Assert.That(panel.Model.Stage.StepId, Is.EqualTo(5));
                Assert.That(panel.Model.IsStageComplete, Is.True);
                Assert.That(panel.CurrentGateView.gameObject.activeSelf, Is.False);
                Assert.That(panel.NextGateView.gameObject.activeSelf, Is.False);
                Assert.That(panel.CompletionPage.activeSelf, Is.True);
            }
            finally
            {
                DOTween.KillAll();
                if (panelObject != null) Object.DestroyImmediate(panelObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void CompletingFiveStages_LoopsGridAreas_ThenShowsCompletionPage()
        {
            GameObject mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            var canvasObject = new GameObject("MiningFiveStageCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject panelObject = null;
            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelObject = Object.Instantiate(mainPrefab, canvasObject.transform);
                MiningMainUIPanel panel = panelObject.GetComponent<MiningMainUIPanel>();
                int completedCount = 0;
                panel.StageCompleted += () => completedCount++;
                panel.InitializeBoard(new MiningBoardOpenArgs(1, 1, 999));
                MiningGateView firstGate = panel.CurrentGateView;
                MiningGateView secondGate = panel.NextGateView;
                Slider rewardSlider = panelObject.transform.Find("SafeArea/RewardArea/RewardSlider")
                    ?.GetComponent<Slider>();
                Assert.That(rewardSlider, Is.Not.Null);
                Assert.That(rewardSlider.normalizedValue, Is.Zero.Within(0.001f));
                Assert.That(panelObject.transform.Find("SafeArea/RewardArea/ChestArea/Chest_1/Label_Step")
                    .gameObject.activeSelf, Is.True);

                for (int stepId = 1; stepId <= 5; stepId++)
                {
                    Assert.That(panel.Model.Stage.StepId, Is.EqualTo(stepId));
                    CompleteCurrentStage(panel);
                    Assert.That(completedCount, Is.EqualTo(stepId));
                    Assert.That(rewardSlider.normalizedValue,
                        Is.EqualTo(Mathf.Min(stepId, 4) / 4f).Within(0.001f));
                    Transform reachedChest = panelObject.transform.Find(
                        $"SafeArea/RewardArea/ChestArea/Chest_{stepId}");
                    Assert.That(reachedChest.Find("Label_Step").gameObject.activeSelf, Is.False);
                    Assert.That(reachedChest.Find("Btn_Claim").gameObject.activeSelf, Is.True);
                    Assert.That(reachedChest.Find("Chest_Open").gameObject.activeSelf, Is.False);

                    if (stepId < 5)
                    {
                        Assert.That(panel.Model.Stage.StepId, Is.EqualTo(stepId + 1));
                        Assert.That(panel.CurrentGridArea.anchoredPosition, Is.EqualTo(Vector2.zero));
                        Assert.That(panel.CompletionPage.activeSelf, Is.False);

                        MiningGateView expectedCurrentGate = stepId % 2 == 1 ? secondGate : firstGate;
                        MiningGateView expectedFollowingGate = stepId % 2 == 1 ? firstGate : secondGate;
                        Assert.That(panel.CurrentGateView, Is.SameAs(expectedCurrentGate));
                        Assert.That(panel.NextGateView, Is.SameAs(expectedFollowingGate));
                        Assert.That(panel.CurrentGateView.gameObject.activeSelf, Is.True);
                        Assert.That(panel.CurrentGateView.ThemeStepId, Is.EqualTo(stepId + 1));
                        AssertGateVisible(panel.CurrentGateView);

                        bool hasFollowingGate = stepId + 1 < 5;
                        Assert.That(panel.NextGateView.gameObject.activeSelf, Is.EqualTo(hasFollowingGate));
                        Assert.That(panel.NextGateView.ThemeStepId,
                            Is.EqualTo(hasFollowingGate ? stepId + 2 : 0));
                        if (hasFollowingGate) AssertGateVisible(panel.NextGateView);
                    }
                    else
                    {
                        Assert.That(panel.Model.IsStageComplete, Is.True);
                        Assert.That(panel.CurrentGateView.gameObject.activeSelf, Is.False);
                        Assert.That(panel.NextGateView.gameObject.activeSelf, Is.False);
                        Assert.That(panel.CompletionPage.activeSelf, Is.True);
                        Assert.That(panel.CompletionPage.transform.GetSiblingIndex(),
                            Is.EqualTo(panel.CompletionPage.transform.parent.childCount - 1));
                    }
                }
            }
            finally
            {
                if (panelObject != null) Object.DestroyImmediate(panelObject);
                Object.DestroyImmediate(canvasObject);
            }
        }

        private static void CompleteCurrentStage(MiningMainUIPanel panel)
        {
            MiningBoardModel model = panel.Model;
            Transform gridPanel = panel.CurrentGridArea.Find("GridPanel");
            var requiredCells = new SortedSet<int>();
            for (int gemIndex = 0; gemIndex < model.Stage.Gems.Count; gemIndex++)
            {
                IReadOnlyList<int> cells = model.GetCellsForGem(model.Stage.Gems[gemIndex].GemId);
                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    requiredCells.Add(cells[cellIndex]);
            }

            foreach (int cellId in requiredCells)
            {
                MiningCellView cell = gridPanel.GetChild(cellId).GetComponent<MiningCellView>();
                Assert.That(cell, Is.Not.Null);
                cell.OnPointerClick(null);
                for (int tweenPass = 0; tweenPass < 8; tweenPass++) DOTween.CompleteAll(true);
                if (model.IsStageComplete) break;
            }

            for (int tweenPass = 0; tweenPass < 8; tweenPass++) DOTween.CompleteAll(true);
            Assert.That(model.IsStageComplete, Is.True);
        }

        private static void AssertPage(MiningActivityDefinition definition, string pageKey, int uiFormId,
            string assetName, string prefabPath)
        {
            ActivityPageDefinition match = default;
            bool found = false;
            foreach (ActivityPageDefinition page in definition.Pages)
            {
                if (page.PageKey != pageKey) continue;
                match = page;
                found = true;
                break;
            }
            Assert.That(found, Is.True, $"Missing Mining page '{pageKey}'.");
            Assert.That(match.UIFormId, Is.EqualTo(uiFormId));
            Assert.That(match.AssetName, Is.EqualTo(assetName));
            Assert.That(match.PrefabAssetPath, Is.EqualTo(prefabPath));
        }

        private static void AssertPopupPrefab<T>(string prefabPath, params string[] buttonProperties)
            where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            T panel = prefab.GetComponent<T>();
            Assert.That(panel, Is.Not.Null, prefabPath);
            Assert.That(prefab.GetComponent<CanvasGroup>(), Is.Not.Null, prefabPath);
            Transform referenceImage = prefab.transform.Find("ReferenceImage");
            Assert.That(referenceImage, Is.Not.Null, prefabPath);
            Assert.That(referenceImage.GetComponent<UnityEngine.UI.Image>()?.sprite, Is.Not.Null, prefabPath);
            Assert.That(referenceImage.GetComponent<AspectRatioFitter>(), Is.Not.Null, prefabPath);
            foreach (string propertyName in buttonProperties)
                Assert.That(GetObjectReference(panel, propertyName), Is.Not.Null,
                    $"{prefabPath} has no binding for {propertyName}.");
        }

        private static Object GetObjectReference(Object target, string propertyPath)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, $"Missing serialized property {propertyPath}.");
            return property.objectReferenceValue;
        }

        private static void InvokeGateSequence(MiningMainUIPanel panel, MiningStageDefinition nextStage)
        {
            MethodInfo method = typeof(MiningMainUIPanel).GetMethod("PlayGateSequence",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(panel, new object[] { nextStage, (System.Action)(() => { }) });
        }

        private static void CompleteTransition(MiningGateView gate)
        {
            FieldInfo field = typeof(MiningGateView).GetField("m_TransitionTween",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Tween transition = field.GetValue(gate) as Tween;
            Assert.That(transition, Is.Not.Null);
            transition.Complete(true);
        }

        private static void CompleteGateEntryDelay(MiningMainUIPanel panel)
        {
            FieldInfo field = typeof(MiningMainUIPanel).GetField("m_GateEntryDelayTween",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Tween delay = field.GetValue(panel) as Tween;
            Assert.That(delay, Is.Not.Null);
            delay.Complete(true);
        }

        private static void AdvanceExitAnimation(MiningGateView gate)
        {
            Animator animator = gate.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            animator.Update(1f);
            Assert.That(gate.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f).Within(0.001f));
        }

        private static void AdvanceEntryAnimation(MiningGateView gate)
        {
            Animator animator = gate.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            animator.Update(0.2f);
            AssertGateVisible(gate);
        }

        private static void AssertGateVisible(MiningGateView gate)
        {
            CanvasGroup canvasGroup = gate.GetComponent<CanvasGroup>();
            Assert.That(gate.gameObject.activeSelf, Is.True);
            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(canvasGroup.alpha, Is.EqualTo(1f).Within(0.001f));
        }

        private static void AssertReward(MiningRewardDefinition reward, string[] resourceKeys,
            RewardGrantMode[] grantModes, long[] amounts)
        {
            Assert.That(reward, Is.Not.Null);
            Assert.That(reward.Entries.Count, Is.EqualTo(resourceKeys.Length));
            Assert.That(grantModes.Length, Is.EqualTo(resourceKeys.Length));
            Assert.That(amounts.Length, Is.EqualTo(resourceKeys.Length));
            for (int index = 0; index < resourceKeys.Length; index++)
            {
                RewardEntry entry = reward.Entries[index];
                Assert.That(entry.ResourceKey, Is.EqualTo(resourceKeys[index]), $"RewardId={reward.id}, entry={index}");
                Assert.That(entry.GrantMode, Is.EqualTo(grantModes[index]), $"RewardId={reward.id}, entry={index}");
                Assert.That(entry.RequestAmount, Is.EqualTo(amounts[index]), $"RewardId={reward.id}, entry={index}");
            }
        }

        private static MiningActivityConfig LoadConfig()
        {
            MiningActivityConfig config = AssetDatabase.LoadAssetAtPath<MiningActivityConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null);
            return config;
        }

    }
}
