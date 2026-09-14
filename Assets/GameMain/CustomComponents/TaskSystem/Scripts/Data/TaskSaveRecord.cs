// # 存档数据结构
using System;
using System.Collections.Generic;

[Serializable]
public class TaskSaveRecord
{
    public string taskId;
    public int status;
    public int[] conditionProgress;
}

[Serializable]
public class TaskSaveWrapper
{
    public List<TaskSaveRecord> records = new List<TaskSaveRecord>();
}
