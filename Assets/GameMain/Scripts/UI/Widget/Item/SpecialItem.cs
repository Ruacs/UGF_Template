
using UnityEngine;
using UnityEngine.UI;
using Log = UnityGameFramework.Runtime.Log;

namespace Lokas
{
    /// <summary>
    /// UI for special items that can be collected during gameplay, such as props or collectibles.
    /// </summary>
    public class SpecialItem : IconCountItemUI
    {
        [SerializeField] private Image m_bgImg;
        [SerializeField] private Image m_iconImg;
        [SerializeField] private Text m_countText;

        private int m_itemId;
        private int m_count;

        public void Init(int itemId, Sprite icon, int count)
        {
            m_itemId = itemId;
            m_iconImg.sprite = icon;
            SetCount(count);
        }

        public void SetCount(int count)
        {
            m_count = count;
            m_countText.text = count.ToString();
        }
    }
}