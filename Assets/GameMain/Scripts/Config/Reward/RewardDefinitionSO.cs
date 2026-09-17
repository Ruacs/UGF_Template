using System;
using UnityEngine;

namespace Lokas
{
    public enum RewardResourceKind { Currency, Item, Life }
    public enum RewardGrantMode { AddQuantity, UnlimitedUse }

    /// <summary>奖励资源的静态身份和外观。数量与限时免消耗引用同一资源。</summary>
    [CreateAssetMenu(fileName = "RewardDefinition", menuName = "GF/Common/Reward/Resource")]
    public sealed class RewardDefinitionSO : DisplayConfigSO
    {
        [SerializeField] private string m_Scope = "common";
        [SerializeField] private string m_ResourceKey;
        [SerializeField] private RewardResourceKind m_Kind = RewardResourceKind.Item;
        [SerializeField] private bool m_AllowUnlimitedUse;
        [Tooltip("限时权益的显式发奖键；实际接收器接入前只支持预览。")]
        [SerializeField] private string m_UnlimitedUseKey;
        [SerializeField] private Sprite m_UnlimitedIcon;

        public string Scope => m_Scope;
        public string ResourceKey => m_ResourceKey;
        public RewardResourceKind Kind => m_Kind;
        public bool AllowUnlimitedUse => m_AllowUnlimitedUse;
        public Sprite GetIcon(RewardGrantMode mode) => mode == RewardGrantMode.UnlimitedUse && m_UnlimitedIcon != null ? m_UnlimitedIcon : sprite;
        public string GetGrantKey(RewardGrantMode mode) => mode == RewardGrantMode.UnlimitedUse ? m_UnlimitedUseKey : m_ResourceKey;

        public void Validate(RewardGrantMode mode)
        {
            if (string.IsNullOrWhiteSpace(m_Scope) || string.IsNullOrWhiteSpace(m_ResourceKey))
                throw new InvalidOperationException($"Reward resource '{name}' needs an explicit scope and key.");
            if (!Enum.IsDefined(typeof(RewardGrantMode), mode))
                throw new InvalidOperationException($"Unknown reward grant mode: {mode}.");
            if (mode == RewardGrantMode.UnlimitedUse && (!m_AllowUnlimitedUse || string.IsNullOrWhiteSpace(m_UnlimitedUseKey)))
                throw new InvalidOperationException($"Reward resource '{name}' does not define unlimited use.");
        }

#if UNITY_EDITOR
        public void Configure(string scope, string resourceKey, string label, Sprite icon, RewardResourceKind kind,
            string unlimitedUseKey = null, Sprite unlimitedIcon = null)
        {
            m_Scope = scope;
            m_ResourceKey = resourceKey;
            displayName = label;
            sprite = icon;
            m_Kind = kind;
            m_AllowUnlimitedUse = !string.IsNullOrWhiteSpace(unlimitedUseKey);
            m_UnlimitedUseKey = unlimitedUseKey;
            m_UnlimitedIcon = unlimitedIcon;
        }
#endif
    }
}
