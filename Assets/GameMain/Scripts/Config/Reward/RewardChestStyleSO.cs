using UnityEngine;

namespace Lokas
{
    public enum RewardChestTier { Common, Rare, Epic, Legendary }

    /// <summary>宝箱品质及其外观。主题可以提供同品质的另一份样式资产。</summary>
    [CreateAssetMenu(fileName = "RewardChestStyle", menuName = "GF/Common/Reward/Chest Style")]
    public sealed class RewardChestStyleSO : DisplayConfigSO
    {
        [SerializeField] private RewardChestTier m_Tier;
        public RewardChestTier Tier => m_Tier;
#if UNITY_EDITOR
        public void Configure(RewardChestTier tier, string label, Sprite icon)
        {
            m_Tier = tier;
            displayName = label;
            sprite = icon;
        }
#endif
    }
}
