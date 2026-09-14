using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Data;

public class TableConstCodeGeneratorEditor : EditorWindow
{
    private const int ID_COLUMN = 1;            // Id 列（0 列为空白）
    private const int COMMENT_COLUMN = 2;       // 注释列（可选）
    private const int NAME_COLUMN = 3;          // 资源名称列
    private const string TABLESUFFIX = ".txt";  //数据表扩展名
    private string m_Namespace = "Lokas";
    private string m_DataTablePath = "Assets/GameMain/DataTables/";
    private string m_OutputPath = "Assets/GameMain/Scripts/Definition";
    private CodeGenType m_CodeGenType = CodeGenType.Class;
    private string[] m_TableNameArray;
    private int m_SelectedTableIndex;
    private bool m_CustomCodeFileName = false;  // 是否自定义代码文件名
    private string m_CustomFileName = "";       // 自定义代码文件名

    [MenuItem("Game Framework/数据表常量代码生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<TableConstCodeGeneratorEditor>("数据表常量生成器");
        window.minSize = new Vector2(420, 220);
        window.Show();

        
    }

    private void OnEnable()
    {
        LoadDataTable();
    }

    private void OnGUI()
    {
        m_Namespace = EditorGUILayout.TextField("命名空间", m_Namespace);
        m_DataTablePath = EditorGUILayout.TextField("数据表路径", m_DataTablePath);
        m_OutputPath = EditorGUILayout.TextField("输出文件夹", m_OutputPath);

        m_CustomCodeFileName = EditorGUILayout.Toggle("自定义文件名", m_CustomCodeFileName);
        if (m_CustomCodeFileName)
        {
            m_CustomFileName = EditorGUILayout.TextField("自定义文件名", m_CustomFileName);
        }

        m_CodeGenType = (CodeGenType)EditorGUILayout.EnumPopup("代码生成类型", m_CodeGenType);


        if (m_TableNameArray == null || m_TableNameArray.Length <= 0) return;

        m_SelectedTableIndex = EditorGUILayout.Popup("选择数据表:",m_SelectedTableIndex,m_TableNameArray);

        GUILayout.Space(10);
        if (GUILayout.Button("生成常量代码"))
        {
            GenerateFromTable(m_DataTablePath + m_TableNameArray[m_SelectedTableIndex] + TABLESUFFIX);
        }
    }




    private void LoadDataTable()
    {
        if (!Directory.Exists(m_DataTablePath))
        {
            Debug.LogError($"数据表路径不存在: {m_DataTablePath}");
            return;
        }

        string[] tableFiles = Directory.GetFiles(m_DataTablePath, "*.txt");
        m_TableNameArray = new string[tableFiles.Length];
        for (int i = 0; i < tableFiles.Length; i++)
        {
            m_TableNameArray[i] = Path.GetFileNameWithoutExtension(tableFiles[i]);
        }

    }


    // ================= 核心生成 =================

    private void GenerateFromTable(string tablePath)
    {
        if (!File.Exists(tablePath))
        {
            Debug.LogError($"数据表不存在: {tablePath}");
            return;
        }

        var lines = File.ReadAllLines(tablePath);
        if (lines.Length == 0)
            return;

        string fileName = Path.GetFileNameWithoutExtension(tablePath);
        string typeName =  m_CustomCodeFileName ? m_CustomFileName :  ToPascalCase(fileName) + "Id";

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("// Auto Generated, DO NOT EDIT.");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine($"namespace {m_Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    public {GetTypeKeyword()} {typeName}");
        sb.AppendLine("    {");

        foreach (var raw in lines)
        {
            if (IsCommentLine(raw))
                continue;

            var cols = raw.Split('\t');

            if (!TryParseRow(cols, out string id, out string name, out string comment))
                continue;

            if (!string.IsNullOrEmpty(comment))
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {comment}");
                sb.AppendLine("        /// </summary>");
            }

            if (m_CodeGenType == CodeGenType.Class)
            {
                sb.AppendLine($"        public const int {name} = {id};");
            }
            else
            {
                sb.AppendLine($"        {name} = {id},");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        Directory.CreateDirectory(m_OutputPath);
        File.WriteAllText(
            Path.Combine(m_OutputPath, typeName + ".cs"),
            sb.ToString(),
            Encoding.UTF8
        );

        AssetDatabase.Refresh();
        Debug.Log($"生成成功: {typeName}.cs");
    }

    // ================= 解析 & 判断 =================

    private bool TryParseRow(string[] cols, out string id, out string name, out string comment)
    {
        id = null;
        name = null;
        comment = null;

        if (cols.Length <= ID_COLUMN)
            return false;

        id = cols[ID_COLUMN].Trim();
        if (!int.TryParse(id, out _))
            return false;

        // 有注释列 + 名称列
        if (cols.Length > NAME_COLUMN)
        {
            comment = cols[COMMENT_COLUMN].Trim();
            name = cols[NAME_COLUMN].Trim();
        }
        // 没注释列，NAME 向前一列
        else if (cols.Length > COMMENT_COLUMN)
        {
            name = cols[COMMENT_COLUMN].Trim();
        }

        return !string.IsNullOrEmpty(name);
    }

    private bool IsCommentLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return true;

        return line.TrimStart().StartsWith("#");
    }

    // ================= 工具方法 =================

    private string GetTypeKeyword()
    {
        return m_CodeGenType == CodeGenType.Enum
            ? "enum"
            : "static class";
    }

    private static string ToPascalCase(string input)
    {
        var parts = input.Split('_');
        StringBuilder sb = new StringBuilder();

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            sb.Append(char.ToUpper(part[0]) + part.Substring(1));
        }

        return sb.ToString();
    }
}

/// <summary>
/// 代码生成类型
/// </summary>
public enum CodeGenType
{
    Class,
    Enum
}
