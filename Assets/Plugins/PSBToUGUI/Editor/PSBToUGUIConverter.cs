using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// PSB To UGUI — 通用转换器
/// 从 PSB 文件读取图层坐标和分组，配合手动切图，一键生成符合 GameFramework 规范的 UGUI 预制体。
/// </summary>
public static class PSBToUGUIConverter
{
    // ═══════════════════════════════════════════
    //  数据结构
    // ═══════════════════════════════════════════

    struct PSDLayer
    {
        public int index;
        public string name;
        public int parentIndex;
        public bool isGroup;
    }

    struct SpriteMeta
    {
        public string name;
        public Vector2 spritePosition; // PSD 坐标，Y 从文档底部算起
    }

    /// <summary>转换配置</summary>
    public class Config
    {
        public string psbPath;
        public string panelName;                                             // e.g. "SignInUIPanel"
        public string prefabOutputDir  = "Assets/GameMain/UI/UIPanel";
        public string scriptOutputDir  = "Assets/GameMain/Scripts/UI/Panel";
        public bool   generateScript   = true;
        public bool   registerInGF     = false;
        public string uiGroupName      = "Default";
    }

    // ═══════════════════════════════════════════
    //  菜单入口
    // ═══════════════════════════════════════════

    [MenuItem("Assets/PSB To UGUI", true)]
    static bool ValidateMenu()
    {
        return Selection.activeObject != null &&
               AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".psb", StringComparison.OrdinalIgnoreCase);
    }

    [MenuItem("Assets/PSB To UGUI")]
    static void MenuFromSelection()
    {
        PSBToUGUIConfigWindow.Open(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Tools/PSB To UGUI")]
    static void MenuFromTools()
    {
        PSBToUGUIConfigWindow.Open(null);
    }

    // ═══════════════════════════════════════════
    //  核心转换流程
    // ═══════════════════════════════════════════

    public static void Convert(Config cfg)
    {
        if (string.IsNullOrEmpty(cfg.psbPath) || !File.Exists(cfg.psbPath))
        {
            Debug.LogError("[PSB2UI] PSB 文件不存在: " + cfg.psbPath);
            return;
        }

        // ── Step 0: 确保 PSD Importer 已安装 ──
        if (!EnsurePSDImporterInstalled())
            return;

        EditorUtility.DisplayProgressBar("PSB To UGUI", "正在导入 PSB...", 0.1f);

        try
        {
            // ── Step 1: 导入 PSB（确保 Character Mode 开启） ──
            ForceCharacterMode(cfg.psbPath);
            var importer = AssetImporter.GetAtPath(cfg.psbPath);
            if (importer == null) { Error("无法获取 Importer"); return; }
            var iType = importer.GetType();

            // ── Step 2: 读取 PSD 参数 ──
            EditorUtility.DisplayProgressBar("PSB To UGUI", "读取图层数据...", 0.2f);
            Vector2 canvasSize = RV(importer, iType, "canvasSize", new Vector2(1080, 1920));
            float cw = canvasSize.x, ch = canvasSize.y;
            Log($"Canvas: {cw}x{ch}");

            // ── Step 3: 图层结构 + 坐标 ──
            var psd = ReadPSDLayers(importer, iType);
            var meta = ReadSpriteMetaData(importer, iType);

            if (psd.Count == 0 || meta.Count == 0)
            {
                Error("PSD 图层数据为空！\n" +
                      "请手动操作：在 Inspector 中选中 PSB 文件，勾选 'Character Rig'，点击 'Apply'，然后重新运行。\n" +
                      "（如果已经勾选过，请尝试取消勾选 → Apply → 重新勾选 → Apply）");
                return;
            }

            var leaves = psd.Values.Where(l => !l.isGroup).ToList();
            // 按名称匹配坐标，避免顺序错位
            var metaByName = meta
                .Where(m => !string.IsNullOrEmpty(m.name))
                .GroupBy(m => m.name)
                .ToDictionary(g => g.Key, g => g.First().spritePosition);
            var posMap = new Dictionary<int, Vector2>();
            foreach (var leaf in leaves)
            {
                string n = leaf.name.Trim();
                if (metaByName.TryGetValue(n, out Vector2 pos) || metaByName.TryGetValue(leaf.name, out pos))
                    posMap[leaf.index] = pos;
            }

            var topItems = psd.Values
                .Where(l => l.parentIndex == -1)
                .OrderByDescending(l => l.index)
                .ToList();

            Log($"图层: {psd.Count} (根节点:{topItems.Count}, 叶:{leaves.Count})");

            // ── Step 4: 查找切图目录 ──
            EditorUtility.DisplayProgressBar("PSB To UGUI", "匹配切图...", 0.4f);
            string spritesDir = FindSpritesDir(cfg.psbPath);
            if (string.IsNullOrEmpty(spritesDir))
            {
                Error($"找不到切图目录！请创建: {Path.GetDirectoryName(cfg.psbPath)}/{Path.GetFileNameWithoutExtension(cfg.psbPath)}_sprites/");
                return;
            }
            EnsureSpritesImported(spritesDir);
            Log($"切图目录: {spritesDir}");

            // ── Step 5: 创建预制体（GF 规范：不带 Canvas） ──
            EditorUtility.DisplayProgressBar("PSB To UGUI", "生成 UGUI...", 0.6f);

            var rootGO = new GameObject(cfg.panelName);
            rootGO.layer = LayerMask.NameToLayer("UI"); // GF 规范: UI 层
            var rootRT = rootGO.AddComponent<RectTransform>();
            // GF 规范: stretch 全屏
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            int created = 0, missed = 0;
            var missedList = new List<string>();
            var btnNames = new List<string>(); // 收集按钮名用于生成脚本

            int uiLayer = LayerMask.NameToLayer("UI");

            // 递归构建层级结构，支持任意深度嵌套
            void BuildLayer(RectTransform parentRT, int parentIndex)
            {
                var children = psd.Values
                    .Where(l => l.parentIndex == parentIndex)
                    .OrderByDescending(l => l.index)
                    .ToList();

                foreach (var layer in children)
                {
                    string lName = layer.name.Trim();

                    if (layer.isGroup)
                    {
                        // 组节点：创建空 RectTransform 容器，递归处理子节点
                        var gGO = new GameObject(lName);
                        gGO.layer = uiLayer;
                        gGO.transform.SetParent(parentRT, false);
                        var gRT = gGO.AddComponent<RectTransform>();
                        gRT.anchorMin = Vector2.zero;
                        gRT.anchorMax = Vector2.one;
                        gRT.offsetMin = gRT.offsetMax = Vector2.zero;
                        BuildLayer(gRT, layer.index);
                    }
                    else
                    {
                        // 叶节点：查找切图并创建 Image
                        Sprite sprite = FindSpriteRecursive(spritesDir, lName);
                        if (sprite == null) { missedList.Add(lName); missed++; continue; }
                        if (!posMap.TryGetValue(layer.index, out Vector2 sprPos)) continue;

                        float sw = sprite.rect.width, sh = sprite.rect.height;
                        float ax = sprPos.x + sw / 2f - cw / 2f;
                        float ay = sprPos.y + sh / 2f - ch / 2f;

                        var go = new GameObject(lName);
                        go.layer = uiLayer;
                        go.transform.SetParent(parentRT, false);
                        var img = go.AddComponent<Image>();
                        img.sprite = sprite;
                        img.color = Color.white;
                        img.type = Image.Type.Simple;

                        bool isBtn = lName.StartsWith("btn_") || lName.StartsWith("btn ");
                        img.raycastTarget = isBtn;
                        if (isBtn)
                        {
                            go.AddComponent<Button>();
                            btnNames.Add(lName);
                        }

                        var rt = go.GetComponent<RectTransform>();
                        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = new Vector2(ax, ay);
                        rt.sizeDelta = new Vector2(sw, sh);
                        created++;
                    }
                }
            }

            BuildLayer(rootRT, -1);

            // ── Step 6: 保存预制体（先不挂脚本） ──
            EditorUtility.DisplayProgressBar("PSB To UGUI", "保存预制体...", 0.7f);
            string prefabPath = $"{cfg.prefabOutputDir}/{cfg.panelName}.prefab";
            EnsureDirectory(cfg.prefabOutputDir);
            PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
            UnityEngine.Object.DestroyImmediate(rootGO);
            Log($"预制体: {prefabPath}");

            // ── Step 7: GF 注册 ──
            if (cfg.registerInGF)
            {
                EditorUtility.DisplayProgressBar("PSB To UGUI", "注册到 GameFramework...", 0.75f);
                RegisterInGF(cfg);
            }

            if (missed > 0)
            {
                Debug.LogWarning($"[PSB2UI] 缺失 {missed} 张切图:");
                foreach (var m in missedList)
                    Debug.LogWarning($"[PSB2UI]   {m}.png");
            }

            // ── Step 8: 生成脚本 ──
            if (cfg.generateScript)
            {
                EditorUtility.DisplayProgressBar("PSB To UGUI", "生成脚本...", 0.8f);
                EnsureDirectory(cfg.scriptOutputDir);
                string scriptPath = $"{cfg.scriptOutputDir}/{cfg.panelName}.cs";
                GenerateScript(scriptPath, cfg.panelName, btnNames);
                Log($"脚本: {scriptPath}");

                // 将待绑定信息保存到 EditorPrefs，编译完成后 [DidReloadScripts] 自动继续
                EditorPrefs.SetString("PSB2UI_PendingPrefab", prefabPath);
                EditorPrefs.SetString("PSB2UI_PendingPanel", cfg.panelName);
                EditorPrefs.SetString("PSB2UI_PendingBtns", string.Join("|", btnNames));

                Log($"元素: {created}, 根节点: {topItems.Count}");
                Log("脚本已生成，等待 Unity 编译完成后自动挂载组件...");
            }
            else
            {
                Log($"═══ 转换完成 ═══  元素: {created}, 根节点: {topItems.Count}");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ═══════════════════════════════════════════
    //  编译完成后自动挂载脚本 + 绑定引用
    // ═══════════════════════════════════════════

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded()
    {
        string prefabPath = EditorPrefs.GetString("PSB2UI_PendingPrefab", "");
        string panelName = EditorPrefs.GetString("PSB2UI_PendingPanel", "");
        string btnsStr = EditorPrefs.GetString("PSB2UI_PendingBtns", "");

        if (string.IsNullOrEmpty(prefabPath) || string.IsNullOrEmpty(panelName))
            return;

        // 清除待处理标记
        EditorPrefs.DeleteKey("PSB2UI_PendingPrefab");
        EditorPrefs.DeleteKey("PSB2UI_PendingPanel");
        EditorPrefs.DeleteKey("PSB2UI_PendingBtns");

        Log("编译完成，自动挂载脚本和绑定引用...");

        var scriptType = FindType(panelName);
        if (scriptType == null)
        {
            Error($"编译后找不到类型 '{panelName}'，请检查脚本是否有编译错误");
            return;
        }

        // 加载预制体 → 添加组件 → 绑定 SerializeField → 保存
        var contents = PrefabUtility.LoadPrefabContents(prefabPath);
        if (contents.GetComponent(scriptType) == null)
            contents.AddComponent(scriptType);

        var script = contents.GetComponent(scriptType);
        if (script != null && !string.IsNullOrEmpty(btnsStr))
        {
            var btnNames = btnsStr.Split('|').ToList();
            var so = new SerializedObject(script);
            var usedFields = new HashSet<string>();
            int wired = 0;

            foreach (var btn in btnNames)
            {
                if (string.IsNullOrEmpty(btn)) continue;
                string fieldName = BtnToFieldName(btn);
                if (usedFields.Contains(fieldName)) continue;
                usedFields.Add(fieldName);

                var prop = so.FindProperty(fieldName);
                if (prop == null) continue;

                var allBtns = contents.GetComponentsInChildren<Button>(true);
                var target = allBtns.FirstOrDefault(b => b.gameObject.name == btn);
                if (target != null)
                {
                    prop.objectReferenceValue = target;
                    wired++;
                }
            }
            so.ApplyModifiedProperties();
            Log($"SerializeField 绑定: {wired}/{usedFields.Count}");
        }

        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        Log("═══ 全部完成！脚本已挂载，引用已绑定 ═══");
    }

    // ═══════════════════════════════════════════
    //  切图查找
    // ═══════════════════════════════════════════

    static string FindSpritesDir(string psbPath)
    {
        string dir = Path.GetDirectoryName(psbPath);
        string name = Path.GetFileNameWithoutExtension(psbPath);
        string[] candidates = {
            $"{dir}/{name}_sprites",
            $"{dir}/sprites",
            $"{dir}/{name}"
        };
        foreach (var c in candidates)
            if (Directory.Exists(c)) return c;
        return null;
    }

    static Sprite FindSprite(string dir, string groupName, string layerName)
    {
        // 优先: {dir}/{group}/{name}.png
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{groupName}/{layerName}.png");
        if (s != null) return s;
        // 其次: {dir}/{name}.png
        s = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{layerName}.png");
        if (s != null) return s;
        // 去空格
        string t = layerName.Trim();
        if (t != layerName)
        {
            s = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{groupName}/{t}.png");
            if (s != null) return s;
            s = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{t}.png");
        }
        return s;
    }

    /// <summary>
    /// 递归查找切图：先在 dir 根目录找，再在所有子目录找，支持嵌套层级的 PSB。
    /// </summary>
    static Sprite FindSpriteRecursive(string dir, string layerName)
    {
        // 1. 根目录直接匹配
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{layerName}.png");
        if (s != null) return s;

        // 2. 去空格后再试
        string t = layerName.Trim();
        if (t != layerName)
        {
            s = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{t}.png");
            if (s != null) return s;
        }

        // 3. 在子目录中递归查找
        if (Directory.Exists(dir))
        {
            foreach (var sub in Directory.GetDirectories(dir))
            {
                s = FindSpriteRecursive(sub.Replace('\\', '/'), layerName);
                if (s != null) return s;
            }
        }
        return null;
    }

    static void EnsureSpritesImported(string dir)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:texture2D", new[] { dir }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.SaveAndReimport();
            }
        }
    }

    // ═══════════════════════════════════════════
    //  C# 脚本生成
    // ═══════════════════════════════════════════

    static void GenerateScript(string path, string panelName, List<string> btnNames)
    {
        // 检测 GF 是否可用
        bool hasGF = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetType("UnityGameFramework.Runtime.UIFormLogic") != null);

        // 自动检测项目命名空间（从 UGuiForm.cs 或其他 UI 脚本读取）
        string ns = DetectNamespace(path);

        // 如果脚本已存在且有用户代码，只更新 auto-generated 区域
        bool fileExists = File.Exists(path);
        string existingContent = fileExists ? File.ReadAllText(path) : "";
        bool hasMarkers = existingContent.Contains("#region Auto-Generated Fields");

        if (fileExists && !hasMarkers)
        {
            Debug.LogWarning($"[PSB2UI] 脚本已存在且无 auto-generated 标记，跳过生成: {path}");
            return;
        }

        // 生成字段和绑定代码
        var fields = new StringBuilder();
        var bindings = new StringBuilder();
        var handlers = new StringBuilder();
        string indent = string.IsNullOrEmpty(ns) ? "    " : "        ";

        // 去重：同名按钮只生成一个字段
        var usedFieldNames = new HashSet<string>();

        foreach (var btn in btnNames)
        {
            string fieldName = BtnToFieldName(btn);
            string methodName = BtnToMethodName(btn);

            if (usedFieldNames.Contains(fieldName))
                continue;
            usedFieldNames.Add(fieldName);

            fields.AppendLine($"{indent}[SerializeField] private Button {fieldName};");
            bindings.AppendLine($"{indent}    {fieldName}.onClick.AddListener({methodName});");
            handlers.AppendLine($"{indent}private void {methodName}()");
            handlers.AppendLine($"{indent}{{");
            if (btn.Contains("close"))
                handlers.AppendLine($"{indent}    Close();");
            else
                handlers.AppendLine($"{indent}    // TODO: 实现按钮逻辑");
            handlers.AppendLine($"{indent}}}");
            handlers.AppendLine();
        }

        string baseClass = hasGF ? "UGuiForm" : "MonoBehaviour";
        string initMethod = hasGF ? "OnInit" : "Awake";
        string initParam = hasGF ? "object userData" : "";
        string initBase = hasGF ? "base.OnInit(userData);" : "";

        var sb = new StringBuilder();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"{indent}public class {panelName} : {baseClass}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}#region Auto-Generated Fields");
        sb.Append(fields);
        sb.AppendLine($"{indent}#endregion");
        sb.AppendLine();

        if (hasGF)
        {
            sb.AppendLine($"{indent}protected override void {initMethod}({initParam})");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    {initBase}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    #region Auto-Generated Bindings");
            sb.Append(bindings);
            sb.AppendLine($"{indent}    #endregion");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
            sb.AppendLine($"{indent}protected override void OnClose(bool isShutdown, object userData)");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    base.OnClose(isShutdown, userData);");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            sb.AppendLine($"{indent}void {initMethod}()");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    #region Auto-Generated Bindings");
            sb.Append(bindings);
            sb.AppendLine($"{indent}    #endregion");
            sb.AppendLine($"{indent}}}");
        }

        sb.AppendLine();
        sb.AppendLine($"{indent}// ── Button Handlers ──");
        sb.Append(handlers);
        sb.AppendLine($"{indent}}}");

        if (!string.IsNullOrEmpty(ns))
            sb.AppendLine("}");

        File.WriteAllText(path, sb.ToString());
        Log($"脚本生成完成 (namespace: {(string.IsNullOrEmpty(ns) ? "无" : ns)})");
    }

    /// <summary>
    /// 从项目中自动检测命名空间：优先找同目录的 .cs，其次找 UGuiForm.cs
    /// </summary>
    static string DetectNamespace(string scriptPath)
    {
        // 1. 先看同目录下其他 .cs 文件的命名空间
        string dir = Path.GetDirectoryName(scriptPath);
        if (Directory.Exists(dir))
        {
            foreach (var cs in Directory.GetFiles(dir, "*.cs"))
            {
                if (cs == scriptPath) continue;
                string ns = ExtractNamespace(cs);
                if (!string.IsNullOrEmpty(ns)) return ns;
            }
        }

        // 2. 再找 UGuiForm.cs
        var guids = AssetDatabase.FindAssets("UGuiForm t:script");
        foreach (var guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith("UGuiForm.cs"))
            {
                string ns = ExtractNamespace(p);
                if (!string.IsNullOrEmpty(ns)) return ns;
            }
        }

        return "";
    }

    static string ExtractNamespace(string csPath)
    {
        try
        {
            foreach (var line in File.ReadLines(csPath))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("namespace "))
                {
                    string ns = trimmed.Substring("namespace ".Length).Trim().TrimEnd('{').Trim();
                    return ns;
                }
            }
        }
        catch { }
        return "";
    }

    static string BtnToFieldName(string btnName)
    {
        // "btn_close" → "m_BtnClose", "btn claimed1" → "m_BtnClaimed1"
        string raw = btnName;
        if (raw.StartsWith("btn_")) raw = raw.Substring(4);
        else if (raw.StartsWith("btn ")) raw = raw.Substring(4);
        return "m_Btn" + ToPascalCase(raw);
    }

    static string BtnToMethodName(string btnName)
    {
        string raw = btnName;
        if (raw.StartsWith("btn_")) raw = raw.Substring(4);
        else if (raw.StartsWith("btn ")) raw = raw.Substring(4);
        return "OnClick" + ToPascalCase(raw);
    }

    static string ToPascalCase(string s)
    {
        var sb = new StringBuilder();
        bool nextUpper = true;
        foreach (char c in s)
        {
            if (c == '_' || c == ' ' || c == '-') { nextUpper = true; continue; }
            sb.Append(nextUpper ? char.ToUpper(c) : c);
            nextUpper = false;
        }
        return sb.ToString();
    }

    // ═══════════════════════════════════════════
    //  SerializeField 自动绑定
    // ═══════════════════════════════════════════

    static void ScheduleWiring(string prefabPath, string panelName, List<string> btnNames)
    {
        int retries = 0;
        void TryWire()
        {
            if (EditorApplication.isCompiling && retries < 30)
            {
                retries++;
                EditorApplication.delayCall += TryWire;
                return;
            }

            WireSerializedFields(prefabPath, panelName, btnNames);
        }

        EditorApplication.delayCall += TryWire;
    }

    static void WireSerializedFields(string prefabPath, string panelName, List<string> btnNames)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning("[PSB2UI] 无法加载预制体进行绑定"); return; }

        var contents = PrefabUtility.LoadPrefabContents(prefabPath);
        var script = contents.GetComponent(panelName);
        if (script == null)
        {
            // 尝试添加脚本组件
            var scriptType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == panelName);

            if (scriptType != null)
                script = contents.AddComponent(scriptType);
        }

        if (script == null)
        {
            Debug.LogWarning($"[PSB2UI] 找不到脚本类型 '{panelName}'，SerializeField 未绑定（编译完成后手动绑定或重新运行工具）");
            PrefabUtility.UnloadPrefabContents(contents);
            return;
        }

        var so = new SerializedObject(script);
        int wired = 0;

        foreach (var btn in btnNames)
        {
            string fieldName = BtnToFieldName(btn);
            var prop = so.FindProperty(fieldName);
            if (prop == null) continue;

            // 在预制体层级中按名称查找 Button
            var allBtns = contents.GetComponentsInChildren<Button>(true);
            var target = allBtns.FirstOrDefault(b => b.gameObject.name == btn);
            if (target != null)
            {
                prop.objectReferenceValue = target;
                wired++;
            }
        }

        so.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);

        Log($"SerializeField 绑定: {wired}/{btnNames.Count}");
    }

    // ═══════════════════════════════════════════
    //  GF 注册
    // ═══════════════════════════════════════════

    static void RegisterInGF(Config cfg)
    {
        // 查找 UIFormId.cs
        var idFiles = AssetDatabase.FindAssets("UIFormId t:script");
        if (idFiles.Length == 0) { Log("UIFormId.cs 不存在，跳过注册"); return; }

        string idPath = AssetDatabase.GUIDToAssetPath(idFiles[0]);
        string idContent = File.ReadAllText(idPath);

        if (idContent.Contains(cfg.panelName))
        {
            Log($"{cfg.panelName} 已在 UIFormId 中注册");
            return;
        }

        // 找到最后一个枚举值，分配下一个 ID
        var matches = System.Text.RegularExpressions.Regex.Matches(idContent, @"=\s*(\d+)");
        int nextId = 100;
        foreach (System.Text.RegularExpressions.Match m in matches)
            nextId = Mathf.Max(nextId, int.Parse(m.Groups[1].Value) + 1);

        // 在最后一个 } 前插入
        int lastBrace = idContent.LastIndexOf('}');
        int insertPos = idContent.LastIndexOf(',', lastBrace);
        if (insertPos < 0) insertPos = idContent.LastIndexOf('\n', lastBrace);
        string newEntry = $"\n        {cfg.panelName} = {nextId},";
        idContent = idContent.Insert(insertPos + 1, newEntry);
        File.WriteAllText(idPath, idContent);
        Log($"注册 UIFormId: {cfg.panelName} = {nextId}");

        // 查找 UIForm.txt
        var txtFiles = AssetDatabase.FindAssets("UIForm t:textasset");
        foreach (var guid in txtFiles)
        {
            string txtPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!txtPath.EndsWith("UIForm.txt")) continue;

            string content = File.ReadAllText(txtPath);
            if (content.Contains(cfg.panelName)) break;

            // GF 数据表格式: \t{Id}\t{备注}\t{AssetName}\t{UIGroupName}\t{AllowMulti}\t{PauseCovered}
            content += $"\n\t{nextId}\t{cfg.panelName}\t{cfg.panelName}\t{cfg.uiGroupName}\tFALSE\tFALSE";
            File.WriteAllText(txtPath, content);
            Log($"注册 UIForm.txt: ID={nextId}");
            break;
        }
    }

    // ═══════════════════════════════════════════
    //  中文→英文面板名映射
    // ═══════════════════════════════════════════

    static readonly Dictionary<string, string> NameMap = new Dictionary<string, string>
    {
        {"签到","SignIn"}, {"商店","Shop"}, {"背包","Bag"}, {"设置","Setting"},
        {"主界面","Main"}, {"登录","Login"}, {"邮件","Mail"}, {"聊天","Chat"},
        {"排行榜","Ranking"}, {"好友","Friend"}, {"任务","Quest"}, {"活动","Activity"},
        {"充值","Recharge"}, {"抽奖","Lottery"}, {"成就","Achievement"},
        {"公告","Notice"}, {"战斗","Battle"}, {"地图","Map"}, {"角色","Role"},
    };

    public static string SanitizePanelName(string psbFileName)
    {
        string name = Path.GetFileNameWithoutExtension(psbFileName);
        // 去掉数字后缀 (签到5 → 签到)
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\d+$", "");

        if (NameMap.TryGetValue(name, out string eng))
            return eng + "UIPanel";

        // 如果已经是英文
        if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z_]+$"))
            return ToPascalCase(name) + "UIPanel";

        return name + "UIPanel";
    }

    // ═══════════════════════════════════════════
    //  PSB 数据读取（通过反射）
    // ═══════════════════════════════════════════

    static Dictionary<int, PSDLayer> ReadPSDLayers(UnityEngine.Object imp, Type iType)
    {
        var r = new Dictionary<int, PSDLayer>();
        var f = iType.GetField("m_MosaicPSDLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f == null) return r;
        var ls = f.GetValue(imp) as IList; if (ls == null) return r;
        var eT = ls.GetType().GetGenericArguments()[0];
        for (int i = 0; i < ls.Count; i++)
        {
            var l = ls[i];
            r[i] = new PSDLayer
            {
                index = i,
                name = GF<string>(l, eT, "name") ?? "",
                parentIndex = GF<int>(l, eT, "parentIndex"),
                isGroup = GF<bool>(l, eT, "isGroup")
            };
        }
        return r;
    }

    static List<SpriteMeta> ReadSpriteMetaData(UnityEngine.Object imp, Type iType)
    {
        var result = new List<SpriteMeta>();
        var f = iType.GetField("m_MosaicSpriteImportData", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f == null) return result;
        var ls = f.GetValue(imp) as IList; if (ls == null) return result;
        var eT = ls.GetType().GetGenericArguments()[0];
        for (int i = 0; i < ls.Count; i++)
        {
            var sd = ls[i];
            result.Add(new SpriteMeta
            {
                name = eT.GetProperty("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(sd)?.ToString() ?? "",
                spritePosition = GF<Vector2>(sd, eT, "spritePosition")
            });
        }
        return result;
    }

    // ═══════════════════════════════════════════
    //  工具方法
    // ═══════════════════════════════════════════

    static T GF<T>(object o, Type t, string n)
    {
        var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) try { return (T)p.GetValue(o); } catch { }
        var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) try { return (T)f.GetValue(o); } catch { }
        return default;
    }

    static float RF(UnityEngine.Object o, Type t, string n, float fb)
    {
        var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) { var v = p.GetValue(o); if (v is float f) return f; }
        return fb;
    }

    static Vector2 RV(UnityEngine.Object o, Type t, string n, Vector2 fb)
    {
        var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) { var v = p.GetValue(o); if (v is Vector2 v2) return v2; if (v is Vector2Int vi) return new Vector2(vi.x, vi.y); }
        return fb;
    }

    static void SetProp(object obj, Type type, string name, object value)
    {
        var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.CanWrite) p.SetValue(obj, value);
    }

    static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    // ═══════════════════════════════════════════
    //  强制启用 Character Mode（直接改 .meta）
    // ═══════════════════════════════════════════

    static void ForceCharacterMode(string psbPath)
    {
        string metaPath = psbPath + ".meta";
        if (!File.Exists(metaPath))
            AssetDatabase.ImportAsset(psbPath, ImportAssetOptions.ForceUpdate);
        if (!File.Exists(metaPath)) return;

        string content = File.ReadAllText(metaPath);

        // 确保 mosaicLayers: 1
        if (content.Contains("mosaicLayers: 0"))
            content = content.Replace("mosaicLayers: 0", "mosaicLayers: 1");

        // 关键：先关闭 characterMode → 导入 → 再开启 → 导入（toggle 触发解析）
        // 检查是否已经是 1 但数据为空（需要 toggle）
        bool needsToggle = content.Contains("characterMode: 1") && content.Contains("mosaicPSDLayers: []");
        bool needsEnable = content.Contains("characterMode: 0");

        if (needsToggle)
        {
            // 先关
            string off = content.Replace("characterMode: 1", "characterMode: 0");
            File.WriteAllText(metaPath, off);
            AssetDatabase.ImportAsset(psbPath, ImportAssetOptions.ForceUpdate);

            // 再开
            string on = File.ReadAllText(metaPath);
            on = on.Replace("characterMode: 0", "characterMode: 1");
            File.WriteAllText(metaPath, on);
            AssetDatabase.ImportAsset(psbPath, ImportAssetOptions.ForceUpdate);
            Log("已 toggle Character Mode 触发图层解析");
        }
        else if (needsEnable)
        {
            content = content.Replace("characterMode: 0", "characterMode: 1");
            File.WriteAllText(metaPath, content);
            AssetDatabase.ImportAsset(psbPath, ImportAssetOptions.ForceUpdate);
            Log("已启用 Character Mode");
        }
        else
        {
            // 已启用且有数据，直接导入
            AssetDatabase.ImportAsset(psbPath, ImportAssetOptions.ForceUpdate);
        }
    }

    // ═══════════════════════════════════════════
    //  PSD Importer 自动安装
    // ═══════════════════════════════════════════

    static bool EnsurePSDImporterInstalled()
    {
        // 检查 PSD Importer 是否已安装（通过查找其核心类型）
        bool installed = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a =>
            {
                try { return a.GetType("UnityEditor.U2D.PSD.PSDImporter") != null; }
                catch { return false; }
            });

        if (installed) return true;

        // 未安装，提示并自动安装
        bool confirm = EditorUtility.DisplayDialog(
            "PSB To UGUI",
            "需要安装 2D PSD Importer 包才能解析 PSB 文件。\n\n是否立即安装？（安装后 Unity 会重新编译）",
            "安装", "取消");

        if (!confirm) return false;

        Log("正在安装 com.unity.2d.psdimporter...");
        var request = UnityEditor.PackageManager.Client.Add("com.unity.2d.psdimporter");

        // 等待安装完成
        while (!request.IsCompleted)
            System.Threading.Thread.Sleep(100);

        if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
        {
            Log("PSD Importer 安装成功！Unity 将重新编译，请编译完成后再次运行 PSB To UGUI。");
            EditorUtility.DisplayDialog("PSB To UGUI",
                "PSD Importer 安装成功！\n\n请等待 Unity 编译完成后，重新选中 PSB 文件运行 PSB To UGUI。",
                "确定");
            return false; // 需要重新编译，本次不继续
        }
        else
        {
            Error($"PSD Importer 安装失败: {request.Error?.message}");
            return false;
        }
    }

    static Type FindType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.Name == typeName) return t;
                }
            }
            catch { }
        }
        return null;
    }

    static void Log(string msg) => Debug.Log($"[PSB2UI] {msg}");
    static void Error(string msg) => Debug.LogError($"[PSB2UI] {msg}");
}
