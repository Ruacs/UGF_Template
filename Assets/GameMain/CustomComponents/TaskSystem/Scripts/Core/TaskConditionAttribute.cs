using System;

[AttributeUsage(AttributeTargets.Class)]
public class TaskConditionAttribute : Attribute
{
    public string DisplayName { get; }
    public TaskConditionAttribute(string displayName) => DisplayName = displayName;
}
