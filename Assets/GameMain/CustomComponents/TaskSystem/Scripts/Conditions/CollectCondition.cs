// 收集条件
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[TaskCondition("收集物品")]
public class CollectCondition : ITaskCondition
{
    public const string EventId = "on_collect";

    public string itemId;
    public int requiredCount;
    private int _current;

    public bool IsCompleted => _current >= requiredCount;
    public float Progress => requiredCount <= 0 ? 1f : Mathf.Clamp01((float)_current / requiredCount);
    public int CurrentCount => _current;

    public void OnEventReceived(string eventId, object data)
    {
        if (eventId != EventId || data == null) return;

        if (data is ValueTuple<string, int> tuple && tuple.Item1 == itemId)
        {
            _current += Mathf.Max(0, tuple.Item2);
            return;
        }

        if (data is KeyValuePair<string, int> kvp && kvp.Key == itemId)
            _current += Mathf.Max(0, kvp.Value);
    }

    public void Reset() => _current = 0;
    public void RestoreCount(int count) => _current = Mathf.Max(0, count);
}
