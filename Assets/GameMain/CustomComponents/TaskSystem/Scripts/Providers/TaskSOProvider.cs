// ScriptableObject 数据源
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Tasks/SO Provider")]
public class TaskSOProvider : ScriptableObject, ITaskDataProvider
{
    [SerializeField] private TaskDataSO[] taskAssets;

    public UniTask<IReadOnlyList<TaskRawData>> LoadAllTasksAsync()
    {
        var result = taskAssets
            .Where(so => so != null)
            .Select(so => so.ToRawData())
            .ToList();
        return UniTask.FromResult<IReadOnlyList<TaskRawData>>(result);
    }
}
