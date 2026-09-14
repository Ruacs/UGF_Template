using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lokas.Editor
{
    [InitializeOnLoad]
    public static class SamplePackageService
    {
        private static double s_ResumeAfter;
        private static double s_NextJournalPoll;
        static SamplePackageService()
        {
            s_ResumeAfter = EditorApplication.timeSinceStartup + 3;
            EditorApplication.update += Resume;
            AssetDatabase.importPackageCompleted += _ => ImportCompleted();
            AssetDatabase.importPackageFailed += (_, error) => ImportFailed(error);
            AssetDatabase.importPackageCancelled += _ => ImportFailed("Unity package import was cancelled.");
        }
        public static SamplePackageJournal LastOperation => SamplePackagePaths.Read<SamplePackageJournal>(SamplePackagePaths.Full(SamplePackagePaths.Journal));

        public static void CreateInitialManifests()
        {
            RequireIdle();
            var index = AssetDatabase.LoadAssetAtPath<SubGameAssetRegistryConfig>(SubGameAssetRegistryConfig.DefaultAssetPath);
            var resources = XDocument.Load(SamplePackageRegistration.Resources).Root.Element("ResourceCollection");
            foreach (var game in new[] { "HexaAway", "RectMatch" })
            {
                var asset = index.Manifests.Single(value => value.GameName == game);
                var manifest = new SamplePackageManifest { game = game, mode = game == "HexaAway" ? 2 : 3,
                    managerPrefab = game == "HexaAway" ? "Assets/GameMain/SubGame/HexaAway/Prefabs/HexaAway.prefab" : "Assets/GameMain/SubGame/RectMatch/Entity/RectMatch.prefab",
                    resourceManifest = AssetDatabase.GetAssetPath(asset), procedure = "Lokas.ProcedureGame" + game };
                if (File.Exists(manifest.ManifestPath)) throw new InvalidOperationException("Manifest already exists: " + game);
                manifest.scenePaths = EditorBuildSettings.scenes.Where(value => manifest.Owns(value.path)).Select(value => value.path).ToArray();
                manifest.uiRows = SamplePackageRegistration.RowsFor(SamplePackageRegistration.UI, asset.Entry.UIForms, 3);
                manifest.sceneRows = SamplePackageRegistration.RowsFor(SamplePackageRegistration.Scenes, asset.Entry.Scenes, 3);
                manifest.configRows = SamplePackageRegistration.RowsFor(SamplePackageRegistration.Config, new[] { "Scene." + game }, 1);
                manifest.resourceDefinitions = resources.Element("Resources").Elements().Where(value => (string)value.Attribute("Name") == game).Select(value => value.ToString(SaveOptions.DisableFormatting)).ToArray();
                manifest.resourceAssets = resources.Element("Assets").Elements().Where(value => manifest.Owns(AssetDatabase.GUIDToAssetPath((string)value.Attribute("Guid"))))
                    .Select(value => value.ToString(SaveOptions.DisableFormatting)).ToArray();
                manifest.Validate();
                SamplePackagePaths.Write(SamplePackagePaths.Full(manifest.ManifestPath), manifest);
                AssetDatabase.ImportAsset(manifest.ManifestPath);
            }
        }

        public static SamplePackageManifest Manifest(string game)
        {
            ValidateGame(game);
            string path = "Assets/GameMain/SubGame/" + game + "/Editor/PackageManifest.json";
            if (!File.Exists(path)) throw new InvalidOperationException("Package not installed or manifest missing: " + game);
            var manifest = SamplePackagePaths.Read<SamplePackageManifest>(SamplePackagePaths.Full(path));
            manifest.Validate();
            if (manifest.game != game) throw new InvalidOperationException("Package identity mismatch.");
            return manifest;
        }
        private static void ValidateGame(string game)
        {
            if (game != "HexaAway" && game != "RectMatch") throw new InvalidOperationException("Only the two predefined samples are supported.");
        }
        public static string Export(string game, string outputDirectory = null, bool recordInstalled = true)
        {
            RequireIdle();
            var manifest = Manifest(game);
            SamplePackageRegistration.CheckSources(manifest, true);
            CheckRegistrationOwnership(manifest);
            var blockers = FindBlockers(manifest);
            if (blockers.Count > 0) throw new InvalidOperationException(string.Join("\n", blockers));
            string directory = SamplePackagePaths.ExportDirectory(outputDirectory);
            Directory.CreateDirectory(directory);
            string package = Path.Combine(directory, game + "-" + manifest.version + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".unitypackage");
            if (File.Exists(package)) throw new IOException("Output already exists.");
            var ownPaths = AssetDatabase.GetAllAssetPaths().Where(manifest.Owns).ToArray();
            var dependencies = AssetDatabase.GetDependencies(ownPaths.Where(path => !AssetDatabase.IsValidFolder(path)).ToArray(), true)
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) && !manifest.Owns(path)).Distinct()
                .Select(path => new SamplePackageDependency { path = path, guid = AssetDatabase.AssetPathToGUID(path) }).ToArray();
            // 明确路径 + Recurse，不启用 IncludeDependencies / IncludeLibraryAssets。
            AssetDatabase.ExportPackage(new[] { manifest.Root, manifest.SceneRoot }, package, ExportPackageOptions.Recurse);
            var files = SamplePackageArchive.Inspect(package, manifest);
            var expected = new HashSet<string>(ownPaths, StringComparer.Ordinal);
            if (!expected.SetEquals(files.Select(value => value.path))) throw new InvalidOperationException("Exported contents differ from owned asset list.");
            var catalog = new SamplePackageCatalog { manifest = manifest, packageHash = SamplePackagePaths.Hash(package),
                unityVersion = Application.unityVersion, files = files, dependencies = dependencies };
            SamplePackagePaths.Write(package + ".json", catalog);
            if (recordInstalled) Remember(catalog);
            return package;
        }

        public static string Inspect(string game)
        {
            RequireIdle();
            var manifest = Manifest(game);
            var blockers = FindBlockers(manifest);
            try { CheckRegistrationOwnership(manifest); SamplePackageRegistration.CheckSources(manifest, false); }
            catch (Exception exception) { blockers.Add(exception.Message); }
            var state = ReadState().installed.FirstOrDefault(value => value.game == game);
            if (state == null) blockers.Add("No recorded installation baseline; export the current package first.");
            else blockers.AddRange(CompareFiles(state.catalog, true));
            return "Sample: " + game + "\nOwned assets: " + AssetDatabase.GetAllAssetPaths().Count(manifest.Owns) +
                "\nBlocking findings: " + blockers.Count + "\n" + string.Join("\n", blockers) +
                "\nAsset dependencies and source identifiers were checked; dynamic string loading still requires the zero/single/double Unity tests.";
        }

        public static void Remove(string game, GameMode primaryAfterRemoval)
        {
            RequireIdle(); RequireNoPending();
            var manifest = Manifest(game);
            var state = ReadState().installed.SingleOrDefault(value => value.game == game);
            if (state == null) throw new InvalidOperationException("Export first to establish a verified baseline.");
            var blockers = FindBlockers(manifest);
            blockers.AddRange(CompareFiles(state.catalog, true));
            if (blockers.Count > 0) throw new InvalidOperationException(string.Join("\n", blockers));
            CheckRegistrationOwnership(manifest);
            SamplePackageRegistration.CheckSources(manifest, false);
            var available = ReadLauncherModes();
            if ((int)primaryAfterRemoval == manifest.mode || (primaryAfterRemoval != GameMode.None && !available.Contains(primaryAfterRemoval)))
                throw new InvalidOperationException("Select an installed surviving primary game or None before removal.");
            var journal = Begin("remove", state.catalog, primaryAfterRemoval);
            try
            {
                journal.packagePath = Export(game, journal.backupDirectory, false);
                SaveJournal(journal);
                AssetDatabase.DisallowAutoRefresh();
                try
                {
                    SamplePackageRegistration.ApplyAssets(manifest, false, primaryAfterRemoval);
                    SamplePackageRegistration.ApplySources(manifest, false);
                    Forget(game);
                    // 目标由固定包白名单派生，并在删除前再次解析到当前工程下。
                    foreach (string path in new[] { manifest.Root, manifest.SceneRoot })
                    {
                        SamplePackagePaths.Asset(path);
                        if (!AssetDatabase.IsValidFolder(path) || !AssetDatabase.DeleteAsset(path)) throw new IOException("Could not remove owned folder: " + path);
                    }
                    journal.phase = "awaiting-compilation";
                    CaptureAfter(journal);
                    SaveJournal(journal);
                }
                finally { AssetDatabase.AllowAutoRefresh(); }
                AssetDatabase.Refresh();
                s_ResumeAfter = EditorApplication.timeSinceStartup + 3;
            }
            catch (Exception exception) { Fail(journal, exception); throw; }
        }

        public static void Import(string package)
        {
            RequireIdle(); RequireNoPending();
            package = Path.GetFullPath(package);
            var catalog = ReadAndVerifyPackage(package);
            var manifest = catalog.manifest;
            if (Directory.Exists(manifest.Root) || Directory.Exists(manifest.SceneRoot))
            {
                var changes = CompareFiles(catalog, true);
                if (changes.Count > 0) throw new InvalidOperationException("Package already exists or has local modifications; no files were overwritten.\n" + string.Join("\n", changes));
                // 幂等回装：只核实/补注册，不重新导入资产。
            }
            foreach (var file in catalog.files)
            {
                CheckImportTarget(SamplePackagePaths.Asset(file.path), file);
                string currentPath = AssetDatabase.GUIDToAssetPath(file.guid);
                if (!string.IsNullOrEmpty(currentPath) && currentPath != file.path)
                    throw new InvalidOperationException("GUID already exists at another path: " + currentPath);
            }
            foreach (var dependency in catalog.dependencies)
                if (AssetDatabase.GUIDToAssetPath(dependency.guid) != dependency.path)
                    throw new InvalidOperationException("Missing/incompatible template dependency: " + dependency.path);
            SamplePackageRegistration.CheckSources(manifest, true);
            var journal = Begin("import", catalog, ReadPrimary());
            journal.packagePath = package;
            try
            {
                journal.phase = "importing";
                SaveJournal(journal);
                // UI ID 先按清单生成；随后脚本到位时不会因缺少枚举成员编译失败。
                AssetDatabase.DisallowAutoRefresh();
                try
                {
                    SamplePackageRegistration.ApplySources(manifest, true);
                    CaptureAfter(journal);
                    SaveJournal(journal);
                    if (!journal.assetsExisted) AssetDatabase.ImportPackage(package, false);
                    else { journal.phase = "awaiting-compilation"; SaveJournal(journal); }
                }
                finally { AssetDatabase.AllowAutoRefresh(); }
                AssetDatabase.Refresh();
                s_ResumeAfter = EditorApplication.timeSinceStartup + 3;
            }
            catch (Exception exception) { Fail(journal, exception); throw; }
        }

        internal static void CheckImportTarget(string fullPath, SamplePackageFile file)
        {
            // 根目录尚不存在也可能留下同名普通文件或孤立 meta；原生导入前同样禁止覆盖。
            if ((file.folder && File.Exists(fullPath)) || (!file.folder && Directory.Exists(fullPath)))
                throw new InvalidOperationException("File/folder path conflict: " + file.path);
            if ((!file.folder && File.Exists(fullPath) && SamplePackagePaths.Hash(fullPath) != file.assetHash) ||
                (File.Exists(fullPath + ".meta") && SamplePackagePaths.Hash(fullPath + ".meta") != file.metaHash))
                throw new InvalidOperationException("Existing import target differs; no files overwritten: " + file.path);
        }

        private static SamplePackageCatalog ReadAndVerifyPackage(string package)
        {
            if (!File.Exists(package) || !File.Exists(package + ".json")) throw new FileNotFoundException("Select an exported unitypackage with its adjacent .json catalog.");
            var catalog = SamplePackagePaths.Read<SamplePackageCatalog>(package + ".json");
            if (catalog?.manifest == null || catalog.files == null || catalog.dependencies == null) throw new InvalidDataException("Invalid package catalog.");
            catalog.manifest.Validate();
            if (catalog.unityVersion != Application.unityVersion)
                throw new InvalidDataException("Use the export Unity version " + catalog.unityVersion + "; validate upgrades in a separate project copy.");
            if (catalog.packageHash != SamplePackagePaths.Hash(package)) throw new InvalidDataException("Package SHA256 mismatch.");
            var actual = SamplePackageArchive.Inspect(package, catalog.manifest);
            if (actual.Length != catalog.files.Length || actual.Where((value, index) => !SameFile(value, catalog.files.OrderBy(item => item.path, StringComparer.Ordinal).ElementAt(index))).Any())
                throw new InvalidDataException("Actual archive members differ from the catalog.");
            var manifestFile = actual.SingleOrDefault(value => value.path == catalog.manifest.ManifestPath);
            if (manifestFile == null || JsonUtility.ToJson(JsonUtility.FromJson<SamplePackageManifest>(
                SamplePackageArchive.ReadAssetText(package, manifestFile.guid))) != JsonUtility.ToJson(catalog.manifest))
                throw new InvalidDataException("Embedded manifest and catalog do not match.");
            CheckRegistrationOwnership(catalog.manifest, actual);
            return catalog;
        }
        private static bool SameFile(SamplePackageFile a, SamplePackageFile b) => a.path == b.path && a.guid == b.guid &&
            a.assetHash == b.assetHash && a.metaHash == b.metaHash && a.folder == b.folder;

        internal static List<string> CompareFiles(SamplePackageCatalog catalog, bool checkExtras, bool allowMissing = false)
        {
            var result = new List<string>();
            foreach (var file in catalog.files)
            {
                string path = SamplePackagePaths.Asset(file.path);
                if (file.folder ? !Directory.Exists(path) : !File.Exists(path))
                {
                    if (!allowMissing) result.Add("Missing: " + file.path);
                    if (File.Exists(path + ".meta") && SamplePackagePaths.Hash(path + ".meta") != file.metaHash) result.Add("Modified orphan meta: " + file.path);
                    continue;
                }
                if ((!file.folder && SamplePackagePaths.Hash(path) != file.assetHash) || SamplePackagePaths.Hash(path + ".meta") != file.metaHash)
                    result.Add("Locally modified: " + file.path);
            }
            if (checkExtras)
            {
                var known = new HashSet<string>(catalog.files.Select(value => value.path), StringComparer.Ordinal);
                foreach (string path in AssetDatabase.GetAllAssetPaths().Where(catalog.manifest.Owns))
                    if (!known.Contains(path)) result.Add("Unrecorded owned asset: " + path);
                foreach (string root in new[] { catalog.manifest.Root, catalog.manifest.SceneRoot })
                    if (Directory.Exists(root)) foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                    {
                        string relative = path.Replace('\\', '/');
                        string asset = relative.EndsWith(".meta", StringComparison.Ordinal) ? relative.Substring(0, relative.Length - 5) : relative;
                        if (!known.Contains(asset)) result.Add("Unimported/unrecorded file: " + relative);
                    }
            }
            return result.Distinct().ToList();
        }

        private static void CheckRegistrationOwnership(SamplePackageManifest manifest, SamplePackageFile[] files = null)
        {
            manifest.Validate();
            var expectedUI = manifest.game == "HexaAway" ? new Dictionary<string, int> { ["HexaAwayUIPanel"] = 216, ["SelectionUIPanel"] = 217 } :
                new Dictionary<string, int> { ["RectMatchUIPanel"] = 203, ["RectMatchTutorialUIPanel"] = 210 };
            if (manifest.uiRows.Length != expectedUI.Count) throw new InvalidDataException("Unexpected UI registration count.");
            var seenUI = new HashSet<string>();
            foreach (var row in manifest.uiRows)
            {
                var values = row.Split('\t');
                if (values.Length != 7 || !seenUI.Add(values[3]) || !expectedUI.TryGetValue(values[3], out int id) || values[1] != id.ToString()) throw new InvalidDataException("UI row is not owned by this sample.");
            }
            if (manifest.sceneRows.Length != 1 || manifest.configRows.Length != 1) throw new InvalidDataException("Unexpected scene/config registration count.");
            var scene = manifest.sceneRows[0].Split('\t');
            var config = manifest.configRows[0].Split('\t');
            int sceneId = manifest.game == "HexaAway" ? 4 : 3;
            if (scene.Length != 5 || scene[1] != sceneId.ToString() || scene[3] != manifest.game + "Scene" ||
                config.Length != 4 || config[1] != "Scene." + manifest.game || config[3] != sceneId.ToString()) throw new InvalidDataException("Scene/config row not owned.");
            var guids = files == null ? new HashSet<string>(AssetDatabase.GetAllAssetPaths().Where(manifest.Owns).Select(AssetDatabase.AssetPathToGUID)) : new HashSet<string>(files.Select(value => value.guid));
            var seenGuids = new HashSet<string>();
            foreach (var text in manifest.resourceAssets)
            {
                var element = XElement.Parse(text);
                string group = (string)element.Attribute("ResourceName");
                if (element.Name != "Asset" || element.HasElements || !seenGuids.Add((string)element.Attribute("Guid")) || !guids.Contains((string)element.Attribute("Guid")) ||
                    (group != manifest.game && group != "ScriptableObjects" && group != "Scenes")) throw new InvalidDataException("Resource assignment not owned.");
            }
            if (manifest.resourceDefinitions.Length != 1) throw new InvalidDataException("Expected one owned resource group.");
            foreach (var text in manifest.resourceDefinitions)
            {
                var element = XElement.Parse(text);
                if (element.Name != "Resource" || element.HasElements || (string)element.Attribute("Name") != manifest.game) throw new InvalidDataException("Resource group not owned.");
            }
        }

        private static List<string> FindBlockers(SamplePackageManifest manifest)
        {
            var blockers = new List<string>();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var launcher = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (launcher.path != SamplePackagePaths.Launcher) launcher = EditorSceneManager.OpenScene(SamplePackagePaths.Launcher, OpenSceneMode.Single);
                foreach (var manager in UnityEngine.Object.FindObjectsOfType<SubGameManagerComponent>(true).Where(value => value.gameObject.scene == launcher && (int)value.GameMode == manifest.mode))
                {
                    var root = PrefabUtility.GetNearestPrefabInstanceRoot(manager);
                    if (root == null || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(manager) != manifest.managerPrefab)
                        blockers.Add("Manager must use the package prefab: " + manager.name);
                    else if (PrefabUtility.HasPrefabInstanceAnyOverrides(root, false))
                        blockers.Add("Manager instance has custom overrides/additions. Review and apply them to its owned prefab before exporting/removing: " + manager.name);
                }
            }
            finally { RestoreSceneSetup(setup); }
            string[] all = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(path)).ToArray();
            var owned = all.Where(manifest.Owns).ToArray();
            foreach (string path in AssetDatabase.GetDependencies(owned, true))
                if (path.StartsWith("Assets/GameMain/SubGame/", StringComparison.Ordinal) && !manifest.Owns(path)) blockers.Add("Cross-game dependency: " + path);
            var managed = new HashSet<string> { SamplePackagePaths.Launcher, SubGameAssetRegistryConfig.DefaultAssetPath };
            var yaml = new HashSet<string> { ".prefab", ".unity", ".asset", ".mat", ".controller", ".overrideController" };
            foreach (string path in all.Where(path => !manifest.Owns(path) && !managed.Contains(path) && yaml.Contains(Path.GetExtension(path))))
                if (AssetDatabase.GetDependencies(path, false).Any(manifest.Owns)) blockers.Add("Unmanaged incoming asset reference: " + path);
            // C# 标识符候选检查，不以字符串/注释中的示例名作为类型依赖。
            // 私有嵌套类型不可能被外部合法引用（如 Clip）；不把同名方法误报为类型。
            var declarations = new Regex(@"\b(?:public|internal)\s+(?:(?:sealed|abstract|static|partial|readonly)\s+)*(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)");
            var ownTypes = new HashSet<string>();
            var sharedTypes = new HashSet<string>();
            foreach (string path in all.Where(path => path.EndsWith(".cs", StringComparison.Ordinal)))
            {
                string source = StripTrivia(File.ReadAllText(path));
                foreach (Match match in declarations.Matches(source)) (manifest.Owns(path) ? ownTypes : sharedTypes).Add(match.Groups[1].Value);
            }
            ownTypes.ExceptWith(sharedTypes);
            if (ownTypes.Count > 0)
            {
                var names = new Regex(@"\b(?:" + string.Join("|", ownTypes.Select(Regex.Escape)) + @")\b");
                foreach (string path in all.Where(path => !manifest.Owns(path) && path != "Assets/GameMain/Scripts/UI/Runtime/UIFormId.cs" && path.EndsWith(".cs", StringComparison.Ordinal)))
                    if (names.IsMatch(StripTrivia(File.ReadAllText(path)))) blockers.Add("Possible C# type dependency: " + path);
            }
            return blockers.Distinct().ToList();
        }
        private static string StripTrivia(string source) => Regex.Replace(source,
            "//[^\\r\\n]*|/\\*[\\s\\S]*?\\*/|@\"(?:\"\"|[^\"])*\"|\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*'", " ");

        private static SamplePackageState ReadState() => SamplePackagePaths.Read<SamplePackageState>(SamplePackagePaths.Full(SamplePackagePaths.State));
        private static void Remember(SamplePackageCatalog catalog)
        {
            var state = ReadState();
            state.installed.RemoveAll(value => value.game == catalog.manifest.game);
            state.installed.Add(new SamplePackageInstalled { game = catalog.manifest.game, catalog = catalog });
            SamplePackagePaths.Write(SamplePackagePaths.Full(SamplePackagePaths.State), state);
        }
        private static void Forget(string game)
        {
            var state = ReadState(); state.installed.RemoveAll(value => value.game == game);
            SamplePackagePaths.Write(SamplePackagePaths.Full(SamplePackagePaths.State), state);
        }
        private static void RequireIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Exit Play Mode and wait for compilation/import.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save all open scenes first.");
            foreach (var path in new[] { SubGameAssetRegistryConfig.DefaultAssetPath })
                if (EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(path))) throw new InvalidOperationException("Save pending asset changes first: " + path);
        }
        private static void RequireNoPending()
        {
            var journal = LastOperation;
            if (!string.IsNullOrEmpty(journal.phase) && journal.phase != "complete" && journal.phase != "restored")
                throw new InvalidOperationException("Resolve the previous package operation first: " + journal.phase);
        }
        private static GameMode[] ReadLauncherModes()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(SamplePackagePaths.Launcher, OpenSceneMode.Single);
                return UnityEngine.Object.FindObjectsOfType<SubGameManagerComponent>(true).Where(value => value.gameObject.scene == scene).Select(value => value.GameMode).ToArray();
            }
            finally { RestoreSceneSetup(setup); }
        }
        private static GameMode ReadPrimary()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try { EditorSceneManager.OpenScene(SamplePackagePaths.Launcher, OpenSceneMode.Single); return UnityEngine.Object.FindObjectOfType<GameManagerComponent>(true).PrimaryGameMode; }
            finally { RestoreSceneSetup(setup); }
        }
        private static SamplePackageJournal Begin(string operation, SamplePackageCatalog catalog, GameMode primary)
        {
            string directory = SamplePackagePaths.Full("Backups/SamplePackages/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + operation + "-" + catalog.manifest.game);
            Directory.CreateDirectory(directory);
            var journal = new SamplePackageJournal { operation = operation, phase = "prepared", game = catalog.manifest.game,
                catalog = catalog, primary = (int)primary, backupDirectory = directory,
                assetsExisted = Directory.Exists(catalog.manifest.Root),
                originalBuildScenes = CaptureBuildScenes(), afterBuildScenes = CaptureBuildScenes(),
                originalScenes = EditorSceneManager.GetSceneManagerSetup().Where(value => value.isLoaded).Select(value => value.path).ToArray(),
                expectedInstalled = ReadState().installed.Select(value => value.game).ToArray() };
            foreach (string path in SamplePackageRegistration.SharedFiles)
            {
                string full = SamplePackagePaths.Full(path);
                var item = new SamplePackageBackupFile { path = path, existed = File.Exists(full), beforeHash = SamplePackagePaths.Hash(full) };
                journal.sharedFiles.Add(item);
                if (!item.existed) continue;
                string target = Path.Combine(directory, "Shared", path);
                Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(full, target);
            }
            SaveJournal(journal);
            return journal;
        }
        private static void CaptureAfter(SamplePackageJournal journal)
        {
            foreach (var item in journal.sharedFiles) item.afterHash = SamplePackagePaths.Hash(SamplePackagePaths.Full(item.path));
            journal.afterBuildScenes = CaptureBuildScenes();
        }
        // Build Settings 是原生实时状态，不能拿延迟落盘的旧文件覆盖内存；只备份/恢复本工具管理的场景列表。
        private static SamplePackageBuildScene[] CaptureBuildScenes() => EditorBuildSettings.scenes
            .Select(value => new SamplePackageBuildScene { path = value.path, enabled = value.enabled }).ToArray();
        internal static bool SameBuildScenes(SamplePackageBuildScene[] one, SamplePackageBuildScene[] two) =>
            one != null && two != null && one.Length == two.Length && one.Zip(two,
                (a, b) => a != null && b != null && a.path == b.path && a.enabled == b.enabled).All(value => value);
        private static void SaveJournal(SamplePackageJournal journal)
        {
            SamplePackagePaths.Write(SamplePackagePaths.Full(SamplePackagePaths.Journal), journal);
            if (!string.IsNullOrEmpty(journal.backupDirectory)) SamplePackagePaths.Write(Path.Combine(journal.backupDirectory, "operation.json"), journal);
        }
        private static void Fail(SamplePackageJournal journal, Exception exception)
        {
            journal.phase = "failed"; journal.error = exception.ToString(); CaptureAfter(journal); SaveJournal(journal);
            Debug.LogError("Sample package operation failed; use Restore Last Operation. Backup: " + journal.backupDirectory + "\n" + exception);
        }
        private static void ImportCompleted()
        {
            var journal = LastOperation;
            if (journal.phase != "importing" && journal.phase != "restoring-import") return;
            journal.phase = journal.phase == "restoring-import" ? "awaiting-restore" : "awaiting-compilation";
            SaveJournal(journal);
            s_ResumeAfter = EditorApplication.timeSinceStartup + 3;
        }
        private static void ImportFailed(string error)
        {
            var journal = LastOperation;
            if (journal.phase == "importing" || journal.phase == "restoring-import") Fail(journal, new IOException(error));
        }
        private static void RestoreOmittedFolders(SamplePackageCatalog catalog, string package)
        {
            // Unity 原生导入可能省略空目录。仅补原目录及原始 meta，不生成新 GUID，也不解包脚本。
            foreach (var file in catalog.files.Where(value => value.folder).OrderBy(value => value.path.Length))
            {
                string full = SamplePackagePaths.Asset(file.path);
                if (Directory.Exists(full)) continue;
                if (!catalog.manifest.Owns(file.path) || File.Exists(full)) throw new IOException("Folder path conflict: " + file.path);
                byte[] meta = SamplePackageArchive.ReadMember(package, file.guid, "asset.meta");
                if (SamplePackagePaths.Hash(meta) != file.metaHash ||
                    (File.Exists(full + ".meta") && SamplePackagePaths.Hash(full + ".meta") != file.metaHash))
                    throw new InvalidDataException("Folder metadata mismatch: " + file.path);
                Directory.CreateDirectory(full);
                if (!File.Exists(full + ".meta")) File.WriteAllBytes(full + ".meta", meta);
                AssetDatabase.ImportAsset(file.path, ImportAssetOptions.ForceUpdate);
            }
        }
        private static void Resume()
        {
            if (EditorApplication.timeSinceStartup < s_ResumeAfter || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.timeSinceStartup < s_NextJournalPoll) return;
            s_NextJournalPoll = EditorApplication.timeSinceStartup + 1;
            var journal = LastOperation;
            // 回调可能跨域重载：完整资源已落盘且 Editor 空闲时可以继续验证。
            if (journal.phase == "importing" || journal.phase == "restoring-import")
            {
                if (journal.catalog.files.Any(value => !value.folder && !File.Exists(SamplePackagePaths.Asset(value.path)))) return;
                ImportCompleted();
                return;
            }
            if (journal.phase != "awaiting-compilation" && journal.phase != "awaiting-restore") return;
            if (EditorUtility.scriptCompilationFailed) { Fail(journal, new InvalidOperationException("Compilation failed after package operation.")); return; }
            try
            {
                bool restoring = journal.phase == "awaiting-restore";
                if (!restoring && journal.operation == "import")
                {
                    RestoreOmittedFolders(journal.catalog, journal.packagePath);
                    var changes = CompareFiles(journal.catalog, true);
                    if (changes.Count > 0) throw new InvalidOperationException("Imported content mismatch:\n" + string.Join("\n", changes));
                    SamplePackageRegistration.ApplyAssets(journal.catalog.manifest, true, (GameMode)journal.primary);
                    Remember(journal.catalog);
                }
                if (restoring && journal.operation == "remove")
                {
                    RestoreOmittedFolders(journal.catalog, journal.packagePath);
                    var changes = CompareFiles(journal.catalog, true);
                    if (changes.Count > 0) throw new InvalidOperationException("Restored content mismatch:\n" + string.Join("\n", changes));
                }
                if (restoring)
                {
                    if (journal.originalBuildScenes == null) throw new InvalidDataException("Missing live build-scene backup; manual recovery required.");
                    EditorBuildSettings.scenes = journal.originalBuildScenes.Select(value => new EditorBuildSettingsScene(value.path, value.enabled)).ToArray();
                }
                ValidateInstalledProject();
                journal.phase = restoring ? "restored" : "complete";
                CaptureAfter(journal); SaveJournal(journal);
                RestoreOpenScenes(journal);
                Debug.Log("Sample package " + journal.operation + " completed: " + journal.game);
            }
            catch (Exception exception) { Fail(journal, exception); }
        }
        public static void FinishPending() { s_ResumeAfter = 0; Resume(); }
        private static void RestoreOpenScenes(SamplePackageJournal journal)
        {
            var paths = (journal.originalScenes ?? Array.Empty<string>()).Where(File.Exists).ToArray();
            if (paths.Length == 0) paths = new[] { SamplePackagePaths.Launcher };
            EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Single);
            foreach (string path in paths.Skip(1)) EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        public static void ValidateInstalledProject()
        {
            var index = AssetDatabase.LoadAssetAtPath<SubGameAssetRegistryConfig>(SubGameAssetRegistryConfig.DefaultAssetPath);
            if (index == null || index.HasLegacyEntries) throw new InvalidOperationException("Invalid resource index.");
            SubGameAssetRegistry.ValidateManifests(index.Manifests);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(SamplePackagePaths.Launcher, OpenSceneMode.Single);
                var owner = UnityEngine.Object.FindObjectOfType<GameManagerComponent>(true);
                var serialized = new SerializedObject(owner);
                var array = serialized.FindProperty("m_SubGameManagers");
                var managers = Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue as SubGameManagerComponent).ToArray();
                new SubGameRuntimeRegistry().Rebuild(managers);
                var sceneManagers = UnityEngine.Object.FindObjectsOfType<SubGameManagerComponent>(true).Where(value => value.gameObject.scene == scene).ToArray();
                if (!new HashSet<SubGameManagerComponent>(managers).SetEquals(sceneManagers)) throw new InvalidOperationException("Unregistered scene game manager.");
                if (!new HashSet<string>(index.Manifests.Select(value => value.GameName)).SetEquals(managers.Select(value => value.GameMode.ToString())))
                    throw new InvalidOperationException("Resource manifests and installed manager list differ.");
                if (owner.PrimaryGameMode != GameMode.None && !managers.Any(value => value.GameMode == owner.PrimaryGameMode)) throw new InvalidOperationException("Primary game is not installed.");
                var component = UnityEngine.Object.FindObjectOfType<UnityGameFramework.Runtime.ProcedureComponent>(true);
                var names = new SerializedObject(component).FindProperty("m_AvailableProcedureTypeNames");
                var available = Enumerable.Range(0, names.arraySize).Select(i => names.GetArrayElementAtIndex(i).stringValue).ToArray();
                foreach (var name in available) if (GameFramework.Utility.Assembly.GetType(name) == null) throw new InvalidOperationException("Missing procedure: " + name);
                foreach (var manager in managers) if (!available.Contains(manager.GameProcedureType.FullName)) throw new InvalidOperationException("Unregistered game procedure.");
                foreach (var game in new[] { "HexaAway", "RectMatch" })
                {
                    bool installed = Directory.Exists("Assets/GameMain/SubGame/" + game);
                    if (installed != index.Manifests.Any(value => value.GameName == game)) throw new InvalidOperationException("Package assets and registrations differ: " + game);
                    if (!installed) continue;
                    var manifest = Manifest(game);
                    CheckRegistrationOwnership(manifest);
                    SamplePackageRegistration.CheckSources(manifest, true);
                    if (SamplePackageRegistration.RowsFor(SamplePackageRegistration.UI, manifest.uiRows.Select(row => row.Split('\t')[3]), 3).Length != manifest.uiRows.Length ||
                        SamplePackageRegistration.RowsFor(SamplePackageRegistration.Scenes, new[] { game + "Scene" }, 3).Length != 1 ||
                        SamplePackageRegistration.RowsFor(SamplePackageRegistration.Config, new[] { "Scene." + game }, 1).Length != 1)
                        throw new InvalidOperationException("Missing UI/Scene/Config source registration: " + game);
                    foreach (string path in manifest.scenePaths)
                        if (!File.Exists(path) || !EditorBuildSettings.scenes.Any(value => value.enabled && value.path == path)) throw new InvalidOperationException("Missing build scene: " + path);
                }
                var resourceEntries = XDocument.Load(SamplePackageRegistration.Resources).Root.Element("ResourceCollection").Element("Assets").Elements();
                foreach (var element in resourceEntries)
                    if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath((string)element.Attribute("Guid")))) throw new InvalidOperationException("ResourceCollection contains an unresolved GUID: " + element);
                foreach (var root in scene.GetRootGameObjects()) foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) throw new InvalidOperationException("Missing Script: " + transform.name);
            }
            finally { RestoreSceneSetup(setup); }
        }

        private static void RestoreSceneSetup(SceneSetup[] setup)
        {
            // BatchMode 首次启动可能没有任何场景；空快照不是可恢复的 SceneManagerSetup。
            if (setup.Length > 0 && setup.Any(value => value.isLoaded) && setup.Count(value => value.isActive) == 1)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        public static void RestoreLastOperation()
        {
            RequireIdle();
            var journal = LastOperation;
            if (journal.phase == "restored" || string.IsNullOrEmpty(journal.backupDirectory)) throw new InvalidOperationException("No restorable operation.");
            journal.catalog.manifest.Validate();
            string backupRoot = SamplePackagePaths.Full("Backups/SamplePackages") + Path.DirectorySeparatorChar;
            string resolvedBackup = Path.GetFullPath(journal.backupDirectory);
            if (!resolvedBackup.StartsWith(backupRoot, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(resolvedBackup) ||
                !new HashSet<string>(journal.sharedFiles.Select(value => value.path)).SetEquals(SamplePackageRegistration.SharedFiles) ||
                journal.sharedFiles.Count != SamplePackageRegistration.SharedFiles.Length || (journal.operation != "import" && journal.operation != "remove"))
                throw new InvalidDataException("Invalid recovery targets; manual recovery required.");
            var buildScenes = CaptureBuildScenes();
            if (!SameBuildScenes(buildScenes, journal.originalBuildScenes) && !SameBuildScenes(buildScenes, journal.afterBuildScenes))
                throw new InvalidOperationException("Build scenes changed after operation; restore manually.");
            foreach (var item in journal.sharedFiles)
            {
                string current = SamplePackagePaths.Hash(SamplePackagePaths.Full(item.path));
                if (current != item.afterHash && current != item.beforeHash) throw new InvalidOperationException("File changed after operation; restore manually: " + item.path);
                if (item.existed && SamplePackagePaths.Hash(Path.Combine(journal.backupDirectory, "Shared", item.path)) != item.beforeHash)
                    throw new InvalidDataException("Backup checksum mismatch: " + item.path);
            }
            if (journal.operation == "remove") ReadAndVerifyPackage(journal.packagePath);
            if (Directory.Exists(journal.catalog.manifest.Root) || Directory.Exists(journal.catalog.manifest.SceneRoot))
            {
                var changes = CompareFiles(journal.catalog, true, allowMissing: true);
                if (changes.Count > 0) throw new InvalidOperationException("Imported files changed; manual restore required.\n" + string.Join("\n", changes));
            }
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                // 先关闭引用待移除资产的场景，恢复文件后由 Unity 按原 GUID 重新导入。
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var item in journal.sharedFiles)
                {
                    string full = SamplePackagePaths.Full(item.path);
                    if (item.existed) File.Copy(Path.Combine(journal.backupDirectory, "Shared", item.path), full, true);
                    else if (File.Exists(full)) File.Delete(full);
                }
                journal.phase = journal.operation == "remove" ? "restoring-import" : "awaiting-restore";
                SaveJournal(journal);
                if (journal.operation == "remove") AssetDatabase.ImportPackage(journal.packagePath, false);
                else if (!journal.assetsExisted)
                    foreach (string path in new[] { journal.catalog.manifest.Root, journal.catalog.manifest.SceneRoot })
                    { SamplePackagePaths.Asset(path); if (AssetDatabase.IsValidFolder(path) && !AssetDatabase.DeleteAsset(path)) throw new IOException("Restore could not remove: " + path); }
            }
            catch (Exception exception) { Fail(journal, exception); throw; }
            finally { AssetDatabase.AllowAutoRefresh(); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            s_ResumeAfter = EditorApplication.timeSinceStartup + 3;
        }
    }
}
