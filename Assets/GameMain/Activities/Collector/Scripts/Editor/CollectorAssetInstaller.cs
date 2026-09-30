using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConfigSO;
using Lokas.Editor;
using UnityEditor;
using UnityEngine;

namespace Lokas.Activities.Collector.Editor
{
    /// <summary>
    /// 安装 Collector 的页面路由、静态活动配置和默认奖励包。不会创建或覆盖设计师维护的页面 Prefab。
    /// </summary>
    public static class CollectorAssetInstaller
    {
        private const string RootPath = "Assets/GameMain/Activities/Collector";
        private const string ConfigPath = RootPath + "/ScriptableObjects/Config/HexaCollector202609.asset";
        private const string DefinitionPath = RootPath + "/ScriptableObjects/Registry/CollectorActivityDefinition.asset";
        private const string RewardDefinitionsPath = RootPath + "/ScriptableObjects/Rewards/Definitions";
        private const string RewardBundlesPath = RootPath + "/ScriptableObjects/Rewards/HexaCollector202609";
        private const string CatalogPath = "Assets/GameMain/ScriptableObjects/ActivitySystem/ActivityModuleCatalog.asset";
        private const string UiPath = RootPath + "/UI";
        private const string UiFormTextPath = "Assets/GameMain/DataTables/UIForm.txt";
        private const int MainUiFormId = UIFormIdRanges.CollectorMain;

        private const string SharedMoneyDefinitionPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/Definitions/currency_money.asset";
        private const string SharedExtraMoveDefinitionPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/Definitions/game_extra_move.asset";
        private const string SharedDrillDefinitionPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/Definitions/game_drill.asset";
        private const string SharedUnlimitedLifeDefinitionPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/Definitions/effect_unlimited_life.asset";
        private const string SharedHammerDefinitionPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/Definitions/game_hammer.asset";
        private const string SharedBombDefinitionPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/Definitions/game_bomb.asset";
        private const string CommonChestStylePath = "Assets/GameMain/ScriptableObjects/Reward/Chests/Chest_Common.asset";

        [MenuItem("Tools/Activity/Collector/Install UI Route")]
        public static void InstallOrUpdate()
        {
            EnsureFolder(RootPath + "/ScriptableObjects/Config");
            EnsureFolder(RootPath + "/ScriptableObjects/Registry");
            EnsureFolder(RewardDefinitionsPath);
            EnsureFolder(RewardBundlesPath);
            int nextConfigId = GetNextConfigId();

            RewardDefinitionSO money = LoadOrCreateRewardDefinition(
                RewardDefinitionsPath + "/currency_money.asset", "common", "currency.money", "金币",
                RewardResourceKind.Currency, LoadSourceIcon(SharedMoneyDefinitionPath, RewardGrantMode.AddQuantity),
                null, ref nextConfigId);
            RewardDefinitionSO extraMove = LoadOrCreateRewardDefinition(
                RewardDefinitionsPath + "/game_extra_move.asset", "Game", "game.extra_move", "额外步数",
                RewardResourceKind.Item, LoadSourceIcon(SharedExtraMoveDefinitionPath, RewardGrantMode.AddQuantity),
                "effect.unlimited_game.extra_move", ref nextConfigId);
            RewardDefinitionSO unlimitedLife = LoadOrCreateRewardDefinition(
                RewardDefinitionsPath + "/effect_unlimited_life.asset", "Game", "life", "生命",
                RewardResourceKind.Life, LoadSourceIcon(SharedUnlimitedLifeDefinitionPath, RewardGrantMode.UnlimitedUse),
                "effect.unlimited_life", ref nextConfigId);
            RewardDefinitionSO drill = LoadOrCreateRewardDefinition(
                RewardDefinitionsPath + "/game_drill.asset", "Game", "game.drill", "钻头",
                RewardResourceKind.Item, LoadSourceIcon(SharedDrillDefinitionPath, RewardGrantMode.AddQuantity),
                "effect.unlimited_game.drill", ref nextConfigId);
            RewardDefinitionSO hammer = LoadOrCreateRewardDefinition(
                RewardDefinitionsPath + "/game_hammer.asset", "Game", "game.hammer", "锤子",
                RewardResourceKind.Item, LoadSourceIcon(SharedHammerDefinitionPath, RewardGrantMode.AddQuantity),
                "effect.unlimited_game.hammer", ref nextConfigId);
            RewardDefinitionSO bomb = LoadOrCreateRewardDefinition(
                RewardDefinitionsPath + "/game_bomb.asset", "Game", "game.bomb", "炸弹",
                RewardResourceKind.Item, LoadSourceIcon(SharedBombDefinitionPath, RewardGrantMode.AddQuantity),
                "effect.unlimited_game.bomb", ref nextConfigId);

            RewardChestStyleSO chestStyle = AssetDatabase.LoadAssetAtPath<RewardChestStyleSO>(CommonChestStylePath);
            if (chestStyle == null)
                Debug.LogWarning($"[Collector] Common chest style is missing: {CommonChestStylePath}");

            CollectorRewardDefinition[] rewards =
            {
                Bundle("Milestone_01_Coins", chestStyle, ref nextConfigId,
                    Entry(money, RewardGrantMode.AddQuantity, 50)),
                Bundle("Milestone_02_ExtraMove", chestStyle, ref nextConfigId,
                    Entry(extraMove, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_03_Drill", chestStyle, ref nextConfigId,
                    Entry(drill, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_04_Coins", chestStyle, ref nextConfigId,
                    Entry(money, RewardGrantMode.AddQuantity, 70)),
                Bundle("Milestone_05_UnlimitedLife", chestStyle, ref nextConfigId,
                    Entry(unlimitedLife, RewardGrantMode.UnlimitedUse, 15 * 60)),
                Bundle("Milestone_06_ExtraMove", chestStyle, ref nextConfigId,
                    Entry(extraMove, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_07_Drill", chestStyle, ref nextConfigId,
                    Entry(drill, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_08_Coins", chestStyle, ref nextConfigId,
                    Entry(money, RewardGrantMode.AddQuantity, 100)),
                Bundle("Milestone_09_ExtraMove", chestStyle, ref nextConfigId,
                    Entry(extraMove, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_10_UnlimitedLife", chestStyle, ref nextConfigId,
                    Entry(unlimitedLife, RewardGrantMode.UnlimitedUse, 15 * 60)),
                Bundle("Milestone_11_Coins_Hammer", chestStyle, ref nextConfigId,
                    Entry(money, RewardGrantMode.AddQuantity, 100),
                    Entry(hammer, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_12_Bomb", chestStyle, ref nextConfigId,
                    Entry(bomb, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_13_Coins", chestStyle, ref nextConfigId,
                    Entry(money, RewardGrantMode.AddQuantity, 200)),
                Bundle("Milestone_14_Drill", chestStyle, ref nextConfigId,
                    Entry(drill, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_15_ExtraMove_UnlimitedLife", chestStyle, ref nextConfigId,
                    Entry(extraMove, RewardGrantMode.AddQuantity, 1),
                    Entry(unlimitedLife, RewardGrantMode.UnlimitedUse, 15 * 60)),
                Bundle("Milestone_16_Coins", chestStyle, ref nextConfigId,
                    Entry(money, RewardGrantMode.AddQuantity, 300)),
                Bundle("Milestone_17_Drill", chestStyle, ref nextConfigId,
                    Entry(drill, RewardGrantMode.AddQuantity, 1)),
                Bundle("Milestone_18_UnlimitedLife", chestStyle, ref nextConfigId,
                    Entry(unlimitedLife, RewardGrantMode.UnlimitedUse, 30 * 60)),
                Bundle("Milestone_19_Coins_ExtraMove_Hammer", chestStyle, ref nextConfigId,
                    Entry(money, RewardGrantMode.AddQuantity, 300),
                    Entry(extraMove, RewardGrantMode.AddQuantity, 1),
                    Entry(hammer, RewardGrantMode.AddQuantity, 1))
            };

            CollectorActivityConfig config = LoadOrCreate<CollectorActivityConfig>(ConfigPath, out bool configCreated);
            if (configCreated || config.Tasks == null || config.Tasks.Count != 19 || config.GetTask(19) == null)
            {
                config.SetInstallationData(
                    "hexa_collector_202609",
                    "HEXA COLLECTOR",
                    "COLLECTOR",
                    1788393600,
                    1790812800,
                    unlockLevel: 1,
                    editorAlwaysUnlocked: true,
                    targetCount: 50,
                    collectionPerWin: 1,
                    collectibleIcon: null,
                    tasks: new[]
                    {
                        Task(1, 50, rewards[0]),
                        Task(2, 50, rewards[1]),
                        Task(3, 50, rewards[2]),
                        Task(4, 50, rewards[3]),
                        Task(5, 50, rewards[4]),
                        Task(6, 100, rewards[5]),
                        Task(7, 100, rewards[6]),
                        Task(8, 150, rewards[7]),
                        Task(9, 150, rewards[8]),
                        Task(10, 150, rewards[9]),
                        Task(11, 200, rewards[10]),
                        Task(12, 200, rewards[11]),
                        Task(13, 200, rewards[12]),
                        Task(14, 400, rewards[13]),
                        Task(15, 400, rewards[14]),
                        Task(16, 400, rewards[15]),
                        Task(17, 800, rewards[16]),
                        Task(18, 800, rewards[17]),
                        Task(19, 800, rewards[18])
                    });
            }

            // 让旧版本配置资产也显式序列化新增的每局收集数量字段，同时保留设计师已经调整的值。
            config.SetCollectionPerWin(config.CollectionPerWin);
            EditorUtility.SetDirty(config);

            var pages = new[]
            {
                new ActivityPageDefinition(
                    CollectorPageKeys.Main,
                    MainUiFormId,
                    "CollectorMainPanel",
                    UiPath + "/CollectorMainPanel.prefab")
            };

            CollectorActivityDefinition definition = LoadOrCreate<CollectorActivityDefinition>(DefinitionPath, out _);
            definition.SetInstallationData(config, new[] { GameMode.Game.ToString() }, pages);
            EditorUtility.SetDirty(definition);

            ActivityModuleCatalogConfig catalog = LoadOrCreate<ActivityModuleCatalogConfig>(CatalogPath);
            var modules = catalog.Modules
                .Where(module => module != null && module.ModuleId != CollectorActivityModule.Id)
                .ToList();
            modules.Add(definition);
            catalog.SetModules(modules.ToArray());
            EditorUtility.SetDirty(catalog);

            EnsureUiFormRow(MainUiFormId, "Collector主页面", "CollectorMainPanel", "Default");
            UIFormPanelGeneratorWindow.SynchronizeRegistrationFiles();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            config.ValidateConfiguration();
            Debug.Log("[Collector] Activity route, config, reward bundles, and level-completion collection rules installed.");
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            return LoadOrCreate<T>(assetPath, out _);
        }

        private static T LoadOrCreate<T>(string assetPath, out bool created) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            created = asset == null;
            if (!created) return asset;

            EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static RewardDefinitionSO LoadOrCreateRewardDefinition(string path, string scope, string resourceKey,
            string label, RewardResourceKind kind, Sprite icon, string unlimitedUseKey, ref int nextConfigId)
        {
            RewardDefinitionSO definition = LoadOrCreate<RewardDefinitionSO>(path, out bool created);
            EnsureConfigId(definition, ref nextConfigId);
            if (created)
            {
                definition.Configure(scope, resourceKey, label, icon, kind, unlimitedUseKey);
                EditorUtility.SetDirty(definition);
            }
            return definition;
        }

        private static CollectorRewardDefinition Bundle(string fileName, RewardChestStyleSO chestStyle,
            ref int nextConfigId, params RewardEntry[] entries)
        {
            string path = RewardBundlesPath + "/" + fileName + ".asset";
            CollectorRewardDefinition bundle = LoadOrCreate<CollectorRewardDefinition>(path, out _);
            EnsureConfigId(bundle, ref nextConfigId);
            bundle.ConfigureEntries(entries, entries != null && entries.Length > 1 ? chestStyle : null);
            EditorUtility.SetDirty(bundle);
            return bundle;
        }

        private static RewardEntry Entry(RewardDefinitionSO definition, RewardGrantMode grantMode, long amount)
        {
            return new RewardEntry(definition, grantMode, amount);
        }

        private static int GetNextConfigId()
        {
            return AssetDatabase.FindAssets("t:IdOnlyConfigSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<IdOnlyConfigSO>)
                .Where(asset => asset != null)
                .Select(asset => asset.id)
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        private static void EnsureConfigId(IdOnlyConfigSO asset, ref int nextConfigId)
        {
            if (asset == null || asset.id > 0) return;
            asset.id = nextConfigId++;
            EditorUtility.SetDirty(asset);
        }

        private static CollectorTaskDefinition Task(int tier, int requiredCount, CollectorRewardDefinition reward)
        {
            return new CollectorTaskDefinition(tier, requiredCount, reward);
        }

        private static Sprite LoadSourceIcon(string path, RewardGrantMode grantMode)
        {
            RewardDefinitionSO source = AssetDatabase.LoadAssetAtPath<RewardDefinitionSO>(path);
            if (source == null)
            {
                Debug.LogWarning($"[Collector] Shared reward definition is missing: {path}");
                return null;
            }
            return source.GetIcon(grantMode);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string name = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureUiFormRow(int id, string note, string assetName, string group)
        {
            if (!File.Exists(UiFormTextPath))
                throw new FileNotFoundException("UIForm source table is missing.", UiFormTextPath);

            var lines = File.ReadAllLines(UiFormTextPath).ToList();
            string prefix = id + "\t";
            int rowIndex = lines.FindIndex(line => line.TrimStart().StartsWith(prefix));
            string row = $"\t{id}\t{note}\t{assetName}\t{group}\tFALSE\tFALSE";
            if (rowIndex >= 0)
            {
                lines[rowIndex] = row;
            }
            else
            {
                lines.Add(row);
            }

            File.WriteAllLines(UiFormTextPath, lines);
        }
    }
}
