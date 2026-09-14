using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class DataTableTextEditorWindow : EditorWindow
{
    private const string DefaultRelativePath = "Assets/GameMain/Editor/DataTable/Config/Template.txt";
    private const string TypeConfigAssetPath = "Assets/GameMain/Editor/DataTable/Config/DataTableTypeConfig.asset";

    private string relativePath = DefaultRelativePath;
    private string absolutePath;
    private Encoding currentEncoding = new UTF8Encoding(false, true);

    private readonly List<List<string>> headerRows = new List<List<string>>();
    private readonly List<List<string>> dataRows = new List<List<string>>();

    private Vector2 headerScroll;
    private Vector2 dataScroll;
    private bool dirty;
    private DataTableTypeConfigSO typeConfig;
    private ReorderableList dataList;
    private bool allowDrag;
    private int deleteColumnIndex = -1;

    [MenuItem("Tools/DataTable/Text Table Editor")]
    private static void Open()
    {
        GetWindow<DataTableTextEditorWindow>("Text Table");
    }

    public static void ShowWindow()
    {
        Open();
    }

    private void OnEnable()
    {
        LoadTypeConfig();
        Load();
        InitDataList();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawWarnings();
        DrawHeaderTable();
        DrawDataTable();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
        {
            Load();
        }

        if (GUILayout.Button("Save", EditorStyles.toolbarButton))
        {
            Save();
        }

        if (GUILayout.Button("Save As...", EditorStyles.toolbarButton))
        {
            SaveAs();
        }

        GUILayout.Space(10);

        GUILayout.FlexibleSpace();

        bool newAllowDrag = GUILayout.Toggle(allowDrag, "Drag Rows", EditorStyles.toolbarButton);
        if (newAllowDrag != allowDrag)
        {
            allowDrag = newAllowDrag;
            if (dataList != null)
            {
                dataList.draggable = allowDrag;
            }
        }

        if (GUILayout.Button("Types...", EditorStyles.toolbarButton))
        {
            SelectOrCreateTypeConfig();
        }

        if (GUILayout.Button("Browse...", EditorStyles.toolbarButton))
        {
            BrowseFile();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("File", GUILayout.Width(32));
        EditorGUILayout.SelectableLabel(relativePath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();

        Rect dropRect = GUILayoutUtility.GetRect(0f, 36f, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, GUIContent.none, EditorStyles.helpBox);
        GUIStyle dropStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        EditorGUI.LabelField(dropRect, "Drag .txt here to open", dropStyle);
        HandleFileDragAndDrop(dropRect);
    }

    private void DrawWarnings()
    {
        if (dirty)
        {
            EditorGUILayout.HelpBox("Unsaved changes.", MessageType.Warning);
        }

        if (typeConfig == null)
        {
            EditorGUILayout.HelpBox("Type config not found. Click Types... to create one.", MessageType.Info);
        }

        if (dataRows.Count == 0)
        {
            EditorGUILayout.HelpBox("No data rows.", MessageType.Info);
        }
    }

    private void DrawHeaderTable()
    {
        if (headerRows.Count == 0)
        {
            return;
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Header", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add Column", GUILayout.Width(100)))
        {
            AddColumn();
        }
        DrawDeleteColumnControls();
        {
            // button handled in DrawDeleteColumnControls
        }
        EditorGUILayout.EndHorizontal();
        headerScroll = EditorGUILayout.BeginScrollView(headerScroll, GUILayout.Height(120));

        int columns = GetColumnCount();
        for (int r = 0; r < headerRows.Count; r++)
        {
            List<string> row = headerRows[r];
            EnsureColumnCount(row, columns);

            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < columns; c++)
            {
                string value = row[c] ?? string.Empty;
                string newValue = DrawHeaderCell(r, c, value);
                if (newValue != value)
                {
                    row[c] = newValue;
                    dirty = true;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawDataTable()
    {
        EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
        dataScroll = EditorGUILayout.BeginScrollView(dataScroll);

        if (dataList == null || dataList.list != dataRows)
        {
            InitDataList();
        }

        dataList.DoLayoutList();

        EditorGUILayout.EndScrollView();
    }

    private void AddColumn()
    {
        int columns = GetColumnCount();
        int newIndex = columns;

        for (int r = 0; r < headerRows.Count; r++)
        {
            List<string> row = headerRows[r];
            EnsureColumnCount(row, columns);

            string defaultValue = string.Empty;
            if (r == 1)
            {
                defaultValue = "NewField";
            }
            else if (r == 2)
            {
                defaultValue = GetDefaultTypeName();
            }
            else if (r == 3)
            {
                defaultValue = "备注";
            }

            row.Insert(newIndex, defaultValue);
        }

        foreach (var row in dataRows)
        {
            EnsureColumnCount(row, columns);
            row.Insert(newIndex, string.Empty);
        }

        dirty = true;
        InitDataList();
    }

    private void DeleteLastColumn()
    {
        int columns = GetColumnCount();
        if (columns <= 0)
        {
            return;
        }

        int removeIndex = columns - 1;
        if (!EditorUtility.DisplayDialog("Delete Column",
            "Delete last column (" + (removeIndex + 1) + ")?", "Delete", "Cancel"))
        {
            return;
        }

        for (int r = 0; r < headerRows.Count; r++)
        {
            if (headerRows[r].Count > removeIndex)
            {
                headerRows[r].RemoveAt(removeIndex);
            }
        }

        for (int r = 0; r < dataRows.Count; r++)
        {
            if (dataRows[r].Count > removeIndex)
            {
                dataRows[r].RemoveAt(removeIndex);
            }
        }

        dirty = true;
        InitDataList();
    }

    private void DrawDeleteColumnControls()
    {
        int columns = GetColumnCount();
        if (columns <= 0)
        {
            deleteColumnIndex = -1;
            return;
        }

        if (deleteColumnIndex < 0 || deleteColumnIndex >= columns)
        {
            deleteColumnIndex = columns - 1;
        }

        string[] options = BuildColumnOptions(columns);
        deleteColumnIndex = EditorGUILayout.Popup(deleteColumnIndex, options, GUILayout.Width(120));

        if (GUILayout.Button("Delete Column", GUILayout.Width(110)))
        {
            DeleteColumn(deleteColumnIndex);
        }
    }

    private string[] BuildColumnOptions(int columns)
    {
        string[] options = new string[columns];
        List<string> header = headerRows.Count > 1 ? headerRows[1] : null;

        for (int i = 0; i < columns; i++)
        {
            string name = header != null && i < header.Count ? header[i] : string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                name = "Column " + (i + 1);
            }
            options[i] = (i + 1) + ": " + name;
        }

        return options;
    }

    private void DeleteColumn(int index)
    {
        int columns = GetColumnCount();
        if (index < 0 || index >= columns)
        {
            return;
        }

        if (!EditorUtility.DisplayDialog("Delete Column",
            "Delete column (" + (index + 1) + ")?", "Delete", "Cancel"))
        {
            return;
        }

        for (int r = 0; r < headerRows.Count; r++)
        {
            if (headerRows[r].Count > index)
            {
                headerRows[r].RemoveAt(index);
            }
        }

        for (int r = 0; r < dataRows.Count; r++)
        {
            if (dataRows[r].Count > index)
            {
                dataRows[r].RemoveAt(index);
            }
        }

        dirty = true;
        InitDataList();
    }

    private void AddRow()
    {
        int columns = GetColumnCount();
        List<string> row = new List<string>(columns);
        for (int i = 0; i < columns; i++)
        {
            row.Add(string.Empty);
        }

        int idIndex = FindIdColumnIndex();
        if (idIndex >= 0)
        {
            int nextId = FindNextId(idIndex);
            row[idIndex] = nextId.ToString();
        }

        dataRows.Add(row);
        dirty = true;
        InitDataList();
    }

    private int FindIdColumnIndex()
    {
        if (headerRows.Count < 2)
        {
            return -1;
        }

        List<string> nameRow = headerRows[1];
        for (int i = 0; i < nameRow.Count; i++)
        {
            if (string.Equals(nameRow[i], "Id", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    private int FindNextId(int idIndex)
    {
        int max = 0;
        for (int i = 0; i < dataRows.Count; i++)
        {
            string value = dataRows[i].Count > idIndex ? dataRows[i][idIndex] : string.Empty;
            int parsed;
            if (int.TryParse(value, out parsed) && parsed > max)
            {
                max = parsed;
            }
        }
        return max + 1;
    }

    private void Load()
    {
        LoadTypeConfig();
        headerRows.Clear();
        dataRows.Clear();
        dirty = false;

        absolutePath = ResolveAbsolutePath(relativePath);
        if (!File.Exists(absolutePath))
        {
            Debug.LogError("Table file not found: " + absolutePath);
            return;
        }

        string[] lines = ReadAllLinesWithEncoding(absolutePath, out currentEncoding);
        foreach (string raw in lines)
        {
            string line = raw.TrimEnd();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            List<string> cols = SplitRow(line);
            if (line.StartsWith("#"))
            {
                headerRows.Add(cols);
            }
            else
            {
                dataRows.Add(cols);
            }
        }

        InitDataList();
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            SaveAs();
            return;
        }

        absolutePath = ResolveAbsolutePath(relativePath);
        if (string.IsNullOrEmpty(absolutePath))
        {
            SaveAs();
            return;
        }

        string directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        StringBuilder sb = new StringBuilder(2048);
        int columns = GetColumnCount();

        foreach (var row in headerRows)
        {
            EnsureColumnCount(row, columns);
            sb.AppendLine(string.Join("\t", row));
        }

        foreach (var row in dataRows)
        {
            EnsureColumnCount(row, columns);
            sb.AppendLine(string.Join("\t", row));
        }

        File.WriteAllText(absolutePath, sb.ToString(), currentEncoding);
        AssetDatabase.Refresh();
        dirty = false;
    }

    private void SaveAs()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string path = EditorUtility.SaveFilePanel("Save Table", projectRoot, "NewTable", "txt");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        relativePath = MakeRelativePath(projectRoot, path);
        absolutePath = path;
        Save();
    }

    private void BrowseFile()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string path = EditorUtility.OpenFilePanel("Open Table", projectRoot, "txt");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        relativePath = MakeRelativePath(projectRoot, path);
        Load();
    }

    private int GetColumnCount()
    {
        int columns = 0;
        foreach (var row in headerRows)
        {
            if (row.Count > columns)
            {
                columns = row.Count;
            }
        }
        foreach (var row in dataRows)
        {
            if (row.Count > columns)
            {
                columns = row.Count;
            }
        }
        return Mathf.Max(columns, 1);
    }

    private static void EnsureColumnCount(List<string> row, int count)
    {
        while (row.Count < count)
        {
            row.Add(string.Empty);
        }
    }

    private static List<string> SplitRow(string line)
    {
        string[] cols = line.Split('\t');
        List<string> list = new List<string>(cols.Length);
        for (int i = 0; i < cols.Length; i++)
        {
            list.Add(cols[i].Trim('"'));
        }
        return list;
    }

    private static string ResolveAbsolutePath(string relPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, relPath));
    }

    private static string MakeRelativePath(string root, string fullPath)
    {
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath;
        }
        string relative = fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string[] ReadAllLinesWithEncoding(string path, out Encoding encoding)
    {
        byte[] bytes = File.ReadAllBytes(path);

        if (HasUtf8Bom(bytes))
        {
            encoding = new UTF8Encoding(true, true);
            return File.ReadAllLines(path, encoding);
        }

        try
        {
            encoding = new UTF8Encoding(false, true);
            return File.ReadAllLines(path, encoding);
        }
        catch (DecoderFallbackException)
        {
            encoding = Encoding.GetEncoding(936);
            return File.ReadAllLines(path, encoding);
        }
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }

    private void HandleFileDragAndDrop(Rect dropRect)
    {
        Event evt = Event.current;
        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
        {
            return;
        }

        if (!dropRect.Contains(evt.mousePosition))
        {
            return;
        }

        bool hasTxt = false;
        string txtPath = null;

        foreach (string path in DragAndDrop.paths)
        {
            if (path != null && path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                hasTxt = true;
                txtPath = path;
                break;
            }
        }

        if (!hasTxt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            return;
        }

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            relativePath = MakeRelativePath(projectRoot, txtPath);
            Load();
            evt.Use();
        }
    }

    private string DrawHeaderCell(int rowIndex, int columnIndex, string value)
    {
        if (columnIndex == 0)
        {
            EditorGUILayout.LabelField(string.IsNullOrEmpty(value) ? " " : value, GUILayout.MinWidth(80));
            return value;
        }

        if (rowIndex == 2 && columnIndex > 0 && typeConfig != null && typeConfig.types != null && typeConfig.types.Count > 0)
        {
            int selectedIndex = typeConfig.types.IndexOf(value);
            if (selectedIndex < 0)
            {
                return EditorGUILayout.TextField(value, GUILayout.MinWidth(80));
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, typeConfig.types.ToArray(), GUILayout.MinWidth(80));
            if (newIndex >= 0 && newIndex < typeConfig.types.Count)
            {
                return typeConfig.types[newIndex];
            }
        }

        return EditorGUILayout.TextField(value, GUILayout.MinWidth(80));
    }

    private string GetDefaultTypeName()
    {
        if (typeConfig != null && typeConfig.types != null && typeConfig.types.Count > 0)
        {
            return typeConfig.types[0];
        }
        return "string";
    }

    private void LoadTypeConfig()
    {
        typeConfig = AssetDatabase.LoadAssetAtPath<DataTableTypeConfigSO>(TypeConfigAssetPath);
    }

    private void SelectOrCreateTypeConfig()
    {
        typeConfig = AssetDatabase.LoadAssetAtPath<DataTableTypeConfigSO>(TypeConfigAssetPath);
        if (typeConfig == null)
        {
            typeConfig = ScriptableObject.CreateInstance<DataTableTypeConfigSO>();
            AssetDatabase.CreateAsset(typeConfig, TypeConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Selection.activeObject = typeConfig;
        EditorGUIUtility.PingObject(typeConfig);
    }

    private void InitDataList()
    {
        dataList = new ReorderableList(dataRows, typeof(List<string>), allowDrag, true, true, true);
        dataList.draggable = allowDrag;

        dataList.drawHeaderCallback = rect =>
        {
            int columns = GetColumnCount();
            if (columns <= 0)
            {
                EditorGUI.LabelField(rect, "Data");
                return;
            }

            float padding = 4f;
            float x = rect.x + padding;
            float y = rect.y;
            float colWidth = Mathf.Max(60f, (rect.width - padding * 2) / columns);
            List<string> header = headerRows.Count > 1 ? headerRows[1] : null;

            for (int c = 0; c < columns; c++)
            {
                string label = header != null && c < header.Count ? header[c] : "Col " + (c + 1);
                Rect r = new Rect(x, y, colWidth - 2f, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(r, label);
                x += colWidth;
            }
        };

        dataList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            int columns = GetColumnCount();
            if (index < 0 || index >= dataRows.Count)
            {
                return;
            }

            List<string> row = dataRows[index];
            EnsureColumnCount(row, columns);

            float padding = 4f;
            float x = rect.x + padding;
            float y = rect.y + 2f;
            float colWidth = Mathf.Max(60f, (rect.width - padding * 2) / columns);

            for (int c = 0; c < columns; c++)
            {
                string value = row[c] ?? string.Empty;
                Rect r = new Rect(x, y, colWidth - 2f, EditorGUIUtility.singleLineHeight);
                if (string.IsNullOrEmpty(value) && c == 0)
                {
                    EditorGUI.LabelField(r, " ");
                }
                else
                {
                    string newValue = EditorGUI.TextField(r, value);
                    if (newValue != value)
                    {
                        row[c] = newValue;
                        dirty = true;
                    }
                }
                x += colWidth;
            }
        };

        dataList.onAddCallback = list =>
        {
            AddRow();
        };

        dataList.onRemoveCallback = list =>
        {
            if (list.index >= 0 && list.index < dataRows.Count)
            {
                dataRows.RemoveAt(list.index);
                dirty = true;
            }
        };

        dataList.onReorderCallback = list =>
        {
            dirty = true;
        };

        dataList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
    }
}
