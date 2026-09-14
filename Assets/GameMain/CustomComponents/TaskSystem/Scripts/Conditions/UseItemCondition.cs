using System;
using UnityEngine;

// 使用道具条件
[Serializable]
[TaskCondition("使用道具")]
public class UseItemCondition : ITaskCondition
{
    public const string EventId = "on_use_item";

    public string itemId;          // 道具ID（为空则任意道具均可）
    public int requiredCount;
    private int _current;

    public bool IsCompleted => _current >= requiredCount;
    public float Progress => requiredCount <= 0 ? 1f : Mathf.Clamp01((float)_current / requiredCount);
    public int CurrentCount => _current;

    public void OnEventReceived(string eventId, object data)
    {
        if (eventId != EventId) return;

        switch (data)
        {
            // (itemId, count)
            case ValueTuple<string, int> tuple:
                if (string.IsNullOrEmpty(itemId) || tuple.Item1 == itemId)
                    _current += Mathf.Max(1, tuple.Item2);
                break;

            // 仅传入道具ID
            case string usedItemId:
                if (string.IsNullOrEmpty(itemId) || usedItemId == itemId)
                    _current++;
                break;
        }
    }

    public void Reset() => _current = 0;
    public void RestoreCount(int count) => _current = Mathf.Max(0, count);
}
