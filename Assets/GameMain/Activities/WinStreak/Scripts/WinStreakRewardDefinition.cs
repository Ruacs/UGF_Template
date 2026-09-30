using UnityEngine;

namespace Lokas.Activities.WinStreak
{
    /// <summary>Win Streak 的奖励包。资源身份和发放模式沿用公共 RewardDataSO 契约。</summary>
    [CreateAssetMenu(fileName = "WinStreakRewardDefinition", menuName = "GF/Activity/Win Streak/Reward Bundle")]
    public sealed class WinStreakRewardDefinition : RewardDataSO
    {
    }
}
