using System.Collections.Generic;
using ConfigSO;
using UnityEngine;

namespace Lokas
{
    [CreateAssetMenu(fileName = "ShopItemConfig", menuName = "Shop/Item Config")]
    /// <summary>
    /// 鍗曚釜鍟嗗簵鍟嗗搧閰嶇疆锛岃礋璐ｆ弿杩板晢鍝佹樉绀哄唴瀹广€佷环鏍煎拰濂栧姳銆?    /// </summary>
    public class ShopItemConfigSO : IdOnlyConfigSO
    {
        [Header("Base")]
        /// <summary>
        /// 鏄剧ず鎺掑簭锛屾暟鍊艰秺灏忚秺闈犲墠銆?        /// </summary>
        public int sortOrder;

        /// <summary>
        /// 鏄惁鍦ㄥ晢搴椾腑鏄剧ず璇ュ晢鍝併€?        /// </summary>
        public bool visible = true;

        /// <summary>
        /// 鍟嗗搧鍗＄墖鏍峰紡閰嶇疆锛岀敤浜庢帶鍒惰儗鏅€佹寜閽€佽鏍囧拰鏂囧瓧棰滆壊銆?        /// </summary>
        public ShopItemStyleConfigSO style;

        [Header("Content")]
        /// <summary>
        /// 鍟嗗搧涓诲浘鏍囷紝鏄剧ず鍦?Item/Icon 鑺傜偣銆?        /// </summary>
        public Sprite icon;

        /// <summary>
        /// 鍟嗗搧鎻忚堪鏂囨湰锛屽綋 contentLocalizationKey 涓虹┖鏃剁洿鎺ヤ娇鐢ㄨ鏂囨湰銆?        /// </summary>
        public string contentText;

        /// <summary>
        /// 鍟嗗搧鎻忚堪鐨勫璇█ Key锛岄厤缃悗浼樺厛浠庢湰鍦板寲琛ㄨ鍙栨枃鏈€?        /// </summary>
        public string contentLocalizationKey;

        /// <summary>
        /// 璐拱鎴愬姛鍚庡彂鏀剧殑濂栧姳鍒楄〃銆?        /// </summary>
        public List<ShopRewardData> rewards = new();

        [Header("View Events")]
        /// <summary>
        /// 鍟嗗搧瑙嗗浘鏍囩锛屼緵 ShopItemView 鐨?View Events 瑙勫垯鍖归厤浣跨敤銆?        /// 閫傚悎閰嶇疆鍙奖鍝?UI 琛ㄧ幇鐨勬爣璁帮紝渚嬪 hot銆乴imited銆乺emove_ads_tip銆?        /// 濡傛灉宸紓鍙互閫氳繃 priceType/rewards/style/productId 琛ㄨ揪锛屼紭鍏堢敤浜嬩欢瑙勫垯閲岀殑瀵瑰簲 Match Mode銆?        /// </summary>
        public List<string> viewTags = new();

        [Header("Price")]
        /// <summary>
        /// 浠锋牸绫诲瀷锛屼緥濡傞噾甯併€佸箍鍛娿€佸唴璐垨鍏嶈垂銆?        /// </summary>
        public ShopPriceType priceType = ShopPriceType.Gold;

        /// <summary>
        /// 浠锋牸鏁板€笺€傞噾甯佸晢鍝佽〃绀烘秷鑰楅噾甯佹暟閲忥紝鍐呰喘鍟嗗搧鍙綔涓哄鐢ㄦ樉绀烘暟鍊笺€?        /// </summary>
        [HideInInspector]
        public float price;

        /// <summary>
        /// 鑷畾涔変环鏍兼樉绀烘枃鏈€備负绌烘椂浼氭牴鎹?priceType 鍜?price 鑷姩鐢熸垚銆?        /// </summary>
        [HideInInspector]
        public string priceText;

        /// <summary>
        /// 鍐呰喘鍟嗗搧閰嶇疆銆傝繍琛屾椂浠锋牸浼樺厛浣跨敤鏈嶅姟鍣ㄨ繑鍥炲€硷紝澶辫触鏃朵娇鐢ㄨ閰嶇疆閲岀殑淇濆簳浠锋牸銆?        /// </summary>
        public ShopProductConfigSO productConfig;

        /// <summary>
        /// 鏃х増鍐呰喘鍟嗗搧 ID锛屼粎鐢ㄤ簬鍏煎鏈縼绉昏祫浜с€?        /// </summary>
        [HideInInspector]
        public string productId;

        [Header("Bonus")]
        /// <summary>
        /// 鏄惁鏄剧ず瑙掓爣濂栧姳鏂囨銆?        /// </summary>
        public bool showBonus;

        /// <summary>
        /// 瑙掓爣鏄剧ず鏂囨湰锛屽綋 bonusLocalizationKey 涓虹┖鏃剁洿鎺ヤ娇鐢ㄨ鏂囨湰銆?        /// </summary>
        public string bonusText;

        /// <summary>
        /// 瑙掓爣鏂囨鐨勫璇█ Key锛岄厤缃悗浼樺厛浠庢湰鍦板寲琛ㄨ鍙栨枃鏈€?        /// </summary>
        public string bonusLocalizationKey;

        public string GetProductId()
        {
            if (productConfig != null && !string.IsNullOrWhiteSpace(productConfig.productId))
                return productConfig.productId;

            return productId;
        }

        public string GetProductName()
        {
            if (productConfig != null && !string.IsNullOrWhiteSpace(productConfig.productName))
                return productConfig.productName;

            return GetProductId();
        }

        public float GetFallbackPrice()
        {
            return productConfig != null ? productConfig.fallbackPrice : price;
        }

        public string GetFallbackPriceText()
        {
            if (productConfig != null)
                return productConfig.GetFallbackPriceText();

            if (!string.IsNullOrWhiteSpace(priceText))
                return priceText;

            return price.ToString();
        }

        public bool HasViewTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || viewTags == null)
                return false;

            for (int i = 0; i < viewTags.Count; i++)
            {
                if (string.Equals(viewTags[i], tag, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}


