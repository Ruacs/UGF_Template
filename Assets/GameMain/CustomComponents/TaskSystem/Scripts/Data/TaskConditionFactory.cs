///# type 字符串 → ITaskCondition
/// 
/// 条件工厂，根据 type 字符串创建对应的 ITaskCondition 实例
using System;
using System.Globalization;

public static class TaskConditionFactory
{
    public static ITaskCondition Create(ConditionRawData raw)
    {
        if (raw == null) throw new ArgumentNullException(nameof(raw));

        var type = raw.type?.Trim().ToLowerInvariant();
        return type switch
        {
            "kill" => new KillCondition
            {
                targetId = raw.targetId,
                requiredCount = Math.Max(1, raw.requiredCount)
            },
            "collect" => new CollectCondition
            {
                itemId = raw.targetId,
                requiredCount = Math.Max(1, raw.requiredCount)
            },
            "reach" => new ReachCondition
            {
                areaId = raw.targetId
            },
            "eliminate_tile" => new EliminateTileCondition
            {
                tileId = int.Parse(raw.targetId,CultureInfo.InvariantCulture),
                requiredCount = Math.Max(1, raw.requiredCount)
            },
            "pass_level" => new PassLevelCondition
            {
                levelId = raw.targetId,
                requiredCount = Math.Max(1, raw.requiredCount)
            },
            "use_item" => new UseItemCondition
            {
                itemId = raw.targetId,
                requiredCount = Math.Max(1, raw.requiredCount)
            },
            _ => throw new NotSupportedException($"Unsupported task condition type: {raw.type}")
        };
    }
}
