using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Lokas.Editor.Tests
{
    public sealed class SamplePackageTests
    {
        private static SamplePackageManifest Example() => new()
        {
            game = "HexaAway", mode = 2,
            managerPrefab = "Assets/GameMain/SubGame/HexaAway/Prefabs/HexaAway.prefab",
            resourceManifest = "Assets/GameMain/SubGame/HexaAway/ScriptableObjects/Registry/HexaAwayAssetRegistryConfig.asset",
            procedure = "Lokas.ProcedureGameHexaAway", scenePaths = new[] { "Assets/GameMain/Scenes/SubGame/HexaAway/HexaAwayScene.unity" },
            uiRows = Array.Empty<string>(), sceneRows = Array.Empty<string>(), configRows = Array.Empty<string>(),
            resourceAssets = Array.Empty<string>(), resourceDefinitions = Array.Empty<string>()
        };
        [TestCase("../outside")]
        [TestCase("Assets/../ProjectSettings/a")]
        [TestCase("Assets\\a")]
        [TestCase("Assets/a:stream")]
        [TestCase("Assets//a")]
        [TestCase("Assets/a.")]
        [TestCase("Assets/a ")]
        [TestCase("D:/outside")]
        public void UnsafePathsAreRejected(string path) => Assert.Throws<InvalidOperationException>(() => SamplePackagePaths.Full(path));

        [Test] public void DefaultExportUsesOnlyTheIgnoredDistributionDirectory()
        {
            string expected = SamplePackagePaths.Full(SamplePackagePaths.PackagesDirectory);
            Assert.AreEqual(expected, SamplePackagePaths.ExportDirectory());
            Assert.AreEqual(expected, SamplePackagePaths.ExportDirectory(expected + Path.DirectorySeparatorChar));
        }
        [TestCase("Assets")]
        [TestCase("Assets/GameMain/SubGame/HexaAway")]
        [TestCase("Assets/AAA_DevAssets")]
        [TestCase("Assets/AAA_DevAssets/Docs")]
        [TestCase("Assets/AAA_DevAssets/SamplePackages")]
        [TestCase("Assets/AAA_DevAssets/SamplePackages~Extra")]
        [TestCase("Assets/AAA_DevAssets/SamplePackages~/Nested")]
        [TestCase("Assets/AAA_DevAssets/SamplePackages~/../Docs")]
        [TestCase("Assets./AAA_DevAssets/SamplePackages~")]
        public void OtherAssetExportLocationsStayBlocked(string directory) =>
            Assert.Throws<InvalidOperationException>(() => SamplePackagePaths.ExportDirectory(directory));
        [TestCase("Backups/SamplePackages/DirectoryTest")]
        [TestCase("SamplePackages")]
        public void ExternalBackupAndLegacyExportDirectoriesRemainSupported(string directory) =>
            Assert.AreEqual(SamplePackagePaths.Full(directory), SamplePackagePaths.ExportDirectory(directory));

        [Test] public void OwnershipRequiresWholeDirectorySegment()
        {
            var manifest = Example(); manifest.Validate();
            Assert.IsTrue(manifest.Owns(manifest.Root + "/Scripts/Test.cs"));
            Assert.IsFalse(manifest.Owns(manifest.Root + "Extra/Test.cs"));
            Assert.IsFalse(manifest.Owns("Assets/GameMain/Audio/Sound/a.wav"));
        }
        [Test] public void UnsupportedTemplateAndUnsafeVersionAreRejected()
        {
            var manifest = Example(); manifest.version = "../bad";
            Assert.Throws<InvalidOperationException>(manifest.Validate);
            manifest.version = "1.0.0"; manifest.template = "other";
            Assert.Throws<InvalidOperationException>(manifest.Validate);
        }
        [Test] public void RowInstallAndRemoveAreIdempotentAndPreserveOthers()
        {
            var original = new[] { "# header", "\t10\tCommon\tCommonPanel\tDefault\tfalse\tfalse" };
            var own = new[] { "\t216\tGame\tSamplePanel\tDefault\tfalse\tfalse" };
            var installed = SamplePackageRegistration.MergeRows(original, own, true, false);
            Assert.AreEqual(3, installed.Length);
            CollectionAssert.AreEqual(installed, SamplePackageRegistration.MergeRows(installed, own, true, false));
            CollectionAssert.AreEqual(original, SamplePackageRegistration.MergeRows(installed, own, false, false));
            CollectionAssert.AreEqual(original, SamplePackageRegistration.MergeRows(original, own, false, false));
        }
        [TestCase("\t216\tOther\tSamplePanel\tDefault\tfalse\tfalse")]
        [TestCase("\t999\tGame\tSamplePanel\tDefault\tfalse\tfalse")]
        [TestCase("\t216\tGame\tOtherPanel\tDefault\tfalse\tfalse")]
        public void ModifiedOrCollidingRowsAreNotOverwritten(string existing)
        {
            var fragment = new[] { "\t216\tGame\tSamplePanel\tDefault\tfalse\tfalse" };
            Assert.Throws<InvalidOperationException>(() => SamplePackageRegistration.MergeRows(new[] { existing }, fragment, true, false));
            Assert.Throws<InvalidOperationException>(() => SamplePackageRegistration.MergeRows(new[] { existing }, fragment, false, false));
        }
        [Test] public void DuplicateRowsFail()
        {
            string row = "\t4\tGame\tScene\t0";
            Assert.Throws<InvalidOperationException>(() => SamplePackageRegistration.MergeRows(new[] { row, row }, new[] { row }, false, false));
        }
        [Test] public void ConfigKeysNotValuesDefineIdentity()
        {
            string one = "\tScene.One\tComment\t1", two = "\tScene.Two\tComment\t1";
            CollectionAssert.AreEqual(new[] { one, two }, SamplePackageRegistration.MergeRows(new[] { one }, new[] { two }, true, true));
        }
        [Test] public void ResourceFragmentsAreIdempotentAndDoNotMutateSource()
        {
            var source = XDocument.Parse("<UnityGameFramework><ResourceCollection><Resources/><Assets/></ResourceCollection></UnityGameFramework>");
            var manifest = Example();
            manifest.resourceDefinitions = new[] { "<Resource Name='HexaAway' Packed='True' />" };
            manifest.resourceAssets = new[] { "<Asset Guid='sample' ResourceName='HexaAway' />" };
            var installed = SamplePackageRegistration.MergeResources(source, manifest, true);
            Assert.AreEqual(0, source.Descendants("Asset").Count());
            Assert.AreEqual(1, SamplePackageRegistration.MergeResources(installed, manifest, true).Descendants("Asset").Count());
            Assert.AreEqual(0, SamplePackageRegistration.MergeResources(installed, manifest, false).Descendants("Asset").Count());
            installed.Descendants("Asset").Single().SetAttributeValue("ResourceName", "Other");
            Assert.Throws<InvalidOperationException>(() => SamplePackageRegistration.MergeResources(installed, manifest, false));
        }
        [Test] public void ForeignResourceInGameGroupPreventsRemoval()
        {
            var source = XDocument.Parse("<UnityGameFramework><ResourceCollection><Resources/><Assets><Asset Guid='foreign' ResourceName='HexaAway'/></Assets></ResourceCollection></UnityGameFramework>");
            Assert.Throws<InvalidOperationException>(() => SamplePackageRegistration.MergeResources(source, Example(), false));
        }
        [Test] public void MissingFileHashSurvivesJsonRoundTrip()
        {
            string hash = SamplePackagePaths.Hash(Path.Combine(Path.GetTempPath(), "GF-missing-" + Guid.NewGuid().ToString("N")));
            var saved = new SamplePackageBackupFile { path = "missing", beforeHash = hash };
            Assert.AreEqual(hash, JsonUtility.FromJson<SamplePackageBackupFile>(JsonUtility.ToJson(saved)).beforeHash);
        }
        [Test] public void LiveBuildSceneSnapshotsPreserveOrderEnabledAndEmptyState()
        {
            var snapshot = new SamplePackageJournal { originalBuildScenes = new[]
            {
                new SamplePackageBuildScene { path = "Assets/One.unity", enabled = true },
                new SamplePackageBuildScene { path = "Assets/Two.unity", enabled = false }
            } };
            var copy = JsonUtility.FromJson<SamplePackageJournal>(JsonUtility.ToJson(snapshot)).originalBuildScenes;
            Assert.IsTrue(SamplePackageService.SameBuildScenes(snapshot.originalBuildScenes, copy));
            Assert.IsFalse(SamplePackageService.SameBuildScenes(copy, copy.Reverse().ToArray()));
            copy[1].enabled = true;
            Assert.IsFalse(SamplePackageService.SameBuildScenes(snapshot.originalBuildScenes, copy));
            Assert.IsTrue(SamplePackageService.SameBuildScenes(Array.Empty<SamplePackageBuildScene>(), Array.Empty<SamplePackageBuildScene>()));
            Assert.IsFalse(SamplePackageService.SameBuildScenes(null, Array.Empty<SamplePackageBuildScene>()));
        }
        [TestCase("identical")]
        [TestCase("changed-file")]
        [TestCase("folder-conflict")]
        [TestCase("orphan-meta")]
        public void ExistingImportTargetsAreNeverSilentlyOverwritten(string kind)
        {
            string path = Path.Combine(Path.GetTempPath(), "GF-ImportTarget-" + Guid.NewGuid().ToString("N"));
            var file = new SamplePackageFile { path = "fixture", assetHash = SamplePackagePaths.Hash(Encoding.UTF8.GetBytes("content")), metaHash = "expected" };
            try
            {
                if (kind == "folder-conflict") Directory.CreateDirectory(path);
                else if (kind == "orphan-meta") File.WriteAllText(path + ".meta", "other-guid");
                else File.WriteAllText(path, kind == "identical" ? "content" : "user changes", new UTF8Encoding(false));
                if (kind == "identical") Assert.DoesNotThrow(() => SamplePackageService.CheckImportTarget(path, file));
                else Assert.Throws<InvalidOperationException>(() => SamplePackageService.CheckImportTarget(path, file));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
                if (Directory.Exists(path)) Directory.Delete(path); // 唯一且为空的测试目录，不递归删除。
            }
        }
        [TestCase("valid")]
        [TestCase("foreign-path")]
        [TestCase("duplicate")]
        [TestCase("link")]
        public void ActualArchivePathsAndMemberTypesAreChecked(string kind)
        {
            string file = Path.Combine(Path.GetTempPath(), "GF-PackageTest-" + Guid.NewGuid().ToString("N") + ".unitypackage");
            const string guid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            try
            {
                using (var target = File.Create(file))
                using (var zip = new GZipStream(target, CompressionMode.Compress))
                {
                    string path = kind == "foreign-path" ? "Assets/GameMain/UI/Foreign.txt" : "Assets/GameMain/SubGame/HexaAway/Test.txt";
                    TarMember(zip, guid + "/pathname", path, kind == "link" ? '2' : '0');
                    TarMember(zip, guid + "/asset.meta", "fileFormatVersion: 2\nguid: " + guid + "\n");
                    TarMember(zip, guid + "/asset", "fixture");
                    if (kind == "duplicate") TarMember(zip, guid + "/pathname", path);
                    zip.Write(new byte[1024], 0, 1024);
                }
                if (kind == "valid") Assert.AreEqual(1, SamplePackageArchive.Inspect(file, Example()).Length);
                else Assert.Throws<InvalidDataException>(() => SamplePackageArchive.Inspect(file, Example()));
            }
            finally { if (File.Exists(file)) File.Delete(file); } // 只删除此测试生成的唯一临时文件。
        }
        private static void TarMember(Stream target, string name, string text, char type = '0')
        {
            byte[] data = Encoding.UTF8.GetBytes(text), header = new byte[512];
            Encoding.ASCII.GetBytes(name).CopyTo(header, 0);
            Encoding.ASCII.GetBytes(Convert.ToString(data.Length, 8).PadLeft(11, '0') + "\0").CopyTo(header, 124);
            header[156] = (byte)type;
            for (int i = 148; i < 156; i++) header[i] = 32;
            Encoding.ASCII.GetBytes(Convert.ToString(header.Sum(value => (int)value), 8).PadLeft(6, '0') + "\0 ").CopyTo(header, 148);
            target.Write(header, 0, header.Length); target.Write(data, 0, data.Length);
            int padding = (512 - data.Length % 512) % 512; target.Write(new byte[padding], 0, padding);
        }
        [Test] public void SaveModulesAreExplicitAndDuplicateSafe()
        {
            var store = new SaveDataStore();
            Assert.IsNull(store.Get<PackageTestSave>());
            var data = new PackageTestSave(); store.Register(data); store.Register(data);
            Assert.AreSame(data, store.Get<PackageTestSave>());
            Assert.AreEqual(0, data.Loads);
            Assert.Throws<InvalidOperationException>(() => store.Register(new PackageTestSave()));
        }
        [Test] public void LateSaveModuleLoadsOnceAndFailedLoadIsNotRegistered()
        {
            var store = new SaveDataStore();
            // 不触碰真实 PlayerPrefs；只模拟 Store 已完成公共数据加载的状态。
            typeof(SaveDataStore).GetField("m_IsLoaded", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(store, true);
            var data = new PackageTestSave { Fail = true };
            Assert.Throws<InvalidOperationException>(() => store.Register(data));
            Assert.IsNull(store.Get<PackageTestSave>());
            data.Fail = false; store.Register(data); store.Register(data);
            Assert.AreEqual(2, data.Loads);
        }
        [Test] public void RuntimeRegistrySupportsZeroSingleDoubleAndAtomicFailure()
        {
            var oneObject = new GameObject("Registry test one");
            var twoObject = new GameObject("Registry test two");
            try
            {
                var one = oneObject.AddComponent<PackageTestManager>(); one.Mode = (GameMode)100;
                var two = twoObject.AddComponent<PackageTestManager>(); two.Mode = (GameMode)101;
                var registry = new SubGameRuntimeRegistry();
                registry.Rebuild(null); Assert.AreEqual(0, registry.Installed.Count);
                registry.Rebuild(new[] { one }); Assert.AreSame(one, registry.Get(one.Mode));
                registry.Rebuild(new[] { one, two }); Assert.AreEqual(2, registry.Installed.Count);
                Assert.Throws<InvalidOperationException>(() => registry.Rebuild(new[] { one, one }));
                Assert.AreSame(two, registry.Get(two.Mode));
                Assert.Throws<InvalidOperationException>(() => registry.Rebuild(new SubGameManagerComponent[] { null }));
                one.Mode = GameMode.None;
                Assert.Throws<InvalidOperationException>(() => registry.Rebuild(new[] { one }));
            }
            finally { UnityEngine.Object.DestroyImmediate(oneObject); UnityEngine.Object.DestroyImmediate(twoObject); }
        }
        private sealed class PackageTestSave : IGameSaveData
        {
            public int Loads; public bool Fail;
            public void Load() { Loads++; if (Fail) throw new InvalidOperationException("test"); }
            public void Save() { }
        }
    }
    public sealed class PackageTestManager : SubGameManagerComponent
    {
        public GameMode Mode;
        public override GameMode GameMode => Mode;
        public override string SceneConfigKey => "Scene.Test";
        public override Type GameProcedureType => typeof(PackageTestProcedure);
        protected override void Awake() { } // 测试对象不注册到全局 GF 组件容器。
    }
    public sealed class PackageTestProcedure : ProcedureGame { }
}
