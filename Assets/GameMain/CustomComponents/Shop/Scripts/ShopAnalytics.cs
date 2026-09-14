using Ads;

namespace Lokas
{
    public static class ShopAnalytics
    {
        public const string PageShop = "Shop";
        public const string PageFail = "Fail";
        public const string PageGetProps = "GetProps";

        private const string EventBuyRequest = "Purchase_Request";
        private const string EventBuyPending = "Purchase_Pending";
        private const string EventBuySuccess = "Purchase_Success";
        private const string EventBuyFail = "Purchase_Fail";

        public static void ReportBuyRequest(string page, ShopItemConfigSO item, string result = "Request")
        {
            ReportBuyEvent(EventBuyRequest, page, item, result, string.Empty);
        }

        public static void ReportBuyPending(string page, ShopItemConfigSO item, ShopBuyResult result)
        {
            ReportBuyEvent(EventBuyPending, page, item, result.ToString(), string.Empty);
        }

        public static void ReportBuySuccess(string page, ShopItemConfigSO item, ShopBuyResult result, string message)
        {
            ReportBuyEvent(EventBuySuccess, page, item, result.ToString(), message);
        }

        public static void ReportBuyFail(string page, ShopItemConfigSO item, string result)
        {
            ReportBuyEvent(EventBuyFail, page, item, result, string.Empty);
        }

        public static void ReportBuyFail(string page, ShopItemConfigSO item, ShopBuyResult result, string message)
        {
            ReportBuyEvent(EventBuyFail, page, item, result.ToString(), message);
        }

        private static void ReportBuyEvent(string eventName, string page, ShopItemConfigSO item, string result, string message)
        {
            AdsAnalytics.EventWithName(eventName,
                ("page", string.IsNullOrWhiteSpace(page) ? PageShop : page),
                ("itemId", item != null ? item.id : 0),
                ("productId", item != null ? item.GetProductId() : string.Empty),
                ("productName", item != null ? item.GetProductName() : string.Empty),
                ("priceType", item != null ? item.priceType.ToString() : "Unknown"),
                ("price", item != null ? item.price : 0f),
                ("mode", GetGameModeName()),
                ("result", result),
                ("message", message ?? string.Empty));
        }

        private static string GetGameModeName()
        {
            return GameEntry.GameManager != null
                ? GameEntry.GameManager.CurrentGameMode.ToString()
                : string.Empty;
        }
    }
}


