using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ConfigSO;
using Lokas.Activities.WinStreak.UI;
using Lokas.Editor;
using UnityEditor;
using UnityEngine;

namespace Lokas.Activities.WinStreak.Editor
{
    /// <summary>可重复执行的 Win Streak Event 2 本地安装器。</summary>
    public static class WinStreakAssetInstaller
    {
        private const string RootPath = "Assets/GameMain/Activities/WinStreak";
        private const string ConfigPath = RootPath + "/ScriptableObjects/Config/HexaWinStreak202609.asset";
        private const string DefinitionPath = RootPath + "/ScriptableObjects/Registry/WinStreakActivityDefinition.asset";
        private const string RewardBundlesPath = RootPath + "/ScriptableObjects/Rewards/Event2";
        private const string UiPath = RootPath + "/UI";
        private const string TexturePath = RootPath + "/Textures";
        private const string CatalogPath = "Assets/GameMain/ScriptableObjects/ActivitySystem/ActivityModuleCatalog.asset";
        private const string UiFormTextPath = "Assets/GameMain/DataTables/UIForm.txt";
        private const string UiPanelTemplatePath = "Assets/GameMain/UI/Template/UIPanel_Template.prefab";
        private const string PopupTemplatePath = "Assets/GameMain/UI/Template/PopupUIPanel_Template.prefab";
        private const string SharedRewardPath = "Assets/GameMain/Activities/SeasonPass/ScriptableObjects/Rewards/Definitions";
        private const string SharedChestPath = "Assets/GameMain/ScriptableObjects/Reward/Chests";

        private const int StartUiFormId = UIFormIdRanges.WinStreakStartPage;
        private const int MainUiFormId = UIFormIdRanges.WinStreakMain;
        private const int DetailUiFormId = UIFormIdRanges.WinStreakDetails;
        private const int EndUiFormId = UIFormIdRanges.WinStreakEndPage;

        private readonly struct UiFormRow
        {
            public int Id { get; }
            public string Note { get; }
            public string AssetName { get; }
            public string Group { get; }

            public UiFormRow(int id, string note, string assetName, string group)
            {
                Id = id;
                Note = note;
                AssetName = assetName;
                Group = group;
            }
        }

        [MenuItem("Tools/Activity/Win Streak/Install Local MVP")]
        public static void InstallOrUpdate()
        {
            EnsureFolder(RootPath + "/ScriptableObjects/Config");
            EnsureFolder(RootPath + "/ScriptableObjects/Registry");
            EnsureFolder(RewardBundlesPath);
            EnsureFolder(UiPath);
            ConfigureUiTextures();

            RewardDefinitionSO money = RequiredAsset<RewardDefinitionSO>(SharedRewardPath + "/currency_money.asset");
            RewardDefinitionSO life = RequiredAsset<RewardDefinitionSO>(SharedRewardPath + "/effect_unlimited_life.asset");
            RewardDefinitionSO hammer = RequiredAsset<RewardDefinitionSO>(SharedRewardPath + "/game_hammer.asset");
            RewardDefinitionSO drill = RequiredAsset<RewardDefinitionSO>(SharedRewardPath + "/game_drill.asset");
            RewardDefinitionSO bomb = RequiredAsset<RewardDefinitionSO>(SharedRewardPath + "/game_bomb.asset");
            RewardChestStyleSO commonChest = RequiredAsset<RewardChestStyleSO>(SharedChestPath + "/Chest_Common.asset");
            RewardChestStyleSO rareChest = RequiredAsset<RewardChestStyleSO>(SharedChestPath + "/Chest_Rare.asset");
            RewardChestStyleSO epicChest = RequiredAsset<RewardChestStyleSO>(SharedChestPath + "/Chest_Epic.asset");
            RewardChestStyleSO legendaryChest = RequiredAsset<RewardChestStyleSO>(SharedChestPath + "/Chest_Legendary.asset");
            int nextConfigId = GetNextConfigId();

            WinStreakRewardDefinition reward5001 = Bundle("Reward_5001_Coins300", commonChest,
                ref nextConfigId, Entry(money, RewardGrantMode.AddQuantity, 300));
            WinStreakRewardDefinition reward5002 = Bundle("Reward_5002_Life15m", commonChest,
                ref nextConfigId, Entry(life, RewardGrantMode.UnlimitedUse, 15 * 60));
            WinStreakRewardDefinition reward5003 = Bundle("Reward_5003_Hammer", commonChest,
                ref nextConfigId, Entry(hammer, RewardGrantMode.AddQuantity, 1));
            WinStreakRewardDefinition reward5005 = Bundle("Reward_5005_Coins600_Life15m", rareChest,
                ref nextConfigId, Entry(money, RewardGrantMode.AddQuantity, 600),
                Entry(life, RewardGrantMode.UnlimitedUse, 15 * 60));
            WinStreakRewardDefinition reward5006 = Bundle("Reward_5006_Coins600_Drill", rareChest,
                ref nextConfigId, Entry(money, RewardGrantMode.AddQuantity, 600),
                Entry(drill, RewardGrantMode.AddQuantity, 1));
            WinStreakRewardDefinition reward5007 = Bundle("Reward_5007_Hammer_Drill", epicChest,
                ref nextConfigId, Entry(hammer, RewardGrantMode.AddQuantity, 1),
                Entry(drill, RewardGrantMode.AddQuantity, 1));
            WinStreakRewardDefinition reward5010 = Bundle("Reward_5010_Coins600_Hammer_Drill", epicChest,
                ref nextConfigId, Entry(money, RewardGrantMode.AddQuantity, 600),
                Entry(hammer, RewardGrantMode.AddQuantity, 1), Entry(drill, RewardGrantMode.AddQuantity, 1));
            WinStreakRewardDefinition reward5012 = Bundle("Reward_5012_Final", legendaryChest,
                ref nextConfigId, Entry(money, RewardGrantMode.AddQuantity, 3000),
                Entry(bomb, RewardGrantMode.AddQuantity, 1), Entry(hammer, RewardGrantMode.AddQuantity, 1),
                Entry(drill, RewardGrantMode.AddQuantity, 1), Entry(life, RewardGrantMode.UnlimitedUse, 60 * 60));

            var checkpoints = new[]
            {
                Checkpoint(2, 5001, WinStreakChestTier.Blue, reward5001),
                Checkpoint(5, 5002, WinStreakChestTier.Blue, reward5002),
                Checkpoint(8, 5003, WinStreakChestTier.Blue, reward5003),
                Checkpoint(16, 5005, WinStreakChestTier.Pink, reward5005),
                Checkpoint(24, 5006, WinStreakChestTier.Pink, reward5006),
                Checkpoint(36, 5007, WinStreakChestTier.Gold, reward5007),
                Checkpoint(48, 5010, WinStreakChestTier.Gold, reward5010),
                Checkpoint(70, 5012, WinStreakChestTier.Orange, reward5012)
            };

            WinStreakActivityConfig config = LoadOrCreate<WinStreakActivityConfig>(ConfigPath, out bool configCreated);
            if (configCreated || config.EventId != 2 || config.Checkpoints == null || config.Checkpoints.Count != 8)
            {
                config.SetInstallationData(
                    eventId: 2,
                    streakId: "hexa_win_streak_event_2_202609",
                    displayName: "HEXA STREAK",
                    entryLabel: "STREAK",
                    startUtcUnixSeconds: 1788393600,
                    endUtcUnixSeconds: 1790812800,
                    unlockLevel: 40,
                    progressPerWin: 1,
                    resetOnLoss: true,
                    editorAlwaysUnlocked: true,
                    checkpoints: checkpoints);
                EditorUtility.SetDirty(config);
            }

            var pages = new[]
            {
                new ActivityPageDefinition(WinStreakPageKeys.Start, StartUiFormId, "WinStreakStartUIPanel",
                    UiPath + "/WinStreakStartUIPanel.prefab"),
                new ActivityPageDefinition(WinStreakPageKeys.Main, MainUiFormId, "WinStreakMainUIPanel",
                    UiPath + "/WinStreakMainUIPanel.prefab"),
                new ActivityPageDefinition(WinStreakPageKeys.Details, DetailUiFormId, "WinStreakDetailUIPanel",
                    UiPath + "/WinStreakDetailUIPanel.prefab"),
                new ActivityPageDefinition(WinStreakPageKeys.End, EndUiFormId, "WinStreakEndUIPanel",
                    UiPath + "/WinStreakEndUIPanel.prefab")
            };

            EnsurePagePrefab<WinStreakStartUIPanel>(pages[0].PrefabAssetPath, PopupTemplatePath);
            EnsurePagePrefab<WinStreakMainUIPanel>(pages[1].PrefabAssetPath, UiPanelTemplatePath);
            EnsurePagePrefab<WinStreakDetailUIPanel>(pages[2].PrefabAssetPath, PopupTemplatePath);
            EnsurePagePrefab<WinStreakEndUIPanel>(pages[3].PrefabAssetPath, PopupTemplatePath);
            WinStreakPrefabLayoutBuilder.EnsureAllLayouts();

            WinStreakActivityDefinition definition = LoadOrCreate<WinStreakActivityDefinition>(DefinitionPath, out _);
            definition.SetInstallationData(config, new[] { GameMode.Game.ToString() }, pages);
            EditorUtility.SetDirty(definition);

            ActivityModuleCatalogConfig catalog = RequiredAsset<ActivityModuleCatalogConfig>(CatalogPath);
            var modules = catalog.Modules
                .Where(module => module != null && module.ModuleId != WinStreakActivityModule.Id)
                .ToList();
            modules.Add(definition);
            catalog.SetModules(modules.ToArray());
            EditorUtility.SetDirty(catalog);

            EnsureUiFormRows(
                new UiFormRow(StartUiFormId, "WinStreak报名页面", "WinStreakStartUIPanel", "Dialog"),
                new UiFormRow(MainUiFormId, "WinStreak主页面", "WinStreakMainUIPanel", "Default"),
                new UiFormRow(DetailUiFormId, "WinStreak规则页面", "WinStreakDetailUIPanel", "Dialog"),
                new UiFormRow(EndUiFormId, "WinStreak结束页面", "WinStreakEndUIPanel", "Dialog"));
            UIFormPanelGeneratorWindow.SynchronizeRegistrationFiles();
            config.ValidateConfiguration();
            ActivityPageRegistry.Install(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WinStreak] Event 2 installed: cumulative streak module, 8 checkpoint rewards, 4 UI pages, Catalog and UIForm routes.");
        }

        [MenuItem("Tools/Activity/Win Streak/Rebuild UI Prefabs From Template")]
        public static void RebuildUiPrefabsFromTemplate()
        {
            ConfigureUiTextures();
            RebuildPagePrefab<WinStreakStartUIPanel>(UiPath + "/WinStreakStartUIPanel.prefab", PopupTemplatePath);
            RebuildPagePrefab<WinStreakMainUIPanel>(UiPath + "/WinStreakMainUIPanel.prefab", UiPanelTemplatePath);
            RebuildPagePrefab<WinStreakDetailUIPanel>(UiPath + "/WinStreakDetailUIPanel.prefab", PopupTemplatePath);
            RebuildPagePrefab<WinStreakEndUIPanel>(UiPath + "/WinStreakEndUIPanel.prefab", PopupTemplatePath);
            WinStreakPrefabLayoutBuilder.RebuildAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WinStreak] Rebuilt all four UI Prefabs from the standard UIPanel templates.");
        }

        private static WinStreakCheckpointDefinition Checkpoint(int checkpoint, int rewardId,
            WinStreakChestTier tier, WinStreakRewardDefinition reward) =>
            new WinStreakCheckpointDefinition(checkpoint, rewardId, tier, reward);

        private static RewardEntry Entry(RewardDefinitionSO definition, RewardGrantMode mode, long amount) =>
            new RewardEntry(definition, mode, amount);

        private static WinStreakRewardDefinition Bundle(string fileName, RewardChestStyleSO chestStyle,
            ref int nextConfigId, params RewardEntry[] entries)
        {
            WinStreakRewardDefinition bundle = LoadOrCreate<WinStreakRewardDefinition>(
                RewardBundlesPath + "/" + fileName + ".asset", out _);
            EnsureConfigId(bundle, ref nextConfigId);
            bundle.ConfigureEntries(entries, entries != null && entries.Length > 1 ? chestStyle : null);
            EditorUtility.SetDirty(bundle);
            return bundle;
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

        private static T RequiredAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required Win Streak asset is missing: " + path);
            return asset;
        }

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (!created) return asset;
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsurePagePrefab<T>(string path, string templatePath) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                RebuildPagePrefab<T>(path, templatePath);
                return;
            }
            if (prefab.GetComponent<T>() != null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.AddComponent<T>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RebuildPagePrefab<T>(string path, string templatePath) where T : Component
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(templatePath) == null)
                throw new InvalidOperationException("Win Streak UI template is missing: " + templatePath);
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            GameObject root = PrefabUtility.LoadPrefabContents(templatePath);
            try
            {
                root.name = Path.GetFileNameWithoutExtension(path);
                RectTransform rect = root.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero;
                }
                if (root.GetComponent<T>() == null) root.AddComponent<T>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureUiTextures()
        {
            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { TexturePath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.Contains("/screenshots/"))
                .ToArray();
            foreach (string path in paths)
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                bool changed = importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single;
                if (!changed) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureUiFormRows(params UiFormRow[] requestedRows)
        {
            if (!File.Exists(UiFormTextPath))
                throw new FileNotFoundException("UIForm source table is missing.", UiFormTextPath);
            var lines = File.ReadAllLines(UiFormTextPath, Encoding.UTF8).ToList();
            bool changed = false;
            foreach (UiFormRow row in requestedRows)
            {
                int assetIndex = FindUiFormRow(lines, row.AssetName);
                int idIndex = FindUiFormRow(lines, row.Id);
                if (assetIndex >= 0)
                {
                    string[] columns = lines[assetIndex].Trim().Split('\t');
                    if (!int.TryParse(columns[0], out int existingId) || existingId != row.Id)
                        throw new InvalidOperationException($"UIForm '{row.AssetName}' already uses a different ID.");
                    continue;
                }
                if (idIndex >= 0)
                {
                    string[] columns = lines[idIndex].Trim().Split('\t');
                    if (columns.Length < 3 || !columns[2].StartsWith("WinStreak", StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            $"UIForm ID '{row.Id}' is already used by '{(columns.Length >= 3 ? columns[2] : "?")}'.");
                    lines[idIndex] = $"\t{row.Id}\t{row.Note}\t{row.AssetName}\t{row.Group}\tFALSE\tFALSE";
                    changed = true;
                    continue;
                }
                lines.Add($"\t{row.Id}\t{row.Note}\t{row.AssetName}\t{row.Group}\tFALSE\tFALSE");
                changed = true;
            }
            if (changed) File.WriteAllLines(UiFormTextPath, lines, Encoding.UTF8);
        }

        private static int FindUiFormRow(IReadOnlyList<string> lines, string assetName)
        {
            for (int index = 0; index < lines.Count; index++)
            {
                string line = lines[index];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] columns = line.Trim().Split('\t');
                if (columns.Length >= 3 && string.Equals(columns[2], assetName, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private static int FindUiFormRow(IReadOnlyList<string> lines, int id)
        {
            for (int index = 0; index < lines.Count; index++)
            {
                string line = lines[index];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] columns = line.Trim().Split('\t');
                if (columns.Length >= 3 && int.TryParse(columns[0], out int value) && value == id) return index;
            }
            return -1;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (string.IsNullOrWhiteSpace(assetFolder) || AssetDatabase.IsValidFolder(assetFolder)) return;
            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string name = Path.GetFileName(assetFolder);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Cannot create asset folder: " + assetFolder);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
