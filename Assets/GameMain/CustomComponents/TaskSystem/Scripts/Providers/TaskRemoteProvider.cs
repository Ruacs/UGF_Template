// ③ 远程数据源（服务端下发）

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class TaskRemoteProvider : ITaskDataProvider
{
    private readonly string _apiUrl;

    public TaskRemoteProvider(string apiUrl)
    {
        _apiUrl = apiUrl;
    }

    public async UniTask<IReadOnlyList<TaskRawData>> LoadAllTasksAsync()
    {
        using var req = UnityWebRequest.Get(_apiUrl);
        req.SetRequestHeader("Authorization", PlayerPrefs.GetString("token"));
        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception($"Task remote load failed: {req.error}");

        var wrapper = JsonUtility.FromJson<TaskRawDataWrapper>(req.downloadHandler.text);
        return wrapper.tasks;
    }

    [Serializable]
    private class TaskRawDataWrapper { public List<TaskRawData> tasks; }
}