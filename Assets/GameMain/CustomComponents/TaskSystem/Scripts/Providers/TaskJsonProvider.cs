using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

///# StreamingAssets JSON 数据源// ② JSON 数据源（StreamingAssets / 热更新包）
public class TaskJsonProvider : ITaskDataProvider
{
    private readonly string _filePath;

    public TaskJsonProvider(string relativePath = "Tasks/task_config.json")
    {
        _filePath = Path.Combine(Application.streamingAssetsPath, relativePath);
    }

    public async UniTask<IReadOnlyList<TaskRawData>> LoadAllTasksAsync()
    {
        // 使用 GF 内置的 WebRequest 兼容 Android
        string text;
#if UNITY_ANDROID && !UNITY_EDITOR
        using var req = UnityWebRequest.Get(_filePath);
        await req.SendWebRequest();
        text = req.downloadHandler.text;
#else
        text = await File.ReadAllTextAsync(_filePath);
#endif
        var wrapper = JsonUtility.FromJson<TaskRawDataWrapper>(text);
        return wrapper.tasks;
    }

    [Serializable]
    private class TaskRawDataWrapper
    {
        public List<TaskRawData> tasks;
    }
}
