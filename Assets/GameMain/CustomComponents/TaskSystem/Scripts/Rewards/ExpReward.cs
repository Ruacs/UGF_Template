using System;
using UnityGameFramework.Runtime;

[TaskReward("经验奖励")]
public class ExpReward : ITaskReward
{
    public static Action<int> OnGrantExp;
    public int amount;

    public void Grant()
    {
        if (amount <= 0) return;

        OnGrantExp?.Invoke(amount);
        Log.Info($"Granting exp reward: {amount}");
    }
}
