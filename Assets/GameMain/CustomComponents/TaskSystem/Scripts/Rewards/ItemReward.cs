using UnityGameFramework.Runtime;

[TaskReward("物品奖励")]
public class ItemReward : ITaskReward
{
    public string itemId;
    public int count;
    public void Grant()
    {
        if (count <= 0 || string.IsNullOrWhiteSpace(itemId)) return;
        Log.Info($"Granting item reward: {itemId} x{count}");
        // 这里直接调用背包系统的接口，假设是 InventorySystem.AddItem(string itemId, int count)
    }
}
