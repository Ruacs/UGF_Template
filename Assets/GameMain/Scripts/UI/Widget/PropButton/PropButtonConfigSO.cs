using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 每种道具一份的按钮配置资产。只保存静态配置，不保存玩家拥有数量。
    /// </summary>
    [CreateAssetMenu(fileName = "PropButtonConfig", menuName = "GF/Common/UI/Prop Button Config")]
    public class PropButtonConfigSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private int m_ItemId;
        [SerializeField] private string m_DisplayName;

        [Header("Visual")]
        [SerializeField] private Sprite m_Icon;
        [SerializeField] private Color m_BackgroundColor = Color.white;

        [Header("Unlock")]
        [SerializeField] private int m_RequiredLevel;

        [Header("Default State")]
        [SerializeField] private int m_DefaultCount;
        [SerializeField] private bool m_IsSelectable;
        [SerializeField] private bool m_ShowPurchaseWhenEmpty = true;

        [Header("Purchase")]
        [SerializeField] private PropButtonPurchaseConfig m_Purchase = new PropButtonPurchaseConfig();

        public int ItemId => m_ItemId;
        public string DisplayName => m_DisplayName;
        public Sprite Icon => m_Icon;
        public Color BackgroundColor => m_BackgroundColor;
        public int RequiredLevel => m_RequiredLevel;
        public int DefaultCount => m_DefaultCount;
        public bool IsSelectable => m_IsSelectable;
        public bool ShowPurchaseWhenEmpty => m_ShowPurchaseWhenEmpty;
        public PropButtonPurchaseConfig Purchase => m_Purchase;

        /// <summary>
        /// 生成运行时配置，避免 UI 层直接持有和修改 SO。
        /// </summary>
        public PropButtonConfig CreateRuntimeConfig()
        {
            return new PropButtonConfig(
                m_ItemId,
                m_DisplayName,
                m_Icon,
                m_BackgroundColor,
                m_RequiredLevel,
                m_DefaultCount,
                m_IsSelectable,
                m_ShowPurchaseWhenEmpty,
                m_Purchase);
        }
    }
}
