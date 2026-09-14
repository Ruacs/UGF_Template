using GameFramework.Resource;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor
{
    public sealed class ResourceExtensionSettingsWindow : EditorWindow
    {
        private const string ConfigAssetPath = "Assets/Plugins/UnityGameFramework/GameFramework/Resource/ResourceExtensionConfig.cs";
        private const string RecommendedExtension = "gfres";
        private static readonly Regex s_ValidExtensionRegex = new Regex("^[a-z0-9]{1,16}$", RegexOptions.Compiled);

        private string m_Extension;
        private string m_StreamingAssetsSummary;

        [MenuItem("Tools/Resource/Resource Extension Settings")]
        public static void Open()
        {
            ResourceExtensionSettingsWindow window = GetWindow<ResourceExtensionSettingsWindow>("Resource Extension");
            window.minSize = new Vector2(500f, 260f);
            window.Show();
        }

        private void OnEnable()
        {
            m_Extension = ResourceExtensionConfig.DefaultExtension;
            RefreshStreamingAssetsSummary();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("GF 资源后缀设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("当前后缀", ResourceExtensionConfig.DefaultExtension);
            }

            m_Extension = EditorGUILayout.TextField("新后缀", m_Extension);
            string normalizedExtension = NormalizeExtension(m_Extension);

            if (!IsValidExtension(normalizedExtension))
            {
                EditorGUILayout.HelpBox("后缀只能包含 1～16 个小写英文字母或数字，不需要输入点号。", MessageType.Error);
            }
            else if (string.Equals(normalizedExtension, "dat", StringComparison.Ordinal))
            {
                EditorGUILayout.HelpBox("使用 .dat 会让 Android 将 global-metadata.dat 一并设为不压缩，不建议用于 GF 资源。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("修改后必须重新构建全部 GF 资源。现有 StreamingAssets 文件不会被此工具直接重命名。", MessageType.Info);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("当前 StreamingAssets", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(m_StreamingAssetsSummary, MessageType.None);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("使用推荐值", GUILayout.Height(28f)))
                {
                    m_Extension = RecommendedExtension;
                }

                using (new EditorGUI.DisabledScope(!IsValidExtension(normalizedExtension)
                                                    || string.Equals(normalizedExtension, ResourceExtensionConfig.DefaultExtension, StringComparison.Ordinal)))
                {
                    if (GUILayout.Button("应用后缀", GUILayout.Height(28f)))
                    {
                        ApplyExtension(normalizedExtension);
                    }
                }

                if (GUILayout.Button("刷新检查", GUILayout.Height(28f)))
                {
                    RefreshStreamingAssetsSummary();
                }
            }
        }

        private void ApplyExtension(string extension)
        {
            string oldExtension = ResourceExtensionConfig.DefaultExtension;
            string warning = $"资源后缀将从 .{oldExtension} 修改为 .{extension}。\n\n"
                             + "修改后必须重新构建全部 GF 资源；旧资源包、旧缓存和旧版本表不会自动迁移。是否继续？";
            if (!EditorUtility.DisplayDialog("修改 GF 资源后缀", warning, "修改", "取消"))
            {
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                EditorUtility.DisplayDialog("修改失败", "无法确定 Unity 项目根目录。", "确定");
                return;
            }

            string configFullPath = Path.Combine(projectRoot, ConfigAssetPath);
            File.WriteAllText(configFullPath, GenerateConfigSource(extension), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ConfigAssetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"GF resource extension changed from '.{oldExtension}' to '.{extension}'. Rebuild all GF resources before the next player build.");
            EditorUtility.DisplayDialog(
                "修改完成",
                $"共享配置已改为 .{extension}。\n\n请等待脚本重新编译，然后使用 GF Resource Builder 重新构建全部资源。",
                "确定");
        }

        private void RefreshStreamingAssetsSummary()
        {
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(streamingAssetsPath))
            {
                m_StreamingAssetsSummary = "目录不存在。重新构建 GF 资源后会自动生成。";
                return;
            }

            string[] files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetExtension(path), ".meta", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (files.Length == 0)
            {
                m_StreamingAssetsSummary = "目录为空。";
                return;
            }

            m_StreamingAssetsSummary = string.Join(", ", files
                .GroupBy(path => string.IsNullOrEmpty(Path.GetExtension(path)) ? "<无后缀>" : Path.GetExtension(path).ToLowerInvariant())
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Key}: {group.Count()} 个"));
        }

        private static string NormalizeExtension(string extension)
        {
            return string.IsNullOrWhiteSpace(extension)
                ? string.Empty
                : extension.Trim().TrimStart('.').ToLowerInvariant();
        }

        private static bool IsValidExtension(string extension)
        {
            return s_ValidExtensionRegex.IsMatch(extension);
        }

        private static string GenerateConfigSource(string extension)
        {
            return string.Join(Environment.NewLine, new[]
            {
                "//------------------------------------------------------------",
                "// Game Framework",
                "// Copyright © 2013-2021 Jiang Yin. All rights reserved.",
                "// Homepage: https://gameframework.cn/",
                "// Feedback mailto: ellan@gameframework.cn",
                "//------------------------------------------------------------",
                string.Empty,
                "namespace GameFramework.Resource",
                "{",
                "    /// <summary>",
                "    /// 资源文件扩展名配置。",
                "    /// </summary>",
                "    /// <remarks>",
                "    /// 此文件由 ResourceExtensionSettingsWindow 统一维护，修改后必须重新构建全部资源。",
                "    /// </remarks>",
                "    public static class ResourceExtensionConfig",
                "    {",
                $"        public const string DefaultExtension = \"{extension}\";",
                "        public const string RemoteVersionListFileName = \"GameFrameworkVersion.\" + DefaultExtension;",
                "        public const string LocalVersionListFileName = \"GameFrameworkList.\" + DefaultExtension;",
                "        public const string RemoteVersionListSearchPattern = \"GameFrameworkVersion.*.\" + DefaultExtension;",
                "    }",
                "}",
                string.Empty
            });
        }
    }
}
