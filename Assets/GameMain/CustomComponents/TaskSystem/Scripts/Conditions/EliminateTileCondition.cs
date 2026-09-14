using System;
using UnityEngine;

// 消除麻将条件
[Serializable]
[TaskCondition("消除麻将")]
public class EliminateTileCondition : ITaskCondition
{
    public const string EventId = "on_eliminate_tile";

    public int tileId;             // 麻将牌类型（0 或负数则不限类型）
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
            // (tileId, count)
            case ValueTuple<int, int> tuple:
                if (tileId <= 0 || tuple.Item1 == tileId)
                    _current += Mathf.Max(1, tuple.Item2);
                break;

            // 仅传入数量（不限类型时）
            case int count:
                if (tileId <= 0)
                    _current += Mathf.Max(1, count);
                break;
        }
    }

    public void Reset() => _current = 0;
    public void RestoreCount(int count) => _current = Mathf.Max(0, count);
}
