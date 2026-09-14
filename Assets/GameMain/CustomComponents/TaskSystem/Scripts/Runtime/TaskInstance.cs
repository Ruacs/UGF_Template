using System;
using System.Collections.Generic;
using System.Linq;

public class TaskInstance
{
    public TaskRawData RawData { get; }
    public TaskStatus Status { get; private set; }
    public IReadOnlyList<ITaskCondition> Conditions { get; }
    public IReadOnlyList<ITaskReward> Rewards { get; }

    public float TotalProgress => Conditions.Count == 0 ? 1f : Conditions.Average(c => c.Progress);
    public bool IsAllMet => Conditions.All(c => c.IsCompleted);

    public TaskInstance(TaskRawData raw, List<ITaskCondition> conditions, List<ITaskReward> rewards)
    {
        RawData = raw ?? throw new ArgumentNullException(nameof(raw));
        Conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
        Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        Status = (raw.prerequisiteIds?.Length > 0) ? TaskStatus.Locked : TaskStatus.Available;
    }

    public void Unlock()
    {
        if (Status == TaskStatus.Locked)
            Status = TaskStatus.Available;
    }

    public bool Start()
    {
        if (Status != TaskStatus.Available) return false;

        Status = TaskStatus.InProgress;
        if (TotalProgress <= 0f)
        {
            foreach (var c in Conditions)
                c.Reset();
        }

        if (IsAllMet)
            Status = TaskStatus.Completed;

        return true;
    }

    public void HandleEvent(string eventId, object data)
    {
        if (Status != TaskStatus.InProgress) return;

        foreach (var c in Conditions)
            c.OnEventReceived(eventId, data);

        if (IsAllMet)
            Status = TaskStatus.Completed;
    }

    public void Tick(float dt)
    {
    }

    public bool Claim()
    {
        if (Status != TaskStatus.Completed) return false;

        foreach (var r in Rewards)
            r.Grant();

        Status = RawData.isRepeatable ? TaskStatus.Available : TaskStatus.Claimed;
        if (RawData.isRepeatable)
        {
            foreach (var c in Conditions)
                c.Reset();
        }

        return true;
    }

    /// <summary>重置到初始状态（用于每日任务刷新）</summary>
    public void ResetToInitial()
    {
        foreach (var c in Conditions)
            c.Reset();
        Status = (RawData.prerequisiteIds?.Length > 0) ? TaskStatus.Locked : TaskStatus.Available;
    }

    public TaskSaveRecord ToSaveRecord() => new TaskSaveRecord
    {
        taskId = RawData.taskId,
        status = (int)Status,
        conditionProgress = Conditions.Select(c => c.CurrentCount).ToArray()
    };

    public void RestoreFromSave(TaskSaveRecord record)
    {
        if (record == null) return;

        Status = (TaskStatus)record.status;
        if (record.conditionProgress == null) return;

        for (int i = 0; i < Conditions.Count && i < record.conditionProgress.Length; i++)
            Conditions[i].RestoreCount(record.conditionProgress[i]);
    }

    public bool IsPrerequisiteSatisfied(IReadOnlyDictionary<string, TaskInstance> allTasks)
    {
        if (RawData.prerequisiteIds == null || RawData.prerequisiteIds.Length == 0) return true;

        for (int i = 0; i < RawData.prerequisiteIds.Length; i++)
        {
            var prerequisiteId = RawData.prerequisiteIds[i];
            if (!allTasks.TryGetValue(prerequisiteId, out var prerequisiteTask)) return false;
            if (prerequisiteTask.Status != TaskStatus.Completed && prerequisiteTask.Status != TaskStatus.Claimed) return false;
        }

        return true;
    }
}
