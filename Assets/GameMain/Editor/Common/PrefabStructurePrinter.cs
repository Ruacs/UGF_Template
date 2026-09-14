using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class PrefabStructurePrinter : MonoBehaviour
{
    // 顶部菜单
    [MenuItem("Tools/打印UI结构(Tree)")]
    public static void PrintSelected()
    {
        Print(Selection.activeGameObject);
    }

    // 👉 右键菜单（Hierarchy）
    [MenuItem("GameObject/打印UI结构(Tree)", false, 0)]
    public static void PrintSelectedFromHierarchy()
    {
        Print(Selection.activeGameObject);
    }

    // 👉 校验（没选物体时禁用）
    [MenuItem("GameObject/打印UI结构(Tree)", true)]
    public static bool ValidatePrintSelectedFromHierarchy()
    {
        return Selection.activeGameObject != null;
    }

    private static void Print(GameObject go)
    {
        if (go == null)
        {
            Debug.LogWarning("没选对象");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // 根节点
        sb.AppendLine(GetRootInfo(go));

        // 子节点
        PrintChild(go.transform, sb, "", true);

        string result = sb.ToString();

        // 👉 输出到控制台
        Debug.Log(result);

        // 👉 复制到剪切板（重点）
        EditorGUIUtility.systemCopyBuffer = result;

        Debug.Log("已复制到剪切板 ✅");
    }

    private static void PrintChild(Transform parent, StringBuilder sb, string indent, bool isRoot)
    {
        int childCount = parent.childCount;

        for (int i = 0; i < childCount; i++)
        {
            var child = parent.GetChild(i);
            bool isLast = (i == childCount - 1);

            string branch = isLast ? "└── " : "├── ";
            sb.AppendLine($"{indent}{branch}{GetNodeInfo(child)}");

            string newIndent = indent + (isLast ? "    " : "│   ");
            PrintChild(child, sb, newIndent, false);
        }
    }

    private static string GetRootInfo(GameObject go)
    {
        var components = go.GetComponents<Component>();
        StringBuilder compSb = new StringBuilder();

        foreach (var c in components)
        {
            if (c == null) continue;
            compSb.Append(c.GetType().Name).Append(",");
        }

        if (compSb.Length > 0)
            compSb.Length--;

        return $"{go.name}({compSb})";
    }

    private static string GetNodeInfo(Transform t)
    {
        var go = t.gameObject;
        var components = go.GetComponents<Component>();

        StringBuilder compSb = new StringBuilder();

        foreach (var c in components)
        {
            if (c == null) continue;
            compSb.Append(c.GetType().Name).Append(",");
        }

        if (compSb.Length > 0)
            compSb.Length--;

        return compSb.Length > 0
            ? $"{go.name}（{compSb}）"
            : $"{go.name}";
    }
}
