// 挂载到 GameFramework 的 GameEntry 对象
using System;
using System.Collections.Generic;
using System.Linq;
using Ads;
using GameFramework.Event;
using Lokas;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameEntry = Lokas.GameEntry;

public class TaskComponent : GameFrameworkComponent
{
    // 在 Inspector 里选择数据源模式
    public enum DataSourceMode { ScriptableObject, Json, Remote, Custom }

    [SerializeField] private DataSourceMode _dataSourceMode = DataSourceMode.ScriptableObject;
    [SerializeField] private TaskSOProvider _soProvider;        // SO 模式
    [SerializeField] private string _jsonRelativePath = "Tasks/task_config.json";
    [SerializeField] private string _remoteApiUrl;

    private TaskManager _taskManager;

    /// <summary>任务系统是否已解锁（玩家当前关卡 &gt;= <see cref="AdsManager.TaskUnlockLevel"/>）。</summary>
    public bool IsUnlocked => GameEntry.SaveData != null;

    // ---- Inspector 运行时调试视图 ----
    [Serializable]
    public class TaskDebugEntry
    {
        public string taskId;
        public string title;
        public string type;
        public TaskStatus status;
        [Range(0f, 1f)] public float progress;
        public string[] conditionDetails;
    }
 

    [Header("Runtime Debug View")]
    [SerializeField] private List<TaskDebugEntry> _debugTasks = new();

    // GF 组件初始化入口
    protected override void Awake()
    {
        base.Awake();
        _taskManager = new TaskManager();
    }

    // GF Start 流程（由 GameEntry 调度）
    private async void Start()
    {
        var provider = BuildProvider();
        await _taskManager.InitializeAsync(provider);
        // 初始化后立即接受所有 Available 任务，避免玩家未打开任务面板时
        // 弹窗因 hasActive=false 而不显示（Android 上尤其明显）
        _taskManager.AcceptAllAvailableTasks();
        SubscribeGFEvents();
        Log.Info("[TaskComponent] Initialized.");
    }

    private ITaskDataProvider BuildProvider() => _dataSourceMode switch
    {
        DataSourceMode.ScriptableObject => _soProvider,
        DataSourceMode.Json => new TaskJsonProvider(_jsonRelativePath),
        DataSourceMode.Remote => new TaskRemoteProvider(_remoteApiUrl),
        _ => _soProvider
    };

    // 注册 GF 游戏事件（替代自制 EventBus）
    private void SubscribeGFEvents()
    {
        var evt = GameEntry.Event;
        evt.Subscribe(TileEliminatedEventArgs.EventId, OnTileEliminated);
        evt.Subscribe(LevelPassedEventArgs.EventId, OnLevelPassed);
        evt.Subscribe(ItemUsedEventArgs.EventId, OnItemUsed);
    }

    // GF Update（每帧驱动任务轮询，轻量检查）
    private void Update()
    {
        _taskManager.Tick(Time.deltaTime);
#if UNITY_EDITOR

        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 测试：消除麻将
            // GameEntry.Event.Fire(this, TileEliminatedEventArgs.Create(TaskUIPanel.CurMatchTileId, 1));
            Log.Info("[TaskComponent] 触发消除麻将事件");
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
        {
            // 测试：通过关卡
            GameEntry.Event.Fire(this, LevelPassedEventArgs.Create(""));
            Log.Info("[TaskComponent] 触发通过关卡事件");
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
        {
            // 测试：使用道具
            GameEntry.Event.Fire(this, ItemUsedEventArgs.Create("", 1));
            Log.Info("[TaskComponent] 触发使用道具事件");
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.R))
        {
            // 测试：模拟次日刷新，重置所有任务
            ResetAllTasks();
            Log.Info("[TaskComponent] 模拟次日刷新，所有任务已重置");
        }

        RefreshDebugView();
#endif
    }

#if UNITY_EDITOR
    private void RefreshDebugView()
    {
        var allTasks = _taskManager.AllTasks;
        if (allTasks == null) return;

        _debugTasks.Clear();
        foreach (var kvp in allTasks)
        {
            var inst = kvp.Value;
            var entry = new TaskDebugEntry
            {
                taskId = inst.RawData.taskId,
                title = inst.RawData.title,
                type = inst.RawData.taskType,
                status = inst.Status,
                progress = inst.TotalProgress,
                conditionDetails = inst.Conditions
                    .Select(c => $"{c.CurrentCount}/{(c.IsCompleted ? "done" : $"{c.Progress:P0}")}")
                    .ToArray()
            };
            _debugTasks.Add(entry);
        }
    }
#endif

    // ---- 事件处理 ----

    private void OnTileEliminated(object sender, GameEventArgs e)
    {
        if (!IsUnlocked) return;

        var args = (TileEliminatedEventArgs)e;

        // if (args.tileId != TaskUIPanel.CurMatchTileId)
        //     return;

        // 事件到来时兜底接受 Available 任务（如每日重置后的首次事件）
        _taskManager.AcceptAllAvailableTasks();
        // 只在任务 InProgress 时才显示弹窗；已 Completed 未领取时不重复弹
        bool hasActive = _taskManager.HasInProgressTaskWithCondition<EliminateTileCondition>();
        _taskManager.BroadcastEvent(EliminateTileCondition.EventId, (args.tileId, args.count));
        ShowTaskPopup(0, hasActive);
    }

    private void OnLevelPassed(object sender, GameEventArgs e)
    {
        if (!IsUnlocked) return;

        var args = (LevelPassedEventArgs)e;

        _taskManager.AcceptAllAvailableTasks();
        bool hasActive = _taskManager.HasInProgressTaskWithCondition<PassLevelCondition>();
        _taskManager.BroadcastEvent(PassLevelCondition.EventId, args.levelId);
        ShowTaskPopup(1, hasActive);
    }

    private void OnItemUsed(object sender, GameEventArgs e)
    {
        if (!IsUnlocked) return;

        var args = (ItemUsedEventArgs)e;
        _taskManager.AcceptAllAvailableTasks();
        bool hasActive = _taskManager.HasInProgressTaskWithCondition<UseItemCondition>();
        _taskManager.BroadcastEvent(UseItemCondition.EventId, (args.itemId, args.count));

        ShowTaskPopup(2, hasActive);
    }

    /// <summary>
    /// 显示任务进度弹窗
    /// </summary>
    /// <param name="groupIndex">任务组索引：0=消除牌, 1=通关, 2=道具</param>
    /// <param name="showPopup">是否显示弹窗</param>
    public void ShowTaskPopup(int groupIndex, bool showPopup = true)
    {
        // if (_taskPopup != null)
        //     _taskPopup.Show(groupIndex, showPopup);
    }

    // ---- 对外 API（UI层调用）----
    public bool AcceptTask(string taskId) => _taskManager.AcceptTask(taskId);
    public bool ClaimTask(string taskId) => _taskManager.ClaimTask(taskId);
    public TaskInstance GetTask(string taskId) => _taskManager.GetTask(taskId);
    public IEnumerable<TaskInstance> GetByStatus(TaskStatus s) => _taskManager.GetByStatus(s);

    /// <summary>重置所有每日任务（模拟次日刷新）</summary>
    public void ResetAllTasks()
    {
        _taskManager.ResetAllTasks();
        Log.Info("[TaskComponent] 所有任务已重置");
    }

    private void OnDestroy()
    {
        var evt = GameEntry.Event;
        if (evt == null) return;
        if (evt.Check(TileEliminatedEventArgs.EventId, OnTileEliminated))
            evt.Unsubscribe(TileEliminatedEventArgs.EventId, OnTileEliminated);
        if (evt.Check(LevelPassedEventArgs.EventId, OnLevelPassed))
            evt.Unsubscribe(LevelPassedEventArgs.EventId, OnLevelPassed);
        if (evt.Check(ItemUsedEventArgs.EventId, OnItemUsed))
            evt.Unsubscribe(ItemUsedEventArgs.EventId, OnItemUsed);
    }
}