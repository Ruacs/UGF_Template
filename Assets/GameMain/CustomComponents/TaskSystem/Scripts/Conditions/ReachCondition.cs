using System;

// 到达区域条件
[Serializable]
[TaskCondition("到达区域")]
public class ReachCondition : ITaskCondition
{
    public const string EventId = "on_reach_area";

    public string areaId;
    private bool _reached;

    public bool IsCompleted => _reached;
    public float Progress => _reached ? 1f : 0f;
    public int CurrentCount => _reached ? 1 : 0;

    public void OnEventReceived(string eventId, object data)
    {
        if (eventId == EventId && data is string reachedAreaId && reachedAreaId == areaId)
            _reached = true;
    }

    public void Reset() => _reached = false;

    public void RestoreCount(int count)
    {
        _reached = count > 0;
    }
}
