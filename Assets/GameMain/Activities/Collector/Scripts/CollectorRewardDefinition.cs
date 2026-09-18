using UnityEngine;

namespace Lokas.Activities.Collector
{
    /// <summary>Collector 单个里程碑使用的奖励包。奖励条目仍复用公共 RewardEntry/RewardDefinitionSO 契约。</summary>
    [CreateAssetMenu(fileName = "CollectorReward", menuName = "GF/Activity/Collector/Reward Bundle")]
    public sealed class CollectorRewardDefinition : RewardDataSO { }
}
