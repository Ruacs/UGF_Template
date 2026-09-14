using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GF_Mahjong.RedDot;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime.RedDot;

public sealed class RedDotConfigDatabaseWindow : EditorWindow
{
    private RedDotConfigDatabase m_Database;
    private SerializedObject m_SerializedDatabase;
    private SerializedProperty m_Entries;
    private Vector2 m_Scroll;

    [MenuItem("Tools/Red Dot/Config Database")]
    public static void Open()
    {
        GetWindow<RedDotConfigDatabaseWindow>("Red Dot Config");
    }

    private void OnEnable()
    {
        LoadDatabase();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        DrawToolbar();

        if (m_Database == null)
        {
            EditorGUILayout.HelpBox("未找到 RedDotConfigDatabase。", MessageType.Warning);
            if (GUILayout.Button("Create Default Database"))
                CreateDatabase();
            return;
        }

        m_SerializedDatabase.Update();
        m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
        EditorGUILayout.PropertyField(m_Entries, true);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Default Entries"))
            AddDefaultEntries();
        if (GUILayout.Button("Validate"))
            ValidateEntries();
        if (GUILayout.Button("Write RedDotPath.cs"))
            WriteRedDotPathFile();
        if (GUILayout.Button("Select Asset"))
            Selection.activeObject = m_Database;
        EditorGUILayout.EndHorizontal();

        m_SerializedDatabase.ApplyModifiedProperties();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Database", GUILayout.Width(70));
        EditorGUI.BeginChangeCheck();
        m_Database = (RedDotConfigDatabase)EditorGUILayout.ObjectField(m_Database, typeof(RedDotConfigDatabase), false);
        if (EditorGUI.EndChangeCheck())
            BindSerializedDatabase();

        if (GUILayout.Button("Load Default", EditorStyles.toolbarButton, GUILayout.Width(90)))
            LoadDatabase();
        EditorGUILayout.EndHorizontal();
    }

    private void LoadDatabase()
    {
        m_Database = AssetDatabase.LoadAssetAtPath<RedDotConfigDatabase>(RedDotConfigDatabase.DefaultAssetPath);
        BindSerializedDatabase();
    }

    private void CreateDatabase()
    {
        m_Database = CreateInstance<RedDotConfigDatabase>();
        AssetDatabase.CreateAsset(m_Database, RedDotConfigDatabase.DefaultAssetPath);
        AssetDatabase.SaveAssets();
        BindSerializedDatabase();
        AddDefaultEntries();
        Selection.activeObject = m_Database;
    }

    private void BindSerializedDatabase()
    {
        if (m_Database == null)
        {
            m_SerializedDatabase = null;
            m_Entries = null;
            return;
        }

        m_SerializedDatabase = new SerializedObject(m_Database);
        m_Entries = m_SerializedDatabase.FindProperty("m_Entries");
    }

    private void AddDefaultEntries()
    {
        if (m_SerializedDatabase == null)
            return;

        m_SerializedDatabase.Update();
        AddIfMissing("Main", "大厅入口", RedDotPath.Main, "大厅一级入口聚合红点");
        AddIfMissing("MainTask", "大厅/任务", RedDotPath.MainTask, "任务入口聚合红点");
        AddIfMissing("MainTaskChest", "大厅/任务/宝箱", RedDotPath.MainTaskChest, "任务宝箱聚合红点");
        AddIfMissing("MainTaskChest1", "大厅/任务/宝箱1", RedDotPath.MainTaskChest1, "任务宝箱1可领取");
        AddIfMissing("MainTaskChest2", "大厅/任务/宝箱2", RedDotPath.MainTaskChest2, "任务宝箱2可领取");
        AddIfMissing("MainTaskChest3", "大厅/任务/宝箱3", RedDotPath.MainTaskChest3, "任务宝箱3可领取");
        AddIfMissing("MainTaskDaily", "大厅/任务/每日任务", RedDotPath.MainTaskDaily, "每日任务可领取");
        AddIfMissing("MainTaskAchievement", "大厅/任务/成就", RedDotPath.MainTaskAchievement, "成就可领取");
        AddIfMissing("MainShop", "大厅/商店", RedDotPath.MainShop, "商店入口聚合红点");
        AddIfMissing("MainShopFree", "大厅/商店/免费礼包", RedDotPath.MainShopFree, "免费礼包可领取");
        AddIfMissing("MainRank", "大厅/排行", RedDotPath.MainRank, "排行入口聚合红点");
        AddIfMissing("MainRankSeasonReward", "大厅/排行/赛季奖励", RedDotPath.MainRankSeasonReward, "排行赛季奖励可领取");
        AddIfMissing("Game", "对局入口", RedDotPath.Game, "对局内聚合红点");
        AddIfMissing("GameItem", "对局/道具", RedDotPath.GameItem, "对局内道具红点");
        m_SerializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(m_Database);
        AssetDatabase.SaveAssets();
    }

    private void AddIfMissing(string key, string displayName, string path, string description)
    {
        for (int i = 0; i < m_Entries.arraySize; i++)
        {
            SerializedProperty item = m_Entries.GetArrayElementAtIndex(i);
            if (item.FindPropertyRelative("m_Key").stringValue == key)
                return;
        }

        int index = m_Entries.arraySize;
        m_Entries.InsertArrayElementAtIndex(index);
        SerializedProperty entry = m_Entries.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("m_Key").stringValue = key;
        entry.FindPropertyRelative("m_DisplayName").stringValue = displayName;
        entry.FindPropertyRelative("m_Path").stringValue = path;
        entry.FindPropertyRelative("m_Description").stringValue = description;
    }

    private void ValidateEntries()
    {
        if (m_Database == null)
            return;

        HashSet<string> keys = new HashSet<string>();
        HashSet<string> paths = new HashSet<string>();
        List<string> errors = new List<string>();

        foreach (RedDotConfigEntry entry in m_Database.Entries)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.Key))
                errors.Add("存在空 Key。");
            else if (!keys.Add(entry.Key))
                errors.Add($"重复 Key：{entry.Key}");

            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                errors.Add($"路径为空：{entry.Key}");
            }
            else
            {
                if (entry.Path.StartsWith(".") || entry.Path.EndsWith(".") || entry.Path.Contains("..") || entry.Path.Contains(" "))
                    errors.Add($"路径格式不合法：{entry.Key} -> {entry.Path}");
                if (!paths.Add(entry.Path))
                    errors.Add($"重复 Path：{entry.Path}");
            }
        }

        if (errors.Count == 0)
        {
            EditorUtility.DisplayDialog("Red Dot Config", "校验通过。", "OK");
            return;
        }

        EditorUtility.DisplayDialog("Red Dot Config", string.Join("\n", errors), "OK");
    }

    private void WriteRedDotPathFile()
    {
        if (m_Database == null)
            return;

        List<RedDotConfigEntry> entries = GetValidEntriesForCode();
        if (entries.Count == 0)
        {
            EditorUtility.DisplayDialog("Red Dot Config", "没有可写入 RedDotPath.cs 的有效配置。", "OK");
            return;
        }

        string path = RedDotPathFilePath;
        File.WriteAllText(path, BuildRedDotPathSource(entries), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(path);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Red Dot Config", $"已写入 {path}", "OK");
    }

    private List<RedDotConfigEntry> GetValidEntriesForCode()
    {
        List<RedDotConfigEntry> result = new List<RedDotConfigEntry>();
        HashSet<string> names = new HashSet<string>();
        HashSet<string> paths = new HashSet<string>();
        List<string> errors = new List<string>();

        foreach (RedDotConfigEntry entry in m_Database.Entries)
        {
            if (entry == null)
                continue;

            string constName = ToConstName(entry.Key);
            if (string.IsNullOrWhiteSpace(constName))
            {
                errors.Add($"Key 无法生成 C# 常量名：{entry.Key}");
                continue;
            }

            if (!names.Add(constName))
            {
                errors.Add($"常量名重复：{constName}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                errors.Add($"路径为空：{entry.Key}");
                continue;
            }

            if (!paths.Add(entry.Path))
            {
                errors.Add($"路径重复：{entry.Path}");
                continue;
            }

            result.Add(entry);
        }

        if (errors.Count > 0)
            EditorUtility.DisplayDialog("Red Dot Config", string.Join("\n", errors), "OK");

        return errors.Count == 0 ? result : new List<RedDotConfigEntry>();
    }

    private static string BuildRedDotPathSource(IReadOnlyList<RedDotConfigEntry> entries)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("// Generated by Tools/Red Dot/Config Database.");
        builder.AppendLine("// Edit RedDotConfigDatabase.asset, then click Write RedDotPath.cs.");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine();
        builder.AppendLine("namespace GF_Mahjong.RedDot");
        builder.AppendLine("{");
        builder.AppendLine("    public static class RedDotPath");
        builder.AppendLine("    {");

        foreach (RedDotConfigEntry entry in entries)
        {
            string constName = ToConstName(entry.Key);
            string path = EscapeString(entry.Path);
            builder.AppendLine($"        public const string {constName} = \"{path}\";");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string ToConstName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        string value = Regex.Replace(key.Trim(), "[^a-zA-Z0-9_]", "_");
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (char.IsDigit(value[0]))
            value = "_" + value;

        return value;
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string RedDotPathFilePath => "Assets/GameMain/CustomComponents/RedDot/Core/RedDotPath.cs";
}
