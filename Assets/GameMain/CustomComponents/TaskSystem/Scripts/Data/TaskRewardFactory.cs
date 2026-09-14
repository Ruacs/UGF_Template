///# type 字符串 → ITaskReward
using System;

public static class TaskRewardFactory
{
    public static ITaskReward Create(RewardRawData raw)
    {
        if (raw == null) throw new ArgumentNullException(nameof(raw));

        var type = raw.type?.Trim().ToLowerInvariant();
        return type switch
        {
            "gold" => new GoldReward { amount = Math.Max(0, raw.amount) },
            "exp" => new ExpReward { amount = Math.Max(0, raw.amount) },
            "item" => new ItemReward
            {
                itemId = raw.targetId,
                count = Math.Max(0, raw.amount)
            },
            "unlock" => new UnlockReward
            {
                featureId = raw.targetId
            },
            _ => throw new NotSupportedException($"Unsupported task reward type: {raw.type}")
        };
    }
}
