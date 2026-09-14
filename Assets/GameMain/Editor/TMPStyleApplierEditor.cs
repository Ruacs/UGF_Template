using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TMPStyleApplier))]
[CanEditMultipleObjects]
public class TMPStyleApplierEditor : Editor
{
    private const float RefreshButtonWidth = 72f;

    private static List<string> s_CachedKeys;
    private static Dictionary<string, List<string>> s_KeySourceMap;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty styleKeyProp = serializedObject.FindProperty("_styleKey");
        EditorGUILayout.Space(2);
        DrawStyleKeyDropdown(styleKeyProp, "Style Key", Repaint);

        serializedObject.ApplyModifiedProperties();
    }

    public static void DrawStyleKeyDropdown(SerializedProperty styleKeyProp, string label = "Style Key", Action repaint = null)
    {
        if (styleKeyProp == null)
            return;

        if (s_CachedKeys == null)
            RefreshCache();

        EditorGUILayout.BeginHorizontal();

        if (s_CachedKeys.Count == 0)
        {
            EditorGUILayout.PropertyField(styleKeyProp, new GUIContent(label));
            DrawRefreshButton(repaint);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("No TMPFontProfile assets found. Create and configure a profile first.", MessageType.Warning);
            return;
        }

        string[] options = new string[s_CachedKeys.Count + 1];
        options[0] = "-- Default Material --";
        for (int i = 0; i < s_CachedKeys.Count; i++)
            options[i + 1] = s_CachedKeys[i];

        string currentKey = styleKeyProp.stringValue;
        int currentIndex = string.IsNullOrEmpty(currentKey)
            ? 0
            : s_CachedKeys.IndexOf(currentKey) + 1;

        bool isOrphan = !string.IsNullOrEmpty(currentKey) && currentIndex == 0;
        if (isOrphan)
        {
            string[] warningOptions = options.Prepend($"Missing: {currentKey}").ToArray();
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.6f, 0.6f);
            EditorGUILayout.Popup(label, 0, warningOptions);
            GUI.color = previousColor;
        }
        else
        {
            string tooltip = currentIndex > 0 && s_KeySourceMap.TryGetValue(currentKey, out List<string> sources)
                ? "Defined in: " + string.Join(", ", sources)
                : "Use the font asset default material.";

            int newIndex = EditorGUILayout.Popup(new GUIContent(label, tooltip), currentIndex, options);
            styleKeyProp.stringValue = newIndex == 0 ? string.Empty : s_CachedKeys[newIndex - 1];
        }

        DrawRefreshButton(repaint);
        EditorGUILayout.EndHorizontal();

        if (isOrphan)
            EditorGUILayout.HelpBox($"Style key '{currentKey}' was not found in any TMPFontProfile.", MessageType.Warning);
    }

    private static void DrawRefreshButton(Action repaint)
    {
        if (!GUILayout.Button("Refresh", GUILayout.Width(RefreshButtonWidth)))
            return;

        RefreshCache();
        repaint?.Invoke();
    }

    private static void RefreshCache()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        s_KeySourceMap = new Dictionary<string, List<string>>();

        string[] guids = AssetDatabase.FindAssets("t:TMPFontProfile");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMPFontProfile profile = AssetDatabase.LoadAssetAtPath<TMPFontProfile>(path);
            if (profile == null || profile.styles == null)
                continue;

            foreach (var entry in profile.styles)
            {
                if (string.IsNullOrEmpty(entry.key))
                    continue;

                if (!s_KeySourceMap.TryGetValue(entry.key, out List<string> sources))
                {
                    sources = new List<string>();
                    s_KeySourceMap[entry.key] = sources;
                }

                sources.Add(profile.name);
            }
        }

        s_CachedKeys = s_KeySourceMap.Keys.OrderBy(key => key).ToList();
    }
}
