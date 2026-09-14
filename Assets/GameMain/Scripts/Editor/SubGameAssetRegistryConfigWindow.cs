using System;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    public sealed class SubGameAssetRegistryConfigWindow : EditorWindow
    {
        private SubGameAssetRegistryConfig m_Config;
        private UnityEditor.Editor m_ConfigEditor;
        private Vector2 m_Scroll;

        [MenuItem("Tools/SubGame/Asset Registry Config")]
        public static void Open() => GetWindow<SubGameAssetRegistryConfigWindow>("SubGame Assets");

        private void OnEnable() => LoadConfig();
        private void OnDisable()
        {
            if (m_ConfigEditor != null) DestroyImmediate(m_ConfigEditor);
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton)) LoadConfig();
                if (GUILayout.Button("Select", EditorStyles.toolbarButton)) Selection.activeObject = m_Config;
                GUILayout.FlexibleSpace();
            }

            if (m_Config == null)
            {
                EditorGUILayout.HelpBox("Global subgame index does not exist.", MessageType.Warning);
                if (GUILayout.Button("Create Empty Index")) CreateConfig();
                return;
            }

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            EditorGUILayout.HelpBox("全局只维护已安装清单引用。游戏资源明细在各自的 ScriptableObjects/Registry/ 中编辑。空列表合法，Missing 引用不合法。", MessageType.Info);
            if (m_Config.HasLegacyEntries)
            {
                EditorGUILayout.HelpBox("检测到旧内嵌条目，运行前必须迁移。工具会先导出备份到项目 Backups/，且不会覆盖已有清单。", MessageType.Warning);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("Back Up and Migrate Legacy Entries"))
                    {
                        try
                        {
                            string backup = SubGameAssetRegistryMigration.Migrate(m_Config);
                            Debug.Log("Subgame manifests migrated. Root backup: " + backup);
                        }
                        catch (Exception exception) { Debug.LogException(exception); }
                    }
                }
            }

            UnityEditor.Editor.CreateCachedEditor(m_Config, null, ref m_ConfigEditor);
            m_ConfigEditor.OnInspectorGUI();

            try
            {
                SubGameAssetRegistry.ValidateManifests(m_Config.Manifests);
                EditorGUILayout.HelpBox("清单引用与逻辑键校验通过（不替代 DataTable / Build Settings / 资源构建检查）。", MessageType.Info);
            }
            catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }

            if (m_Config.Manifests != null)
                foreach (var manifest in m_Config.Manifests)
                {
                    if (manifest == null) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(manifest.GameName, manifest, typeof(SubGameAssetManifest), false);
                        if (GUILayout.Button("Edit", GUILayout.Width(45)))
                        {
                            Selection.activeObject = manifest;
                            EditorGUIUtility.PingObject(manifest);
                        }
                    }
                }
            EditorGUILayout.EndScrollView();
        }

        private void LoadConfig()
        {
            m_Config = AssetDatabase.LoadAssetAtPath<SubGameAssetRegistryConfig>(SubGameAssetRegistryConfig.DefaultAssetPath);
            if (m_ConfigEditor != null) DestroyImmediate(m_ConfigEditor);
            m_ConfigEditor = null;
        }

        private void CreateConfig()
        {
            const string folder = "Assets/GameMain/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/GameMain", "ScriptableObjects");
            m_Config = CreateInstance<SubGameAssetRegistryConfig>();
            AssetDatabase.CreateAsset(m_Config, SubGameAssetRegistryConfig.DefaultAssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = m_Config;
        }
    }
}
