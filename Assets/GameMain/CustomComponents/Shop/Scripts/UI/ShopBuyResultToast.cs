using UnityEngine;

namespace Lokas
{
    public static class ShopBuyResultToast
    {
        public static void Show(ShopBuyResult result, string message = null)
        {
            if (result == ShopBuyResult.Pending)
                return;

            if (!string.IsNullOrWhiteSpace(message))
            {
                PromptUIPanel.ShowToast(message);
                return;
            }

            string text = result switch
            {
                ShopBuyResult.Success => "Purchase successful",
                ShopBuyResult.NotEnoughGold => "Not enough gold",
                ShopBuyResult.InvalidItem => "Invalid item",
                ShopBuyResult.NotVisible => "Item unavailable",
                _ => "Purchase failed"
            };

            PromptUIPanel.ShowToast(text);
        }
    }
}
