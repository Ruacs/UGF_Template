using System;
using System.Collections.Generic;
using System.Linq;
using Lokas.Editor;
using Lokas.Activities.SeasonPass.UI;
using UnityEditor;
using UnityEngine;

namespace Lokas.Activities.SeasonPass.Editor
{
    /// <summary>可重复执行的安装器：创建活动配置和注册信息，不创建或修改设计师维护的 Prefab。</summary>
    public static class SeasonPassAssetInstaller
    {
        private const string ConfigPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Config/SchoolPass202609.asset";
        private const string DefinitionPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Registry/SeasonPassActivityDefinition.asset";
        private const string CatalogPath = "Assets/GameMain/ScriptableObjects/ActivitySystem/ActivityModuleCatalog.asset";
        private const string UiPath = "Assets/GameMain/Activities/SeasonPass/UI";

        [MenuItem("Tools/Activity/Season Pass/Install Config And Validate UI")]
        public static void InstallOrUpdate()
        {
            SeasonPassActivityConfig config = AssetDatabase.LoadAssetAtPath<SeasonPassActivityConfig>(ConfigPath);
            if (config == null)
            {
                config = LoadOrCreate<SeasonPassActivityConfig>(ConfigPath);
                config.SetInstallationData("school_pass_202609", "开学通行证", 1788393600, 1790812800,
                    unlockLevel: 50, chargePerCompletedLevel: 1, editorAlwaysUnlocked: true, tiers: CreateSchoolPassTiers(), entryLabel: "PASS");
                EditorUtility.SetDirty(config);
            }
            SeasonPassRewardAssetMigration.Migrate(config, ConfigPath);

            var pages = new[]
            {
                new ActivityPageDefinition(SeasonPassPageKeys.Main, UIFormIdRanges.SeasonPassMain, "SeasonPassMainPanel", UiPath + "/SeasonPassMainPanel.prefab"),
                new ActivityPageDefinition(SeasonPassPageKeys.Rules, UIFormIdRanges.SeasonPassRules, "SeasonPassRulesPanel", UiPath + "/SeasonPassRulesPanel.prefab"),
                new ActivityPageDefinition(SeasonPassPageKeys.GoldPassPurchase, UIFormIdRanges.SeasonPassGoldPassPurchase, "SeasonPassGoldPassPurchasePanel", UiPath + "/SeasonPassGoldPassPurchasePanel.prefab",
                    new[] { "SeasonPassRewardPanel" })
            };
            SeasonPassActivityDefinition definition = LoadOrCreate<SeasonPassActivityDefinition>(DefinitionPath);
            definition.SetInstallationData(config, new[] { GameMode.Game.ToString() }, pages);
            EditorUtility.SetDirty(definition);

            ActivityModuleCatalogConfig catalog = LoadOrCreate<ActivityModuleCatalogConfig>(CatalogPath);
            var modules = catalog.Modules.Where(module => module != null && module.ModuleId != SeasonPassActivityModule.Id).ToList();
            modules.Add(definition);
            catalog.SetModules(modules.ToArray());
            EditorUtility.SetDirty(catalog);

            UIFormPanelGeneratorWindow.SynchronizeRegistrationFiles();
            ReportDesignerOwnedPrefabs(pages);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Season Pass config and registrations installed: 25 tiers and UIForm IDs 300-302. Module UI Prefabs remain designer-owned.");
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void ReportDesignerOwnedPrefabs(IEnumerable<ActivityPageDefinition> pages)
        {
            foreach (ActivityPageDefinition page in pages)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(page.PrefabAssetPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[SeasonPass] Waiting for designer Prefab: {page.PrefabAssetPath} ({page.AssetName}).");
                    continue;
                }

                switch (page.AssetName)
                {
                    case "SeasonPassMainPanel":
                        SeasonPassMainPanel main = prefab.GetComponent<SeasonPassMainPanel>();
                        ReportBindings(main, page.PrefabAssetPath, "m_Title", "m_Countdown", "m_Progress", "m_ProgressFill",
                            "m_ActivateButton", "m_RulesButton", "m_CloseButton", "m_TierContent", "m_TierRowTemplate",
                            "m_BonusBankRowRoot");
                        if (main != null)
                        {
                            var mainSerialized = new SerializedObject(main);
                            var template = mainSerialized.FindProperty("m_TierRowTemplate")?.objectReferenceValue as SeasonPassTierRowView;
                            ReportTierRowBindings(template, page.PrefabAssetPath + " / TierRowTemplate");
                            var bonusBankRoot = mainSerialized.FindProperty("m_BonusBankRowRoot")?.objectReferenceValue as Transform;
                            ReportBindings(bonusBankRoot != null ? bonusBankRoot.GetComponent<SeasonPassBonusBankView>() : null,
                                page.PrefabAssetPath + " / BonusBank", "m_LockedMask", "m_RewardContentRoot", "m_TokenImg",
                                "m_TierText", "m_ProgressText", "m_ProgressFill", "m_BankCoinsText", "m_BonusBankButton");
                        }
                        break;
                    case "SeasonPassRulesPanel":
                        ReportBindings(prefab.GetComponent<SeasonPassRulesPanel>(), page.PrefabAssetPath, "m_CloseButton");
                        break;
                    case "SeasonPassGoldPassPurchasePanel":
                        ReportBindings(prefab.GetComponent<SeasonPassGoldPassPurchasePanel>(), page.PrefabAssetPath);
                        break;
                }
            }
        }

        private static void ReportTierRowBindings(SeasonPassTierRowView row, string label)
        {
            ReportBindings(row, label, "m_TierText", "m_ProgressFill",
                "m_FreeLane.m_RewardContentRoot", "m_FreeLane.m_RewardSlot", "m_FreeLane.m_ClaimButton",
                "m_FreeLane.m_ClaimButtonText", "m_FreeLane.m_LockedMask", "m_FreeLane.m_ClaimedMask",
                "m_PremiumLane.m_RewardContentRoot", "m_PremiumLane.m_RewardSlot", "m_PremiumLane.m_ClaimButton",
                "m_PremiumLane.m_ClaimButtonText", "m_PremiumLane.m_LockedMask", "m_PremiumLane.m_ClaimedMask");
        }

        private static void ReportBindings(UnityEngine.Object target, string label, params string[] propertyNames)
        {
            if (target == null)
            {
                Debug.LogWarning($"[SeasonPass] {label} is missing its required page component.");
                return;
            }

            var serialized = new SerializedObject(target);
            var missing = new List<string>();
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null) missing.Add(propertyName);
            }
            if (missing.Count > 0)
                Debug.LogWarning($"[SeasonPass] {label} has unbound fields: {string.Join(", ", missing)}.");
        }

        private static SeasonPassTierDefinition[] CreateSchoolPassTiers()
        {
            return new[]
            {
                Tier(1, 0, F(Money(300))),
                Tier(2, 2, F(ExtraMove(1)), P(Bomb(1))),
                Tier(3, 3, F(Hammer(1)), P(Money(1000))),
                Tier(4, 3, F(Money(300)), P(Hammer(2))),
                Tier(5, 5, F(Drill(1)), P(Money(700), ExtraMove(2), UnlimitedLife(30))),
                Tier(6, 6, F(Hammer(1)), P(Money(1200))),
                Tier(7, 6, F(ExtraMove(1)), P(Bomb(1))),
                Tier(8, 7, F(Money(400)), P(Hammer(2))),
                Tier(9, 7, F(Drill(1)), P(Money(1300))),
                Tier(10, 8, F(Money(300), ExtraMove(1), Hammer(1)), P(Bomb(1), Money(1200), ExtraMove(2))),
                Tier(11, 8, F(Hammer(1)), P(Bomb(1))),
                Tier(12, 9, F(Money(500)), P(Money(1800))),
                Tier(13, 10, F(Bomb(1)), P(Hammer(2))),
                Tier(14, 12, F(Hammer(1)), P(ExtraMove(2))),
                Tier(15, 13, F(Money(600)), P(Bomb(1), ExtraMove(2), Hammer(2))),
                Tier(16, 16, F(ExtraMove(1)), P(Money(2000))),
                Tier(17, 18, F(Bomb(1)), P(Hammer(2))),
                Tier(18, 19, F(Money(700)), P(Money(2000))),
                Tier(19, 20, F(Hammer(1)), P(ExtraMove(3))),
                Tier(20, 25, F(Money(700), Drill(1), ExtraMove(2)), P(Money(1500), Drill(1), ExtraMove(2), Hammer(2))),
                Tier(21, 30, F(ExtraMove(2)), P(Hammer(2))),
                Tier(22, 35, F(Money(800)), P(Money(2500))),
                Tier(23, 45, F(Hammer(2)), P(Bomb(2))),
                Tier(24, 55, F(Money(1200)), P(Money(3000))),
                Tier(25, 70, F(Money(2500), ExtraMove(2), Hammer(1)), P(Bomb(2), Money(3500), ExtraMove(3), Hammer(2)))
            };
        }

        private static SeasonPassTierDefinition Tier(int tier, int charge, LegacySeasonPassRewardDefinition[] free, LegacySeasonPassRewardDefinition[] premium = null)
            => new SeasonPassTierDefinition(tier, charge, free, premium ?? Array.Empty<LegacySeasonPassRewardDefinition>());
        private static LegacySeasonPassRewardDefinition[] F(params LegacySeasonPassRewardDefinition[] rewards) => rewards;
        private static LegacySeasonPassRewardDefinition[] P(params LegacySeasonPassRewardDefinition[] rewards) => rewards;
        private static LegacySeasonPassRewardDefinition Money(int amount) => new LegacySeasonPassRewardDefinition("金币", "currency.money", amount);
        private static LegacySeasonPassRewardDefinition ExtraMove(int amount) => new LegacySeasonPassRewardDefinition("额外步数", "game.extra_move", amount);
        private static LegacySeasonPassRewardDefinition Bomb(int amount) => new LegacySeasonPassRewardDefinition("炸弹", "game.bomb", amount);
        private static LegacySeasonPassRewardDefinition Hammer(int amount) => new LegacySeasonPassRewardDefinition("锤子", "game.hammer", amount);
        private static LegacySeasonPassRewardDefinition Drill(int amount) => new LegacySeasonPassRewardDefinition("钻头", "game.drill", amount);
        private static LegacySeasonPassRewardDefinition UnlimitedLife(int minutes) => new LegacySeasonPassRewardDefinition("无限生命", "effect.unlimited_life", minutes, "minute");
    }
}
