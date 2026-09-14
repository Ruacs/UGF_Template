using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    /// <summary>一次性迁移旧内嵌条目，先备份和验证，再改变全局索引。</summary>
    public static class SubGameAssetRegistryMigration
    {
        public static string Migrate(SubGameAssetRegistryConfig config)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before migrating assets.");
            if (config == null || !config.HasLegacyEntries)
                throw new InvalidOperationException("No legacy entries to migrate.");
            if (config.Manifests == null || config.Manifests.Count != 0)
                throw new InvalidOperationException("Mixed legacy entries and manifests require manual reconciliation.");

            var manifests = new List<SubGameAssetManifest>();
            var paths = new List<string>();
            try
            {
                foreach (var entry in config.LegacyEntries)
                {
                    var manifest = ScriptableObject.CreateInstance<SubGameAssetManifest>();
                    manifests.Add(manifest);
                    manifest.Initialize(entry);
                }

                // 先做完整检查，包括重复 Key 和路径片段，不能边迁移边忽略坏数据。
                SubGameAssetRegistry.ValidateManifests(manifests);
                foreach (var manifest in manifests)
                {
                    string root = "Assets/GameMain/SubGame/" + manifest.GameName;
                    if (!AssetDatabase.IsValidFolder(root))
                        throw new InvalidOperationException("Game folder is missing: " + root);
                    string path = root + "/ScriptableObjects/Registry/" + manifest.GameName + "AssetRegistryConfig.asset";
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path))
                        throw new InvalidOperationException("Refusing to overwrite existing manifest: " + path);
                    paths.Add(path);
                }

                string configPath = AssetDatabase.GetAssetPath(config);
                if (string.IsNullOrEmpty(configPath)) throw new InvalidOperationException("Save the root config before migration.");
                string backupFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Backups"));
                Directory.CreateDirectory(backupFolder);
                string backupPath = Path.Combine(backupFolder, "SubGameRegistry-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".unitypackage");
                AssetDatabase.ExportPackage(configPath, backupPath, ExportPackageOptions.Default);

                for (int i = 0; i < manifests.Count; i++)
                {
                    string gameRoot = "Assets/GameMain/SubGame/" + manifests[i].GameName;
                    EnsureFolder(gameRoot, "ScriptableObjects");
                    EnsureFolder(gameRoot + "/ScriptableObjects", "Registry");
                    AssetDatabase.CreateAsset(manifests[i], paths[i]);
                }
                AssetDatabase.SaveAssets();

                // 隔离程序化迁移的 Undo 组，避免后续 EditMode 测试的清理回滚本次已保存资产。
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Migrate subgame resource manifests");
                try
                {
                    Undo.RecordObject(config, "Migrate subgame resource manifests");
                    config.CompleteLegacyMigration(manifests);
                    EditorUtility.SetDirty(config);
                    Undo.FlushUndoRecordObjects();
                    AssetDatabase.SaveAssetIfDirty(config);
                    Undo.CollapseUndoOperations(undoGroup);
                }
                finally { Undo.IncrementCurrentGroup(); }
                return backupPath;
            }
            finally
            {
                // 已落盘的新资产在异常时保留，避免删除用户数据；原索引在验证成功前不会被清空。
                foreach (var manifest in manifests)
                    if (manifest != null && !EditorUtility.IsPersistent(manifest))
                        UnityEngine.Object.DestroyImmediate(manifest);
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
