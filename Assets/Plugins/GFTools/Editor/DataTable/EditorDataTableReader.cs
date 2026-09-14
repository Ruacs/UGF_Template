
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityGameFramework.Runtime;
public static class EditorDataTableReader 
{
    public static List<T> LoadDataRows<T>(string filePath) where T : DataRowBase, new()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"数据表不存在：{filePath}");
            return null;
        }

        var list = new List<T>();

        var lines = File.ReadAllLines(filePath);

        foreach (var raw in lines)
        {
            string line = raw.TrimEnd();
            if (string.IsNullOrEmpty(line))
                continue;

            // 跳过注释行
            if (line.StartsWith("#"))
                continue;

            // 跳过空列/废行
            if (line.Length < 2)
                continue;

            // 实例化行对象
            T row = new T();

            try
            {
                // 直接复用 UGF 的 ParseDataRow(string)
                if (row.ParseDataRow(line, null))
                {
                    list.Add(row);
                }
                else
                {
                    Debug.LogError($"解析失败：{line}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"解析异常：{line}\n{e}");
            }
        }

        return list;
    }
}
