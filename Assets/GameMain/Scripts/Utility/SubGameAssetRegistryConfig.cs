using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    [CreateAssetMenu(fileName = AssetName, menuName = "GF/SubGame/Asset Registry Config")]
    public sealed class SubGameAssetRegistryConfig : ScriptableObject
    {
        public const string AssetName = "SubGameAssetRegistryConfig";
        public const string DefaultAssetPath = "Assets/GameMain/ScriptableObjects/SubGameAssetRegistryConfig.asset";

        [SerializeField] private List<SubGameAssetManifest> m_Manifests = new();

        // 仅供旧资产迁移，运行时不再将内嵌条目当作第二份注册来源。
        [SerializeField, HideInInspector] private List<SubGameAssetRegistryEntry> m_Entries = new();
        public IReadOnlyList<SubGameAssetManifest> Manifests => m_Manifests;
        public bool HasLegacyEntries => m_Entries != null && m_Entries.Count > 0;

        public void RegisterAll() => SubGameAssetRegistry.RegisterConfig(this);

        public void SetManifests(IEnumerable<SubGameAssetManifest> manifests)
        {
            if (manifests == null) throw new ArgumentNullException(nameof(manifests));
            var entries = new List<SubGameAssetManifest>(manifests);
            SubGameAssetRegistry.ValidateManifests(entries);
            m_Manifests = entries;
        }

#if UNITY_EDITOR
        public IReadOnlyList<SubGameAssetRegistryEntry> LegacyEntries => m_Entries;
        public void CompleteLegacyMigration(IEnumerable<SubGameAssetManifest> manifests)
        {
            SetManifests(manifests);
            m_Entries.Clear();
        }
#endif
    }

    [Serializable]
    public sealed class SubGameAssetRegistryEntry
    {
        [SerializeField] private string m_GameName;
        [SerializeField] private List<string> m_UIForms = new List<string>();
        [SerializeField] private List<string> m_Scenes = new List<string>();
        [SerializeField] private List<SubGameScriptableObjectRegistryEntry> m_ScriptableObjects = new List<SubGameScriptableObjectRegistryEntry>();

        public SubGameAssetRegistryEntry() { }

        public SubGameAssetRegistryEntry(string gameName, IEnumerable<string> uiForms, IEnumerable<string> scenes,
            IEnumerable<SubGameScriptableObjectRegistryEntry> scriptableObjects)
        {
            m_GameName = gameName;
            m_UIForms = new List<string>(uiForms ?? Array.Empty<string>());
            m_Scenes = new List<string>(scenes ?? Array.Empty<string>());
            m_ScriptableObjects = new List<SubGameScriptableObjectRegistryEntry>(
                scriptableObjects ?? Array.Empty<SubGameScriptableObjectRegistryEntry>());
        }

        public string GameName => m_GameName;
        public IReadOnlyList<string> UIForms => m_UIForms;
        public IReadOnlyList<string> Scenes => m_Scenes;
        public IReadOnlyList<SubGameScriptableObjectRegistryEntry> ScriptableObjects => m_ScriptableObjects;
        public bool IsValid => !string.IsNullOrEmpty(m_GameName);
    }

    [Serializable]
    public sealed class SubGameScriptableObjectRegistryEntry
    {
        [SerializeField] private string m_AssetName;
        [SerializeField] private string m_Category;

        public SubGameScriptableObjectRegistryEntry() { }
        public SubGameScriptableObjectRegistryEntry(string assetName, string category)
        {
            m_AssetName = assetName;
            m_Category = category;
        }

        public string AssetName => m_AssetName;
        public string Category => m_Category;
        public bool IsValid => !string.IsNullOrEmpty(m_AssetName) && !string.IsNullOrEmpty(m_Category);
    }
}
