using System;
using GameFramework;
using GameFramework.Event;

[Serializable]
public sealed class TaskClaimedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(TaskClaimedEventArgs).GetHashCode();

    public string taskId;

    public override int Id => EventId;

    public static TaskClaimedEventArgs Create(string taskId)
    {
        TaskClaimedEventArgs args = ReferencePool.Acquire<TaskClaimedEventArgs>();
        args.taskId = taskId;
        return args;
    }

    public override void Clear()
    {
        taskId = null;
    }

}
