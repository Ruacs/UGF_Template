using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public class JsonLevelMergeTool : EditorWindow
{
    private TextAsset oldJsonFile;
    private TextAsset newJsonFile;

    [MenuItem("Tools/Level/Json配置合并工具")]
    public static void ShowWindow()
    {
        GetWindow<JsonLevelMergeTool>("Json配置合并工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("按 Levels.id 替换关卡数据", EditorStyles.boldLabel);

        oldJsonFile = (TextAsset)EditorGUILayout.ObjectField("原配置文件", oldJsonFile, typeof(TextAsset), false);
        newJsonFile = (TextAsset)EditorGUILayout.ObjectField("新配置文件", newJsonFile, typeof(TextAsset), false);

        if (GUILayout.Button("合并并导出"))
        {
            MergeJson();
        }
    }

    private void MergeJson()
    {
        if (oldJsonFile == null || newJsonFile == null)
        {
            Debug.LogError("请选择原配置文件和新配置文件");
            return;
        }

        var oldConfig = JsonConvert.DeserializeObject<LevelConfig>(oldJsonFile.text);
        var newLevels = ParseNewLevels(newJsonFile.text);

        if (oldConfig == null || oldConfig.Levels == null)
        {
            Debug.LogError("原配置文件格式错误：需要包含 StartLevel 和 Levels。");
            return;
        }

        if (newLevels == null || newLevels.Count == 0)
        {
            Debug.LogError("新配置文件格式错误：未读取到任何关卡数据。");
            return;
        }

        Dictionary<int, LevelData> newLevelMap = new Dictionary<int, LevelData>();

        foreach (var level in newLevels)
        {
            if (level == null) continue;
            newLevelMap[level.id] = level;
        }

        int replaceCount = 0;

        for (int i = 0; i < oldConfig.Levels.Count; i++)
        {
            int id = oldConfig.Levels[i].id;

            if (newLevelMap.TryGetValue(id, out var newLevel))
            {
                oldConfig.Levels[i] = newLevel;
                replaceCount++;
            }
        }

        string outputPath = EditorUtility.SaveFilePanel(
            "保存合并后的Json",
            Application.dataPath,
            "LevelConfig_Merged.json",
            "json"
        );

        if (string.IsNullOrEmpty(outputPath))
            return;

        string json = JsonConvert.SerializeObject(oldConfig, Formatting.Indented);
        File.WriteAllText(outputPath, json);

        Debug.Log($"合并完成，替换关卡数量：{replaceCount}");
        AssetDatabase.Refresh();
    }

    private static List<LevelData> ParseNewLevels(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<LevelData>();

        JToken root;
        try
        {
            root = JToken.Parse(json);
        }
        catch (JsonReaderException)
        {
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
                throw;

            root = JToken.Parse($"[{trimmed}]");
        }

        switch (root.Type)
        {
            case JTokenType.Array:
                return root.ToObject<List<LevelData>>() ?? new List<LevelData>();

            case JTokenType.Object:
                var obj = (JObject)root;
                JToken levelsToken = obj["Levels"] ?? obj["levels"];
                if (levelsToken != null)
                    return levelsToken.ToObject<List<LevelData>>() ?? new List<LevelData>();

                var singleLevel = obj.ToObject<LevelData>();
                return singleLevel != null ? new List<LevelData> { singleLevel } : new List<LevelData>();

            default:
                return new List<LevelData>();
        }
    }
}

[System.Serializable]
public class LevelConfig
{
    public int StartLevel;
    public List<LevelData> Levels;
}

[System.Serializable]
public class LevelData
{
    public int id;
    public string Name;
    public int TimeSeconds;
    public int Difficulty;
    public int LevelMode;
    public int Cols;
    public int Rows;
    public int TypeCount;
    public List<int> CollectTargetIds;
    public int RanchScore;
    public string PatternRows;
}
