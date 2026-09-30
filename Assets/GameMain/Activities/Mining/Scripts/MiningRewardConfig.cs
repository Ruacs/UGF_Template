using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    /// <summary>RewardId 到独立奖励包的映射，供 Event01 与 Event02 共同引用。</summary>
    [CreateAssetMenu(fileName = "MiningRewardConfig", menuName = "GF/Activity/Mining/Reward Config")]
    public sealed class MiningRewardConfig : ScriptableObject
    {
        public const int FirstRewardId = 9001;
        public const int RewardCount = 5;

        [SerializeField] private MiningRewardDefinition[] m_Rewards = Array.Empty<MiningRewardDefinition>();

        public IReadOnlyList<MiningRewardDefinition> Rewards => m_Rewards;

        public bool TryGetReward(int rewardId, out MiningRewardDefinition reward)
        {
            if (m_Rewards != null)
            {
                for (int index = 0; index < m_Rewards.Length; index++)
                {
                    MiningRewardDefinition candidate = m_Rewards[index];
                    if (candidate != null && candidate.id == rewardId)
                    {
                        reward = candidate;
                        return true;
                    }
                }
            }

            reward = null;
            return false;
        }

        public MiningRewardDefinition GetReward(int rewardId)
        {
            if (TryGetReward(rewardId, out MiningRewardDefinition reward)) return reward;
            throw new InvalidOperationException($"Mining RewardId={rewardId} is not configured.");
        }

        public void ValidateConfiguration()
        {
            if (m_Rewards == null || m_Rewards.Length != RewardCount)
                throw new InvalidOperationException($"Mining needs {RewardCount} independent reward bundles.");

            var rewardIds = new HashSet<int>();
            for (int index = 0; index < m_Rewards.Length; index++)
            {
                MiningRewardDefinition reward = m_Rewards[index];
                if (reward == null)
                    throw new InvalidOperationException($"Mining reward bundle {index} is null.");
                if (!rewardIds.Add(reward.id))
                    throw new InvalidOperationException($"Mining RewardId={reward.id} is duplicated.");

                reward.ValidateEntries();
            }

            for (int rewardId = FirstRewardId; rewardId < FirstRewardId + RewardCount; rewardId++)
            {
                if (!rewardIds.Contains(rewardId))
                    throw new InvalidOperationException($"Mining RewardId={rewardId} is missing.");
            }
        }
    }
}
