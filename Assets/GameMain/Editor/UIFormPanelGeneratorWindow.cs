using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Lokas.Editor.DataTableTools;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Lokas.Editor
{
    public sealed class UIFormPanelGeneratorWindow : EditorWindow
    {
        private const string PendingRequestKey = "Lokas.UIFormPanelGenerator.PendingRequest";
        private const string ScriptDir = "Assets/GameMain/Scripts/UI/Panel";
        private const string PrefabDir = "Assets/GameMain/UI/UIPanel";
        private const string UIPanelTemplatePrefabPath = "Assets/GameMain/UI/Template/UIPanel_Template.prefab";
        private const string UIFormIdPath = "Assets/GameMain/Scripts/UI/Runtime/UIFormId.cs";
        private const string UIFormTextPath = "Assets/GameMain/DataTables/UIForm.txt";
        private const string UIFormBytesPath = "Assets/GameMain/DataTables/UIForm.bytes";
        private const string AssemblyCSharpProjectPath = "Assembly-CSharp.csproj";

        private readonly List<UIFormEntry> m_Entries = new List<UIFormEntry>();
        private Vector2 m_ScrollPosition;
        private string m_PageName = string.Empty;
        private string m_Note = string.Empty;
        private UIGroupType m_UIGroup = UIGroupType.Default;
        private bool m_PauseCoveredUIForm;
        private bool m_CreateOrUpdatePrefab = true;

        [MenuItem("Tools/UI/UI Panel Manager")]
        private static void Open()
        {
            UIFormPanelGeneratorWindow window = GetWindow<UIFormPanelGeneratorWindow>("UI Panel Manager");
            window.RefreshEntries();
        }

        private void OnEnable()
        {
            RefreshEntries();
        }

        private void OnGUI()
        {
            DrawConfigArea();
            EditorGUILayout.Space(8f);
            DrawCreateArea();
            EditorGUILayout.Space(8f);
            DrawManageArea();
        }

        private void DrawConfigArea()
        {
            EditorGUILayout.LabelField("UIForm Config", EditorStyles.boldLabel);
            DrawConfigFileRow("UIForm.txt", UIFormTextPath);
            DrawConfigFileRow("UIFormId.cs", UIFormIdPath);
            DrawConfigFileRow("Template", UIPanelTemplatePrefabPath);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Sync UIFormId", GUILayout.Width(160f)))
                {
                    SyncUIFormIdCommentsFromText();
                    AssetDatabase.Refresh();
                    Debug.Log("UI Panel Manager synced UIFormId.cs from UIForm.txt.");
                }
            }
        }

        private static void DrawConfigFileRow(string label, string assetPath)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(82f));
                EditorGUILayout.SelectableLabel(assetPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                {
                    PingAsset(assetPath);
                }

                if (GUILayout.Button("Open", GUILayout.Width(52f)))
                {
                    OpenAsset(assetPath);
                }
            }
        }

        private void DrawCreateArea()
        {
            EditorGUILayout.LabelField("Add UI Panel", EditorStyles.boldLabel);
            m_PageName = EditorGUILayout.TextField("Page Name", m_PageName);
            m_Note = EditorGUILayout.TextField("Note", m_Note);
            m_UIGroup = (UIGroupType)EditorGUILayout.EnumPopup("UI Group", m_UIGroup);
            m_PauseCoveredUIForm = EditorGUILayout.Toggle("Pause Covered", m_PauseCoveredUIForm);
            m_CreateOrUpdatePrefab = EditorGUILayout.Toggle("Create / Update Prefab", m_CreateOrUpdatePrefab);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(m_PageName)))
                {
                    if (GUILayout.Button("Add / Update", GUILayout.Height(24f)))
                    {
                        CreateOrUpdate(m_PageName, m_Note, m_UIGroup.ToString(), m_PauseCoveredUIForm, m_CreateOrUpdatePrefab);
                        RefreshEntries();
                    }
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(90f), GUILayout.Height(24f)))
                {
                    RefreshEntries();
                }
            }
        }

        private void DrawManageArea()
        {
            EditorGUILayout.LabelField("Current UI Panels", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Id", GUILayout.Width(48f));
                GUILayout.Label("Asset Name", GUILayout.MinWidth(160f));
                GUILayout.Label("Note", GUILayout.MinWidth(120f));
                GUILayout.Label("Group", GUILayout.Width(90f));
                GUILayout.Label("Pause", GUILayout.Width(48f));
                GUILayout.Space(96f);
            }

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            foreach (UIFormEntry entry in m_Entries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(entry.id.ToString(), GUILayout.Width(48f));
                    GUILayout.Label(entry.assetName, GUILayout.MinWidth(160f));
                    GUILayout.Label(entry.note, GUILayout.MinWidth(120f));
                    GUILayout.Label(entry.uiGroupName, GUILayout.Width(90f));
                    GUILayout.Label(entry.pauseCoveredUIForm ? "TRUE" : "FALSE", GUILayout.Width(48f));

                    if (GUILayout.Button("Ping", GUILayout.Width(44f)))
                    {
                        PingPanelAssets(entry.assetName);
                    }

                    if (GUILayout.Button("Delete", GUILayout.Width(58f)))
                    {
                        DeleteWithConfirm(entry);
                        break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void CreateOrUpdate(string pageName, string note, string uiGroupName, bool pauseCoveredUIForm, bool createOrUpdatePrefab)
        {
            string panelName = NormalizePanelName(pageName);
            if (!Regex.IsMatch(panelName, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                EditorUtility.DisplayDialog("UI Panel Manager", $"Invalid C# class name: {panelName}", "OK");
                return;
            }

            Directory.CreateDirectory(ScriptDir);
            if (createOrUpdatePrefab)
            {
                Directory.CreateDirectory(PrefabDir);
            }

            int id = EnsureUIFormText(panelName, note, uiGroupName, pauseCoveredUIForm);
            EnsureUIFormId(panelName, id, note);
            bool scriptCreated = EnsureScript(panelName);
            GenerateUIFormBytes();

            if (!createOrUpdatePrefab)
            {
                AssetDatabase.Refresh();
                Debug.Log($"UI Panel Manager registered script only: {panelName} ({id}). Prefab was not changed.");
                return;
            }

            EditorPrefs.SetString(PendingRequestKey, JsonUtility.ToJson(new PendingRequest
            {
                panelName = panelName,
                prefabPath = $"{PrefabDir}/{panelName}.prefab"
            }));

            if (!scriptCreated && TryEnsurePrefabNow(panelName, $"{PrefabDir}/{panelName}.prefab"))
            {
                EditorPrefs.DeleteKey(PendingRequestKey);
            }

            AssetDatabase.Refresh();
            Debug.Log($"UI Panel Manager add requested: {panelName} ({id}). Prefab will be updated after scripts reload.");
        }

        private void DeleteWithConfirm(UIFormEntry entry)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete UI Panel",
                $"Delete {entry.assetName}?\n\nThis removes UIForm.txt/UIForm.bytes/UIFormId entry and deletes the script and prefab assets if they exist.",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            DeletePanel(entry.assetName);
            RefreshEntries();
        }

        private static void DeletePanel(string panelName)
        {
            RemoveUIFormText(panelName);
            RemoveUIFormId(panelName);
            GenerateUIFormBytes();

            DeleteAssetIfExists($"{ScriptDir}/{panelName}.cs");
            DeleteAssetIfExists($"{PrefabDir}/{panelName}.prefab");
            RemoveCSharpProjectReference(panelName);

            AssetDatabase.Refresh();
            Debug.Log($"UI Panel Manager deleted: {panelName}");
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            string json = EditorPrefs.GetString(PendingRequestKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            EditorPrefs.DeleteKey(PendingRequestKey);
            PendingRequest request = JsonUtility.FromJson<PendingRequest>(json);
            Type panelType = Type.GetType($"Lokas.{request.panelName}, Assembly-CSharp");
            if (panelType == null || !typeof(Component).IsAssignableFrom(panelType))
            {
                Debug.LogError($"UI Panel Manager failed: can not find compiled component Lokas.{request.panelName}.");
                return;
            }

            EnsurePrefab(request.panelName, request.prefabPath, panelType);

            AssetDatabase.Refresh();
            Debug.Log($"UI Panel Manager complete: {request.panelName}");
        }

        private static bool TryEnsurePrefabNow(string panelName, string prefabPath)
        {
            Type panelType = Type.GetType($"Lokas.{panelName}, Assembly-CSharp");
            if (panelType == null || !typeof(Component).IsAssignableFrom(panelType))
            {
                return false;
            }

            EnsurePrefab(panelName, prefabPath, panelType);
            return true;
        }

        private static void EnsurePrefab(string panelName, string prefabPath, Type panelType)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                CreatePrefabFromTemplate(panelName, prefabPath, panelType);
            }
            else
            {
                GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
                instance.name = panelName;
                ReplaceRootUIFormComponent(instance, panelType);

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        private static void CreatePrefabFromTemplate(string panelName, string prefabPath, Type panelType)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UIPanelTemplatePrefabPath) == null)
            {
                Debug.LogWarning($"UI Panel Manager template not found: {UIPanelTemplatePrefabPath}. Creating a minimal prefab instead.");
                CreateMinimalPrefab(panelName, prefabPath, panelType);
                return;
            }

            if (!AssetDatabase.CopyAsset(UIPanelTemplatePrefabPath, prefabPath))
            {
                throw new InvalidOperationException($"Copy template prefab failed: {UIPanelTemplatePrefabPath} -> {prefabPath}");
            }

            AssetDatabase.ImportAsset(prefabPath);

            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
            instance.name = panelName;
            ConfigureRootRectTransform(instance);
            ReplaceRootUIFormComponent(instance, panelType);
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
        }

        private static void CreateMinimalPrefab(string panelName, string prefabPath, Type panelType)
        {
            GameObject root = new GameObject(panelName, typeof(RectTransform), typeof(CanvasGroup));
            ConfigureRootRectTransform(root);
            ReplaceRootUIFormComponent(root, panelType);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);
        }

        private static void ConfigureRootRectTransform(GameObject root)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                root.layer = uiLayer;
            }

            RectTransform rectTransform = root.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = root.AddComponent<RectTransform>();
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void ReplaceRootUIFormComponent(GameObject root, Type panelType)
        {
            Component existingPanel = root.GetComponent(panelType);
            Component sourcePanel = existingPanel ?? FindRootUIFormComponent(root);
            if (existingPanel == null)
            {
                existingPanel = root.AddComponent(panelType);
                CopyCommonSerializedProperties(sourcePanel, existingPanel);
            }

            foreach (Component component in root.GetComponents<Component>())
            {
                if (component == null || component == existingPanel)
                {
                    continue;
                }

                if (typeof(UGuiForm).IsAssignableFrom(component.GetType()))
                {
                    DestroyImmediate(component);
                }
            }
        }

        private static Component FindRootUIFormComponent(GameObject root)
        {
            foreach (Component component in root.GetComponents<Component>())
            {
                if (component != null && typeof(UGuiForm).IsAssignableFrom(component.GetType()))
                {
                    return component;
                }
            }

            return null;
        }

        private static void CopyCommonSerializedProperties(Component source, Component target)
        {
            if (source == null || target == null)
            {
                return;
            }

            SerializedObject sourceObject = new SerializedObject(source);
            SerializedObject targetObject = new SerializedObject(target);
            SerializedProperty property = sourceObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath == "m_Script" || targetObject.FindProperty(property.propertyPath) == null)
                {
                    continue;
                }

                targetObject.CopyFromSerializedProperty(property);
            }

            targetObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void RefreshEntries()
        {
            m_Entries.Clear();
            if (!File.Exists(UIFormTextPath))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(UIFormTextPath, Encoding.UTF8))
            {
                if (!TryParseEntry(line, out UIFormEntry entry))
                {
                    continue;
                }

                m_Entries.Add(entry);
            }

            m_Entries.Sort((a, b) => a.id.CompareTo(b.id));
            Repaint();
        }

        private static bool TryParseEntry(string line, out UIFormEntry entry)
        {
            entry = default;
            if (line.StartsWith("#", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string[] columns = line.Split('\t');
            if (columns.Length < 7 || !int.TryParse(columns[1], out int id))
            {
                return false;
            }

            entry = new UIFormEntry
            {
                id = id,
                note = columns[2],
                assetName = columns[3],
                uiGroupName = columns[4],
                allowMultiInstance = ParseBool(columns[5]),
                pauseCoveredUIForm = ParseBool(columns[6])
            };
            return true;
        }

        private static bool ParseBool(string value)
        {
            return string.Equals(value.Trim(), "TRUE", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePanelName(string pageName)
        {
            string name = Regex.Replace(pageName.Trim(), @"\s+", string.Empty);
            if (!name.EndsWith("UIPanel", StringComparison.Ordinal))
            {
                name += "UIPanel";
            }

            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private static int EnsureUIFormText(string panelName, string note, string uiGroupName, bool pauseCoveredUIForm)
        {
            NormalizeUIFormTextFile();
            string[] lines = File.ReadAllLines(UIFormTextPath, Encoding.UTF8);
            int maxId = 0;
            foreach (string line in lines)
            {
                if (!TryParseEntry(line, out UIFormEntry entry))
                {
                    continue;
                }

                maxId = Mathf.Max(maxId, entry.id);
                if (entry.assetName == panelName)
                {
                    UpdateUIFormTextLine(panelName, note, uiGroupName, pauseCoveredUIForm);
                    return entry.id;
                }
            }

            int nextId = maxId + 1;
            string pause = pauseCoveredUIForm ? "TRUE" : "FALSE";
            string displayNote = string.IsNullOrWhiteSpace(note) ? panelName : note.Trim();
            File.AppendAllText(UIFormTextPath, $"{Environment.NewLine}\t{nextId}\t{displayNote}\t{panelName}\t{uiGroupName}\tFALSE\t{pause}", Encoding.UTF8);
            return nextId;
        }

        private static void UpdateUIFormTextLine(string panelName, string note, string uiGroupName, bool pauseCoveredUIForm)
        {
            string[] lines = File.ReadAllLines(UIFormTextPath, Encoding.UTF8);
            string pause = pauseCoveredUIForm ? "TRUE" : "FALSE";
            string displayNote = string.IsNullOrWhiteSpace(note) ? panelName : note.Trim();
            bool changed = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!TryParseEntry(lines[i], out UIFormEntry entry) || entry.assetName != panelName)
                {
                    continue;
                }

                lines[i] = $"\t{entry.id}\t{displayNote}\t{panelName}\t{uiGroupName}\t{(entry.allowMultiInstance ? "TRUE" : "FALSE")}\t{pause}";
                changed = true;
                break;
            }

            if (changed)
            {
                File.WriteAllLines(UIFormTextPath, lines, Encoding.UTF8);
            }
        }

        private static void RemoveUIFormText(string panelName)
        {
            NormalizeUIFormTextFile();
            string[] lines = File.ReadAllLines(UIFormTextPath, Encoding.UTF8);
            List<string> keptLines = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                if (TryParseEntry(line, out UIFormEntry entry) && entry.assetName == panelName)
                {
                    continue;
                }

                keptLines.Add(line);
            }

            File.WriteAllLines(UIFormTextPath, keptLines, Encoding.UTF8);
        }

        private static void NormalizeUIFormTextFile()
        {
            if (!File.Exists(UIFormTextPath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(UIFormTextPath, Encoding.UTF8);
            List<string> normalizedLines = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] columns = line.TrimEnd().Split('\t');
                if (columns.Length < 7)
                {
                    Array.Resize(ref columns, 7);
                    for (int i = 0; i < columns.Length; i++)
                    {
                        columns[i] ??= string.Empty;
                    }
                }

                normalizedLines.Add(string.Join("\t", columns));
            }

            File.WriteAllLines(UIFormTextPath, normalizedLines, Encoding.UTF8);
        }

        private static void EnsureUIFormId(string panelName, int id, string note)
        {
            string content = File.ReadAllText(UIFormIdPath, Encoding.UTF8);
            if (Regex.IsMatch(content, $@"\b{Regex.Escape(panelName)}\s*="))
            {
                content = UpdateUIFormIdComment(content, panelName, note);
                File.WriteAllText(UIFormIdPath, content, Encoding.UTF8);
                return;
            }

            string entry = BuildUIFormIdEntry(panelName, id, note);
            int insertIndex = FindUIFormIdEnumEnd(content);
            if (insertIndex < 0)
            {
                throw new InvalidOperationException($"Can not find enum end in {UIFormIdPath}.");
            }

            content = content.Insert(insertIndex, entry);
            File.WriteAllText(UIFormIdPath, content, Encoding.UTF8);
        }

        public static void SynchronizeRegistrationFiles()
        {
            SyncUIFormIdCommentsFromText();
            GenerateUIFormBytes();
        }

        private static void SyncUIFormIdCommentsFromText()
        {
            NormalizeUIFormTextFile();
            List<UIFormEntry> entries = new List<UIFormEntry>();
            foreach (string line in File.ReadAllLines(UIFormTextPath, Encoding.UTF8))
            {
                if (TryParseEntry(line, out UIFormEntry entry))
                {
                    entries.Add(entry);
                }
            }

            string content = File.ReadAllText(UIFormIdPath, Encoding.UTF8);
            content = RewriteUIFormIdEnum(content, entries);
            File.WriteAllText(UIFormIdPath, content, Encoding.UTF8);
        }

        private static string RewriteUIFormIdEnum(string content, List<UIFormEntry> entries)
        {
            Match enumMatch = FindUIFormIdEnumMatch(content);
            if (!enumMatch.Success)
            {
                throw new InvalidOperationException($"Can not find enum declaration in {UIFormIdPath}.");
            }

            int enumEndIndex = FindUIFormIdEnumEnd(content);
            if (enumEndIndex < 0)
            {
                throw new InvalidOperationException($"Can not find enum end in {UIFormIdPath}.");
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine("        Undefined = 0,");
            foreach (UIFormEntry entry in entries)
            {
                builder.Append(BuildUIFormIdEntry(entry.assetName, entry.id, entry.note));
            }

            builder.Append("    ");
            return content.Substring(0, enumMatch.Index + enumMatch.Length) + builder + content.Substring(enumEndIndex);
        }

        private static string UpdateUIFormIdCommentById(string content, int id, string note)
        {
            string displayNote = string.IsNullOrWhiteSpace(note) ? id.ToString() : note.Trim();
            string pattern =
                $@"(?:(^[ \t]*///\s*<summary>\s*\r?\n[ \t]*///.*\r?\n[ \t]*///\s*</summary>\s*\r?\n))?([ \t]*[A-Za-z_][A-Za-z0-9_]*\s*=\s*{id}\s*,)";

            return Regex.Replace(content, pattern, match =>
            {
                string indent = Regex.Match(match.Groups[2].Value, @"^[ \t]*").Value;
                string comment =
                    $"{indent}/// <summary>{Environment.NewLine}" +
                    $"{indent}/// {displayNote}{Environment.NewLine}" +
                    $"{indent}/// </summary>{Environment.NewLine}";
                return comment + match.Groups[2].Value;
            }, RegexOptions.Multiline);
        }

        private static string BuildUIFormIdEntry(string panelName, int id, string note)
        {
            string displayNote = string.IsNullOrWhiteSpace(note) ? panelName : note.Trim();
            return
                $"        /// <summary>{Environment.NewLine}" +
                $"        /// {displayNote}{Environment.NewLine}" +
                $"        /// </summary>{Environment.NewLine}" +
                $"        {panelName} = {id},{Environment.NewLine}";
        }

        private static string UpdateUIFormIdComment(string content, string panelName, string note)
        {
            string displayNote = string.IsNullOrWhiteSpace(note) ? panelName : note.Trim();
            string escapedPanelName = Regex.Escape(panelName);
            string pattern =
                $@"(?:(^[ \t]*///\s*<summary>\s*\r?\n[ \t]*///.*\r?\n[ \t]*///\s*</summary>\s*\r?\n))?([ \t]*{escapedPanelName}\s*=\s*\d+\s*,)";

            return Regex.Replace(content, pattern, match =>
            {
                string indent = Regex.Match(match.Groups[2].Value, @"^[ \t]*").Value;
                string comment =
                    $"{indent}/// <summary>{Environment.NewLine}" +
                    $"{indent}/// {displayNote}{Environment.NewLine}" +
                    $"{indent}/// </summary>{Environment.NewLine}";
                return comment + match.Groups[2].Value;
            }, RegexOptions.Multiline);
        }

        private static int FindUIFormIdEnumEnd(string content)
        {
            Match match = FindUIFormIdEnumMatch(content);
            if (!match.Success)
            {
                return -1;
            }

            int depth = 1;
            for (int i = match.Index + match.Length; i < content.Length; i++)
            {
                if (content[i] == '{')
                {
                    depth++;
                }
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static Match FindUIFormIdEnumMatch(string content)
        {
            return Regex.Match(content, @"public\s+enum\s+UIFormId\s*(?::\s*byte)?\s*\{");
        }

        private static void RemoveUIFormId(string panelName)
        {
            string content = File.ReadAllText(UIFormIdPath, Encoding.UTF8);
            string pattern = $@"(?:(^[ \t]*///\s*<summary>\s*\r?\n[ \t]*///.*\r?\n[ \t]*///\s*</summary>\s*\r?\n))?^[ \t]*{Regex.Escape(panelName)}\s*=\s*\d+\s*,\s*\r?\n?";
            content = Regex.Replace(content, pattern, string.Empty, RegexOptions.Multiline);
            File.WriteAllText(UIFormIdPath, content, Encoding.UTF8);
        }

        private static bool EnsureScript(string panelName)
        {
            string scriptPath = $"{ScriptDir}/{panelName}.cs";
            if (File.Exists(scriptPath))
            {
                EnsureCSharpProjectReference(panelName);
                return false;
            }

            string script = $@"using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{{
    public class {panelName} : UGuiForm
    {{
        [SerializeField] private Button m_BtnClose;

        protected override void OnInit(object userData)
        {{
            base.OnInit(userData);

            if (m_BtnClose == null)
            {{
                Transform closeTransform = transform.Find(""Btn_Close"") ?? transform.Find(""{panelName.Replace("UIPanel", "Root")}/Btn_Close"");
                if (closeTransform != null)
                {{
                    m_BtnClose = closeTransform.GetComponent<Button>();
                }}
            }}

            if (m_BtnClose != null)
            {{
                m_BtnClose.onClick.AddListener(OnClickClose);
            }}
        }}

        private void OnClickClose()
        {{
            PlayUISound(SoundId.UI_Close);
            Close();
        }}
    }}
}}
";
            File.WriteAllText(scriptPath, script, Encoding.UTF8);
            EnsureCSharpProjectReference(panelName);
            return true;
        }

        private static void EnsureCSharpProjectReference(string panelName)
        {
            if (!File.Exists(AssemblyCSharpProjectPath))
            {
                return;
            }

            string includePath = $@"Assets\GameMain\Scripts\UI\Panel\{panelName}.cs";
            string content = File.ReadAllText(AssemblyCSharpProjectPath, Encoding.UTF8);
            if (content.Contains(includePath))
            {
                return;
            }

            string entry = $"    <Compile Include=\"{includePath}\" />{Environment.NewLine}";
            string marker = "  </ItemGroup>";
            int insertIndex = content.IndexOf(marker, StringComparison.Ordinal);
            if (insertIndex < 0)
            {
                return;
            }

            content = content.Insert(insertIndex, entry);
            File.WriteAllText(AssemblyCSharpProjectPath, content, Encoding.UTF8);
        }

        private static void RemoveCSharpProjectReference(string panelName)
        {
            if (!File.Exists(AssemblyCSharpProjectPath))
            {
                return;
            }

            string includePath = Regex.Escape($@"Assets\GameMain\Scripts\UI\Panel\{panelName}.cs");
            string content = File.ReadAllText(AssemblyCSharpProjectPath, Encoding.UTF8);
            content = Regex.Replace(content, $@"^[ \t]*<Compile Include=""{includePath}"" />\s*\r?\n?", string.Empty, RegexOptions.Multiline);
            File.WriteAllText(AssemblyCSharpProjectPath, content, Encoding.UTF8);
        }

        private static void PingPanelAssets(string panelName)
        {
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{PrefabDir}/{panelName}.prefab");
            if (prefab != null)
            {
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;
                return;
            }

            UnityEngine.Object script = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{ScriptDir}/{panelName}.cs");
            if (script != null)
            {
                EditorGUIUtility.PingObject(script);
                Selection.activeObject = script;
            }
        }

        private static void PingAsset(string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("UI Panel Manager", $"Asset not found:\n{assetPath}", "OK");
                return;
            }

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private static void OpenAsset(string assetPath)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("UI Panel Manager", $"Asset not found:\n{assetPath}", "OK");
                return;
            }

            AssetDatabase.OpenAsset(asset);
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void GenerateUIFormBytes()
        {
            NormalizeUIFormTextFile();
            DataTableProcessor processor = new DataTableProcessor(UIFormTextPath, new UTF8Encoding(false, true), 1, 2, null, 3, 4, 1);
            if (!processor.GenerateDataFile(UIFormBytesPath))
            {
                throw new InvalidOperationException($"Generate {UIFormBytesPath} failed.");
            }
        }

        [Serializable]
        private sealed class PendingRequest
        {
            public string panelName;
            public string prefabPath;
        }

        private struct UIFormEntry
        {
            public int id;
            public string note;
            public string assetName;
            public string uiGroupName;
            public bool allowMultiInstance;
            public bool pauseCoveredUIForm;
        }
    }
}

