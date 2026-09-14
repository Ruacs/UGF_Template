using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 道具补充配置。价格、购买数量和打开页面的含义由外部购买流程解释。
    /// </summary>
    [Serializable]
    public class PropButtonPurchaseConfig
    {
        [SerializeField] private PropButtonPurchaseMode m_Mode = PropButtonPurchaseMode.Ad;
        [SerializeField] private int m_CurrencyId;
        [SerializeField] private int m_Price;
        [SerializeField] private int m_PurchaseAmount = 1;
        [SerializeField] private string m_PurchasePageKey;

        public PropButtonPurchaseMode Mode => m_Mode;
        public int CurrencyId => m_CurrencyId;
        public int Price => m_Price;
        public int PurchaseAmount => m_PurchaseAmount;
        public string PurchasePageKey => m_PurchasePageKey;

        /// <summary>是否存在可展示的补充入口。</summary>
        public bool HasPurchaseOption => m_Mode != PropButtonPurchaseMode.None;
    }

    /// <summary>
    /// 道具按钮运行时配置，由 SO 或业务配置转换而来，不直接读写存档。
    /// </summary>
    [Serializable]
    public class PropButtonConfig
    {
        [SerializeField] private int m_ItemId;
        [SerializeField] private string m_DisplayName;
        [SerializeField] private Sprite m_Icon;
        [SerializeField] private Color m_BackgroundColor = Color.white;
        [SerializeField] private int m_RequiredLevel;
        [SerializeField] private int m_DefaultCount;
        [SerializeField] private bool m_IsSelectable;
        [SerializeField] private bool m_ShowPurchaseWhenEmpty = true;
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
        public PropButtonPurchaseMode PurchaseMode => m_Purchase != null ? m_Purchase.Mode : PropButtonPurchaseMode.None;

        /// <summary>
        /// Unity 序列化和外部手动填充用的默认构造。
        /// </summary>
        public PropButtonConfig()
        {
        }

        /// <summary>
        /// 运行时手动创建配置时使用。
        /// </summary>
        public PropButtonConfig(
            int itemId,
            string displayName,
            Sprite icon,
            Color backgroundColor,
            int requiredLevel = 0,
            int defaultCount = 0,
            bool isSelectable = false,
            bool showPurchaseWhenEmpty = true,
            PropButtonPurchaseConfig purchase = null)
        {
            m_ItemId = itemId;
            m_DisplayName = displayName;
            m_Icon = icon;
            m_BackgroundColor = backgroundColor;
            m_RequiredLevel = requiredLevel;
            m_DefaultCount = defaultCount;
            m_IsSelectable = isSelectable;
            m_ShowPurchaseWhenEmpty = showPurchaseWhenEmpty;
            m_Purchase = purchase;
        }
    }

}
