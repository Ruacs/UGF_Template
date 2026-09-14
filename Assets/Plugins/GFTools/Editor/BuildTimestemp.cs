using UnityEditor;
using System.IO;
using System;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditor.Callbacks;

public class BuildTimestamp
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string path)
    {
        if (target == BuildTarget.WebGL || target == BuildTarget.iOS || target == BuildTarget.tvOS) return;

        try
        {
            string directory = Path.GetDirectoryName(path);
            string originalName = Path.GetFileName(path);

            // 获取并清理版本号
            string version = CleanVersion(PlayerSettings.bundleVersion);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (File.Exists(path))
            {
                RenameFile(target, path, directory, originalName, version, timestamp);
            }
            else if (Directory.Exists(path))
            {
                RenameDirectory(path, directory, originalName, version, timestamp);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"重命名失败: {e.Message}");
        }
    }

    private static string CleanVersion(string version)
    {
        if (string.IsNullOrEmpty(version)) return "";

        // 替换非法字符为下划线，保留数字、字母和点
        return Regex.Replace(version, @"[^\w\.]", "_");
    }

    private static void RenameFile(BuildTarget target, string path, string directory,
        string originalName, string version, string timestamp)
    {
        string extension = Path.GetExtension(originalName);
        string fileName = Path.GetFileNameWithoutExtension(originalName);

        // 构建新文件名逻辑
        string newFileName = BuildNewName(fileName, version, timestamp) + extension;
        string newPath = Path.Combine(directory, newFileName);

        File.Move(path, newPath);
        Debug.Log($"构建文件已重命名为: {newPath}");
    }

    private static void RenameDirectory(string path, string directory,
        string originalName, string version, string timestamp)
    {
        string newDirName = BuildNewName(originalName, version, timestamp);
        string newPath = Path.Combine(directory, newDirName);

        Directory.Move(path, newPath);
        Debug.Log($"构建目录已重命名为: {newPath}");
    }

    private static string BuildNewName(string baseName, string version, string timestamp)
    {
        // 版本号为空时的处理逻辑
        if (!string.IsNullOrEmpty(version))
        {
            return $"{baseName}_{version}_{timestamp}";
        }
        return $"{baseName}_{timestamp}";
    }
}
