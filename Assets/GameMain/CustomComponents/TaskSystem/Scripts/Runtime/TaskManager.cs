using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Lokas;
using System.Linq;

public class TaskManager
{
    private readonly Dictionary<string, TaskInstance> _tasks = new();
    private ITaskDataProvider _provider;

    public async UniTask InitializeAsync(ITaskDataProvider provider)
    {
        _provider = provider;
        var rawList = await provider.LoadAllTasksAsync();
        LoadSaveData(out var savedProgress);

        foreach (var raw in rawList)
        {
            var conditions = raw.conditions?.Select(TaskConditionFactory.Create).ToList()
                             ?? new List<ITaskCondition>();
            var rewards    = raw.rewards?.Select(TaskRewardFactory.Create).ToList()
                             ?? new List<ITaskReward>();

            var instance = new TaskInstance(raw, conditions, rewards);

            // 恢复存档进度
            if (savedProgress.TryGetValue(raw.taskId, out var saved))
                instance.RestoreFromSave(saved);

            _tasks[raw.taskId] = instance;
        }

        RefreshLockState();
        CheckAndResetIfNewDay();
    }

    public void Tick(float dt)
    {
        // 轻量 tick：可用于超时任务、定时刷新日常任务等
        foreach (var t in _tasks.Values)
            t.Tick(dt);
    }

    public void BroadcastEvent(string eventId, object data)
    {
        foreach (var task in _tasks.Values)
            task.HandleEvent(eventId, data);

        RefreshLockState();
        SaveProgress();
    }

    public bool AcceptTask(string taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task)) return false;
        if (!task.Start()) return false;
        SaveProgress();
        return true;
    }

    /// <summary>
    /// 把当前所有处于 Available 状态的任务批量切到 InProgress。
    /// 初始化完成后调用，保证事件到来时任务已在 InProgress，不再依赖玩家是否打开过任务面板。
    /// </summary>
    public void AcceptAllAvailableTasks()
    {
        bool changed = false;
        foreach (var task in _tasks.Values)
        {
            if (task.Status == TaskStatus.Available && task.Start())
                changed = true;
        }
        if (changed) SaveProgress();
    }

    public bool ClaimTask(string taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task)) return false;
        if (!task.Claim()) return false;

        // 用 GF EventComponent 通知全局（UI刷新、成就系统等）
        GameEntry.Event.Fire(this, TaskClaimedEventArgs.Create(taskId));

        RefreshLockState();
        SaveProgress();
        return true;
    }

    /// <summary>重置所有任务到初始状态并清除存档</summary>
    public void ResetAllTasks()
    {
        foreach (var task in _tasks.Values)
            task.ResetToInitial();

        RefreshLockState();
        SaveProgress();
        SaveLastResetDate(DateTime.Today);
    }

    /// <summary>检测是否需要跨天重置，需要则自动执行</summary>
    public bool CheckAndResetIfNewDay()
    {
        var lastReset = LoadLastResetDate();
        if (lastReset >= DateTime.Today) return false;

        ResetAllTasks();
        return true;
    }

    private void SaveLastResetDate(DateTime date)
    {
        GameEntry.SaveData.SetData("task_last_reset", date.ToString("yyyy-MM-dd"));
    }

    private DateTime LoadLastResetDate()
    {
        var str = GameEntry.SaveData.GetData("task_last_reset", "");
        if (DateTime.TryParse(str, out var date)) return date.Date;
        return DateTime.MinValue;
    }

    public bool HasInProgressTaskWithCondition<T>() where T : ITaskCondition =>
        _tasks.Values.Any(t => t.Status == TaskStatus.InProgress && t.Conditions.OfType<T>().Any());

    public IReadOnlyDictionary<string, TaskInstance> AllTasks => _tasks;

    public TaskInstance GetTask(string taskId) =>
        _tasks.TryGetValue(taskId, out var t) ? t : null;

    public IEnumerable<TaskInstance> GetByStatus(TaskStatus status) =>
        _tasks.Values.Where(t => t.Status == status);

    // ---- 存档（接入 GF SaveComponent）----
    private void SaveProgress()
    {
        var save = GameEntry.SaveData;
        var data = new TaskSaveWrapper
        {
            records = _tasks.Values.Select(t => t.ToSaveRecord()).ToList()
        };
        save.SetData("task_progress", JsonUtility.ToJson(data));
    }

    private void LoadSaveData(out Dictionary<string, TaskSaveRecord> result)
    {
        result = new Dictionary<string, TaskSaveRecord>();
        var save = GameEntry.SaveData;

        var json = save.GetData("task_progress", "");
        if (string.IsNullOrEmpty(json)) return;

        var wrapper = JsonUtility.FromJson<TaskSaveWrapper>(json);
        foreach (var r in wrapper.records)
            result[r.taskId] = r;
    }

    private void RefreshLockState()
    {
        foreach (var task in _tasks.Values)
        {
            if (task.Status != TaskStatus.Locked) continue;
            if (IsPrerequisitesMet(task.RawData.prerequisiteIds))
                task.Unlock();
        }
    }

    private bool IsPrerequisitesMet(string[] ids)
    {
        if (ids == null || ids.Length == 0) return true;
        return ids.All(id =>
            _tasks.TryGetValue(id, out var t) && t.Status == TaskStatus.Claimed);
    }
}

