using Lokas;

[TaskReward("金币奖励")]
public class GoldReward : ITaskReward
{
    public int amount;
    public void Grant() => GameEntry.SaveData.Money += amount;
}