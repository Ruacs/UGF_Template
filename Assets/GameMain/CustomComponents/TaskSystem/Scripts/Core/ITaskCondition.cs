

public interface ITaskCondition
{
    bool IsCompleted { get; }
    float Progress { get; }
    int CurrentCount { get; }          // 供存档读取
    void RestoreCount(int count);      // 存档恢复
    void OnEventReceived(string eventId, object data);
    void Reset();
}

