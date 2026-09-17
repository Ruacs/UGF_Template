using System;
using System.Collections.Generic;
using Lokas.Activities.SeasonPass;
using Lokas.Activities.SeasonPass.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor.Tests
{
    public sealed class RewardConfigurationTests
    {
        private const string ConfigPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Config/SchoolPass202609.asset";
        private readonly List<UnityEngine.Object> m_Owned = new List<UnityEngine.Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (UnityEngine.Object item in m_Owned) if (item != null) UnityEngine.Object.DestroyImmediate(item);
            m_Owned.Clear();
        }

        [Test]
        public void MigrationPreservesEveryLegacyRequestInOrderAndCreatesEditableBundles()
        {
            var config = AssetDatabase.LoadAssetAtPath<SeasonPassActivityConfig>(ConfigPath);
            config.ValidateConfiguration();
            int bundles = 0;
            foreach (SeasonPassTierDefinition tier in config.Tiers)
            {
                Assert.That(tier.NeedsRewardMigration, Is.False);
                CheckRequests(tier.LegacyFreeRewards, tier.FreeReward);
                CheckRequests(tier.LegacyPremiumRewards, tier.PremiumReward);
                if (tier.FreeReward != null) bundles++;
                if (tier.PremiumReward != null) bundles++;
            }
            Assert.That(bundles, Is.EqualTo(49));
            Assert.That(config.Tiers.Count, Is.EqualTo(25));
            Assert.That(config.GetTier(5).PremiumRewards[2].Amount, Is.EqualTo(1800));
            Assert.That(config.GetTier(5).PremiumRewards[2].Unit, Is.EqualTo("minute"));
            Assert.That(config.GetTier(5).PremiumRewards[2].RequestAmount, Is.EqualTo(30));
        }

        [Test]
        public void RepeatedMigrationKeepsBundleReferencesAndDesignerChanges()
        {
            var source = AssetDatabase.LoadAssetAtPath<SeasonPassActivityConfig>(ConfigPath);
            var clone = Own(UnityEngine.Object.Instantiate(source));
            SeasonPassRewardDefinition original = clone.GetTier(1).FreeReward;
            var edited = Own(UnityEngine.Object.Instantiate(original));
            edited.ConfigureEntries(new[] { new RewardEntry(original.Entries[0].Definition, RewardGrantMode.AddQuantity, 777) });
            clone.GetTier(1).SetRewardAssets(edited, null);
            SeasonPassRewardAssetMigration.Migrate(clone, ConfigPath);
            Assert.That(clone.GetTier(1).FreeReward, Is.SameAs(edited));
            Assert.That(edited.Entries[0].Amount, Is.EqualTo(777));
            Assert.That(source.GetTier(1).FreeReward, Is.SameAs(original));
            Assert.That(original.Entries[0].Amount, Is.EqualTo(300));
        }

        [Test]
        public void QuantityAndUnlimitedUseShareResourceButKeepTheirValuesSeparate()
        {
            var resource = Own(ScriptableObject.CreateInstance<RewardDefinitionSO>());
            resource.Configure("TestGame", "prop.hint", "Hint", null, RewardResourceKind.Item, "effect.unlimited_hint");
            var count = new RewardEntry(resource, RewardGrantMode.AddQuantity, 3);
            var timed = new RewardEntry(resource, RewardGrantMode.UnlimitedUse, 1800);
            count.Validate();
            timed.Validate();
            Assert.That(RewardPresentation.FormatValue(count), Is.EqualTo("×3"));
            Assert.That(RewardPresentation.FormatValue(timed), Is.EqualTo("∞ 30m"));
            Assert.That(timed.Unit, Is.EqualTo("second"));
            Assert.That(timed.RequestAmount, Is.EqualTo(1800));
            Assert.That(RewardPresentation.FormatDuration(5401), Is.EqualTo("1h 30m 1s"));
        }

        [Test]
        public void UnsupportedModesMissingResourcesAndNonpositiveAmountsAreRejected()
        {
            var resource = Own(ScriptableObject.CreateInstance<RewardDefinitionSO>());
            resource.Configure("common", "currency.money", "Money", null, RewardResourceKind.Currency);
            Assert.Throws<InvalidOperationException>(() => new RewardEntry(resource, RewardGrantMode.UnlimitedUse, 60).Validate());
            Assert.Throws<InvalidOperationException>(() => new RewardEntry(resource, RewardGrantMode.AddQuantity, 0).Validate());
            Assert.Throws<InvalidOperationException>(() => new RewardEntry(null, RewardGrantMode.AddQuantity, 1).Validate());
        }

        [Test]
        public void MultiRewardRequiresAnExplicitChestStyleAndSingleRewardDoesNot()
        {
            var source = AssetDatabase.LoadAssetAtPath<SeasonPassActivityConfig>(ConfigPath);
            var bundle = Own(ScriptableObject.CreateInstance<SeasonPassRewardDefinition>());
            bundle.ConfigureEntries(source.GetTier(1).FreeRewards);
            Assert.DoesNotThrow(bundle.ValidateEntries);
            bundle.ConfigureEntries(source.GetTier(10).FreeRewards);
            Assert.Throws<InvalidOperationException>(bundle.ValidateEntries);
            var style = AssetDatabase.LoadAssetAtPath<RewardChestStyleSO>(SeasonPassRewardAssetMigration.StyleFolder + "/Chest_Rare.asset");
            bundle.ConfigureEntries(source.GetTier(10).FreeRewards, style);
            Assert.DoesNotThrow(bundle.ValidateEntries);
            Assert.That(bundle.ChestStyle.Tier, Is.EqualTo(RewardChestTier.Rare));
        }

        [Test]
        public void MainTemplateAndStandaloneRowBothBindSlotsAndKeepMasksFromBlockingPreview()
        {
            var main = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameMain/Activities/SeasonPass/UI/SeasonPassMainPanel.prefab");
            var mainSerialized = new SerializedObject(main.GetComponent<SeasonPassMainPanel>());
            Assert.That(mainSerialized.FindProperty("m_TierContent").objectReferenceValue, Is.Not.Null);
            var template = (SeasonPassTierRowView)mainSerialized.FindProperty("m_TierRowTemplate").objectReferenceValue;
            Assert.That(template, Is.Not.Null);
            CheckRow(template);
            var row = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameMain/Activities/SeasonPass/UI/SeasonPassTierRowView.prefab");
            CheckRow(row.GetComponent<SeasonPassTierRowView>());
        }

        private static void CheckRow(SeasonPassTierRowView row)
        {
            var serialized = new SerializedObject(row);
            foreach (string lane in new[] { "m_FreeLane", "m_PremiumLane" })
            {
                Assert.That(serialized.FindProperty(lane + ".m_RewardSlot").objectReferenceValue, Is.Not.Null);
                var mask = (GameObject)serialized.FindProperty(lane + ".m_LockedMask").objectReferenceValue;
                foreach (UnityEngine.UI.Graphic graphic in mask.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    Assert.That(graphic.raycastTarget, Is.False);
            }
        }

        [Test]
        public void RewardAssetsUseOnlyEntriesAndMigratedHintResourcesRemainExplicit()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:RewardDataSO"))
            {
                var bundle = AssetDatabase.LoadAssetAtPath<RewardDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                bundle.ValidateEntries();
                Assert.That(new SerializedObject(bundle).FindProperty("rewardDatas"), Is.Null);
                Assert.That(new SerializedObject(bundle).FindProperty("m_UseResourceEntries"), Is.Null);
            }
            var hint = AssetDatabase.LoadAssetAtPath<RewardDefinitionSO>("Assets/GameMain/ScriptableObjects/Reward/Definitions/Game_Hint.asset");
            Assert.That(hint.ResourceKey, Is.EqualTo("prop.hint"));
            Assert.That(hint.Scope, Is.EqualTo("Game"));
            Assert.That(hint.sprite, Is.Not.Null);
        }

        [Test]
        public void QuantityResolutionAppliesMultiplierWithoutChangingConfiguredAmount()
        {
            var money = Own(ScriptableObject.CreateInstance<RewardDefinitionSO>());
            money.Configure("common", "currency.money", "Money", null, RewardResourceKind.Currency);
            var entry = new RewardEntry(money, RewardGrantMode.AddQuantity, 300);
            Assert.That(RewardQuantityUtility.TryResolve(entry, 2, out QuantityReward reward, out _), Is.True);
            Assert.That(reward.Amount, Is.EqualTo(600));
            Assert.That(reward.ResourceKey, Is.EqualTo("currency.money"));
            Assert.That(entry.Amount, Is.EqualTo(300));
            Assert.That(RewardPresentation.FormatValue(entry, 2), Is.EqualTo("×600"));
        }

        [Test]
        public void QuantityResolutionRejectsDurationsOverflowAndWrongScopes()
        {
            var resource = Own(ScriptableObject.CreateInstance<RewardDefinitionSO>());
            resource.Configure("Game", "prop.hint", "Hint", null, RewardResourceKind.Item, "effect.unlimited_hint");
            Assert.That(RewardQuantityUtility.TryResolve(new RewardEntry(resource, RewardGrantMode.UnlimitedUse, 1800), 1, out _, out _), Is.False);
            Assert.That(RewardQuantityUtility.TryResolve(new RewardEntry(resource, RewardGrantMode.AddQuantity, int.MaxValue), 2, out _, out _), Is.False);
            var entry = new RewardEntry(resource, RewardGrantMode.AddQuantity, 3);
            Assert.That(RewardQuantityUtility.TryResolve(entry, 1, out QuantityReward reward, out _), Is.True);
            Assert.That(reward.GameMode, Is.EqualTo(GameMode.Game));
            resource.Configure("UnknownGame", "prop.hint", "Hint", null, RewardResourceKind.Item);
            Assert.That(RewardQuantityUtility.TryResolve(entry, 1, out _, out _), Is.False);
            resource.Configure("Game", "currency.money", "Money", null, RewardResourceKind.Currency);
            Assert.That(RewardQuantityUtility.TryResolve(entry, 1, out _, out _), Is.False);
            Assert.That(RewardQuantityUtility.TryResolve(entry, 0, out _, out _), Is.False);
        }

        [Test]
        public void ClaimAndEventPayloadsKeepTheirOwnRewardList()
        {
            var source = AssetDatabase.LoadAssetAtPath<SeasonPassActivityConfig>(ConfigPath);
            var entries = new List<RewardEntry>(source.GetTier(1).FreeRewards);
            var claim = new ChestRewardData(false, ChestSkinType.Chest_1, entries);
            var notification = OnClaimRewardsEventArgs.Create(1, entries);
            entries.Clear();
            Assert.That(claim.rewardDatas.Count, Is.EqualTo(1));
            Assert.That(notification.rewardDatas.Count, Is.EqualTo(1));
            notification.Clear();
            Assert.That(notification.rewardDatas, Is.Null);
            Assert.That(claim.rewardDatas.Count, Is.EqualTo(1));
        }

        private static void CheckRequests(IReadOnlyList<LegacySeasonPassRewardDefinition> old, SeasonPassRewardDefinition bundle)
        {
            if (old.Count == 0) { Assert.That(bundle, Is.Null); return; }
            Assert.That(bundle, Is.Not.Null);
            Assert.That(AssetDatabase.Contains(bundle), Is.True);
            Assert.That(bundle.Entries.Count, Is.EqualTo(old.Count));
            for (int i = 0; i < old.Count; i++)
            {
                Assert.That(bundle.Entries[i].ResourceKey, Is.EqualTo(old[i].ResourceKey));
                Assert.That(bundle.Entries[i].Unit, Is.EqualTo(old[i].Unit));
                Assert.That(bundle.Entries[i].RequestAmount, Is.EqualTo(old[i].Amount));
            }
        }

        private T Own<T>(T item) where T : UnityEngine.Object { m_Owned.Add(item); return item; }
    }
}
