using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor.Tests
{
    public sealed class SubGameAssetRegistryTests
    {
        private readonly List<UnityEngine.Object> m_Created = new();

        [SetUp]
        public void SetUp() => SubGameAssetRegistry.Clear();

        [TearDown]
        public void TearDown()
        {
            SubGameAssetRegistry.Clear();
            foreach (var item in m_Created) UnityEngine.Object.DestroyImmediate(item);
            m_Created.Clear();
        }

        private SubGameAssetManifest Manifest(string game, string ui = null, string scene = null, string so = null)
        {
            var result = ScriptableObject.CreateInstance<SubGameAssetManifest>();
            m_Created.Add(result);
            result.name = game + "Manifest";
            result.Initialize(new SubGameAssetRegistryEntry(game,
                ui == null ? Array.Empty<string>() : new[] { ui },
                scene == null ? Array.Empty<string>() : new[] { scene },
                so == null ? Array.Empty<SubGameScriptableObjectRegistryEntry>() :
                    new[] { new SubGameScriptableObjectRegistryEntry(so, "Database") }));
            return result;
        }

        [Test]
        public void EmptyIndexIsValidAndClearsPreviousGameMappings()
        {
            SubGameAssetRegistry.Rebuild(new[] { Manifest("One", "Panel") });
            SubGameAssetRegistry.Rebuild(Array.Empty<SubGameAssetManifest>());
            Assert.IsFalse(SubGameAssetRegistry.TryGetUIFormGameName("Panel", out _));
            Assert.IsFalse(SubGameAssetRegistry.TryParseScopedAssetName("One/Data", out _, out _));
        }

        [Test]
        public void MissingManifestIsAnErrorNotAnEmptyTemplate()
        {
            Assert.Throws<InvalidOperationException>(() =>
                SubGameAssetRegistry.Rebuild(new SubGameAssetManifest[] { null }));
        }

        [Test]
        public void DuplicateOwnerAndDuplicateKeysAreRejected()
        {
            var one = Manifest("One", "Panel", "Scene", "Config");
            Assert.Throws<InvalidOperationException>(() => SubGameAssetRegistry.Rebuild(new[] { one, one }));
            Assert.Throws<InvalidOperationException>(() =>
                SubGameAssetRegistry.Rebuild(new[] { one, Manifest("Two", "Panel") }));
            Assert.Throws<InvalidOperationException>(() =>
                SubGameAssetRegistry.Rebuild(new[] { one, Manifest("Two", scene: "Scene") }));
            Assert.Throws<InvalidOperationException>(() =>
                SubGameAssetRegistry.Rebuild(new[] { one, Manifest("Two", so: "Config") }));
        }

        [Test]
        public void DuplicateWithinSingleManifestIsRejected()
        {
            var one = Manifest("One");
            one.Initialize(new SubGameAssetRegistryEntry("One", new[] { "Panel", "Panel" }, null, null));
            Assert.Throws<InvalidOperationException>(() => SubGameAssetRegistry.Rebuild(new[] { one }));
        }

        [Test]
        public void FailedRebuildKeepsOriginalCompleteIndex()
        {
            SubGameAssetRegistry.Rebuild(new[] { Manifest("One", "OldPanel") });
            Assert.Throws<InvalidOperationException>(() =>
                SubGameAssetRegistry.Rebuild(new[] { Manifest("Two", "NewPanel"), null }));
            Assert.IsTrue(SubGameAssetRegistry.TryGetUIFormGameName("OldPanel", out var owner));
            Assert.AreEqual("One", owner);
            Assert.IsFalse(SubGameAssetRegistry.TryGetUIFormGameName("NewPanel", out _));
        }

        [Test]
        public void RemovingOneOwnerDoesNotAffectOtherDomains()
        {
            SubGameAssetRegistry.Rebuild(new[] { Manifest("One", "OnePanel", "OneScene", "OneConfig"),
                Manifest("Two", "TwoPanel", "TwoScene", "TwoConfig") });
            Assert.IsTrue(SubGameAssetRegistry.UnregisterSubGame("One"));
            Assert.IsFalse(SubGameAssetRegistry.UnregisterSubGame("One"));
            Assert.IsFalse(SubGameAssetRegistry.TryGetUIFormGameName("OnePanel", out _));
            Assert.IsFalse(SubGameAssetRegistry.TryGetSceneGameName("OneScene", out _));
            Assert.IsFalse(SubGameAssetRegistry.TryGetScriptableObjectLocation("OneConfig", out _, out _));
            Assert.IsTrue(SubGameAssetRegistry.TryGetUIFormGameName("TwoPanel", out var owner));
            Assert.AreEqual("Two", owner);
            Assert.IsTrue(SubGameAssetRegistry.TryGetScriptableObjectLocation("TwoConfig", out owner, out var category));
            Assert.AreEqual("Database", category);
        }

        [Test]
        public void RepeatedRebuildIsIdempotentAndRemovesOldMappings()
        {
            var one = Manifest("One", "OldPanel");
            SubGameAssetRegistry.Rebuild(new[] { one });
            SubGameAssetRegistry.Rebuild(new[] { one });
            one.Initialize(new SubGameAssetRegistryEntry("One", new[] { "NewPanel" }, null, null));
            SubGameAssetRegistry.Rebuild(new[] { one });
            Assert.IsFalse(SubGameAssetRegistry.TryGetUIFormGameName("OldPanel", out _));
            Assert.IsTrue(SubGameAssetRegistry.TryGetUIFormGameName("NewPanel", out _));
        }

        [Test]
        public void IncrementalRegistrationCannotOverwriteAnotherOwner()
        {
            SubGameAssetRegistry.RegisterUIForm("Panel", "One");
            SubGameAssetRegistry.RegisterUIForm("Panel", "One");
            Assert.Throws<InvalidOperationException>(() => SubGameAssetRegistry.RegisterUIForm("Panel", "Two"));
            SubGameAssetRegistry.RegisterScriptableObject("Config", "One", "Database");
            Assert.Throws<InvalidOperationException>(() => SubGameAssetRegistry.RegisterScriptableObject("Config", "One", "Rule"));
        }

        [Test]
        public void InvalidOwnerPathCannotEscapeGameDirectory()
        {
            Assert.Throws<InvalidOperationException>(() =>
                SubGameAssetRegistry.ValidateManifests(new[] { Manifest("../Shared") }));
        }

        [Test]
        public void SavedProjectIndexContainsOnlyValidGameOwnedManifests()
        {
            var root = AssetDatabase.LoadAssetAtPath<SubGameAssetRegistryConfig>(SubGameAssetRegistryConfig.DefaultAssetPath);
            Assert.IsNotNull(root);
            Assert.IsFalse(root.HasLegacyEntries, "Run the resource manifest migration first.");
            var dependencies = AssetDatabase.GetDependencies(SubGameAssetRegistryConfig.DefaultAssetPath, true);
            SubGameAssetRegistry.RegisterConfig(root);
            foreach (var manifest in root.Manifests)
            {
                var path = AssetDatabase.GetAssetPath(manifest);
                StringAssert.StartsWith("Assets/GameMain/SubGame/" + manifest.GameName + "/ScriptableObjects/Registry/", path);
                CollectionAssert.Contains(dependencies, path);
                foreach (var panel in manifest.Entry.UIForms)
                {
                    Assert.IsTrue(SubGameAssetRegistry.TryGetUIFormGameName(panel, out var owner));
                    Assert.AreEqual(manifest.GameName, owner);
                }
                foreach (var scene in manifest.Entry.Scenes)
                {
                    Assert.IsTrue(SubGameAssetRegistry.TryGetSceneGameName(scene, out var owner));
                    Assert.AreEqual(manifest.GameName, owner);
                }
                foreach (var so in manifest.Entry.ScriptableObjects)
                {
                    Assert.IsTrue(SubGameAssetRegistry.TryGetScriptableObjectLocation(so.AssetName, out var owner, out var category));
                    Assert.AreEqual(manifest.GameName, owner);
                    Assert.AreEqual(so.Category, category);
                }
            }
        }
    }
}

