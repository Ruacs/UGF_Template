using UnityEngine;

namespace Lokas
{
    [CreateAssetMenu(fileName = "ShopProductConfig", menuName = "Shop/Product Config")]
    public class ShopProductConfigSO : ScriptableObject
    {
        [Header("Product")]
        public string productId;
        public string productName;

        [Header("Fallback Price")]
        public float fallbackPrice;
        public string fallbackPriceText;

        public string GetFallbackPriceText()
        {
            if (!string.IsNullOrWhiteSpace(fallbackPriceText))
                return fallbackPriceText;

            return fallbackPrice.ToString();
        }
    }
}


