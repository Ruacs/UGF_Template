using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor.FontCharset.Tests
{
    public static class FontCharsetValidation
    {
        [MenuItem("Game Framework/字体与字符集/运行工具验证", priority = 160)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || FontCharsetService.HasPendingRecovery) throw new InvalidOperationException("请在无待恢复事务的编辑态运行。");
            var passed = new List<string>();
            var owned = new List<string>();
            FontCharsetConfig config = null;
            TMPLanguageFontConfig languages = null;
            var entries = new List<LanguageEntry>();
            string folder = "Assets/AAA_DevAssets/Fonts/Editor/__Validation_" + Guid.NewGuid().ToString("N");
            try
            {
                Assert(FontCharsetCollector.VisibleText("<color=#fff>A</color><sprite=0> {0:N0} {{x}} <noparse><b></noparse>") == "A  {x} <b>", "TMP tags, noparse and placeholders");
                Assert(FontCharsetCollector.CodePoints("𠮷𠮷\nA ").Distinct().Count() == 3, "Unicode scalar and space retention");
                Assert(FontCharsetCollector.VisibleText("<uppercase>a</uppercase>").Contains("A"), "Case-transform coverage");
                var values = FontCharsetCollector.ReadXml("<Dictionaries><Dictionary Language='Japanese'><String Key='A' Value='&lt;b&gt;猫&lt;/b&gt;&amp;犬'/></Dictionary></Dictionaries>", "Japanese", "test");
                Assert(values[0].Value == "猫&犬", "XML decoding before rich text parsing");
                ExpectThrow(() => FontCharsetCollector.ReadXml("<!DOCTYPE X [<!ENTITY x 'bad'>]><Dictionaries/>", "Japanese", "test"), "DTD rejection");
                ExpectThrow(() => FontCharsetCollector.VisibleText("<style=Title>text</style>"), "Unsupported style expansion is explicit");
                ExpectThrow(() => FontCharsetCollector.CodePoints("\uD800").ToArray(), "Invalid surrogate rejection");
                passed.Add("Collector: XML, rich text, noparse, placeholders, Unicode, case conversion, DTD/style rejection");

                var original = FontCharsetConfig.Load();
                Assert(original != null, "Default configuration exists");
                AssetDatabase.CreateFolder("Assets/AAA_DevAssets/Fonts/Editor", Path.GetFileName(folder));
                owned.Add(folder);
                config = ScriptableObject.CreateInstance<FontCharsetConfig>();
                languages = ScriptableObject.CreateInstance<TMPLanguageFontConfig>();
                config.languageConfig = languages;
                for (int i = 0; i < original.entries.Count; i++)
                {
                    var row = original.entries[i];
                    string fontPath = "Assets/GameMain/Fonts/__FontCharsetValidation_" + Guid.NewGuid().ToString("N") + ".asset";
                    Assert(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(row.targetFont), fontPath), "Copy fixture font");
                    owned.Add(fontPath);
                    string txtPath = folder + "/charset" + i + ".txt";
                    File.WriteAllText(txtPath, FontCharsetCollector.Text(FontCharsetCollector.CodePoints(row.charset.text).Take(2)), new System.Text.UTF8Encoding(false));
                    AssetDatabase.ImportAsset(txtPath);
                    var entry = new FontCharsetEntry
                    {
                        language = row.language, sourceFont = row.sourceFont,
                        targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath),
                        charset = AssetDatabase.LoadAssetAtPath<TextAsset>(txtPath), extraText = "0123456789ABCxyz%", autoSize = false, pointSize = row.pointSize,
                        atlasWidth = 2048, atlasHeight = 1024, padding = row.padding, renderMode = row.renderMode,
                        localizationFiles = new List<TextAsset>(row.localizationFiles)
                    };
                    config.entries.Add(entry);
                    var language = ScriptableObject.CreateInstance<LanguageEntry>();
                    language.languageKey = (GameFramework.Localization.Language)Enum.Parse(typeof(GameFramework.Localization.Language), row.language);
                    language.fontAssetName = Path.GetFileNameWithoutExtension(fontPath);
                    AssetDatabase.CreateAsset(language, folder + "/language" + i + ".asset");
                    entries.Add(language); languages.languageProfiles.Add(language);
                }
                // Persist configuration too: the transaction must restore disk-backed fingerprints.
                AssetDatabase.CreateAsset(languages, folder + "/languages.asset");
                AssetDatabase.CreateAsset(config, folder + "/config.asset");
                var paths = owned.Where(p => p.EndsWith(".asset")).Concat(config.entries.Select(e => AssetDatabase.GetAssetPath(e.charset))).ToArray();
                var before = paths.ToDictionary(p => p, File.ReadAllBytes);
                var scan = FontCharsetService.Scan(config, false);
                Assert(scan.languages.All(s => s.append.Length > 0 && FontCharsetCollector.CodePoints(s.entry.charset.text).All(c => s.required.Contains(c))), "Preserve previous TXT and font glyphs");
                Assert(Unchanged(before), "Scan is read only");
                Assert(!FontCharsetService.Update(config, false, (s, p) => true).success && Unchanged(before), "Cancellation leaves assets unchanged");
                passed.Add("Read-only scan, union of existing glyphs/TXT, cancellation");

                foreach (var entry in config.entries) entry.selected = false;
                config.entries[0].selected = true;
                EditorUtility.SetDirty(config);
                var editedFont = config.entries[0].targetFont;
                string editedPath = AssetDatabase.GetAssetPath(editedFont);
                var untouchedMaterial = config.entries[1].targetFont.material;
                var editedColor = new Color(0.2f, 0.4f, 0.6f, 1);
                editedFont.name += " Edited";
                editedFont.material.SetColor("_FaceColor", editedColor);
                EditorUtility.SetDirty(editedFont); EditorUtility.SetDirty(editedFont.material);
                EditorUtility.SetDirty(untouchedMaterial);
                bool generationStarted = false;
                var unsaved = FontCharsetService.Update(config, true, (message, progress) =>
                {
                    if (!message.StartsWith("生成 ")) return false;
                    generationStarted = true;
                    return true;
                });
                Assert(!unsaved.success && unsaved.savedAssetPaths.Contains(editedPath) && unsaved.savedAssetPaths.Contains(AssetDatabase.GetAssetPath(config)) && unsaved.savedAssetPaths.Length == 2 && generationStarted && unsaved.unsavedAssetPaths.Length == 0,
                    "Dirty font/subassets auto-save before generation without a manual save step");
                Assert(!AssetDatabase.LoadAllAssetsAtPath(editedPath).Any(EditorUtility.IsDirty) && EditorUtility.IsDirty(untouchedMaterial),
                    "Scoped save includes subassets and leaves unrelated unsaved assets untouched");
                AssetDatabase.ImportAsset(editedPath, ImportAssetOptions.ForceUpdate);
                editedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(editedPath);
                Assert(editedFont.name.EndsWith(" Edited") && editedFont.material.GetColor("_FaceColor") == editedColor, "Current font/material edits survive save and reimport");
                AssetDatabase.SaveAssetIfDirty(untouchedMaterial);
                foreach (var entry in config.entries) entry.selected = true;
                EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
                before = paths.ToDictionary(p => p, File.ReadAllBytes);
                passed.Add("Unsaved edits: automatic scoped save, cancellation preserves edits, persisted subassets, unrelated dirty assets preserved");

                config.entries[0].extraText += char.ConvertFromUtf32(0x10FFFF);
                var missing = FontCharsetService.Update(config, false);
                Assert(!missing.success && missing.languages[0].missingSource.Contains(0x10FFFFu) && Unchanged(before), "Missing source glyph aborts batch");
                config.entries[0].extraText = "0123456789ABCxyz%";
                int width = config.entries[0].atlasWidth, height = config.entries[0].atlasHeight;
                config.entries[0].atlasWidth = config.entries[0].atlasHeight = 32;
                var capacity = FontCharsetService.Update(config, false);
                Assert(!capacity.success && capacity.languages[0].status == "图集容量不足" && Unchanged(before), "Atlas overflow leaves all assets unchanged");
                config.entries[0].atlasWidth = width; config.entries[0].atlasHeight = height;
                passed.Add("Missing-source and atlas-capacity failures are distinct and do not write");

                var rollback = FontCharsetService.UpdateCore(config, false, null, i => { if (i == 0) throw new IOException("Injected write failure"); });
                Assert(!rollback.success && rollback.message.Contains("本批次写入已恢复") && Unchanged(before) && !FontCharsetService.HasPendingRecovery, "Write failure restores entire batch: " + rollback.message);
                passed.Add("Injected mid-commit failure: byte-identical rollback and cleared journal");

                // Import during recovery reloads disk-backed config; restore its transient runtime test mapping.
                config.languageConfig = languages;
                var success = FontCharsetService.Update(config, false);
                Assert(success.success, "Successful generation: " + success.message);
                Assert(config.entries[0].targetFont.material.GetColor("_FaceColor") == editedColor, "Generation and rollback preserve auto-saved material edits");
                var after = paths.ToDictionary(p => p, File.ReadAllBytes);
                var again = FontCharsetService.Update(config, false);
                Assert(again.success && again.message == "无需更新。" && Unchanged(after), "Second run is a no-op");
                passed.Add("Successful four-font generation at 2048x1024, persisted coverage/IDs, repeat update is a no-op");
                foreach (var row in config.entries) { row.autoSize = true; row.atlasWidth = row.atlasHeight = 1024; }
                EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
                var beforeAuto = paths.ToDictionary(p => p, File.ReadAllBytes);
                var cancelledAuto = FontCharsetService.Update(config, false, (message, progress) => message.Contains("采样字号"));
                Assert(!cancelledAuto.success && Unchanged(beforeAuto), "Auto-size cancellation leaves original assets intact");
                var automatic = FontCharsetService.Update(config, false);
                Assert(automatic.success, "Auto-size generation: " + automatic.message);
                foreach (var row in automatic.languages)
                {
                    var font = row.entry.targetFont;
                    Assert(row.actualPointSize >= 1 && row.actualPointSize <= 512 && Mathf.RoundToInt(font.faceInfo.pointSize) == row.actualPointSize,
                        "Report uses actual generated point size");
                    Assert(font.atlasWidth == 1024 && font.atlasHeight == 1024 && font.creationSettings.pointSizeSamplingMode == 0,
                        "Auto-sizing keeps atlas resolution and stores Auto Sizing creator mode");
                }
                var autoSnapshot = paths.ToDictionary(p => p, File.ReadAllBytes);
                Assert(FontCharsetService.Update(config, false).message == "无需更新。" && Unchanged(autoSnapshot), "Auto-size rerun is a no-op");
                passed.Add("Auto Sizing at 1024x1024: cancellation, four-font coverage, actual size metadata, repeat update is a no-op");
                passed.Add("Auto Sizing results: " + string.Join(", ", automatic.languages.Select(row => row.language + "=" + row.actualPointSize)));
                foreach (var row in config.entries)
                    Assert(row.targetFont.atlasPopulationMode == AtlasPopulationMode.Static, "Static mode remains unchanged");
                File.WriteAllLines("Library/FontCharset/validation.txt", passed.Concat(new[] { "PASS" }));
                Debug.Log("FontCharset validation passed: " + passed.Count + " groups.");
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory("Library/FontCharset");
                File.WriteAllLines("Library/FontCharset/validation.txt", passed.Concat(new[] { "FAIL", ex.ToString() }));
                Debug.LogException(ex);
            }
            finally
            {
                // Only assets created by this invocation are removed via AssetDatabase.
                if (!FontCharsetService.HasPendingRecovery)
                {
                    foreach (string path in owned.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
                    if (languages != null) UnityEngine.Object.DestroyImmediate(languages);
                    foreach (var entry in entries) if (entry != null) UnityEngine.Object.DestroyImmediate(entry);
                }
            }
        }
        private static bool Unchanged(Dictionary<string, byte[]> files) => files.All(p => File.ReadAllBytes(p.Key).SequenceEqual(p.Value));
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void ExpectThrow(Action action, string label)
        {
            try { action(); } catch { return; }
            throw new InvalidOperationException(label);
        }
    }
}
