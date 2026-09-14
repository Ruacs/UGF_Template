using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Lokas.Editor
{
    [Serializable]
    public sealed class SamplePackageManifest
    {
        public const string Compatibility = "GF.Template.SubGames.1";
        public int schema = 1;
        public string template = Compatibility;
        public string game;
        public string version = "1.0.0";
        public int mode;
        public string managerPrefab;
        public string resourceManifest;
        public string procedure;
        public string[] scenePaths;
        public string[] uiRows;
        public string[] sceneRows;
        public string[] configRows;
        public string[] resourceDefinitions;
        public string[] resourceAssets;
        public string Root => "Assets/GameMain/SubGame/" + game;
        public string SceneRoot => "Assets/GameMain/Scenes/SubGame/" + game;
        public string ManifestPath => Root + "/Editor/PackageManifest.json";
        public bool Owns(string path) => IsUnder(path, Root) || IsUnder(path, SceneRoot);
        public static bool IsUnder(string path, string root) => path != null && (path == root || path.StartsWith(root + "/", StringComparison.Ordinal));

        public void Validate()
        {
            if (schema != 1 || template != Compatibility || (game != "HexaAway" && game != "RectMatch") ||
                mode != (game == "HexaAway" ? 2 : 3) || version == null || !Regex.IsMatch(version, @"\A[0-9]+\.[0-9]+\.[0-9]+(?:-[a-zA-Z0-9-]+)?\z"))
                throw new InvalidOperationException("Unsupported sample package or template version.");
            if (managerPrefab == null || resourceManifest == null || !Owns(managerPrefab) || !Owns(resourceManifest) ||
                !managerPrefab.EndsWith(".prefab", StringComparison.Ordinal) || !resourceManifest.EndsWith(".asset", StringComparison.Ordinal) ||
                procedure != "Lokas.ProcedureGame" + game || scenePaths == null || scenePaths.Length == 0)
                throw new InvalidOperationException("Incomplete package registration: " + game);
            foreach (string path in new[] { managerPrefab, resourceManifest }.Concat(scenePaths))
            {
                SamplePackagePaths.Asset(path);
                if (!Owns(path)) throw new InvalidOperationException("Package path is not owned: " + path);
            }
            if (scenePaths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != scenePaths.Length ||
                scenePaths.Any(path => !IsUnder(path, SceneRoot) || !path.EndsWith(".unity", StringComparison.Ordinal)))
                throw new InvalidOperationException("Invalid scene registration.");
            if (uiRows == null || sceneRows == null || configRows == null || resourceDefinitions == null || resourceAssets == null)
                throw new InvalidOperationException("Missing registration fragments.");
        }
    }

    [Serializable] public sealed class SamplePackageFile
    {
        public string path;
        public string guid;
        public string assetHash;
        public string metaHash;
        public bool folder;
    }
    [Serializable] public sealed class SamplePackageDependency { public string path; public string guid; }
    [Serializable] public sealed class SamplePackageCatalog
    {
        public SamplePackageManifest manifest;
        public string packageHash;
        public string unityVersion;
        public SamplePackageFile[] files;
        public SamplePackageDependency[] dependencies;
    }
    [Serializable] public sealed class SamplePackageInstalled
    {
        public string game;
        public SamplePackageCatalog catalog;
    }
    [Serializable] public sealed class SamplePackageState
    {
        public List<SamplePackageInstalled> installed = new();
    }
    [Serializable] public sealed class SamplePackageBackupFile
    {
        public string path;
        public bool existed;
        public string beforeHash;
        public string afterHash;
    }
    [Serializable] public sealed class SamplePackageBuildScene { public string path; public bool enabled; }
    [Serializable] public sealed class SamplePackageJournal
    {
        public string operation;
        public string phase;
        public string game;
        public string backupDirectory;
        public string packagePath;
        public SamplePackageCatalog catalog;
        public List<SamplePackageBackupFile> sharedFiles = new();
        public string[] originalScenes;
        public string error;
        public string[] expectedInstalled;
        public int primary;
        public bool assetsExisted;
        public SamplePackageBuildScene[] originalBuildScenes;
        public SamplePackageBuildScene[] afterBuildScenes;
    }

    internal static class SamplePackagePaths
    {
        public const string Launcher = "Assets/GameMain/Scenes/GameLauncher.unity";
        public const string PackagesDirectory = "Assets/AAA_DevAssets/SamplePackages~";
        public const string State = "ProjectSettings/GFSamplePackages.json";
        public const string Journal = "Library/GFSamplePackages/operation.json";
        public static string Workspace => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        public static string Full(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains('\\') ||
                relative.Split('/').Any(segment => segment == ".." || segment == "." || segment.Length == 0 ||
                    segment.EndsWith(".", StringComparison.Ordinal) || segment.EndsWith(" ", StringComparison.Ordinal) ||
                    segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                throw new InvalidOperationException("Unsafe relative path: " + relative);
            string result = Path.GetFullPath(Path.Combine(Workspace, relative));
            if (!result.StartsWith(Workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Path escapes project: " + relative);
            string current = Workspace;
            foreach (string segment in relative.Split('/'))
            {
                current = Path.Combine(current, segment);
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Linked/reparse-point paths require manual handling: " + relative);
            }
            return result;
        }
        public static string Asset(string path)
        {
            if (path == null || !path.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("Not an asset path: " + path);
            return Full(path);
        }
        public static string ExportDirectory(string directory = null)
        {
            directory ??= Full(PackagesDirectory);
            string normalized = directory.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory) || normalized.StartsWith("//?/", StringComparison.Ordinal) ||
                normalized.StartsWith("//./", StringComparison.Ordinal) || normalized.Split('/').Any(segment =>
                    segment.EndsWith(".", StringComparison.Ordinal) || segment.EndsWith(" ", StringComparison.Ordinal)))
                throw new InvalidOperationException("Unsafe export directory: " + directory);
            string full = Path.GetFullPath(Path.IsPathRooted(directory) ? directory : Path.Combine(Workspace, directory));
            string comparable = full.Replace('\\', '/').TrimEnd('/');
            string assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/').TrimEnd('/');
            if (string.Equals(comparable, assets, StringComparison.OrdinalIgnoreCase) ||
                comparable.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase))
            {
                string allowed = Full(PackagesDirectory);
                if (!string.Equals(comparable, allowed.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Inside Assets, export only to " + PackagesDirectory + "; use a directory outside Assets for backups.");
                full = allowed; // 固定大小写和实际落点；不能借近似目录名放开整个 AAA_DevAssets。
            }
            // 外部输出目录也不能通过 junction/symlink 绕过 Assets 边界。
            for (var current = new DirectoryInfo(full); current != null; current = current.Parent)
                if ((Directory.Exists(current.FullName) || File.Exists(current.FullName)) &&
                    (File.GetAttributes(current.FullName) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Linked export directories require manual handling: " + directory);
            if (File.Exists(full)) throw new IOException("Export directory is occupied by a file: " + full);
            return full;
        }
        public static string Hash(string path)
        {
            // JsonUtility 会把 null string 序列化为空串；缺失文件统一为空串，确保跨重载恢复校验一致。
            if (!File.Exists(path)) return string.Empty;
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        public static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        public static T Read<T>(string path) where T : class, new() => File.Exists(path) ?
            JsonUtility.FromJson<T>(File.ReadAllText(path, Encoding.UTF8)) ?? throw new InvalidDataException("Invalid JSON: " + path) : new T();
        public static void Write(string path, object data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
