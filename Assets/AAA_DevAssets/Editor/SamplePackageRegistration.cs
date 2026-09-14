using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Lokas.Editor.DataTableTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lokas.Editor
{
    internal static class SamplePackageRegistration
    {
        public const string UI = "Assets/GameMain/DataTables/UIForm.txt";
        public const string Scenes = "Assets/GameMain/DataTables/Scene.txt";
        public const string Config = "Assets/GameMain/Configs/DefaultConfig.txt";
        public const string Resources = "Assets/GameMain/Configs/ResourceCollection.xml";
        public static readonly string[] SharedFiles =
        {
            SamplePackagePaths.Launcher, UI, "Assets/GameMain/DataTables/UIForm.bytes", "Assets/GameMain/Scripts/UI/Runtime/UIFormId.cs",
            Scenes, "Assets/GameMain/DataTables/Scene.bytes", Config, Resources, SubGameAssetRegistryConfig.DefaultAssetPath,
            SamplePackagePaths.State
        };

        public static string[] RowsFor(string path, IEnumerable<string> keys, int keyColumn)
        {
            var set = new HashSet<string>(keys, StringComparer.Ordinal);
            return File.ReadAllLines(path).Where(line => !line.StartsWith("#") && line.Split('\t').Length > keyColumn &&
                set.Contains(line.Split('\t')[keyColumn].Trim())).ToArray();
        }
        public static string[] MergeRows(IEnumerable<string> existing, IEnumerable<string> fragments, bool install, bool isConfig)
        {
            var result = existing.ToList();
            foreach (string fragment in fragments)
            {
                var columns = fragment.Split('\t');
                if (columns.Length < 4 || string.IsNullOrWhiteSpace(columns[1])) throw new InvalidOperationException("Invalid registration row.");
                string id = columns[1].Trim();
                string asset = isConfig ? null : columns[3].Trim();
                var matches = result.Where(line => !line.StartsWith("#") && line.Split('\t').Length >= 4 &&
                    (line.Split('\t')[1].Trim() == id || (!isConfig && line.Split('\t')[3].Trim() == asset))).ToArray();
                if (matches.Length > 1 || matches.Any(line => !EquivalentRow(line, fragment)))
                    throw new InvalidOperationException("Conflicting or modified registration: " + id + " / " + asset);
                if (install) { if (matches.Length == 0) result.Add(fragment); }
                else foreach (var line in matches) result.Remove(line);
            }
            return result.ToArray();
        }
        private static bool EquivalentRow(string a, string b) =>
            a.Split('\t').Select(value => value.Trim()).SequenceEqual(b.Split('\t').Select(value => value.Trim()));

        public static XDocument MergeResources(XDocument source, SamplePackageManifest manifest, bool install)
        {
            var document = new XDocument(source);
            var collection = document.Root?.Element("ResourceCollection") ?? throw new InvalidDataException("Missing ResourceCollection.");
            MergeElements(collection.Element("Resources"), manifest.resourceDefinitions, "Name", install);
            MergeElements(collection.Element("Assets"), manifest.resourceAssets, "Guid", install);
            if (!install && collection.Element("Assets").Elements().Any(element => (string)element.Attribute("ResourceName") == manifest.game))
                throw new InvalidOperationException("Resource group still contains assets not owned by this package: " + manifest.game);
            return document;
        }
        private static void MergeElements(XElement parent, string[] fragments, string key, bool install)
        {
            if (parent == null) throw new InvalidDataException("Missing resource section.");
            foreach (var text in fragments)
            {
                var fragment = XElement.Parse(text);
                string id = (string)fragment.Attribute(key);
                if (string.IsNullOrEmpty(id)) throw new InvalidDataException("Invalid resource registration.");
                var matches = parent.Elements(fragment.Name).Where(element => (string)element.Attribute(key) == id).ToArray();
                if (matches.Length > 1 || matches.Any(element => !EquivalentElement(element, fragment)))
                    throw new InvalidOperationException("Conflicting or modified resource registration: " + id);
                if (install) { if (matches.Length == 0) parent.Add(fragment); }
                else foreach (var element in matches) element.Remove();
            }
        }
        private static bool EquivalentElement(XElement a, XElement b) => a.Name == b.Name &&
            a.Attributes().OrderBy(value => value.Name.ToString()).Select(value => value.ToString())
                .SequenceEqual(b.Attributes().OrderBy(value => value.Name.ToString()).Select(value => value.ToString()));

        public static void CheckSources(SamplePackageManifest manifest, bool install)
        {
            MergeRows(File.ReadAllLines(UI), manifest.uiRows, install, false);
            MergeRows(File.ReadAllLines(Scenes), manifest.sceneRows, install, false);
            MergeRows(File.ReadAllLines(Config), manifest.configRows, install, true);
            MergeResources(XDocument.Load(Resources), manifest, install);
        }
        public static void ApplySources(SamplePackageManifest manifest, bool install)
        {
            // 先全部构造并验证，再写入；外层操作已备份这些精确文件。
            var ui = MergeRows(File.ReadAllLines(UI), manifest.uiRows, install, false);
            var scenes = MergeRows(File.ReadAllLines(Scenes), manifest.sceneRows, install, false);
            var config = MergeRows(File.ReadAllLines(Config), manifest.configRows, install, true);
            var resources = MergeResources(XDocument.Load(Resources), manifest, install);
            var utf8 = new UTF8Encoding(false);
            File.WriteAllLines(UI, ui, utf8);
            File.WriteAllLines(Scenes, scenes, utf8);
            File.WriteAllLines(Config, config, utf8);
            resources.Save(Resources);
            UIFormPanelGeneratorWindow.SynchronizeRegistrationFiles();
            var processor = DataTableGenerator.CreateDataTableProcessor("Scene");
            if (!DataTableGenerator.CheckRawData(processor, "Scene") || !processor.GenerateDataFile("Assets/GameMain/DataTables/Scene.bytes"))
                throw new InvalidOperationException("Scene table generation failed.");
        }

        public static void ApplyAssets(SamplePackageManifest manifest, bool install, GameMode primary)
        {
            var root = AssetDatabase.LoadAssetAtPath<SubGameAssetRegistryConfig>(SubGameAssetRegistryConfig.DefaultAssetPath);
            if (root == null || root.HasLegacyEntries) throw new InvalidOperationException("Missing or legacy resource index.");
            var manifests = root.Manifests.ToList();
            if (manifests.Any(value => value == null)) throw new InvalidOperationException("Resource index contains Missing references.");
            var owned = manifests.Where(value => value.GameName == manifest.game).ToArray();
            if (owned.Any(value => AssetDatabase.GetAssetPath(value) != manifest.resourceManifest))
                throw new InvalidOperationException("Another resource manifest owns the same game.");
            if (install)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SubGameAssetManifest>(manifest.resourceManifest);
                if (asset == null) throw new InvalidOperationException("Imported manifest is missing.");
                if (owned.Length == 0) manifests.Add(asset);
            }
            else manifests.RemoveAll(value => value.GameName == manifest.game);
            var scene = SceneManager.GetActiveScene();
            if (scene.path != SamplePackagePaths.Launcher)
                scene = EditorSceneManager.OpenScene(SamplePackagePaths.Launcher, OpenSceneMode.Single);
            var owners = UnityEngine.Object.FindObjectsOfType<GameManagerComponent>(true).Where(value => value.gameObject.scene == scene).ToArray();
            if (owners.Length != 1) throw new InvalidOperationException("Launcher must have one GameManager.");
            var owner = owners[0];
            var serialized = new SerializedObject(owner);
            var list = serialized.FindProperty("m_SubGameManagers");
            var registered = Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue as SubGameManagerComponent).ToList();
            new SubGameRuntimeRegistry().Rebuild(registered);
            var managers = UnityEngine.Object.FindObjectsOfType<SubGameManagerComponent>(true).Where(value => value.gameObject.scene == scene).ToList();
            if (!new HashSet<SubGameManagerComponent>(registered).SetEquals(managers))
                throw new InvalidOperationException("Scene managers differ from explicit installed list; resolve the Launcher configuration first.");
            if (primary != GameMode.None && !managers.Any(value => value.GameMode == primary && (install || (int)primary != manifest.mode)) &&
                !(install && (int)primary == manifest.mode)) throw new InvalidOperationException("Selected primary game is not installed.");
            var gameManagers = managers.Where(value => (int)value.GameMode == manifest.mode).ToArray();
            if (gameManagers.Length > 1) throw new InvalidOperationException("Duplicate installed manager.");
            if (gameManagers.Any(value => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(value) != manifest.managerPrefab))
                throw new InvalidOperationException("Manager instance does not belong to this package prefab.");
            if (install && gameManagers.Length == 0)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(manifest.managerPrefab);
                if (prefab == null || prefab.GetComponent<SubGameManagerComponent>() == null)
                    throw new InvalidOperationException("Manager prefab missing or script compilation incomplete.");
                var entry = scene.GetRootGameObjects().Single(value => value.name == "GameEntry");
                var parent = entry.transform.Find("Customs/SubGameManager");
                if (parent == null) throw new InvalidOperationException("Missing explicit SubGameManager mount point.");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                managers.Add(instance.GetComponent<SubGameManagerComponent>());
            }
            if (!install)
            {
                foreach (var manager in gameManagers)
                {
                    managers.Remove(manager);
                    UnityEngine.Object.DestroyImmediate(PrefabUtility.GetNearestPrefabInstanceRoot(manager));
                }
            }
            // 使用现有列表顺序，移除只改自己的项；新项最后加入。
            var ordered = registered.Where(value => value != null && managers.Contains(value)).ToList();
            foreach (var value in managers) if (!ordered.Contains(value)) ordered.Add(value);
            list.arraySize = ordered.Count;
            for (int i = 0; i < ordered.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
            if (primary != GameMode.None && !ordered.Any(value => value.GameMode == primary))
                throw new InvalidOperationException("Selected primary game is not installed.");
            serialized.FindProperty("m_PrimaryGameMode").intValue = (int)primary;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
            var procedures = UnityEngine.Object.FindObjectsOfType<UnityGameFramework.Runtime.ProcedureComponent>(true).Where(value => value.gameObject.scene == scene).ToArray();
            if (procedures.Length != 1) throw new InvalidOperationException("Launcher must have one ProcedureComponent.");
            var procedure = new SerializedObject(procedures[0]);
            var names = procedure.FindProperty("m_AvailableProcedureTypeNames");
            var all = Enumerable.Range(0, names.arraySize).Select(index => names.GetArrayElementAtIndex(index).stringValue).ToList();
            if (install) { if (!all.Contains(manifest.procedure)) all.Add(manifest.procedure); }
            else all.RemoveAll(value => value == manifest.procedure);
            names.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++) names.GetArrayElementAtIndex(i).stringValue = all[i];
            procedure.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(procedures[0]);
            root.SetManifests(manifests);
            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssetIfDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Launcher.");

            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (install)
            {
                foreach (var path in manifest.scenePaths)
                    if (!buildScenes.Any(value => value.path == path)) buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
            else buildScenes.RemoveAll(value => manifest.scenePaths.Contains(value.path));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
    }
}
