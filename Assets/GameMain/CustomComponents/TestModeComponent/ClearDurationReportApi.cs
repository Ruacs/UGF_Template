using System;
using System.Collections;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class ClearDurationReportApi
{
    // 按你的电脑 / 局域网地址改这里
    // 本机编辑器测试可用: http://127.0.0.1:8788
    // 真机测试要改成电脑局域网 IP，例如: http://192.168.1.23:8788
    public static string BaseUrl = "http://wf42a7c8.natappfree.cc";

    [Serializable]
    public class ClearDurationReport
    {
        public string layout;
        public string levelId;
        public float clearDuration;
    }

    [Serializable]
    private class ApiResponse
    {
        public bool ok;
    }

    public static IEnumerator PostClearDuration(
        string layout,
        string levelId,
        float clearDuration,
        Action<bool, string> callback = null)
    {
        var report = new ClearDurationReport
        {
            layout = layout,
            levelId = levelId,
            clearDuration = clearDuration
        };

        string json = JsonUtility.ToJson(report);
        string url = $"{BaseUrl}/api/clear-durations";

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? request.downloadHandler.text : request.error;

        callback?.Invoke(success, message);
    }

    public static async UniTask<(bool success, string message)> PostClearDurationAsync(
        string layout,
        string levelId,
        float clearDuration)
    {

        return (false, "Async operation not implemented");
        var report = new ClearDurationReport
        {
            layout = layout,
            levelId = levelId,
            clearDuration = clearDuration
        };

        string json = JsonUtility.ToJson(report);
        string url = $"{BaseUrl}/api/clear-durations";

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest().ToUniTask();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? request.downloadHandler.text : request.error;

        return (success, message);
    }



    [Serializable]
    public class DeadlockReport
    {
        public string layout;
        public string levelId;
        public string result;
    }

    public static IEnumerator PostDeadlockNoMatch(
        string layout,
        string levelId,
        Action<bool, string> callback = null)
    {
        var report = new DeadlockReport
        {
            layout = layout,
            levelId = levelId,
            result = "NoMatch"
        };

        string json = JsonUtility.ToJson(report);
        string url = $"{BaseUrl}/api/deadlock-reports";

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? request.downloadHandler.text : request.error;

        callback?.Invoke(success, message);
    }

    public static async UniTask<(bool success, string message)> PostDeadlockNoMatchAsync(
        string layout,
        string levelId)
    {
        return (false, "Async operation not implemented");
        var report = new DeadlockReport
        {
            layout = layout,
            levelId = levelId,
            result = "NoMatch"
        };

        string json = JsonUtility.ToJson(report);
        string url = $"{BaseUrl}/api/deadlock-reports";

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest().ToUniTask();

        bool success = request.result == UnityWebRequest.Result.Success;
        string message = success ? request.downloadHandler.text : request.error;

        return (success, message);
    }
}
