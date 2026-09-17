using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    [CustomEditor(typeof(ActivityComponent))]
    public sealed class ActivityComponentEditor : UnityEditor.Editor
    {
        private bool m_ShowConfiguredModules = true;
        private bool m_ShowRuntimeModules = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CatalogAssetName"),
                new GUIContent("Catalog Asset Name", "活动目录的 GF ScriptableObject 逻辑资源名。"));
            DrawRewardReceiverSelector(serializedObject.FindProperty("m_RewardReceiverTypeName"));
            serializedObject.ApplyModifiedProperties();

            var component = (ActivityComponent)target;
            ActivityModuleCatalogConfig catalog = FindCatalog(component.CatalogAssetName);

            EditorGUILayout.Space();
            m_ShowConfiguredModules = EditorGUILayout.Foldout(m_ShowConfiguredModules,
                catalog == null ? "Configured Activity Modules" : $"Configured Activity Modules ({catalog.Modules.Count})", true);
            if (m_ShowConfiguredModules)
                DrawConfiguredModules(catalog);

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                m_ShowRuntimeModules = EditorGUILayout.Foldout(m_ShowRuntimeModules, "Runtime Module Status", true);
                if (m_ShowRuntimeModules) DrawRuntimeModules(component);
            }
        }

        private static void DrawRewardReceiverSelector(SerializedProperty receiverTypeName)
        {
            var typeNames = new List<string> { "None (LogActivityRewardReceiver)" };
            typeNames.AddRange(TypeCache.GetTypesDerivedFrom<IActivityRewardReceiver>()
                .Where(type => type.IsClass && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => type.FullName).OrderBy(name => name, StringComparer.Ordinal));

            int selected = Math.Max(0, typeNames.IndexOf(receiverTypeName.stringValue));
            int next = EditorGUILayout.Popup(new GUIContent("Reward Receiver", "领取时实际接收奖励的实现类型。"), selected, typeNames.ToArray());
            if (next != selected)
                receiverTypeName.stringValue = next == 0 ? string.Empty : typeNames[next];
        }

        private static ActivityModuleCatalogConfig FindCatalog(string catalogAssetName)
        {
            if (string.IsNullOrWhiteSpace(catalogAssetName)) return null;

            // 全局活动目录走现有 ScriptableObject 资源根目录；先按逻辑名精确定位，
            // 再回退到名称搜索以支持未来由其他资源域提供的目录。
            string directPath = $"Assets/GameMain/ScriptableObjects/{catalogAssetName}.asset";
            ActivityModuleCatalogConfig directCatalog = AssetDatabase.LoadAssetAtPath<ActivityModuleCatalogConfig>(directPath);
            if (directCatalog != null) return directCatalog;

            string expectedAssetName = Path.GetFileName(catalogAssetName);
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ActivityModuleCatalogConfig)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), expectedAssetName, StringComparison.Ordinal)) continue;

                ActivityModuleCatalogConfig catalog = AssetDatabase.LoadAssetAtPath<ActivityModuleCatalogConfig>(path);
                if (catalog != null) return catalog;
            }

            return null;
        }

        private static void DrawConfiguredModules(ActivityModuleCatalogConfig catalog)
        {
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("No ActivityModuleCatalogConfig matches the configured logical asset name. " +
                    "Create or restore the catalog before entering Play mode.", MessageType.Warning);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Catalog", catalog, typeof(ActivityModuleCatalogConfig), false);

            if (catalog.Modules.Count == 0)
            {
                EditorGUILayout.HelpBox("The catalog contains no activity modules.", MessageType.Info);
                return;
            }

            foreach (ActivityModuleDefinition definition in catalog.Modules)
            {
                if (definition == null)
                {
                    EditorGUILayout.HelpBox("The catalog contains a missing activity module definition.", MessageType.Error);
                    continue;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(definition, typeof(ActivityModuleDefinition), false);
                    EditorGUILayout.LabelField("Module ID", definition.ModuleId);
                    EditorGUILayout.LabelField("Game IDs", FormatGameIds(definition));
                    EditorGUILayout.LabelField("Pages", definition.Pages.Count.ToString());
                }
            }

            EditorGUILayout.HelpBox("This list is read-only. Edit the module definitions in the activity catalog; " +
                "ProcedurePreload will load and install them through the normal GF resource flow.", MessageType.None);
        }

        private static void DrawRuntimeModules(ActivityComponent component)
        {
            if (component.Host == null)
            {
                EditorGUILayout.HelpBox("The activity host has not been initialized.", MessageType.Info);
                return;
            }

            var modules = component.Host.GetModules();
            if (modules.Count == 0)
            {
                EditorGUILayout.HelpBox("No activity modules are installed yet.", MessageType.Info);
                return;
            }

            foreach (ActivityModuleSnapshot module in modules)
            {
                string status = $"{module.ModuleId}  •  {module.State}  •  generation {module.Generation}";
                EditorGUILayout.LabelField(status, EditorStyles.boldLabel);
                if (module.LastError != null)
                    EditorGUILayout.HelpBox(module.LastError.Message, MessageType.Error);
            }
        }

        private static string FormatGameIds(ActivityModuleDefinition definition)
        {
            if (definition.GameIds == null || definition.GameIds.Count == 0) return "None";
            return string.Join(", ", definition.GameIds);
        }
    }
}
