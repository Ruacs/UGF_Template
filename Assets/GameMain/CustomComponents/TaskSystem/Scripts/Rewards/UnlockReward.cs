using UnityGameFramework.Runtime;

[TaskReward("功能解锁奖励")]
public class UnlockReward : ITaskReward
{
    public string featureId;

    public void Grant()
    {
        // Placeholder: integrate with real feature unlock system when available.
        Log.Info($"Granting unlock reward: {featureId}");
    }
}
