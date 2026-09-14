using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// 接口 — 所有数据源都实现它
public interface ITaskDataProvider
{
    // 异步加载，兼容本地和远程
    UniTask<IReadOnlyList<TaskRawData>> LoadAllTasksAsync();
}