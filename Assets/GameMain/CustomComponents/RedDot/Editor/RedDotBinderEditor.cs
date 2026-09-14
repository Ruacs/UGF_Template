using System.Collections.Generic;
using GF_Mahjong.RedDot;
using GF_Mahjong.UI;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime.RedDot;

[CustomEditor(typeof(RedDotBinder))]
public class RedDotBinderEditor : Editor
{
    private SerializedProperty m_ConfigDatabase;
    private SerializedProperty m_ConfigKey;
    private SerializedProperty m_Path;
    private SerializedProperty m_RedDotRoot;
    private SerializedProperty m_DisplayType;
    private SerializedProperty m_DotRoot;
    private SerializedProperty m_CountRoot;
    private SerializedProperty m_CountBackground;
    private SerializedProperty m_CountText;
    private SerializedProperty m_TMPCountText;
    private SerializedProperty m_MaxDisplayCount;
    private SerializedProperty m_OverflowSuffix;
    private SerializedProperty m_HideWhenZero;

    private void OnEnable()
    {
        m_ConfigDatabase = serializedObject.FindProperty("m_ConfigDatabase");
        m_ConfigKey = serializedObject.FindProperty("m_ConfigKey");
        m_Path = serializedObject.FindProperty("m_Path");
        m_RedDotRoot = serializedObject.FindProperty("m_RedDotRoot");
        m_DisplayType = serializedObject.FindProperty("m_DisplayType");
        m_DotRoot = serializedObject.FindProperty("m_DotRoot");
        m_CountRoot = serializedObject.FindProperty("m_CountRoot");
        m_CountBackground = serializedObject.FindProperty("m_CountBackground");
        m_CountText = serializedObject.FindProperty("m_CountText");
        m_TMPCountText = serializedObject.FindProperty("m_TMPCountText");
        m_MaxDisplayCount = serializedObject.FindProperty("m_MaxDisplayCount");
        m_OverflowSuffix = serializedObject.FindProperty("m_OverflowSuffix");
        m_HideWhenZero = serializedObject.FindProperty("m_HideWhenZero");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBinding();
        DrawDisplay();
        DrawVisibility();

        if (serializedObject.ApplyModifiedProperties())
            ApplyEditorPreview();
    }

    private void DrawBinding()
    {
        EditorGUILayout.LabelField("Binding", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_ConfigDatabase);

        RedDotConfigDatabase database = m_ConfigDatabase.objectReferenceValue as RedDotConfigDatabase;
        if (database == null)
        {
            database = AssetDatabase.LoadAssetAtPath<RedDotConfigDatabase>(RedDotConfigDatabase.DefaultAssetPath);
            if (database != null)
                m_ConfigDatabase.objectReferenceValue = database;
        }

        if (database == null)
        {
            EditorGUILayout.HelpBox("RedDotConfigDatabase 不存在，请先创建配置库。", MessageType.Warning);
            if (GUILayout.Button("Open Red Dot Config"))
                RedDotConfigDatabaseWindow.Open();
        }
        else
        {
            DrawConfigPopup(database);
        }

        EditorGUILayout.PropertyField(m_RedDotRoot);
    }

    private void DrawConfigPopup(RedDotConfigDatabase database)
    {
        IReadOnlyList<RedDotConfigEntry> entries = database.Entries;
        if (entries == null || entries.Count == 0)
        {
            EditorGUILayout.HelpBox("配置库为空，请在 Red Dot Config 中添加路径。", MessageType.Warning);
            if (GUILayout.Button("Open Red Dot Config"))
                RedDotConfigDatabaseWindow.Open();
            return;
        }

        List<string> options = new List<string> { "未选择" };
        for (int i = 0; i < entries.Count; i++)
        {
            RedDotConfigEntry entry = entries[i];
            string label = entry == null ? "<Missing>" : $"{entry.DisplayName}    ({entry.Path})";
            options.Add(label);
        }

        int selectedIndex = FindSelectedIndex(entries);
        EditorGUI.BeginChangeCheck();
        selectedIndex = EditorGUILayout.Popup("Red Dot Config", selectedIndex, options.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            if (selectedIndex <= 0)
            {
                m_ConfigKey.stringValue = string.Empty;
                m_Path.stringValue = string.Empty;
            }
            else
            {
                RedDotConfigEntry entry = entries[selectedIndex - 1];
                m_ConfigKey.stringValue = entry.Key;
                m_Path.stringValue = entry.Path;
            }
        }

        string resolvedPath = ResolvePath(database);
        EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(resolvedPath) ? "当前未绑定红点路径。" : $"Path: {resolvedPath}", MessageType.None);

        if (!string.IsNullOrWhiteSpace(m_ConfigKey.stringValue) && !database.TryGetEntry(m_ConfigKey.stringValue, out _))
            EditorGUILayout.HelpBox($"配置 Key 不存在：{m_ConfigKey.stringValue}", MessageType.Error);

        if (GUILayout.Button("Open Red Dot Config"))
            RedDotConfigDatabaseWindow.Open();
    }

    private int FindSelectedIndex(IReadOnlyList<RedDotConfigEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            RedDotConfigEntry entry = entries[i];
            if (entry != null && entry.Key == m_ConfigKey.stringValue)
                return i + 1;
        }

        if (!string.IsNullOrWhiteSpace(m_Path.stringValue))
        {
            for (int i = 0; i < entries.Count; i++)
            {
                RedDotConfigEntry entry = entries[i];
                if (entry != null && entry.Path == m_Path.stringValue)
                    return i + 1;
            }
        }

        return 0;
    }

    private string ResolvePath(RedDotConfigDatabase database)
    {
        if (database != null && database.TryGetPath(m_ConfigKey.stringValue, out string path))
            return path;

        return m_Path.stringValue;
    }

    private void DrawDisplay()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_DisplayType);

        RedDotDisplayType displayType = (RedDotDisplayType)m_DisplayType.enumValueIndex;
        bool showDot = NeedsDot(displayType);
        bool showCount = NeedsCount(displayType);
        bool showBackground = NeedsBackground(displayType);
        bool showCustom = displayType == RedDotDisplayType.Custom;

        if (showDot || showCustom)
            EditorGUILayout.PropertyField(m_DotRoot);
        if (showCount || showCustom)
        {
            EditorGUILayout.PropertyField(m_CountRoot);
            EditorGUILayout.PropertyField(m_CountText);
            EditorGUILayout.PropertyField(m_TMPCountText);
        }
        if (showBackground || showCustom)
            EditorGUILayout.PropertyField(m_CountBackground);

        if (showCount || showCustom)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Count", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_MaxDisplayCount);
            EditorGUILayout.PropertyField(m_OverflowSuffix);
        }
    }

    private void DrawVisibility()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visibility", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_HideWhenZero);
    }

    private static bool NeedsDot(RedDotDisplayType displayType)
    {
        return displayType == RedDotDisplayType.DotOnly ||
               displayType == RedDotDisplayType.DotAndBackgroundAndCount;
    }

    private static bool NeedsCount(RedDotDisplayType displayType)
    {
        return displayType == RedDotDisplayType.CountOnly ||
               displayType == RedDotDisplayType.BackgroundAndCount ||
               displayType == RedDotDisplayType.DotAndBackgroundAndCount;
    }

    private static bool NeedsBackground(RedDotDisplayType displayType)
    {
        return displayType == RedDotDisplayType.BackgroundAndCount ||
               displayType == RedDotDisplayType.DotAndBackgroundAndCount;
    }

    private void ApplyEditorPreview()
    {
        foreach (Object item in targets)
        {
            RedDotBinder binder = item as RedDotBinder;
            if (binder == null)
                continue;

            binder.ApplyEditorPreview();
            EditorUtility.SetDirty(binder);
        }
    }
}
