using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Tasks/TaskDataSO")]
public class TaskDataSO : ScriptableObject
{
    [Tooltip("任务唯一标识符")]
    public string taskId;

    [Tooltip("任务标题，显示在UI上")]
    public string title;

    [TextArea]
    [Tooltip("任务描述文本")]
    public string description;

    [Tooltip("任务类型：主线 / 支线 / 每日")]
    public TaskType taskType;

    [Tooltip("是否可重复完成（如每日任务）")]
    public bool isRepeatable;

    [Tooltip("前置任务ID列表，全部完成后本任务才可解锁")]
    public string[] prerequisiteIds;

    [SerializeReference]
    [Tooltip("任务完成条件列表（需全部满足）")]
    public List<ITaskCondition> conditions = new();

    [SerializeReference]
    [Tooltip("任务奖励列表（领取时全部发放）")]
    public List<ITaskReward> rewards = new();

    public TaskRawData ToRawData()
    {
        var rawConditions = (conditions ?? new List<ITaskCondition>())
            .Where(condition => condition != null)
            .Select(ToConditionRawData)
            .ToArray();

        var rawRewards = (rewards ?? new List<ITaskReward>())
            .Where(reward => reward != null)
            .Select(ToRewardRawData)
            .ToArray();

        return new TaskRawData
        {
            taskId = taskId,
            title = title,
            description = description,
            taskType = taskType.ToString().ToLowerInvariant(),
            isRepeatable = isRepeatable,
            prerequisiteIds = prerequisiteIds,
            conditions = rawConditions,
            rewards = rawRewards
        };
    }

    private static ConditionRawData ToConditionRawData(ITaskCondition condition)
    {
        if (condition is KillCondition kill)
        {
            return new ConditionRawData
            {
                type = "kill",
                targetId = kill.targetId,
                requiredCount = kill.requiredCount
            };
        }

        if (condition is CollectCondition collect)
        {
            return new ConditionRawData
            {
                type = "collect",
                targetId = collect.itemId,
                requiredCount = collect.requiredCount
            };
        }

        if (condition is ReachCondition reach)
        {
            return new ConditionRawData
            {
                type = "reach",
                targetId = reach.areaId,
                requiredCount = 1
            };
        }

        if (condition is EliminateTileCondition eliminate)
        {
            return new ConditionRawData
            {
                type = "eliminate_tile",
                targetId = eliminate.tileId.ToString(),
                requiredCount = eliminate.requiredCount
            };
        }

        if (condition is PassLevelCondition passLevel)
        {
            return new ConditionRawData
            {
                type = "pass_level",
                targetId = passLevel.levelId,
                requiredCount = passLevel.requiredCount
            };
        }

        if (condition is UseItemCondition useItem)
        {
            return new ConditionRawData
            {
                type = "use_item",
                targetId = useItem.itemId,
                requiredCount = useItem.requiredCount
            };
        }

        throw new NotSupportedException($"Unsupported condition type in TaskDataSO: {condition.GetType().Name}");
    }

    private static RewardRawData ToRewardRawData(ITaskReward reward)
    {
        if (reward is GoldReward gold)
        {
            return new RewardRawData
            {
                type = "gold",
                amount = gold.amount
            };
        }

        if (reward is ExpReward exp)
        {
            return new RewardRawData
            {
                type = "exp",
                amount = exp.amount
            };
        }

        if (reward is ItemReward item)
        {
            return new RewardRawData
            {
                type = "item",
                targetId = item.itemId,
                amount = item.count
            };
        }

        if (reward is UnlockReward unlock)
        {
            return new RewardRawData
            {
                type = "unlock",
                targetId = unlock.featureId,
                amount = 1
            };
        }

        throw new NotSupportedException($"Unsupported reward type in TaskDataSO: {reward.GetType().Name}");
    }
}
