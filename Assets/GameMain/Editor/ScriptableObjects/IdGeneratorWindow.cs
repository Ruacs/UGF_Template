using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using ConfigSO; // 你自己定义的命名空间

public class IdGeneratorWindow : EditorWindow
{
    private DefaultAsset targetFolder;
    private bool overrideExistingId = false;
    private int customStartId = -1;

    private Type[] configTypes;
    private string[] configTypeNames;
    private int selectedTypeIndex = 0;

    private List<IdOnlyConfigSO> cachedConfigs = new List<IdOnlyConfigSO>();
    private readonly Dictionary<int, int> manualIdInputs = new Dictionary<int, int>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Config/ID Generator")]
    public static void ShowWindow() => GetWindow<IdGeneratorWindow>("ID Generator");

    private void OnEnable()
    {
        // 收集所有 IdOnlyConfigSO 子类
        configTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                typeof(IdOnlyConfigSO).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToArray();

        configTypeNames = configTypes.Select(t => t.Name).ToArray();
        RefreshConfigList();
    }

    private void OnGUI()
    {
        GUILayout.Label("Config ID Generator", EditorStyles.boldLabel);
        GUILayout.Space(8);

        EditorGUI.BeginChangeCheck();

        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "目标文件夹",
            targetFolder,
            typeof(DefaultAsset),
            false
        );
        EditorGUILayout.HelpBox("每种类型资源都是统一编号，不区分主题，请选择包含所有的该类型资源的文件夹\n 可直接选择 ScriptableObjects 文件夹", MessageType.Info);

        selectedTypeIndex = EditorGUILayout.Popup(
            "Config 类型",
            selectedTypeIndex,
            configTypeNames
        );

        customStartId = EditorGUILayout.IntField(
            new GUIContent("起始 ID", "-1 表示使用 ConfigIdRange 中配置的起始 ID"),
            customStartId
        );
        EditorGUILayout.HelpBox("起始 ID 默认为 -1，表示使用 ConfigIdRange。输入其他值时，将从该 ID 开始生成，适合同资源类型下按子类型分段编号。", MessageType.Info);

        if (EditorGUI.EndChangeCheck())
        {
            RefreshConfigList(); // 文件夹或类型改变时刷新列表
        }

        overrideExistingId = EditorGUILayout.ToggleLeft(
            "覆盖已有 ID（危险）",
            overrideExistingId
        );

        GUILayout.Space(10);

        GUI.enabled = targetFolder != null && configTypes.Length > 0;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("生成 / 补全 ID", GUILayout.Height(30)))
        {
            GenerateIds();
            RefreshConfigList();
        }

        if (GUILayout.Button("清除 ID（全部置 0）", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "确认清除",
                "这将清空当前类型下所有配置的 ID，是否继续？",
                "确定",
                "取消"))
            {
                ClearIds();
                RefreshConfigList();
            }
        }
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        GUILayout.Space(15);
        DrawConfigList();
    }

    // ====================== 核心逻辑 ======================

    private void GenerateIds()
    {
        Type configType = configTypes[selectedTypeIndex];
        int startId = customStartId != -1
            ? customStartId
            : ConfigIdRange.GetStartId(configType);
        if (startId < 0) return;

        string folderPath = AssetDatabase.GetAssetPath(targetFolder);

        // 找到所有 ScriptableObject，再筛选类型
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folderPath });

        cachedConfigs = new List<IdOnlyConfigSO>();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<IdOnlyConfigSO>(path);
            if (asset != null && configType.IsAssignableFrom(asset.GetType()))
            {
                cachedConfigs.Add(asset);
            }
        }

        // 找出已存在的最大 ID
        int maxId = cachedConfigs
            .Where(a => a.id >= startId)
            .Select(a => a.id)
            .DefaultIfEmpty(startId - 1)
            .Max();

        int nextId = maxId + 1;

        foreach (var asset in cachedConfigs)
        {
            if (asset.id != 0 && !overrideExistingId) continue;
            asset.id = nextId++;
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ {configType.Name} ID 生成完成，共处理 {cachedConfigs.Count} 个");
    }

    private void ClearIds()
    {
        Type configType = configTypes[selectedTypeIndex];
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folderPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<IdOnlyConfigSO>(path);
            if (asset != null && configType.IsAssignableFrom(asset.GetType()))
            {
                asset.id = 0;
                EditorUtility.SetDirty(asset);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🧹 ID 已全部清除");
    }

    // ====================== 列表显示 ======================

    private void RefreshConfigList()
    {
        cachedConfigs.Clear();
        manualIdInputs.Clear();

        if (targetFolder == null || configTypes.Length == 0) return;

        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        Type selectedType = configTypes[selectedTypeIndex];

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folderPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<IdOnlyConfigSO>(path);
            if (asset != null && selectedType.IsAssignableFrom(asset.GetType()))
            {
                cachedConfigs.Add(asset);
            }
        }

        cachedConfigs = cachedConfigs
            .OrderBy(c => c.id <= 0) // ID=0 高亮在上面
            .ThenBy(c => c.id)
            .ToList();

        foreach (var config in cachedConfigs)
        {
            if (config == null) continue;
            manualIdInputs[config.GetInstanceID()] = Mathf.Max(0, config.id);
        }
    }

    private void DrawConfigList()
    {
        EditorGUILayout.LabelField($"当前配置列表（数量：{cachedConfigs.Count}）", EditorStyles.boldLabel);

        if (cachedConfigs.Count == 0)
        {
            EditorGUILayout.HelpBox("当前文件夹下没有该类型的配置文件", MessageType.Info);
            return;
        }

        // 统计重复 ID
        var duplicateIds = cachedConfigs
            .Where(c => c.id > 0)
            .GroupBy(c => c.id)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToHashSet();

        // 整个列表背景
        GUIStyle listBackground = new GUIStyle(GUI.skin.label);
        listBackground.padding = new RectOffset(4, 4, 4, 4);
        listBackground.margin = new RectOffset(5, 5, 5, 5);
        listBackground.normal.background = Texture2D.blackTexture; // 浅灰色背景

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
        EditorGUILayout.BeginVertical(listBackground,GUILayout.ExpandWidth(true));

        foreach (var config in cachedConfigs)
        {
            EditorGUILayout.BeginHorizontal("box", GUILayout.ExpandWidth(true)); // 每行独立框
            Color originalColor = GUI.color;

            if (config.id <= 0)
                GUI.color = Color.yellow;        // ID = 0 高亮
            else if (duplicateIds.Contains(config))
                GUI.color = Color.red;           // 重复 ID 高亮
            else
                GUI.color = Color.white;

            EditorGUILayout.LabelField(config.name, GUILayout.Width(220));
            EditorGUILayout.LabelField($"ID: {config.id}", GUILayout.Width(80));
            int key = config.GetInstanceID();
            int inputValue = manualIdInputs.TryGetValue(key, out var value) ? value : Mathf.Max(0, config.id);
            int newInputValue = EditorGUILayout.IntField(inputValue, GUILayout.Width(60));
            manualIdInputs[key] = Mathf.Max(0, newInputValue);

            GUI.enabled = manualIdInputs[key] > 0 && manualIdInputs[key] != config.id;
            if (GUILayout.Button("设置", GUILayout.Width(45)))
            {
                int targetId = manualIdInputs[key];
                if (HasDuplicateId(config, targetId))
                {
                    EditorUtility.DisplayDialog("ID 冲突", $"ID {targetId} 已存在于同类型配置中，请更换。", "确定");
                }
                else
                {
                    Undo.RecordObject(config, "Set Config ID");
                    config.id = targetId;
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    RefreshConfigList();
                    GUIUtility.ExitGUI();
                }
            }
            GUI.enabled = true;

            if (GUILayout.Button("Ping", GUILayout.Width(45)))
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }

            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2); // 行间距
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private bool HasDuplicateId(IdOnlyConfigSO current, int targetId)
    {
        if (targetId <= 0) return false;

        for (int i = 0; i < cachedConfigs.Count; i++)
        {
            var asset = cachedConfigs[i];
            if (asset == null || asset == current) continue;
            if (asset.id == targetId) return true;
        }

        return false;
    }

}
