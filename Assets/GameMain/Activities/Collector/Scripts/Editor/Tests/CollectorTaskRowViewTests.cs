using Lokas;
using Lokas.Activities.Collector;
using Lokas.Activities.Collector.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lokas.Activities.Collector.Editor.Tests
{
    public sealed class CollectorTaskRowViewTests
    {
        private const string RowPrefabPath = "Assets/GameMain/Activities/Collector/UI/CollectorTaskRowView.prefab";
        private const string MainPanelPrefabPath = "Assets/GameMain/Activities/Collector/UI/CollectorMainPanel.prefab";
        private const string HomeWidgetPrefabPath = "Assets/GameMain/Activities/Collector/UI/CollectorHomeWidget.prefab";
        private const string MainUiPrefabPath = "Assets/GameMain/UI/UIPanel/MainUIPanel.prefab";
        private const string ConfigPath = "Assets/GameMain/Activities/Collector/ScriptableObjects/Config/HexaCollector202609.asset";
        private const string DefinitionPath = "Assets/GameMain/Activities/Collector/ScriptableObjects/Registry/CollectorActivityDefinition.asset";

        [Test]
        public void HomeWidgetPrefab_BindsProgressTimerAndDoubleRewardSlots()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HomeWidgetPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Collector home widget Prefab is missing: {HomeWidgetPrefabPath}");

            CollectorHomeWidget widget = prefab.GetComponent<CollectorHomeWidget>();
            Assert.That(widget, Is.Not.Null, "CollectorHomeWidget must be mounted on the Prefab root.");
            SerializedObject serialized = new SerializedObject(widget);

            AssertBound(serialized, "m_Background");
            AssertBound(serialized, "m_OpenButton");
            AssertBound(serialized, "m_CollectButton");
            AssertBound(serialized, "m_CollectibleIcon");
            AssertBound(serialized, "m_Progress");
            AssertBound(serialized, "m_ProgressFill");
            AssertBound(serialized, "m_RewardSlot1");
            AssertBound(serialized, "m_RewardSlot1Root");
            AssertBound(serialized, "m_RewardSlot1CanvasGroup");
            AssertBound(serialized, "m_RewardSlot2");
            AssertBound(serialized, "m_RewardSlot2Root");
            AssertBound(serialized, "m_RewardSlot2CanvasGroup");
            AssertBound(serialized, "m_ClockIcon");
            AssertBound(serialized, "m_Countdown");

            Transform collectBox = prefab.transform.Find("Root/CollectBox");
            Assert.That(collectBox, Is.Not.Null, "CollectorHomeWidget must contain Root/CollectBox.");
            Assert.That(collectBox.Find("RewardSlotView_1")?.GetComponent<RewardSlotView>(), Is.Not.Null,
                "RewardSlotView_1 must contain a RewardSlotView component.");
            Assert.That(collectBox.Find("RewardSlotView_1")?.GetComponent<CanvasGroup>(), Is.Not.Null,
                "RewardSlotView_1 must contain a CanvasGroup for the exit fade.");
            Assert.That(collectBox.Find("RewardSlotView_2")?.GetComponent<RewardSlotView>(), Is.Not.Null,
                "RewardSlotView_2 must contain a RewardSlotView component.");
            Assert.That(collectBox.Find("RewardSlotView_2")?.GetComponent<CanvasGroup>(), Is.Not.Null,
                "RewardSlotView_2 must contain a CanvasGroup for the exit fade.");
        }

        [Test]
        public void MainPanelPrefab_BindsAllRequiredUiReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPanelPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Collector main panel Prefab is missing: {MainPanelPrefabPath}");

            CollectorMainPanel panel = prefab.GetComponent<CollectorMainPanel>();
            Assert.That(panel, Is.Not.Null, "CollectorMainPanel must be mounted on the Prefab root.");
            SerializedObject serialized = new SerializedObject(panel);

            AssertBound(serialized, "m_Title");
            AssertBound(serialized, "m_RulesButton");
            AssertBound(serialized, "m_CloseButton");
            AssertBound(serialized, "m_Progress");
            AssertBound(serialized, "m_ProgressFill");
            AssertBound(serialized, "m_CollectButton");
            AssertBound(serialized, "m_CollectIcon");
            AssertBound(serialized, "m_CurrentReward");
            AssertBound(serialized, "m_NextReward");
            AssertBound(serialized, "m_TaskScroll");
            AssertBound(serialized, "m_TaskContent");
            AssertBound(serialized, "m_TaskRowTemplate");
        }

        [Test]
        public void MainUiPrefab_BindsCollectorHomeWidget()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainUiPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Main UI Prefab is missing: {MainUiPrefabPath}");

            MainUIPanel panel = prefab.GetComponent<MainUIPanel>();
            Assert.That(panel, Is.Not.Null, "MainUIPanel must be mounted on the Main UI Prefab root.");
            AssertBound(new SerializedObject(panel), "m_CollectorHomeWidget");
        }

        [Test]
        public void CollectorConfig_ContainsDefaultMilestoneRewards()
        {
            CollectorActivityConfig config = AssetDatabase.LoadAssetAtPath<CollectorActivityConfig>(ConfigPath);
            Assert.That(config, Is.Not.Null, $"Collector config is missing: {ConfigPath}");

            Assert.DoesNotThrow(config.ValidateConfiguration);
            Assert.That(config.TargetCount, Is.EqualTo(50));
            Assert.That(config.CollectionPerWin, Is.EqualTo(1));
            Assert.That(config.Tasks.Count, Is.EqualTo(19));

            AssertReward(config.GetTask(1), 50, ("currency.money", RewardGrantMode.AddQuantity, 50));
            AssertReward(config.GetTask(2), 50, ("game.extra_move", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(3), 50, ("game.drill", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(4), 50, ("currency.money", RewardGrantMode.AddQuantity, 70));
            AssertReward(config.GetTask(5), 50, ("effect.unlimited_life", RewardGrantMode.UnlimitedUse, 900));
            AssertReward(config.GetTask(6), 100, ("game.extra_move", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(7), 100, ("game.drill", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(8), 150, ("currency.money", RewardGrantMode.AddQuantity, 100));
            AssertReward(config.GetTask(9), 150, ("game.extra_move", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(10), 150, ("effect.unlimited_life", RewardGrantMode.UnlimitedUse, 900));
            AssertReward(config.GetTask(11), 200,
                ("currency.money", RewardGrantMode.AddQuantity, 100),
                ("game.hammer", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(12), 200, ("game.bomb", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(13), 200, ("currency.money", RewardGrantMode.AddQuantity, 200));
            AssertReward(config.GetTask(14), 400, ("game.drill", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(15), 400,
                ("game.extra_move", RewardGrantMode.AddQuantity, 1),
                ("effect.unlimited_life", RewardGrantMode.UnlimitedUse, 900));
            AssertReward(config.GetTask(16), 400, ("currency.money", RewardGrantMode.AddQuantity, 300));
            AssertReward(config.GetTask(17), 800, ("game.drill", RewardGrantMode.AddQuantity, 1));
            AssertReward(config.GetTask(18), 800, ("effect.unlimited_life", RewardGrantMode.UnlimitedUse, 1800));
            AssertReward(config.GetTask(19), 800,
                ("currency.money", RewardGrantMode.AddQuantity, 300),
                ("game.extra_move", RewardGrantMode.AddQuantity, 1),
                ("game.hammer", RewardGrantMode.AddQuantity, 1));
        }

        [Test]
        public void CollectorDefinition_CreatesModuleWithConfiguredRewards()
        {
            CollectorActivityDefinition definition = AssetDatabase.LoadAssetAtPath<CollectorActivityDefinition>(DefinitionPath);
            Assert.That(definition, Is.Not.Null, $"Collector definition is missing: {DefinitionPath}");
            Assert.That(definition.Config, Is.Not.Null);

            CollectorActivityModule module = definition.CreateModule() as CollectorActivityModule;
            Assert.That(module, Is.Not.Null);
            Assert.That(module.Config, Is.SameAs(definition.Config));
            Assert.DoesNotThrow(module.Config.ValidateConfiguration);
        }

        [Test]
        public void SetListPosition_HidesOuterConnectorHalves()
        {
            CollectorTaskRowView row = CreateRow();
            try
            {
                AssertLineVisibility(row, 0, 3, expectedLine1: false, expectedLine2: true);
                AssertLineVisibility(row, 1, 3, expectedLine1: true, expectedLine2: true);
                AssertLineVisibility(row, 2, 3, expectedLine1: true, expectedLine2: false);
                AssertLineVisibility(row, 0, 1, expectedLine1: false, expectedLine2: false);
            }
            finally
            {
                Object.DestroyImmediate(row.gameObject);
            }
        }

        private static CollectorTaskRowView CreateRow()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Collector row Prefab is missing: {RowPrefabPath}");

            CollectorTaskRowView row = Object.Instantiate(prefab).GetComponent<CollectorTaskRowView>();
            Assert.That(row, Is.Not.Null, "CollectorTaskRowView must be mounted on the Prefab root.");
            return row;
        }

        private static void AssertLineVisibility(CollectorTaskRowView row, int rowIndex, int rowCount,
            bool expectedLine1, bool expectedLine2)
        {
            row.SetListPosition(rowIndex, rowCount);

            SerializedObject serialized = new SerializedObject(row);
            GameObject line1 = serialized.FindProperty("m_Line1").objectReferenceValue as GameObject;
            GameObject line2 = serialized.FindProperty("m_Line2").objectReferenceValue as GameObject;
            Assert.That(line1, Is.Not.Null, "Line_1 must be bound in the row Prefab Inspector.");
            Assert.That(line2, Is.Not.Null, "Line_2 must be bound in the row Prefab Inspector.");
            Assert.That(line1.activeSelf, Is.EqualTo(expectedLine1));
            Assert.That(line2.activeSelf, Is.EqualTo(expectedLine2));
        }

        private static void AssertBound(SerializedObject serialized, string propertyPath)
        {
            SerializedProperty property = serialized.FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, $"Missing serialized field: {propertyPath}");
            Assert.That(property.objectReferenceValue, Is.Not.Null,
                $"Bind '{propertyPath}' on CollectorMainPanel.prefab.");
        }

        private static void AssertReward(CollectorTaskDefinition task, int requiredCount,
            params (string resourceKey, RewardGrantMode grantMode, long amount)[] expected)
        {
            Assert.That(task, Is.Not.Null);
            Assert.That(task.RequiredCount, Is.EqualTo(requiredCount));
            Assert.That(task.Rewards.Count, Is.EqualTo(expected.Length));
            for (int index = 0; index < expected.Length; index++)
            {
                RewardEntry reward = task.Rewards[index];
                Assert.That(reward.ResourceKey, Is.EqualTo(expected[index].resourceKey));
                Assert.That(reward.GrantMode, Is.EqualTo(expected[index].grantMode));
                Assert.That(reward.Amount, Is.EqualTo(expected[index].amount));
            }
        }
    }
}
