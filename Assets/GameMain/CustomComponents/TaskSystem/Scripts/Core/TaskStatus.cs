public enum TaskStatus
{
    Locked,      // 未解锁（前置任务未完成）
    Available,   // 可接取
    InProgress,  // 进行中
    Completed,   // 已完成待领取
    Claimed      // 已领取奖励
}
