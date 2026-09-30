using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    /// <summary>安装到项目的活动清单。清单只负责装配和资源声明，不承载活动规则。</summary>
    [CreateAssetMenu(fileName = "ActivityModuleCatalog", menuName = "GF/Activity/Module Catalog")]
    public sealed class ActivityModuleCatalogConfig : ScriptableObject
    {
        public const string AssetName = "ActivitySystem/ActivityModuleCatalog";

        [SerializeField] private ActivityModuleDefinition[] m_Modules = Array.Empty<ActivityModuleDefinition>();

        public IReadOnlyList<ActivityModuleDefinition> Modules => m_Modules;

#if UNITY_EDITOR
        public void SetModules(ActivityModuleDefinition[] modules)
        {
            m_Modules = modules ?? Array.Empty<ActivityModuleDefinition>();
        }
#endif
    }

    /// <summary>活动模块自己的安装定义。具体活动继承它来创建自己的业务模块。</summary>
    public abstract class ActivityModuleDefinition : ScriptableObject, IActivityModuleFactory
    {
        [SerializeField] private string m_ModuleId;
        [SerializeField] private string[] m_GameIds = Array.Empty<string>();
        [SerializeField] private ActivityPageDefinition[] m_Pages = Array.Empty<ActivityPageDefinition>();

        public string ModuleId => m_ModuleId;
        public IReadOnlyList<string> GameIds => m_GameIds;
        public IReadOnlyList<ActivityPageDefinition> Pages => m_Pages;

        public abstract IActivityModule CreateModule();

        internal void ValidateDefinition()
        {
            ActivityContract.RequireId(m_ModuleId, nameof(m_ModuleId));
            if (m_Pages == null || m_Pages.Length == 0)
                throw new InvalidOperationException($"Activity module '{m_ModuleId}' declares no pages.");

            var pageKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (ActivityPageDefinition page in m_Pages)
            {
                page.Validate(m_ModuleId);
                if (!pageKeys.Add(page.PageKey))
                    throw new InvalidOperationException($"Activity module '{m_ModuleId}' repeats page key '{page.PageKey}'.");
            }
        }

#if UNITY_EDITOR
        public void SetInstallation(string moduleId, string[] gameIds, ActivityPageDefinition[] pages)
        {
            m_ModuleId = ActivityContract.RequireId(moduleId, nameof(moduleId));
            m_GameIds = gameIds ?? Array.Empty<string>();
            m_Pages = pages ?? Array.Empty<ActivityPageDefinition>();
        }
#endif
    }

    [Serializable]
    public struct ActivityPageDefinition
    {
        [SerializeField] private string m_PageKey;
        [SerializeField] private int m_UIFormId;
        [SerializeField] private string m_AssetName;
        [SerializeField] private string m_PrefabAssetPath;
        [SerializeField] private string[] m_AssetNameAliases;

        public string PageKey => m_PageKey;
        public int UIFormId => m_UIFormId;
        public string AssetName => m_AssetName;
        public string PrefabAssetPath => m_PrefabAssetPath;
        public IReadOnlyList<string> AssetNameAliases => m_AssetNameAliases ?? Array.Empty<string>();

        public ActivityPageDefinition(string pageKey, int uiFormId, string assetName, string prefabAssetPath,
            string[] assetNameAliases = null)
        {
            m_PageKey = ActivityContract.RequireId(pageKey, nameof(pageKey));
            if (!UIFormIdRanges.IsActivityId(uiFormId)) throw new ArgumentOutOfRangeException(nameof(uiFormId));
            m_UIFormId = uiFormId;
            m_AssetName = ActivityContract.RequireId(assetName, nameof(assetName));
            m_PrefabAssetPath = ActivityContract.RequireId(prefabAssetPath, nameof(prefabAssetPath));
            m_AssetNameAliases = assetNameAliases ?? Array.Empty<string>();
        }

        internal void Validate(string moduleId)
        {
            ActivityContract.RequireId(m_PageKey, nameof(m_PageKey));
            if (!UIFormIdRanges.IsActivityId(m_UIFormId))
                throw new InvalidOperationException($"Activity module '{moduleId}' has an invalid UI form ID on page '{m_PageKey}'.");
            ActivityContract.RequireId(m_AssetName, nameof(m_AssetName));
            ActivityContract.RequireId(m_PrefabAssetPath, nameof(m_PrefabAssetPath));
            foreach (string alias in AssetNameAliases)
            {
                ActivityContract.RequireId(alias, nameof(m_AssetNameAliases));
                if (string.Equals(alias, m_AssetName, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Activity module '{moduleId}' page '{m_PageKey}' repeats its primary asset name as an alias.");
            }
            if (!m_PrefabAssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Activity module '{moduleId}' page '{m_PageKey}' must reference a prefab asset.");
        }
    }

    /// <summary>运行时仅保存已安装页面的资源映射；资源名不能由调用上下文推断所属模块。</summary>
    public static class ActivityPageRegistry
    {
        private static readonly Dictionary<string, ActivityPageDefinition> s_Pages = new Dictionary<string, ActivityPageDefinition>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> s_Prefabs = new Dictionary<string, string>(StringComparer.Ordinal);

        public static void Install(ActivityModuleCatalogConfig catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var pages = new Dictionary<string, ActivityPageDefinition>(StringComparer.Ordinal);
            var prefabs = new Dictionary<string, string>(StringComparer.Ordinal);
            var uiFormIds = new Dictionary<int, string>();
            foreach (ActivityModuleDefinition definition in catalog.Modules)
            {
                if (definition == null) throw new InvalidOperationException("Activity module catalog contains a null definition.");
                definition.ValidateDefinition();
                foreach (ActivityPageDefinition page in definition.Pages)
                {
                    string pageId = BuildPageId(definition.ModuleId, page.PageKey);
                    if (!pages.TryAdd(pageId, page))
                        throw new InvalidOperationException($"Activity page '{pageId}' is registered more than once.");
                    if (!uiFormIds.TryAdd(page.UIFormId, pageId))
                        throw new InvalidOperationException(
                            $"UIForm ID '{page.UIFormId}' is shared by activity pages '{uiFormIds[page.UIFormId]}' and '{pageId}'.");
                    if (!prefabs.TryAdd(page.AssetName, page.PrefabAssetPath))
                        throw new InvalidOperationException($"Activity UI asset name '{page.AssetName}' is registered more than once.");
                    foreach (string alias in page.AssetNameAliases)
                    {
                        if (!prefabs.TryAdd(alias, page.PrefabAssetPath))
                            throw new InvalidOperationException($"Activity UI asset alias '{alias}' is registered more than once.");
                    }
                }
            }

            s_Pages.Clear();
            s_Prefabs.Clear();
            foreach (KeyValuePair<string, ActivityPageDefinition> item in pages) s_Pages.Add(item.Key, item.Value);
            foreach (KeyValuePair<string, string> item in prefabs) s_Prefabs.Add(item.Key, item.Value);
        }

        public static void Clear()
        {
            s_Pages.Clear();
            s_Prefabs.Clear();
        }

        public static bool TryGetPage(string moduleId, string pageKey, out ActivityPageDefinition page)
        {
            return s_Pages.TryGetValue(BuildPageId(moduleId, pageKey), out page);
        }

        public static bool TryGetPrefabAsset(string assetName, out string prefabAssetPath)
        {
            return s_Prefabs.TryGetValue(assetName, out prefabAssetPath);
        }

        private static string BuildPageId(string moduleId, string pageKey)
        {
            return ActivityContract.RequireId(moduleId, nameof(moduleId)) + "/" + ActivityContract.RequireId(pageKey, nameof(pageKey));
        }
    }
}
