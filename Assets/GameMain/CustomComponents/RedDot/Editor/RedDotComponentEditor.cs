using System.Collections.Generic;
using System.Linq;
using GF_Mahjong.RedDot;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime.RedDot;

[CustomEditor(typeof(RedDotComponent))]
public sealed class RedDotComponentEditor : Editor
{
    private string m_SearchText = string.Empty;
    private bool m_OnlyActive;
    private Vector2 m_Scroll;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Nodes", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后可查看实时红点节点状态。", MessageType.Info);
            return;
        }

        RedDotComponent component = (RedDotComponent)target;
        List<RedDotNode> nodes = component.GetAllNodes()
            .OrderBy(node => node.Path)
            .ToList();

        DrawToolbar(nodes);
        DrawNodeTable(nodes);

        if (Application.isPlaying)
            Repaint();
    }

    private void DrawToolbar(IReadOnlyList<RedDotNode> nodes)
    {
        int activeCount = nodes.Count(node => node.IsActive);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Total: {nodes.Count}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"Active: {activeCount}", GUILayout.Width(90));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear Search", GUILayout.Width(95)))
            m_SearchText = string.Empty;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_SearchText = EditorGUILayout.TextField("Search", m_SearchText);
        m_OnlyActive = EditorGUILayout.ToggleLeft("Only Active", m_OnlyActive, GUILayout.Width(95));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawNodeTable(IEnumerable<RedDotNode> nodes)
    {
        IEnumerable<RedDotNode> filtered = nodes;
        if (m_OnlyActive)
            filtered = filtered.Where(node => node.IsActive);
        if (!string.IsNullOrWhiteSpace(m_SearchText))
            filtered = filtered.Where(node => node.Path.ToLowerInvariant().Contains(m_SearchText.Trim().ToLowerInvariant()));

        List<RedDotNode> visibleNodes = filtered.ToList();
        if (visibleNodes.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有匹配的红点节点。", MessageType.None);
            return;
        }

        DrawNodeTableHeader();
        m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(360));

        foreach (RedDotNode node in visibleNodes)
            DrawNodeRow(node);

        EditorGUILayout.EndScrollView();
    }

    private static void DrawNodeTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Path", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Self", EditorStyles.boldLabel, GUILayout.Width(45));
        EditorGUILayout.LabelField("Total", EditorStyles.boldLabel, GUILayout.Width(45));
        EditorGUILayout.LabelField("Active", EditorStyles.boldLabel, GUILayout.Width(55));
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawNodeRow(RedDotNode node)
    {
        GUIStyle pathStyle = node.IsActive ? EditorStyles.boldLabel : EditorStyles.label;
        int depth = GetDepth(node);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(depth * 12);
        EditorGUILayout.LabelField(node.Path, pathStyle);
        EditorGUILayout.LabelField(node.SelfCount.ToString(), GUILayout.Width(45));
        EditorGUILayout.LabelField(node.TotalCount.ToString(), GUILayout.Width(45));
        EditorGUILayout.LabelField(node.IsActive ? "Yes" : "No", GUILayout.Width(55));
        EditorGUILayout.EndHorizontal();
    }

    private static int GetDepth(RedDotNode node)
    {
        int depth = 0;
        RedDotNode current = node.Parent;
        while (current != null && !string.IsNullOrEmpty(current.Path))
        {
            depth++;
            current = current.Parent;
        }

        return depth;
    }
}
