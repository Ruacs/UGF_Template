using System;

[AttributeUsage(AttributeTargets.Class)]
public class TaskRewardAttribute : Attribute
{
    public string DisplayName { get; }
    public TaskRewardAttribute(string displayName) => DisplayName = displayName;
}