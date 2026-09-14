using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Lokas.Editor
{
    /// <summary>只读检查原生 unitypackage 的 tar 成员；导入前不提取、不执行脚本。</summary>
    internal static class SamplePackageArchive
    {
        private sealed class Entry
        {
            public string guid;
            public string path;
            public string assetHash;
            public string metaHash;
            public bool folder;
        }
        public static SamplePackageFile[] Inspect(string package, SamplePackageManifest manifest)
        {
            var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var members = new HashSet<string>(StringComparer.Ordinal);
            using var file = File.OpenRead(package);
            using var zip = new GZipStream(file, CompressionMode.Decompress);
            byte[] header = new byte[512];
            long total = 0;
            while (ReadBlock(zip, header))
            {
                if (header.All(value => value == 0)) break;
                ValidateHeader(header);
                string name = Text(header, 0, 100).TrimEnd('/');
                if (name.StartsWith("./", StringComparison.Ordinal)) name = name.Substring(2);
                string prefix = Text(header, 345, 155);
                if (!string.IsNullOrEmpty(prefix)) name = prefix + "/" + name;
                long size = Convert.ToInt64(Text(header, 124, 12).Trim(), 8);
                if (size < 0 || (total += size) > 4L * 1024 * 1024 * 1024) throw new InvalidDataException("Package exceeds validation limit.");
                char type = (char)header[156];
                if (type != '\0' && type != '0' && type != '5') throw new InvalidDataException("Unsupported tar entry type.");
                var parts = name.Split('/');
                if (parts.Length == 0 || parts[0].Length != 32 || !parts[0].All(Uri.IsHexDigit))
                    throw new InvalidDataException("Invalid Unity asset GUID in archive: " + name);
                if (type == '5')
                {
                    if (parts.Length != 1 || size != 0) throw new InvalidDataException("Invalid archive directory.");
                    continue;
                }
                if (parts.Length != 2 || !members.Add(name)) throw new InvalidDataException("Unexpected or duplicate archive member: " + name);
                if (!entries.TryGetValue(parts[0], out var entry)) entries.Add(parts[0], entry = new Entry { guid = parts[0] });
                switch (parts[1])
                {
                    case "pathname":
                        if (size > 4096) throw new InvalidDataException("Overlong path.");
                        entry.path = Encoding.UTF8.GetString(ReadBytes(zip, (int)size)).TrimEnd('\0', '\r', '\n');
                        SamplePackagePaths.Asset(entry.path);
                        break;
                    case "asset.meta":
                        if (size > 16 * 1024 * 1024) throw new InvalidDataException("Overlarge meta.");
                        byte[] meta = ReadBytes(zip, (int)size);
                        string text = Encoding.UTF8.GetString(meta);
                        if (!text.Contains("guid: " + entry.guid)) throw new InvalidDataException("Meta GUID mismatch.");
                        entry.folder = text.Contains("folderAsset: yes");
                        entry.metaHash = SamplePackagePaths.Hash(meta);
                        break;
                    case "asset": entry.assetHash = HashBytes(zip, size); break;
                    case "preview.png": Skip(zip, size); break;
                    default: throw new InvalidDataException("Unexpected archive member: " + name);
                }
                Padding(zip, size);
            }
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries.Values)
            {
                if (entry.path == null || entry.metaHash == null || (!entry.folder && entry.assetHash == null) || !paths.Add(entry.path))
                    throw new InvalidDataException("Incomplete or conflicting asset record.");
                if (!manifest.Owns(entry.path)) throw new InvalidDataException("Package contains a non-owned asset: " + entry.path);
            }
            return entries.Values.Select(entry => new SamplePackageFile { path = entry.path, guid = entry.guid,
                assetHash = entry.folder ? string.Empty : entry.assetHash, metaHash = entry.metaHash, folder = entry.folder }).OrderBy(entry => entry.path, StringComparer.Ordinal).ToArray();
        }
        internal static string ReadAssetText(string package, string guid) => Encoding.UTF8.GetString(ReadMember(package, guid, "asset"));
        internal static byte[] ReadMember(string package, string guid, string member)
        {
            using var file = File.OpenRead(package);
            using var zip = new GZipStream(file, CompressionMode.Decompress);
            byte[] header = new byte[512];
            while (ReadBlock(zip, header) && header.Any(value => value != 0))
            {
                ValidateHeader(header);
                string name = Text(header, 0, 100);
                if (name.StartsWith("./", StringComparison.Ordinal)) name = name.Substring(2);
                long size = Convert.ToInt64(Text(header, 124, 12).Trim(), 8);
                if (name == guid + "/" + member)
                {
                    if (size > 16 * 1024 * 1024) throw new InvalidDataException("Metadata too large.");
                    return ReadBytes(zip, (int)size);
                }
                Skip(zip, size); Padding(zip, size);
            }
            throw new InvalidDataException("Archive member missing: " + guid + "/" + member);
        }
        private static void ValidateHeader(byte[] header)
        {
            long expected = Convert.ToInt64(Text(header, 148, 8).Trim(), 8);
            long actual = header.Select((value, index) => index >= 148 && index < 156 ? 32 : (int)value).Sum();
            if (expected != actual) throw new InvalidDataException("Tar header checksum mismatch.");
        }
        private static string Text(byte[] bytes, int offset, int length) => Encoding.ASCII.GetString(bytes, offset, length).TrimEnd('\0', ' ');
        private static bool ReadBlock(Stream stream, byte[] buffer)
        {
            int count = 0;
            while (count < buffer.Length)
            {
                int read = stream.Read(buffer, count, buffer.Length - count);
                if (read == 0) { if (count == 0) return false; throw new EndOfStreamException(); }
                count += read;
            }
            return true;
        }
        private static byte[] ReadBytes(Stream stream, int count) { var bytes = new byte[count]; if (count > 0 && !ReadBlock(stream, bytes)) throw new EndOfStreamException(); return bytes; }
        private static void Padding(Stream stream, long size) => Skip(stream, (512 - size % 512) % 512);
        private static void Skip(Stream stream, long count) => HashBytes(stream, count);
        private static string HashBytes(Stream stream, long count)
        {
            using var sha = SHA256.Create();
            byte[] buffer = new byte[65536];
            while (count > 0)
            {
                int read = stream.Read(buffer, 0, (int)Math.Min(count, buffer.Length));
                if (read == 0) throw new EndOfStreamException();
                sha.TransformBlock(buffer, 0, read, buffer, 0);
                count -= read;
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
