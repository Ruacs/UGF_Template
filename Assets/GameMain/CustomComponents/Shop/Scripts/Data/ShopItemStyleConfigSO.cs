using UnityEngine;

namespace Lokas
{
    [CreateAssetMenu(fileName = "ShopItemStyleConfig", menuName = "Shop/Item Style")]
    /// <summary>
    /// 鍟嗗簵鍟嗗搧鍗＄墖鏍峰紡閰嶇疆锛岃礋璐ｆ帶鍒跺悓涓€ Item prefab 鐨勮瑙夊樊寮傘€?    /// </summary>
    public class ShopItemStyleConfigSO : ScriptableObject
    {
        [Header("Sprites")]
        /// <summary>
        /// 鍗＄墖鑳屾櫙鍥撅紝搴旂敤鍒?Item/bg銆?        /// </summary>
        public Sprite background;

        /// <summary>
        /// 璐拱鎸夐挳鑳屾櫙鍥撅紝搴旂敤鍒?Item/btn_Buy銆?        /// </summary>
        public Sprite buyButtonBackground;

        /// <summary>
        /// 瑙掓爣鑳屾櫙鍥撅紝搴旂敤鍒?Item/corner_mark銆?        /// </summary>
        public Sprite cornerMarkBackground;

        [Header("Text Colors")]
        /// <summary>
        /// 鍟嗗搧鎻忚堪鏂囨湰棰滆壊锛屽簲鐢ㄥ埌 Item/tmp_Content銆?        /// </summary>
        public Color contentTextColor = Color.white;

        /// <summary>
        /// 浠锋牸鏂囨湰棰滆壊锛屽簲鐢ㄥ埌 Item/btn_Buy/tmp_price銆?        /// </summary>
        public Color priceTextColor = Color.white;

        /// <summary>
        /// 瑙掓爣鏂囨棰滆壊锛屽簲鐢ㄥ埌 Item/corner_mark/tmp_bonus銆?        /// </summary>
        public Color bonusTextColor = Color.white;

        public string contentTextStyleKey = string.Empty;

        [Header("Visibility")]
        /// <summary>
        /// 褰撳墠鏍峰紡鏄惁鍏佽鏄剧ず瑙掓爣銆傛渶缁堟槸鍚︽樉绀鸿繕浼氬彈鍟嗗搧 showBonus 鎺у埗銆?        /// </summary>
        public bool showCornerMark = true;

        /// <summary>
        /// 鏄惁鏄剧ず RewardBox 鑺傜偣銆?        /// </summary>
        public bool showRewardBox = true;
    }
}


