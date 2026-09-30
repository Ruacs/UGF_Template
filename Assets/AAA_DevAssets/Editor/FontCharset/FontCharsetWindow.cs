using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lokas.Editor.FontCharset
{
    public sealed class FontCharsetWindow : EditorWindow
    {
        private FontCharsetConfig config;
        private SerializedObject serialized;
        private Vector2 scroll;
        private FontCharsetReport report;
        private FontStylePreviewPanel stylePreview;
        private FontStylePreset previewPreset;
        private TMPFontProfile previewProfile;
        private string previewLanguage, previewStyleKey = "";
        private int previewSource;
        private int page, languageIndex, detailIndex = -1;
        private bool advanced, showInputs, showProject, showFailure;
        private static readonly string[] Pages = { "更新字符集", "字体设置", "预览" };
        private static readonly int[] AtlasSizes = { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        private static readonly string[] AtlasSizeLabels = AtlasSizes.Select(size => size.ToString()).ToArray();
        private bool Unavailable => FontCharsetService.Busy || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling;

        [MenuItem("Game Framework/字体与字符集/打开窗口", priority = 100)]
        public static void Open() => GetWindow<FontCharsetWindow>("字体与字符集");
        [MenuItem("Game Framework/字体与字符集/预览样式", priority = 102)]
        public static void OpenStylePreview()
        {
            var window = GetWindow<FontCharsetWindow>("字体与字符集");
            window.page = 2;
            window.scroll = Vector2.zero;
            if (Selection.activeObject is FontStylePreset preset)
            {
                window.previewSource = 1;
                window.previewPreset = preset;
            }
            window.Repaint();
        }
        [MenuItem("Game Framework/字体与字符集/创建默认配置", priority = 101)]
        public static void CreateDefault()
        {
            Selection.activeObject = FontCharsetConfig.CreateDefault();
            var window = GetWindow<FontCharsetWindow>("字体与字符集");
            window.SetConfig(FontCharsetConfig.Load());
        }
        [MenuItem("Game Framework/字体与字符集/扫描全部", priority = 120)]
        public static void ScanAll() => RunMenu(false);
        [MenuItem("Game Framework/字体与字符集/更新选中语言", priority = 121)]
        public static void UpdateSelected() => RunMenu(true);
        [MenuItem("Game Framework/字体与字符集/恢复未完成更新", priority = 140)]
        public static void Recover() { FontCharsetService.RecoverPending(); Debug.Log("字体事务恢复完成。"); }

        private static void RunMenu(bool update)
        {
            FontCharsetReport result;
            try
            {
                var config = FontCharsetConfig.Load();
                result = update ? FontCharsetService.Update(config, true) : FontCharsetService.Scan(config, false);
            }
            catch (Exception ex)
            {
                result = new FontCharsetReport { success = false, operation = update ? "update" : "scan", message = ex.Message };
            }
            FontCharsetService.WriteReport(result);
            foreach (var window in Resources.FindObjectsOfTypeAll<FontCharsetWindow>())
                if (window.config == FontCharsetConfig.Load()) { window.report = result; window.page = 0; window.Repaint(); }
            Debug.Log(result.message + " 报告: " + FontCharsetService.ReportPath);
        }

        private void OnEnable()
        {
            minSize = new Vector2(660, 480);
            // Start with an explicit check, rather than presenting stale reports as current state.
            SetConfig(FontCharsetConfig.Load());
        }
        private void SetConfig(FontCharsetConfig value)
        {
            config = value; serialized = null; report = null;
            languageIndex = 0; detailIndex = -1; page = 0;
            advanced = showInputs = showProject = showFailure = false;
            scroll = Vector2.zero;
        }
        private void OnDisable() { stylePreview?.Dispose(); stylePreview = null; }
        private void OnProjectChange() { stylePreview?.Invalidate(); Repaint(); }
        private void OnInspectorUpdate() { if (page == 2) Repaint(); }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(16, 16, 12, 12) }))
            {
                EditorGUILayout.LabelField("字体与字符集", new GUIStyle(EditorStyles.boldLabel) { fontSize = 19 }, GUILayout.Height(28));
                EditorGUILayout.LabelField("保存多语言文案后，在这里检查并补齐字体。", EditorStyles.miniLabel);
                GUILayout.Space(12);
                int next = GUILayout.Toolbar(page, Pages, GUILayout.Height(28));
                if (next != page) { page = next; scroll = Vector2.zero; showFailure = false; }
                GUILayout.Space(12);
                if (config == null) { DrawSetup(); return; }
                if (serialized == null || serialized.targetObject != config) serialized = new SerializedObject(config);
                if (FontCharsetService.HasPendingRecovery)
                {
                    EditorGUILayout.HelpBox("上次更新未完成，请先恢复，再继续操作。", MessageType.Error);
                    using (new EditorGUI.DisabledScope(Unavailable))
                        if (GUILayout.Button("恢复上次更新")) Execute(() => { FontCharsetService.RecoverPending(); return null; });
                }
                if (Unavailable) EditorGUILayout.HelpBox("请在编辑态、编译完成后操作。", MessageType.Info);
                using (new EditorGUI.DisabledScope(Unavailable))
                {
                    scroll = EditorGUILayout.BeginScrollView(scroll);
                    if (page == 0) DrawUpdate();
                    else if (page == 1) DrawSettings();
                    else DrawPreviewPage();
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawSetup()
        {
            GUILayout.Space(20);
            EditorGUILayout.LabelField("首次使用", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("创建简中、繁中、日语、韩语的配置，即可开始检查。", EditorStyles.wordWrappedLabel);
            GUILayout.Space(12);
            using (new EditorGUI.DisabledScope(Unavailable))
            {
                if (GUILayout.Button("创建四语言配置", GUILayout.Height(34)))
                {
                    try { SetConfig(FontCharsetConfig.CreateDefault()); }
                    catch (Exception ex) { report = new FontCharsetReport { message = ex.Message }; }
                }
                GUILayout.Space(12);
                var chosen = (FontCharsetConfig)EditorGUILayout.ObjectField("使用已有配置", config, typeof(FontCharsetConfig), false);
                if (chosen != config) SetConfig(chosen);
            }
            if (report != null) EditorGUILayout.HelpBox(report.message, MessageType.Error);
        }

        private void DrawUpdate()
        {
            var selected = config.entries.Where(e => e.selected).ToArray();
            bool checkedSelection = selected.Length > 0 && report != null && report.success && selected.All(e => Result(e) != null);
            bool needsUpdate = checkedSelection && selected.Any(e => Result(e).status == "待更新");
            bool needsSave = report != null && report.unsavedAssetPaths.Length > 0;
            string nextStep = selected.Length == 0 ? "先勾选需要处理的语言。" :
                needsSave ? "保存未完成，可点击下方「重试更新」。工具会自动保存相关资产后继续。" :
                report != null && !report.success ? "检查未通过，请查看对应语言的详情或调整字体设置。" :
                !checkedSelection ? "选好语言后，点击「检查选中语言」。" :
                needsUpdate ? "检查完成。点击「更新字符集与图集」即可同步。" : "选中的语言已同步，无需更新。";
            EditorGUILayout.HelpBox(nextStep, report != null && !report.success ? MessageType.Warning : MessageType.Info);
            GUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("1  选择语言", EditorStyles.boldLabel);
                if (GUILayout.Button("全选", EditorStyles.miniButtonLeft, GUILayout.Width(46))) SelectAll(true);
                if (GUILayout.Button("清空", EditorStyles.miniButtonRight, GUILayout.Width(46))) SelectAll(false);
            }
            GUILayout.Space(5);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < config.entries.Count; i++)
                {
                    var entry = config.entries[i];
                    var row = Result(entry);
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(34)))
                    {
                        bool value = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(18));
                        if (value != entry.selected) { entry.selected = value; EditorUtility.SetDirty(config); }
                        EditorGUILayout.LabelField(LanguageName(entry.language), EditorStyles.boldLabel, GUILayout.Width(68));
                        EditorGUILayout.LabelField(entry.targetFont == null ? "未设置字体" : entry.targetFont.name, EditorStyles.miniLabel, GUILayout.MinWidth(90));
                        EditorGUILayout.LabelField(Status(row), GUILayout.Width(130));
                        using (new EditorGUI.DisabledScope(row == null))
                            if (GUILayout.Button(detailIndex == i ? "收起" : "详情", EditorStyles.miniButton, GUILayout.Width(44))) detailIndex = detailIndex == i ? -1 : i;
                        if (GUILayout.Button("设置", EditorStyles.miniButton, GUILayout.Width(44))) GoToLanguage(i, 1);
                    }
                }
            }
            GUILayout.Space(14);
            EditorGUILayout.LabelField("2  检查并更新", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(selected.Length == 0))
                    if (GUILayout.Button("检查选中语言", GUILayout.Height(34))) { detailIndex = -1; Execute(() => FontCharsetService.Scan(config, true)); }
                using (new EditorGUI.DisabledScope(!needsUpdate || FontCharsetService.HasPendingRecovery))
                {
                    Color previous = GUI.backgroundColor;
                    if (needsUpdate) GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                    if (GUILayout.Button("更新字符集与图集", GUILayout.Height(34))) UpdateFonts();
                    GUI.backgroundColor = previous;
                }
            }
            EditorGUILayout.LabelField("更新会自动保存相关字体、材质与配置，再备份并生成；保留已有字符。", EditorStyles.wordWrappedMiniLabel);
            if (report != null && !report.success)
            {
                GUILayout.Space(8);
                if (needsSave)
                {
                    EditorGUILayout.HelpBox(report.message, MessageType.Warning);
                    using (new EditorGUI.DisabledScope(FontCharsetService.HasPendingRecovery))
                        if (GUILayout.Button("重试更新", GUILayout.Height(30))) UpdateFonts();
                }
                else
                {
                    showFailure = EditorGUILayout.Foldout(showFailure, "查看失败原因", true);
                    if (showFailure) EditorGUILayout.HelpBox(report.message, MessageType.Warning);
                }
            }
            if (detailIndex >= 0 && detailIndex < config.entries.Count) DrawDetails(detailIndex);
            if (!string.IsNullOrEmpty(report?.backupPath))
            {
                GUILayout.Space(10);
                if (GUILayout.Button("打开本次备份", EditorStyles.linkLabel)) EditorUtility.RevealInFinder(report.backupPath);
            }
        }

        private FontCharsetScan Result(FontCharsetEntry entry) => report?.languages.FirstOrDefault(r => r.language == entry.language && r.fontPath == AssetDatabase.GetAssetPath(entry.targetFont));
        private static string Status(FontCharsetScan row)
        {
            if (row == null) return "未检查";
            if (row.status == "已更新" || row.status == "无需更新") return "已同步";
            if (row.missingSource.Length > 0) return "源字体缺 " + row.missingSource.Length + " 字";
            if (row.status == "图集容量不足") return "图集放不下";
            int count = Math.Max(row.append.Length, row.missingAtlas.Length);
            return count > 0 ? "待补 " + count + " 个字符" : "需生成图集";
        }
        private void SelectAll(bool value)
        {
            foreach (var entry in config.entries) entry.selected = value;
            EditorUtility.SetDirty(config);
        }
        private void DrawDetails(int index)
        {
            var entry = config.entries[index];
            var row = Result(entry);
            if (row == null) return;
            GUILayout.Space(14);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(LanguageName(entry.language) + " · 检查详情", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("所需 " + row.required.Length + "  /  字符集新增 " + row.append.Length + "  /  图集缺字 " + row.missingAtlas.Length);
                EditorGUILayout.LabelField((row.status == "已更新" ? "实际生成字号：" : "当前字体字号：") + row.actualPointSize);
                if (row.append.Length > 0)
                {
                    EditorGUILayout.LabelField("新增字符", EditorStyles.miniLabel);
                    EditorGUILayout.TextArea(FontCharsetCollector.Text(row.append), GUILayout.MinHeight(40));
                }
                if (!string.IsNullOrEmpty(row.details)) EditorGUILayout.HelpBox(row.details, MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("调整此语言设置")) GoToLanguage(index, 1);
                    if (GUILayout.Button("预览此字体")) GoToLanguage(index, 2);
                }
            }
        }

        private void DrawSettings()
        {
            if (!ChooseLanguage()) return;
            serialized.Update();
            var row = serialized.FindProperty("entries").GetArrayElementAtIndex(languageIndex);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("字体", EditorStyles.boldLabel);
            Draw(row, "sourceFont", "源字体");
            using (new EditorGUI.DisabledScope(true)) Draw(row, "targetFont", "输出字体");
            EditorGUILayout.LabelField("替换源字体会重建字形，并可能影响页面换行。", EditorStyles.miniLabel);
            GUILayout.Space(12);
            EditorGUILayout.LabelField("补充文字", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("文案会从本地化 XML 读取。这里只填动态名称、数字等额外内容。", EditorStyles.wordWrappedMiniLabel);
            var extra = row.FindPropertyRelative("extraText");
            extra.stringValue = EditorGUILayout.TextArea(extra.stringValue, GUILayout.Height(55));
            GUILayout.Space(12);
            advanced = EditorGUILayout.Foldout(advanced, "图集参数 · 容量不足时调整", true);
            if (advanced)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var autoSize = row.FindPropertyRelative("autoSize");
                    autoSize.boolValue = EditorGUILayout.Popup("采样字号", autoSize.boolValue ? 0 : 1, new[] { "自动大小 (Auto Sizing)", "自定义 (Custom Size)" }) == 0;
                    if (!autoSize.boolValue) Draw(row, "pointSize", "自定义字号");
                    Draw(row, "padding", "字形间距 Padding");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("图集尺寸");
                        var width = row.FindPropertyRelative("atlasWidth");
                        var height = row.FindPropertyRelative("atlasHeight");
                        width.intValue = EditorGUILayout.IntPopup(width.intValue, AtlasSizeLabels, AtlasSizes);
                        GUILayout.Label("×", GUILayout.Width(12));
                        height.intValue = EditorGUILayout.IntPopup(height.intValue, AtlasSizeLabels, AtlasSizes);
                    }
                    Draw(row, "renderMode", "渲染模式");
                    EditorGUILayout.LabelField(autoSize.boolValue ? "在指定图集内自动选择字号（1–512），不会扩大图集。" : "保持指定字号；放不下时报告容量不足。", EditorStyles.miniLabel);
                }
            }
            showInputs = EditorGUILayout.Foldout(showInputs, "文字来源与文件映射", true);
            if (showInputs)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    Draw(row, "charset", "字符集文件");
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("localizationFiles"), new GUIContent("本地化 XML"), true);
                    Draw(row, "targetFont", "输出字体"); Draw(row, "language", "语言 Key");
                }
            }
            showProject = EditorGUILayout.Foldout(showProject, "项目配置", true);
            FontCharsetConfig chosen = config;
            if (showProject)
            {
                EditorGUILayout.PropertyField(serialized.FindProperty("languageConfig"), new GUIContent("运行时语言映射"));
                chosen = (FontCharsetConfig)EditorGUILayout.ObjectField("制作配置", config, typeof(FontCharsetConfig), false);
            }
            if (serialized.ApplyModifiedProperties()) report = null;
            if (chosen != config) { SetConfig(chosen); return; }
            GUILayout.Space(16);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存设置并去检查", GUILayout.Height(32))) { SaveConfig(); page = 0; report = null; scroll = Vector2.zero; }
                if (GUILayout.Button("预览此字体", GUILayout.Height(32), GUILayout.Width(130))) GoToLanguage(languageIndex, 2);
            }
        }

        private bool ChooseLanguage()
        {
            if (config.entries.Count == 0) { EditorGUILayout.HelpBox("当前配置没有语言条目。", MessageType.Warning); return false; }
            languageIndex = Mathf.Clamp(languageIndex, 0, config.entries.Count - 1);
            int next = EditorGUILayout.Popup("当前语言", languageIndex, config.entries.Select(e => LanguageName(e.language)).ToArray());
            if (next != languageIndex) { languageIndex = next; advanced = showInputs = false; }
            return true;
        }
        private void GoToLanguage(int index, int targetPage)
        {
            languageIndex = index; page = targetPage; scroll = Vector2.zero;
            advanced = Result(config.entries[index])?.status == "图集容量不足";
        }
        private void DrawPreviewPage()
        {
            if (!ChooseLanguage()) return;
            var entry = config.entries[languageIndex];
            if (stylePreview == null) stylePreview = new FontStylePreviewPanel();
            stylePreview.Font = entry.targetFont;
            if (previewLanguage != entry.language)
            {
                previewLanguage = entry.language;
                stylePreview.Sample = Sample(entry.language);
                previewProfile = config.languageConfig != null &&
                    Enum.TryParse(entry.language, out GameFramework.Localization.Language language)
                    ? config.languageConfig.GetProfile(language) : null;
            }
            GUILayout.Space(8);
            previewSource = EditorGUILayout.Popup("样式来源", previewSource, new[] { "字体默认材质", "直接选择预设", "Profile / 样式键" });
            FontStylePreset selected = null;
            if (previewSource == 1)
            {
                previewPreset = (FontStylePreset)EditorGUILayout.ObjectField("样式预设", previewPreset, typeof(FontStylePreset), false);
                selected = previewPreset;
            }
            else if (previewSource == 2)
            {
                previewProfile = (TMPFontProfile)EditorGUILayout.ObjectField("Font Profile", previewProfile, typeof(TMPFontProfile), false);
                if (previewProfile != null && previewProfile.styles != null)
                {
                    var keys = previewProfile.styles.Where(e => e != null && !string.IsNullOrEmpty(e.key)).Select(e => e.key).Distinct().ToList();
                    var labels = new[] { "（空键：默认材质）" }.Concat(keys).ToArray();
                    int index = keys.IndexOf(previewStyleKey) + 1;
                    index = EditorGUILayout.Popup("Style Key", index, labels);
                    previewStyleKey = index == 0 ? "" : keys[index - 1];
                    // Match the runtime's case-sensitive, first-entry-wins lookup.
                    selected = string.IsNullOrEmpty(previewStyleKey) ? null :
                        previewProfile.styles.FirstOrDefault(e => e != null && e.key == previewStyleKey)?.preset;
                    if (previewProfile.styles.Any(e => e == null))
                        EditorGUILayout.HelpBox("Profile 含空条目，请修复配置后再做运行验证。", MessageType.Warning);
                    if (!string.IsNullOrEmpty(previewStyleKey) && previewProfile.styles.Count(e => e != null && e.key == previewStyleKey) > 1)
                        EditorGUILayout.HelpBox("样式键重复：与运行时一致，使用第一条。", MessageType.Warning);
                    if (!string.IsNullOrEmpty(previewStyleKey) && selected == null)
                        EditorGUILayout.HelpBox("此键没有预设，使用字体默认材质。", MessageType.Info);
                }
                else EditorGUILayout.HelpBox("请选择已配置样式的 Profile。", MessageType.Info);
            }
            if (selected != null && GUILayout.Button("编辑此预设"))
            {
                Selection.activeObject = selected;
                EditorGUIUtility.PingObject(selected);
            }
            stylePreview.DrawControls();
            stylePreview.DrawPreview(selected);
            EditorGUILayout.LabelField("样式与运行时共用 ApplyTo；实际页面的布局、裁切与运行时缓存仍需在游戏中检查。", EditorStyles.wordWrappedMiniLabel);
        }
        private static string LanguageName(string key)
        {
            switch (key)
            {
                case "ChineseSimplified": return "简体中文";
                case "ChineseTraditional": return "繁体中文";
                case "Japanese": return "日语";
                case "Korean": return "韩语";
                default: return key;
            }
        }
        private static string Sample(string key)
        {
            switch (key)
            {
                case "ChineseSimplified": return "结算奖励 0123456789";
                case "ChineseTraditional": return "結算獎勵 0123456789";
                case "Japanese": return "報酬を受け取る 0123456789";
                case "Korean": return "보상 받기 0123456789";
                default: return "0123456789";
            }
        }
        private static void Draw(SerializedProperty row, string name, string label) => EditorGUILayout.PropertyField(row.FindPropertyRelative(name), new GUIContent(label));
        private void SaveConfig() { serialized?.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(config); }
        private void Execute(Func<FontCharsetReport> action)
        {
            try { report = action(); if (report != null) FontCharsetService.WriteReport(report); }
            catch (Exception ex) { report = new FontCharsetReport { message = ex.Message, success = false }; FontCharsetService.WriteReport(report); }
            finally { EditorUtility.ClearProgressBar(); Repaint(); }
        }
        private void UpdateFonts()
        {
            SaveConfig();
            stylePreview?.Invalidate();
            Execute(() => FontCharsetService.Update(config, true, (message, progress) => EditorUtility.DisplayCancelableProgressBar("更新字体与字符集", message, progress)));
        }
    }
}
