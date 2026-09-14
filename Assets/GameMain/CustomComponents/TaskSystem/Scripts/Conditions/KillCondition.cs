using System;
using UnityEngine;
// 击杀条件
[Serializable]
[TaskCondition("击杀敌人")]
public class KillCondition : ITaskCondition
{
    public const string EventId = "on_kill";

    public string targetId;       // 目标类型（"slime", "boss_01" 等）
    public int requiredCount;
    private int _current;

    public bool IsCompleted => _current >= requiredCount;
    public float Progress => requiredCount <= 0 ? 1f : Mathf.Clamp01((float)_current / requiredCount);
    public int CurrentCount => _current;

    public void OnEventReceived(string eventId, object data)
    {
        if (eventId != EventId) return;

        if (data is string enemyId && enemyId == targetId)
        {
            _current++;
            return;
        }

        if (data is EnemyDeadEventArgs args && args.enemyId == targetId)
            _current += Mathf.Max(1, args.count);
    }

    public void Reset() => _current = 0;
    public void RestoreCount(int count) => _current = Mathf.Max(0, count);
}
