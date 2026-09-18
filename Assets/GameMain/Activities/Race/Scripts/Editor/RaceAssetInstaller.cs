using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lokas.Activities.Race.UI;
using Lokas.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Race.Editor
{
    /// <summary>
    /// 可重复执行的 Race 安装器。首次创建本地 MVP 配置和空白页面 Prefab，后续不会覆盖设计师接管的 Prefab 或奖励数值。
    /// </summary>
    public static class RaceAssetInstaller
    {
        private const string RootPath = "Assets/GameMain/Activities/Race";
        private const string ConfigPath = RootPath + "/ScriptableObjects/Config/HexaRace202609.asset";
        private const string DefinitionPath = RootPath + "/ScriptableObjects/Registry/RaceActivityDefinition.asset";
        private const string RewardDefinitionsPath = RootPath + "/ScriptableObjects/Rewards/Definitions";
        private const string RewardBundlesPath = RootPath + "/ScriptableObjects/Rewards/HexaRace202609";
        private const string UiPath = RootPath + "/UI";
        private const string UiPanelTemplatePath = "Assets/GameMain/UI/Template/UIPanel_Template.prefab";
        private const string PopupTemplatePath = "Assets/GameMain/UI/Template/PopupUIPanel_Template.prefab";
        private const string CatalogPath = "Assets/GameMain/ScriptableObjects/ActivitySystem/ActivityModuleCatalog.asset";
        private const string UiFormTextPath = "Assets/GameMain/DataTables/UIForm.txt";

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

        [MenuItem("Tools/Activity/Race/Install Local MVP")]
        public static void InstallOrUpdate()
        {
            EnsureFolder(RootPath + "/ScriptableObjects/Config");
            EnsureFolder(RootPath + "/ScriptableObjects/Registry");
            EnsureFolder(RewardDefinitionsPath);
            EnsureFolder(RewardBundlesPath);
            EnsureFolder(UiPath);

            RewardDefinitionSO money = LoadOrCreateRewardDefinition(RewardDefinitionsPath + "/currency_money.asset",
                "common", "currency.money", "金币", RewardResourceKind.Currency);
            RaceRewardDefinition first = LoadOrCreateBundle(RewardBundlesPath + "/Rank_01.asset", money, 500);
            RaceRewardDefinition second = LoadOrCreateBundle(RewardBundlesPath + "/Rank_02.asset", money, 300);
            RaceRewardDefinition thirdToFifth = LoadOrCreateBundle(RewardBundlesPath + "/Rank_03_05.asset", money, 150);

            RaceActivityConfig config = LoadOrCreate<RaceActivityConfig>(ConfigPath, out bool configCreated);
            if (configCreated)
            {
                config.SetInstallationData(
                    "hexa_race_202609",
                    "HEXA RACE",
                    "RACE",
                    1788220800,
                    1798761599,
                    unlockLevel: 1,
                    editorAlwaysUnlocked: true,
                    termHours: 0.5f,
                    targetStageDifference: 10,
                    defaultStartType: RaceStartType.Free,
                    rankRewards: new[]
                    {
                        new RaceRankRewardDefinition("冠军奖励", 1, 1, first),
                        new RaceRankRewardDefinition("亚军奖励", 2, 2, second),
                        new RaceRankRewardDefinition("完赛奖励", 3, 5, thirdToFifth)
                    });
                EditorUtility.SetDirty(config);
            }

            var pages = new[]
            {
                new ActivityPageDefinition(RacePageKeys.Start, 219, "RaceStartUIPanel", UiPath + "/RaceStartUIPanel.prefab"),
                new ActivityPageDefinition(RacePageKeys.Main, 220, "RaceMainUIPanel", UiPath + "/RaceMainUIPanel.prefab"),
                new ActivityPageDefinition(RacePageKeys.Details, 221, "RaceDetailUIPanel", UiPath + "/RaceDetailUIPanel.prefab")
            };
            EnsurePagePrefab<RaceStartUIPanel>(pages[0].PrefabAssetPath, PopupTemplatePath);
            EnsurePagePrefab<RaceMainUIPanel>(pages[1].PrefabAssetPath, UiPanelTemplatePath);
            EnsurePagePrefab<RaceDetailUIPanel>(pages[2].PrefabAssetPath, PopupTemplatePath);

            RaceActivityDefinition definition = LoadOrCreate<RaceActivityDefinition>(DefinitionPath, out _);
            definition.SetInstallationData(config, new[] { GameMode.Game.ToString() }, pages);
            EditorUtility.SetDirty(definition);

            ActivityModuleCatalogConfig catalog = LoadOrCreate<ActivityModuleCatalogConfig>(CatalogPath, out _);
            var modules = catalog.Modules.Where(module => module != null && module.ModuleId != RaceActivityModule.Id).ToList();
            modules.Add(definition);
            catalog.SetModules(modules.ToArray());
            EditorUtility.SetDirty(catalog);

            EnsureUiFormRows(
                new UiFormRow(219, "Race报名页面", "RaceStartUIPanel", "Dialog"),
                new UiFormRow(220, "Race赛道页面", "RaceMainUIPanel", "Default"),
                new UiFormRow(221, "Race规则页面", "RaceDetailUIPanel", "Dialog"));
            UIFormPanelGeneratorWindow.SynchronizeRegistrationFiles();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Race] Local MVP installed: module, three page registrations, configuration, rank rewards, and base Prefabs.");
        }

        /// <summary>
        /// Recreates only the three Race page shells from the project's standard UI panel template.
        /// Use this to repair the early bare Prefabs created before template-based generation was added.
        /// </summary>
        [MenuItem("Tools/Activity/Race/Rebuild UI Prefabs From Template")]
        public static void RebuildUiPrefabsFromTemplate()
        {
            EnsureFolder(UiPath);
            EnsureTemplatePagePrefab<RaceStartUIPanel>(UiPath + "/RaceStartUIPanel.prefab", PopupTemplatePath);
            EnsureTemplatePagePrefab<RaceMainUIPanel>(UiPath + "/RaceMainUIPanel.prefab", UiPanelTemplatePath);
            EnsureTemplatePagePrefab<RaceDetailUIPanel>(UiPath + "/RaceDetailUIPanel.prefab", PopupTemplatePath);
            RacePrefabLayoutBuilder.RebuildAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Race] Rebuilt the documented Race hierarchy and repaired any non-template Race page roots.");
        }

        private static RewardDefinitionSO LoadOrCreateRewardDefinition(string path, string scope, string resourceKey,
            string label, RewardResourceKind kind)
        {
            RewardDefinitionSO definition = LoadOrCreate<RewardDefinitionSO>(path, out bool created);
            if (created)
            {
                definition.Configure(scope, resourceKey, label, null, kind);
                EditorUtility.SetDirty(definition);
            }
            return definition;
        }

        private static RaceRewardDefinition LoadOrCreateBundle(string path, RewardDefinitionSO resource, long amount)
        {
            RaceRewardDefinition bundle = LoadOrCreate<RaceRewardDefinition>(path, out bool created);
            if (created)
            {
                bundle.ConfigureEntries(new[] { new RewardEntry(resource, RewardGrantMode.AddQuantity, amount) });
                EditorUtility.SetDirty(bundle);
            }
            return bundle;
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
            if (prefab != null)
            {
                if (IsLegacyBarePagePrefab<T>(prefab) ||
                    (templatePath == PopupTemplatePath && prefab.transform.Find("Root") == null) ||
                    (templatePath == UiPanelTemplatePath && prefab.transform.Find("Btn_Close") == null))
                {
                    RebuildPagePrefab<T>(path, templatePath);
                    RacePrefabLayoutBuilder.EnsureDocumentLayout(path);
                    return;
                }

                if (HasTemplateRoot<T>(prefab))
                {
                    RacePrefabLayoutBuilder.EnsureDocumentLayout(path);
                    return;
                }

                Debug.LogWarning($"[Race] Existing Prefab does not match UIPanel_Template: {path}. " +
                                 "Use Tools/Activity/Race/Rebuild UI Prefabs From Template to replace it.");
                return;
            }

            RebuildPagePrefab<T>(path, templatePath);
            RacePrefabLayoutBuilder.EnsureDocumentLayout(path);
        }

        private static bool IsLegacyBarePagePrefab<T>(GameObject prefab) where T : Component
        {
            return prefab.transform.childCount == 0 && prefab.GetComponent<T>() != null &&
                prefab.GetComponent<Image>() == null && prefab.GetComponent<CanvasGroup>() == null &&
                prefab.GetComponents<Component>().Length <= 2;
        }

        private static bool HasTemplateRoot<T>(GameObject prefab) where T : Component
        {
            RectTransform rectTransform = prefab.GetComponent<RectTransform>();
            Transform popupBody = prefab.transform.Find("Root");
            bool hasCanvasGroup = prefab.GetComponent<CanvasGroup>() != null ||
                (popupBody != null && popupBody.GetComponent<CanvasGroup>() != null);
            return rectTransform != null && rectTransform.anchorMin == Vector2.zero && rectTransform.anchorMax == Vector2.one &&
                prefab.GetComponent<Image>() != null && hasCanvasGroup &&
                prefab.GetComponent<T>() != null;
        }

        private static void EnsureTemplatePagePrefab<T>(string path, string templatePath) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string marker = templatePath == PopupTemplatePath ? "Root/Btn_Close" : "Btn_Close";
            if (prefab != null && HasTemplateRoot<T>(prefab) && prefab.transform.Find(marker) != null) return;
            RebuildPagePrefab<T>(path, templatePath);
        }

        private static void RebuildPagePrefab<T>(string path, string templatePath) where T : Component
        {
            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(templatePath);
            if (template == null)
                throw new InvalidOperationException($"Race UI template is missing: {templatePath}");

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            GameObject root = PrefabUtility.LoadPrefabContents(templatePath);
            try
            {
                root.name = Path.GetFileNameWithoutExtension(path);
                RectTransform rectTransform = root.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
                if (root.GetComponent<T>() == null) root.AddComponent<T>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureUiFormRows(params UiFormRow[] requestedRows)
        {
            if (!File.Exists(UiFormTextPath)) throw new FileNotFoundException("UIForm source table is missing.", UiFormTextPath);
            var lines = new List<string>(File.ReadAllLines(UiFormTextPath, Encoding.UTF8));
            bool changed = false;
            foreach (UiFormRow row in requestedRows)
            {
                string[] existing = FindUiFormRow(lines, row.AssetName);
                if (existing != null)
                {
                    if (!int.TryParse(existing[0], out int existingId) || existingId != row.Id)
                        throw new InvalidOperationException($"UIForm '{row.AssetName}' already uses a different ID.");
                    continue;
                }

                string[] idOwner = FindUiFormRow(lines, row.Id);
                if (idOwner != null)
                {
                    if (!idOwner[2].StartsWith("Race", StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            $"UIForm ID '{row.Id}' is already used by '{idOwner[2]}'; choose a new Race page ID before installing.");
                    for (int index = 0; index < lines.Count; index++)
                    {
                        string[] columns = lines[index].Trim().Split('\t');
                        if (columns.Length < 6 || !int.TryParse(columns[0], out int existingId) || existingId != row.Id) continue;
                        columns[1] = row.Note;
                        columns[2] = row.AssetName;
                        columns[3] = row.Group;
                        lines[index] = "\t" + string.Join("\t", columns);
                        changed = true;
                        break;
                    }
                    continue;
                }
                lines.Add($"\t{row.Id}\t{row.Note}\t{row.AssetName}\t{row.Group}\tFALSE\tFALSE");
                changed = true;
            }
            if (changed) File.WriteAllLines(UiFormTextPath, lines, Encoding.UTF8);
        }

        private static string[] FindUiFormRow(IEnumerable<string> lines, string assetName)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] columns = line.Trim().Split('\t');
                if (columns.Length >= 3 && string.Equals(columns[2], assetName, StringComparison.Ordinal)) return columns;
            }
            return null;
        }

        private static string[] FindUiFormRow(IEnumerable<string> lines, int id)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] columns = line.Trim().Split('\t');
                if (columns.Length >= 3 && int.TryParse(columns[0], out int existingId) && existingId == id) return columns;
            }
            return null;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (string.IsNullOrWhiteSpace(assetFolder) || AssetDatabase.IsValidFolder(assetFolder)) return;
            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string name = Path.GetFileName(assetFolder);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"Cannot create asset folder '{assetFolder}'.");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
