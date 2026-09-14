using System;
using UnityEngine;
using UnityEngine.Events;

namespace Lokas
{
    /// <summary>
    /// ShopItemView 浜嬩欢瑙勫垯鐨勫尮閰嶆潯浠躲€?    /// 鐢ㄤ簬鎶婁笉鍚屽晢鍝佺殑鎻愮ず銆佺壒鏁堛€佽鏍囩瓑琛ㄧ幇宸紓浜ょ粰棰勫埗浣撲笂鐨?UnityEvent 閰嶇疆銆?    /// </summary>
    public enum ShopItemViewEventMatchMode
    {
        /// <summary>濮嬬粓鍛戒腑銆傞€傚悎鍋氭瘡娆″埛鏂伴兘闇€瑕佹墽琛岀殑榛樿 UI 閲嶇疆銆?/summary>
        Always = 0,

        /// <summary>鏍规嵁 ShopItemConfigSO.viewTags 鏄惁鍖呭惈鎸囧畾鏍囩鏉ュ尮閰嶃€?/summary>
        ViewTag = 1,

        /// <summary>鏍规嵁鍟嗗搧浠锋牸绫诲瀷鍖归厤锛屼緥濡?Gold銆丄D銆両AP銆?/summary>
        PriceType = 2,

        /// <summary>鏍规嵁鍟嗗搧濂栧姳绫诲瀷鍖归厤锛屼緥濡?NoAds銆丆ommonProp銆?/summary>
        RewardTarget = 3,

        /// <summary>鏍规嵁鍟嗗搧浣跨敤鐨?ShopItemStyleConfigSO 鍖归厤銆?/summary>
        Style = 4,

        /// <summary>鏍规嵁鍟嗗搧鐨勫唴璐?ProductId 鍖归厤銆?/summary>
        ProductId = 5,

        /// <summary>鏍规嵁鍟嗗搧鏄惁鏄剧ず bonus 瑙掓爣鍖归厤銆?/summary>
        ShowBonus = 6,

        /// <summary>鏍规嵁鍟嗗搧 visible 寮€鍏冲尮閰嶃€?/summary>
        Visible = 7,
    }

    /// <summary>
    /// 鍟嗗搧 Item 鐨勯厤缃┍鍔ㄤ簨浠惰鍒欍€?    /// 閰嶇疆鍦?ShopItemView 棰勫埗浣撲笂锛屾瘡娆?SetData/RefreshView 鏃堕兘浼氶噸鏂板垽鏂苟瑙﹀彂銆?    /// 浣跨敤鏂瑰紡锛?    /// 1. 鍦ㄥ晢鍝侀厤缃?ShopItemConfigSO.viewTags 涓坊鍔犳爣绛撅紝渚嬪 "hot"銆?    /// 2. 鍦?ShopItemView 鐨?View Events 涓坊鍔犺鍒欙紝Match Mode 閫夋嫨 ViewTag锛孷iew Tag 濉?"hot"銆?    /// 3. On Matched 鎷栧叆瑕佹樉绀虹殑鐗规晥/鎻愮ず瀵硅薄骞惰皟鐢?GameObject.SetActive(true)銆?    /// 4. On Unmatched 鎷栧叆鍚屼竴涓璞″苟璋冪敤 GameObject.SetActive(false)锛岄伩鍏嶅鐢?Item 鏃舵畫鐣欑姸鎬併€?    /// </summary>
    [Serializable]
    public class ShopItemViewEventRule
    {
        /// <summary>瑙勫垯鍚嶇О锛屼粎鐢ㄤ簬 Inspector 灞曠ず锛屽缓璁啓娓呮鐢ㄩ€旓紝渚嬪 Show Hot Effect銆?/summary>
        public string name;

        /// <summary>褰撳墠瑙勫垯浣跨敤鍝鏉′欢杩涜鍖归厤銆?/summary>
        public ShopItemViewEventMatchMode matchMode = ShopItemViewEventMatchMode.ViewTag;

        /// <summary>matchMode 涓?ViewTag 鏃朵娇鐢紝瀵瑰簲 ShopItemConfigSO.viewTags銆?/summary>
        public string viewTag;

        /// <summary>matchMode 涓?PriceType 鏃朵娇鐢ㄣ€?/summary>
        public ShopPriceType priceType;

        /// <summary>matchMode 涓?RewardTarget 鏃朵娇鐢ㄣ€?/summary>
        public ShopRewardTarget rewardTarget;

        /// <summary>matchMode 涓?Style 鏃朵娇鐢ㄣ€?/summary>
        public ShopItemStyleConfigSO style;

        /// <summary>matchMode 涓?ProductId 鏃朵娇鐢紝澶у皬鍐欎笉鏁忔劅銆?/summary>
        public string productId;

        /// <summary>matchMode 涓?ShowBonus 鎴?Visible 鏃朵娇鐢紝鐢ㄤ簬鎸囧畾鏈熸湜鐨?bool 鍊笺€?/summary>
        public bool expectedBool = true;

        /// <summary>瑙勫垯鍛戒腑鏃惰Е鍙戙€傚父鐢ㄤ簬鏄剧ず鑺傜偣銆佹挱鏀剧壒鏁堛€佸垏鎹㈠姩鐢荤姸鎬併€?/summary>
        public UnityEvent onMatched = new();

        /// <summary>瑙勫垯鏈懡涓椂瑙﹀彂銆傚父鐢ㄤ簬闅愯棌鑺傜偣锛岄槻姝㈡粴鍔ㄥ垪琛ㄥ鐢ㄦ椂鐘舵€佹畫鐣欍€?/summary>
        public UnityEvent onUnmatched = new();

        /// <summary>
        /// 鍒ゆ柇褰撳墠鍟嗗搧閰嶇疆鏄惁鍛戒腑瑙勫垯銆?        /// 杩欓噷淇濇寔绾暟鎹垽鏂紝鐪熸鐨?UI 琛屼负浜ょ粰 UnityEvent 鍦ㄩ鍒朵綋涓婇厤缃€?        /// </summary>
        public bool IsMatch(ShopItemConfigSO config)
        {
            if (config == null)
                return false;

            switch (matchMode)
            {
                case ShopItemViewEventMatchMode.Always:
                    return true;
                case ShopItemViewEventMatchMode.ViewTag:
                    return config.HasViewTag(viewTag);
                case ShopItemViewEventMatchMode.PriceType:
                    return config.priceType == priceType;
                case ShopItemViewEventMatchMode.RewardTarget:
                    return HasReward(config, rewardTarget);
                case ShopItemViewEventMatchMode.Style:
                    return config.style == style;
                case ShopItemViewEventMatchMode.ProductId:
                    return string.Equals(config.GetProductId(), productId, StringComparison.OrdinalIgnoreCase);
                case ShopItemViewEventMatchMode.ShowBonus:
                    return config.showBonus == expectedBool;
                case ShopItemViewEventMatchMode.Visible:
                    return config.visible == expectedBool;
                default:
                    return false;
            }
        }

        private static bool HasReward(ShopItemConfigSO config, ShopRewardTarget target)
        {
            if (config.rewards == null)
                return false;

            for (int i = 0; i < config.rewards.Count; i++)
            {
                if (config.rewards[i] != null && config.rewards[i].target == target)
                    return true;
            }

            return false;
        }
    }
}


