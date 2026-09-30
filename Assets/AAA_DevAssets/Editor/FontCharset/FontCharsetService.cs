using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace Lokas.Editor.FontCharset
{
    [Serializable]
    public sealed class FontCharsetScan
    {
        public string language, fontPath, charsetPath, signature;
        public string status, details;
        public uint[] required, append, missingAtlas, missingSource;
        public bool needsBuild, sourceChanged;
        public int actualPointSize;
        [NonSerialized] public FontCharsetEntry entry;
        [NonSerialized] public Dictionary<uint, List<string>> origins = new Dictionary<uint, List<string>>();
    }

    [Serializable]
    public sealed class FontCharsetReport
    {
        public bool success;
        public string operation, message, backupPath;
        public string[] unsavedAssetPaths = Array.Empty<string>();
        public string[] savedAssetPaths = Array.Empty<string>();
        public List<FontCharsetScan> languages = new List<FontCharsetScan>();
    }

    public static class FontCharsetService
    {
        public const string ReportPath = "Library/FontCharset/last-report.json";
        private const string JournalPath = "Library/FontCharset/pending.json";
        public static bool Busy { get; private set; }
        public static bool HasPendingRecovery => File.Exists(JournalPath);
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        [Serializable] private sealed class Journal { public string backup; public List<string> paths = new List<string>(); }
        private sealed class Staged : IDisposable
        {
            public FontCharsetScan scan;
            public TMP_FontAsset font;
            public Material[] materials;
            public Dictionary<long, string> identities;
            public void Dispose()
            {
                DestroyTemporaryFont(font);
            }
        }

        private sealed class UnsavedAssetsException : InvalidOperationException
        {
            public readonly string[] paths;
            public UnsavedAssetsException(string[] paths)
                : base("以下资产仍有未保存修改，本次生成结果尚未写入。请点击「重试更新」，工具会自动保存后重新生成：\n" + string.Join("\n", paths)) { this.paths = paths; }
        }

        private static void CheckWritableAssets(IEnumerable<string> paths)
        {
            var unique = paths.Distinct().ToArray();
            foreach (string path in unique)
                if ((File.GetAttributes(path) & FileAttributes.ReadOnly) != 0) throw new InvalidOperationException("文件只读: " + path);
            var unsaved = unique.Where(path => AssetDatabase.LoadAllAssetsAtPath(path).Any(EditorUtility.IsDirty)).ToArray();
            if (unsaved.Length > 0) throw new UnsavedAssetsException(unsaved);
        }

        // Save current edits as the rollback baseline, limited to this update's assets and subassets.
        private static string[] SaveRelatedAssets(IEnumerable<string> assetPaths)
        {
            var paths = assetPaths.Distinct().ToArray();
            var saved = paths.Where(path => AssetDatabase.LoadAllAssetsAtPath(path).Any(EditorUtility.IsDirty)).ToArray();
            foreach (string path in saved)
            {
                AssetPath(AssetDatabase.LoadMainAssetAtPath(path));
                if ((File.GetAttributes(path) & FileAttributes.ReadOnly) != 0) throw new InvalidOperationException("文件只读: " + path);
            }
            foreach (string path in paths)
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (EditorUtility.IsDirty(asset)) AssetDatabase.SaveAssetIfDirty(asset);
            CheckWritableAssets(paths);
            return saved;
        }

        public static void WriteReport(FontCharsetReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true), Utf8);
        }

        public static FontCharsetReport Scan(FontCharsetConfig config, bool selectedOnly)
        {
            var report = new FontCharsetReport { operation = "scan", success = true };
            if (config == null) throw new InvalidOperationException("请先创建或指定配置。");
            var entries = config.entries.Where(e => !selectedOnly || e.selected).ToArray();
            if (entries.Length == 0) throw new InvalidOperationException("没有选中语言。");
            var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var charsetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                var scan = ScanEntry(entry);
                if (!targetPaths.Add(scan.fontPath) || !charsetPaths.Add(scan.charsetPath)) throw new InvalidOperationException("多个条目指向同一个字体或字符集。");
                if (config.languageConfig == null) throw new InvalidOperationException("未设置运行时 TMPLanguageFontConfig。");
                var matching = config.languageConfig.languageProfiles.Where(e => e != null && e.languageKey.ToString() == entry.language).ToArray();
                if (matching.Length != 1 || Lokas.AssetUtility.GetTMPFontAsset(matching[0].fontAssetName, true) != scan.fontPath)
                    throw new InvalidOperationException(entry.language + " 的 LanguageEntry 与目标字体不一致。");
                report.languages.Add(scan);
                if (scan.missingSource.Length > 0) report.success = false;
            }
            report.message = report.success ? "扫描完成。更新前会重新扫描；当前未修改资产。" : "源字体缺字，不能更新。";
            return report;
        }

        public static FontCharsetScan ScanEntry(FontCharsetEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.language) || entry.sourceFont == null || entry.targetFont == null || entry.charset == null)
                throw new InvalidOperationException("语言、源字体、目标字体、字符集均不能为空。");
            var font = entry.targetFont;
            string path = AssetPath(font), charset = AssetPath(entry.charset), source = AssetPath(entry.sourceFont);
            if (!charset.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("字符集必须是 TXT。");
            if (font.atlasPopulationMode != AtlasPopulationMode.Static || font.atlasTextures.Length != 1 || font.atlasTexture == null || font.material == null)
                throw new InvalidOperationException(entry.language + " 首版仅支持已有的单图集 Static 字体。");
            if (AssetDatabase.GetAssetPath(font.atlasTexture) != path || AssetDatabase.GetAssetPath(font.material) != path)
                throw new InvalidOperationException("Atlas 和默认材质必须是目标字体的子资产。");
            if ((!entry.autoSize && (entry.pointSize < 1 || entry.pointSize > 512)) || entry.padding < 0 || entry.padding > 64 ||
                !ValidSize(entry.atlasWidth) || !ValidSize(entry.atlasHeight) || entry.padding * 2 >= Math.Min(entry.atlasWidth, entry.atlasHeight))
                throw new InvalidOperationException("无效字号、Padding 或图集尺寸（支持 8–8192 的 2 次幂，Padding 须小于短边的一半）。");
            if ((entry.renderMode.ToString().IndexOf("SDF", StringComparison.Ordinal)) < 0) throw new InvalidOperationException("首版仅支持 SDF 渲染模式。");
            var scan = new FontCharsetScan { entry = entry, language = entry.language, fontPath = path, charsetPath = charset };
            var required = new HashSet<uint>();
            Action<string, string> add = (text, origin) =>
            {
                foreach (uint code in FontCharsetCollector.CodePoints(text))
                {
                    required.Add(code);
                    if (!scan.origins.TryGetValue(code, out var origins)) scan.origins[code] = origins = new List<string>();
                    if (origins.Count < 4 && !origins.Contains(origin)) origins.Add(origin);
                }
            };
            string existing = File.ReadAllText(charset, Utf8);
            add(existing, "已有字符集");
            add(FontCharsetCollector.Text(font.characterTable.Select(c => c.unicode)), "字体已有字符");
            foreach (var textAsset in entry.localizationFiles)
            {
                if (textAsset == null) throw new InvalidOperationException(entry.language + " 存在空的 XML 来源。");
                string xmlPath = AssetPath(textAsset);
                foreach (var pair in FontCharsetCollector.ReadXml(File.ReadAllText(xmlPath, Utf8), entry.language, xmlPath)) add(pair.Value, pair.Key);
            }
            add(entry.extraText ?? "", "手工补充文本（按字面收集）");
            scan.required = required.OrderBy(c => c).ToArray();
            var charsetCodes = new HashSet<uint>(FontCharsetCollector.CodePoints(existing));
            var fontCodes = new HashSet<uint>(font.characterTable.Select(c => c.unicode));
            scan.append = scan.required.Where(c => !charsetCodes.Contains(c)).ToArray();
            scan.missingAtlas = scan.required.Where(c => !fontCodes.Contains(c)).ToArray();
            FontEngine.InitializeFontEngine();
            if (FontEngine.LoadFontFace(entry.sourceFont, entry.autoSize ? 64 : entry.pointSize) != FontEngineError.Success)
                throw new InvalidOperationException("无法读取源字体，请检查 Include Font Data: " + source);
            scan.missingSource = scan.required.Where(c => !FontEngine.TryGetGlyphWithUnicodeValue(c, GlyphLoadFlags.LOAD_NO_BITMAP, out var glyph) || glyph.index == 0).ToArray();
            string settings = AssetDatabase.AssetPathToGUID(source) + "|" + (entry.autoSize ? "auto-v1" : "fixed-" + entry.pointSize) + "|" + entry.atlasWidth + "|" + entry.atlasHeight + "|" + entry.padding + "|" + (int)entry.renderMode;
            using (var sha = SHA256.Create())
                scan.signature = Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(source).Concat(Utf8.GetBytes(settings + "|" + string.Join(",", scan.required))).ToArray()));
            scan.sourceChanged = font.creationSettings.sourceFontFileGUID != AssetDatabase.AssetPathToGUID(source);
            scan.needsBuild = entry.generatedSignature != scan.signature || scan.missingAtlas.Length > 0 || scan.sourceChanged ||
                font.atlasWidth != entry.atlasWidth || font.atlasHeight != entry.atlasHeight || font.atlasPadding != entry.padding ||
                font.atlasRenderMode != entry.renderMode || (!entry.autoSize && Mathf.RoundToInt(font.faceInfo.pointSize) != entry.pointSize) ||
                font.creationSettings.pointSizeSamplingMode != (entry.autoSize ? 0 : 1);
            scan.actualPointSize = Mathf.RoundToInt(font.faceInfo.pointSize);
            scan.status = scan.missingSource.Length > 0 ? "源字体缺字" : scan.needsBuild || scan.append.Length > 0 ? "待更新" : "无需更新";
            scan.details = scan.missingSource.Length == 0 ? (scan.sourceChanged ? "更换源字体，将重建全部字形。" : "") : MissingDetails(scan, scan.missingSource);
            return scan;
        }

        private static bool ValidSize(int value) => value >= 8 && value <= 8192 && (value & (value - 1)) == 0;
        private static string AssetPath(Object value)
        {
            string path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || !File.Exists(path)) throw new InvalidOperationException("资产必须位于当前项目 Assets 中: " + value);
            return path;
        }
        private static string MissingDetails(FontCharsetScan scan, IEnumerable<uint> codes) => string.Join("\n", codes.Select(c => FontCharsetCollector.Describe(new[] { c }) + " ← " + string.Join("；", scan.origins[c])));

        public static FontCharsetReport Update(FontCharsetConfig config, bool selectedOnly, Func<string, float, bool> cancel = null)
            => UpdateCore(config, selectedOnly, cancel, null);

        // afterWrite is an internal failure-injection seam used by the transaction validation.
        internal static FontCharsetReport UpdateCore(FontCharsetConfig config, bool selectedOnly, Func<string, float, bool> cancel, Action<int> afterWrite)
        {
            if (Busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("请在编辑态、编译结束后更新，且不要重复运行。");
            if (HasPendingRecovery) throw new InvalidOperationException("存在未完成事务，请先恢复上次更新。");
            Busy = true;
            var staged = new List<Staged>();
            Journal journal = null;
            FontCharsetReport report = null;
            try
            {
                report = Scan(config, selectedOnly);
                report.operation = "update";
                if (!report.success) throw new InvalidOperationException("源字体缺字：\n" + string.Join("\n", report.languages.Where(s => s.missingSource.Length > 0).Select(s => s.language + "\n" + s.details)));
                var changes = report.languages.Where(s => s.needsBuild || s.append.Length > 0).ToArray();
                foreach (var scan in changes)
                    staged.Add(new Staged { scan = scan, identities = Identities(scan.fontPath), materials = TMP_EditorUtility.FindMaterialReferences(scan.entry.targetFont).Append(scan.entry.targetFont.material).Distinct().ToArray() });
                if (staged.Count == 0) { report.success = true; report.message = "无需更新。"; return report; }
                var paths = staged.SelectMany(s => s.materials.Select(AssetPath).Concat(new[] { s.scan.fontPath, s.scan.charsetPath })).Distinct().ToList();
                string configPath = AssetDatabase.GetAssetPath(config);
                if (!string.IsNullOrEmpty(configPath)) paths.Add(configPath);
                if (cancel?.Invoke("准备保存相关资产", 0) == true) throw new OperationCanceledException("已取消，未写入生成结果。");
                report.savedAssetPaths = SaveRelatedAssets(paths);
                for (int i = 0; i < changes.Length; i++)
                {
                    var scan = changes[i];
                    if (cancel?.Invoke("生成 " + scan.language, (float)i / changes.Length) == true) throw new OperationCanceledException("已取消，未写入生成结果。");
                    var stage = staged[i];
                    if (!scan.needsBuild) continue;
                    stage.font = Generate(scan, cancel);
                }
                if (cancel?.Invoke("准备写回", 1) == true) throw new OperationCanceledException("已取消，未写入生成结果。");
                CheckWritableAssets(paths);
                journal = Backup(paths.Distinct());
                report.backupPath = journal.backup;
                AssetDatabase.DisallowAutoRefresh();
                try
                {
                    for (int i = 0; i < staged.Count; i++)
                    {
                        Commit(staged[i]);
                        afterWrite?.Invoke(i);
                    }
                    foreach (var stage in staged) Verify(stage);
                    foreach (var stage in staged) stage.scan.entry.generatedSignature = stage.scan.signature;
                    if (!string.IsNullOrEmpty(configPath)) { EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config); }
                    File.Delete(JournalPath);
                    journal = null;
                }
                finally { AssetDatabase.AllowAutoRefresh(); }
                report.success = true;
                report.message = "字符集与图集已同步，保存后覆盖率和子资产引用验证通过。";
                foreach (var stage in staged) { stage.scan.status = "已更新"; stage.scan.missingAtlas = Array.Empty<uint>(); }
                return report;
            }
            catch (Exception ex)
            {
                if (report == null) report = new FontCharsetReport { operation = "update" };
                report.success = false;
                report.message = ex.Message;
                if (ex is UnsavedAssetsException unsaved) report.unsavedAssetPaths = unsaved.paths;
                if (journal != null)
                {
                    try { Restore(journal); report.message += "\n本批次写入已恢复。"; }
                    catch (Exception recovery) { report.message += "\n恢复失败，请使用备份: " + journal.backup + "\n" + recovery.Message; }
                }
                return report;
            }
            finally
            {
                foreach (var stage in staged) stage.Dispose();
                Busy = false;
                if (report != null)
                {
                    if (report.savedAssetPaths.Length > 0) report.message += "\n已自动保存本次涉及的 " + report.savedAssetPaths.Length + " 个资产；原有编辑已保留。";
                    WriteReport(report);
                }
            }
        }

        private static void DestroyTemporaryFont(TMP_FontAsset font)
        {
            if (font == null) return;
            foreach (var texture in font.atlasTextures) if (texture != null) Object.DestroyImmediate(texture);
            if (font.material != null) Object.DestroyImmediate(font.material);
            Object.DestroyImmediate(font);
        }

        // Test real packing at each candidate size; never resize the requested atlas.
        // A bounded binary search keeps the best successful candidate in memory.
        private static TMP_FontAsset Generate(FontCharsetScan scan, Func<string, float, bool> cancel)
        {
            var entry = scan.entry;
            TMP_FontAsset best = null;
            uint[] absent = scan.required;
            int low = entry.autoSize ? 1 : entry.pointSize;
            int high = entry.autoSize ? 512 : entry.pointSize;
            int attempt = 0;
            try
            {
                while (low <= high)
                {
                    int size = low + (high - low) / 2;
                    if (cancel?.Invoke("生成 " + scan.language + " · 采样字号 " + size, Mathf.Min(0.95f, attempt++ / 10f)) == true)
                        throw new OperationCanceledException("已取消，未写入生成结果。");
                    var candidate = TMP_FontAsset.CreateFontAsset(entry.sourceFont, size, entry.padding, entry.renderMode, entry.atlasWidth, entry.atlasHeight, AtlasPopulationMode.Dynamic, false);
                    if (candidate == null) throw new InvalidOperationException("临时字体创建失败: " + entry.language);
                    try
                    {
                        candidate.TryAddCharacters(scan.required, out uint[] missing, true);
                        var actual = new HashSet<uint>(candidate.characterTable.Select(c => c.unicode));
                        absent = scan.required.Where(c => !actual.Contains(c)).ToArray();
                        if ((missing == null || missing.Length == 0) && absent.Length == 0)
                        {
                            DestroyTemporaryFont(best);
                            best = candidate; candidate = null;
                            scan.actualPointSize = size;
                            low = size + 1;
                        }
                        else high = size - 1;
                    }
                    finally { DestroyTemporaryFont(candidate); }
                }
                if (best == null)
                {
                    scan.status = "图集容量不足";
                    scan.details = (entry.autoSize ? "自动大小在 1–512 范围内未找到可容纳全部字符的字号。" : "自定义字号下图集未能容纳全部字符。") + "\n" + MissingDetails(scan, absent);
                    throw new InvalidOperationException(scan.language + " " + scan.details + "\n请调整图集尺寸或 Padding。");
                }
                var result = best; best = null;
                return result;
            }
            finally { DestroyTemporaryFont(best); }
        }

        private static void Commit(Staged stage)
        {
            var scan = stage.scan;
            var entry = scan.entry;
            if (stage.font != null)
            {
                var target = entry.targetFont;
                var texture = target.atlasTexture;
                var name = texture.name;
                var filter = texture.filterMode;
                var wrap = texture.wrapMode;
                bool readable = texture.isReadable;
                EditorUtility.CopySerialized(stage.font.atlasTexture, texture);
                texture.name = name; texture.hideFlags = HideFlags.None; texture.filterMode = filter; texture.wrapMode = wrap;
                texture.Apply(false, !readable);
                var destination = new SerializedObject(target);
                var source = new SerializedObject(stage.font);
                string[] fields = { "m_FaceInfo", "m_GlyphTable", "m_CharacterTable", "m_FontFeatureTable", "m_UsedGlyphRects", "m_FreeGlyphRects", "m_AtlasWidth", "m_AtlasHeight", "m_AtlasPadding", "m_AtlasRenderMode", "m_SourceFontFileGUID", "m_SourceFontFile_EditorRef" };
                foreach (string field in fields)
                {
                    var property = source.FindProperty(field);
                    if (property == null || destination.FindProperty(field) == null) throw new InvalidOperationException("当前 TMP 版本不支持字段: " + field);
                    destination.CopyFromSerializedProperty(property);
                }
                destination.ApplyModifiedPropertiesWithoutUndo();
                var settings = target.creationSettings;
                settings.sourceFontFileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.sourceFont));
                settings.pointSizeSamplingMode = entry.autoSize ? 0 : 1; settings.pointSize = scan.actualPointSize;
                settings.padding = entry.padding; settings.atlasWidth = entry.atlasWidth; settings.atlasHeight = entry.atlasHeight;
                settings.characterSetSelectionMode = 8; settings.characterSequence = FontCharsetCollector.Text(scan.required);
                settings.referencedTextAssetGUID = AssetDatabase.AssetPathToGUID(scan.charsetPath);
                settings.renderMode = (int)entry.renderMode;
                target.creationSettings = settings;
                target.ReadFontAssetDefinition();
                foreach (var material in stage.materials)
                {
                    material.SetFloat(ShaderUtilities.ID_TextureWidth, entry.atlasWidth);
                    material.SetFloat(ShaderUtilities.ID_TextureHeight, entry.atlasHeight);
                    material.SetFloat(ShaderUtilities.ID_GradientScale, entry.padding + 1);
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssetIfDirty(material);
                }
                EditorUtility.SetDirty(texture);
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
                AssetDatabase.ImportAsset(scan.fontPath, ImportAssetOptions.ForceUpdate);
                TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, target);
            }
            if (scan.append.Length > 0)
            {
                File.AppendAllText(scan.charsetPath, FontCharsetCollector.Text(scan.append), Utf8);
                AssetDatabase.ImportAsset(scan.charsetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static Dictionary<long, string> Identities(string path)
        {
            var result = new Dictionary<long, string>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);
                result.Add(localId, guid + ":" + asset.GetType().FullName);
            }
            return result;
        }

        private static void Verify(Staged stage)
        {
            var scan = stage.scan;
            var saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(scan.fontPath);
            var codes = new HashSet<uint>(saved.characterTable.Select(c => c.unicode));
            var textCodes = new HashSet<uint>(FontCharsetCollector.CodePoints(File.ReadAllText(scan.charsetPath, Utf8)));
            var ids = Identities(scan.fontPath);
            var glyphIds = new HashSet<uint>(saved.glyphTable.Select(g => g.index));
            bool invalidGlyphs = saved.characterTable.Any(c => !glyphIds.Contains(c.glyphIndex)) || saved.glyphTable.Any(g => g.glyphRect.x < 0 || g.glyphRect.y < 0 || g.glyphRect.x + g.glyphRect.width > saved.atlasTexture.width || g.glyphRect.y + g.glyphRect.height > saved.atlasTexture.height);
            if (scan.required.Any(c => !codes.Contains(c) || !textCodes.Contains(c)) || invalidGlyphs || saved.atlasTexture.width != scan.entry.atlasWidth || saved.atlasTexture.height != scan.entry.atlasHeight || ids.Count != stage.identities.Count || stage.identities.Any(p => !ids.TryGetValue(p.Key, out string id) || id != p.Value) || saved.material.mainTexture != saved.atlasTexture)
                throw new InvalidOperationException("保存后验证失败: " + scan.language);
        }

        private static Journal Backup(IEnumerable<string> paths)
        {
            var journal = new Journal { backup = "Backups/FontCharset/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) };
            foreach (string path in paths)
            {
                foreach (string file in new[] { path, path + ".meta" })
                {
                    if (!File.Exists(file)) continue;
                    string target = Path.Combine(journal.backup, file);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(file, target, false);
                    journal.paths.Add(file);
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(JournalPath));
            File.WriteAllText(Path.Combine(journal.backup, "manifest.json"), JsonUtility.ToJson(journal, true), Utf8);
            File.WriteAllText(JournalPath, JsonUtility.ToJson(journal, true), Utf8);
            return journal;
        }

        public static void RecoverPending()
        {
            if (Busy || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先结束当前操作和 Play Mode。");
            if (!HasPendingRecovery) return;
            Restore(JsonUtility.FromJson<Journal>(File.ReadAllText(JournalPath, Utf8)));
        }

        private static void Restore(Journal journal)
        {
            string root = Path.GetFullPath(".") + Path.DirectorySeparatorChar;
            string backupRoot = Path.GetFullPath("Backups/FontCharset") + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(journal.backup).StartsWith(backupRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException("无效备份路径。");
            foreach (string path in journal.paths)
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase) || path.Contains("..") || !File.Exists(Path.Combine(journal.backup, path))) throw new IOException("无效恢复路径: " + path);
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                foreach (string path in journal.paths) File.Copy(Path.Combine(journal.backup, path), path, true);
                foreach (string path in journal.paths.Where(p => !p.EndsWith(".meta", StringComparison.Ordinal))) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                File.Delete(JournalPath);
            }
            finally { AssetDatabase.AllowAutoRefresh(); }
        }
    }
}
