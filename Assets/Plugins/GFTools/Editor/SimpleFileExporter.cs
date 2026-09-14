using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;

public class SimpleFileExporter : Editor
{
    [MenuItem("Tools/导出选中文件夹的文件名")]
    public static void ExportSelectedFolderFileNames()
    {
        if (Selection.activeObject == null)
        {
            Debug.LogError("请先选择一个文件夹");
            return;
        }

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        
        if (!Directory.Exists(path))
        {
            Debug.LogError("请选择有效的文件夹路径");
            return;
        }

        // 获取所有文件（不包括子文件夹）
        string[] files = Directory.GetFiles(path)
            .Where(file => !file.EndsWith(".meta"))
            .Select(Path.GetFileName)
            .ToArray();

        if (files.Length == 0)
        {
            Debug.Log("该文件夹中没有文件");
            return;
        }

        //for (int i = 0; i < files.Length; i++)
        //{
        //    files[i] = "\"" + files[i] + "\"";
        //}

        // 用分号连接文件名
        string result = string.Join(";", files);
        
        // 复制到剪贴板
        GUIUtility.systemCopyBuffer = result;
        
        Debug.Log($"已导出 {files.Length} 个文件名到剪贴板:\n{result}");
    }
}