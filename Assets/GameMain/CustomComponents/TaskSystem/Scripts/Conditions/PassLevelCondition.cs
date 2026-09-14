using System;
using UnityEngine;

// 通过关卡条件
[Serializable]
[TaskCondition("通过关卡")]
public class PassLevelCondition : ITaskCondition
{
    public const string EventId = "on_pass_level";

    public string levelId;         // 关卡ID（为空则任意关卡均可）
    public int requiredCount;
    private int _current;

    public bool IsCompleted => _current >= requiredCount;
    public float Progress => requiredCount <= 0 ? 1f : Mathf.Clamp01((float)_current / requiredCount);
    public int CurrentCount => _current;

    public void OnEventReceived(string eventId, object data)
    {
        if (eventId != EventId) return;

        if (data is string passedLevelId)
        {
            if (string.IsNullOrEmpty(levelId) || passedLevelId == levelId)
                _current++;
        }
    }

    public void Reset() => _current = 0;
    public void RestoreCount(int count) => _current = Mathf.Max(0, count);
}
