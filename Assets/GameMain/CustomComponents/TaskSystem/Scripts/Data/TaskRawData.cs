
using System;
// 中间数据层 — 所有数据源统一转换为此结构
[Serializable]
public class TaskRawData
{
    public string taskId;
    public string title;
    public string description;
    public string taskType;           // "main" / "side" / "daily"
    public bool isRepeatable;
    public string[] prerequisiteIds;
    public ConditionRawData[] conditions;
    public RewardRawData[] rewards;
}

