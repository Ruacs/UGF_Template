using System;
using System.Collections.Generic;
using System.Linq;
using Ads;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProductInfo = YzAdComponent.ProductInfo;

namespace Lokas
{
    public class ShopComponent : GameFrameworkComponent
    {
        private const string StandaloneNoAdsProductId = "10001";
        private const string NoAdsBundleProductId = "10002";
        private const string PurchasedProductEventKeyPrefix = "Shop_ProductPurchased_";

        [SerializeField] private bool m_enableShop = true;
        [SerializeField] private ShopCatalogSO m_Catalog;

        public event Action<ShopItemConfigSO, ShopBuyResult, string> OnBuyFinished;
        public event Action OnProductInfoUpdated;
        public event Action<string> OnProductInfoUpdateFailed;
        public event Action<bool> OnEnableShopChanged;

        private readonly Dictionary<string, ProductInfo> m_ProductInfoById = new();
        private readonly HashSet<string> m_PendingIapProductIds = new();
        private bool m_IsQueryingProductInfo;
        private bool m_HasRequestedProductInfo;
        public ShopCatalogSO Catalog
        {
            get => m_Catalog;
            set
            {
                m_Catalog = value;
                RefreshProductInfo();
            }
        }
        public bool EnableShop
        {
            get => m_enableShop;
            set
            {
                if (m_enableShop == value)
                    return;

                m_enableShop = value;
                OnEnableShopChanged?.Invoke(m_enableShop);
            }
        }

        // 保留旧入口阈值，进度来源改为明确配置的主玩法；不再写死 HexaAway。
        public bool IsUnlocked => AdsServerConfig.TryGetPrimaryLevel(out int level) && level >= AdsManager.ShopUnlockLevel;
        public bool IsAvailable => EnableShop && IsUnlocked;


        public IReadOnlyList<ShopItemConfigSO> GetVisibleItems()
        {
            if (!IsAvailable || m_Catalog == null || m_Catalog.items == null)
                return Array.Empty<ShopItemConfigSO>();

            return m_Catalog.items
                .Where(IsItemVisible)
                .OrderBy(item => item.sortOrder)
                .ThenBy(item => item.id)
                .ToList();
        }

        public ShopItemConfigSO GetItem(int id)
        {
            if (m_Catalog == null || m_Catalog.items == null)
                return null;

            return m_Catalog.items.FirstOrDefault(item => item != null && item.id == id);
        }

        public ShopBuyResult TryBuy(int itemId)
        {
            return TryBuy(GetItem(itemId));
        }

        public ShopBuyResult TryBuy(ShopItemConfigSO item)
        {
            ShopBuyResult result = ValidateBeforeBuy(item);
            if (result != ShopBuyResult.Success)
            {
                OnBuyFinished?.Invoke(item, result, string.Empty);
                return result;
            }

            result = Pay(item);
            if (result == ShopBuyResult.Success)
            {
                GrantRewards(item);
                MarkPurchased(item);
                OnBuyFinished?.Invoke(item, result, string.Empty);
            }
            else if (result != ShopBuyResult.Pending)
            {
                OnBuyFinished?.Invoke(item, result, string.Empty);
            }

            return result;
        }

        public ShopBuyResult ValidateBeforeBuy(ShopItemConfigSO item)
        {
            if (item == null)
                return ShopBuyResult.InvalidItem;

            if (!IsAvailable)
                return ShopBuyResult.NotVisible;

            if (!IsItemVisible(item))
                return ShopBuyResult.NotVisible;

            if (item.priceType == ShopPriceType.Gold && GameEntry.SaveData.Money < item.price)
                return ShopBuyResult.NotEnoughGold;

            if (item.priceType == ShopPriceType.IAP &&
                !string.IsNullOrWhiteSpace(item.GetProductId()) &&
                m_PendingIapProductIds.Contains(item.GetProductId()))
            {
                return ShopBuyResult.Pending;
            }

            return ShopBuyResult.Success;
        }

        public void RefreshProductInfo(bool force = false)
        {
            if (m_IsQueryingProductInfo || (!force && m_HasRequestedProductInfo))
                return;

            m_HasRequestedProductInfo = true;
            m_IsQueryingProductInfo = true;

            AdsManager.GetAllProducedInfo(productInfos =>
            {
                m_ProductInfoById.Clear();
                if (productInfos != null)
                {
                    for (int i = 0; i < productInfos.Count; i++)
                    {
                        ProductInfo productInfo = productInfos[i];
                        if (productInfo == null || string.IsNullOrWhiteSpace(productInfo.pid))
                            continue;

                        m_ProductInfoById[productInfo.pid] = productInfo;
                    }
                }

                m_IsQueryingProductInfo = false;
                OnProductInfoUpdated?.Invoke();
            }, msg =>
            {
                m_HasRequestedProductInfo = false;
                m_IsQueryingProductInfo = false;
                Log.Warning("[ShopComponent] Query product info failed: {0}", msg);
                OnProductInfoUpdateFailed?.Invoke(msg);
                OnProductInfoUpdated?.Invoke();
            });
        }

        public string GetPriceText(ShopItemConfigSO item)
        {
            if (item == null)
                return string.Empty;

            switch (item.priceType)
            {
                case ShopPriceType.Free:
                    return "Free";
                case ShopPriceType.Gold:
                    return item.price.ToString();
                case ShopPriceType.AD:
                    return "Ad";
                case ShopPriceType.IAP:
                    ProductInfo productInfo = GetProductInfo(item);
                    if (productInfo != null && !string.IsNullOrWhiteSpace(productInfo.formattedPrice))
                        return productInfo.formattedPrice;

                    return item.GetFallbackPriceText();
                default:
                    return item.GetFallbackPrice().ToString();
            }
        }

        private ProductInfo GetProductInfo(ShopItemConfigSO item)
        {
            if (item == null)
                return null;

            string productId = item.GetProductId();
            if (string.IsNullOrWhiteSpace(productId))
                return null;

            return m_ProductInfoById.TryGetValue(productId, out ProductInfo productInfo) ? productInfo : null;
        }

        private static bool IsItemVisible(ShopItemConfigSO item)
        {
            if (item == null || !item.visible)
                return false;

            var primary = GameEntry.SubGames?.Get(AdsServerConfig.PrimaryGameMode);
            if (item.rewards != null && item.rewards.Any(reward => reward != null &&
                (reward.target == ShopRewardTarget.GameProp || reward.target == ShopRewardTarget.CommonProp) &&
                (primary == null || !primary.CanGrantProp(reward.propType)))) return false;

            if (IsPurchasedOnceProduct(item))
                return false;

            return !ShouldHideNoAdsRewardItem(item);
        }

        private static bool ShouldHideNoAdsRewardItem(ShopItemConfigSO item)
        {
            return GameEntry.SaveData != null &&
                GameEntry.SaveData.IsNoAds &&
                HasReward(item, ShopRewardTarget.NoAds) &&
                string.Equals(item.GetProductId(), StandaloneNoAdsProductId, StringComparison.Ordinal);
        }

        private static bool IsPurchasedOnceProduct(ShopItemConfigSO item)
        {
            return item != null &&
                string.Equals(item.GetProductId(), NoAdsBundleProductId, StringComparison.Ordinal) &&
                GameEntry.SaveData != null &&
                GameEntry.SaveData.GetEventData(GetPurchasedProductEventKey(NoAdsBundleProductId));
        }

        private static void MarkPurchased(ShopItemConfigSO item)
        {
            if (item == null || GameEntry.SaveData == null)
                return;

            string productId = item.GetProductId();
            if (string.Equals(productId, NoAdsBundleProductId, StringComparison.Ordinal))
                GameEntry.SaveData.SetEventData(GetPurchasedProductEventKey(productId));
        }

        private static string GetPurchasedProductEventKey(string productId)
        {
            return PurchasedProductEventKeyPrefix + productId;
        }

        private static bool HasReward(ShopItemConfigSO item, ShopRewardTarget target)
        {
            if (item == null || item.rewards == null)
                return false;

            for (int i = 0; i < item.rewards.Count; i++)
            {
                if (item.rewards[i] != null && item.rewards[i].target == target)
                    return true;
            }

            return false;
        }

        private ShopBuyResult Pay(ShopItemConfigSO item)
        {
            switch (item.priceType)
            {
                case ShopPriceType.Free:
                    return ShopBuyResult.Success;
                case ShopPriceType.Gold:
                    if (GameEntry.SaveData.Money < item.price)
                        return ShopBuyResult.NotEnoughGold;

                    GameEntry.SaveData.Money -= (int)item.price;
                    return ShopBuyResult.Success;
                case ShopPriceType.AD:
                    Log.Warning("[ShopComponent] Item '{0}' uses {1}, payment flow is not connected yet.", item.id, item.priceType);
                    return ShopBuyResult.PaymentUnavailable;
                case ShopPriceType.IAP:
                    return StartIapPurchase(item);
                default:
                    return ShopBuyResult.InvalidItem;
            }
        }

        private ShopBuyResult StartIapPurchase(ShopItemConfigSO item)
        {
            string productId = item.GetProductId();
            if (string.IsNullOrWhiteSpace(productId))
                return ShopBuyResult.InvalidItem;

            string productName = item.GetProductName();
            m_PendingIapProductIds.Add(productId);

            AdsManager.PurchaseProduct(productId, productName, () =>
            {
                m_PendingIapProductIds.Remove(productId);
                GrantRewards(item);
                MarkPurchased(item);
                OnBuyFinished?.Invoke(item, ShopBuyResult.Success, string.Empty);
            }, msg =>
            {
                m_PendingIapProductIds.Remove(productId);
                Log.Warning("[ShopComponent] IAP item '{0}' failed: {1}", item.id, msg);
                OnBuyFinished?.Invoke(item, ShopBuyResult.PaymentUnavailable, msg);
            });

            return ShopBuyResult.Pending;
        }

        private void GrantRewards(ShopItemConfigSO item)
        {
            if (item.rewards == null)
                return;

            foreach (ShopRewardData reward in item.rewards)
            {
                if (reward == null)
                    continue;

                if (reward.target != ShopRewardTarget.NoAds && reward.count <= 0)
                    continue;

                GrantReward(reward);
            }
        }

        private void GrantReward(ShopRewardData reward)
        {
            switch (reward.target)
            {
                case ShopRewardTarget.Gold:
                    GameEntry.SaveData.Money += reward.count;
                    break;
                case ShopRewardTarget.GameProp:
                    AddGameProp(reward.propType, reward.count);
                    break;
                case ShopRewardTarget.CommonProp:
                    AddGameProp(reward.propType, reward.count);
                    break;
                case ShopRewardTarget.NoAds:
                    GameEntry.SaveData.IsNoAds = true; 
                    break;
            }
        }

        private static void AddGameProp(PropType propType, int count)
        {
            if (GameEntry.SubGames?.Get(AdsServerConfig.PrimaryGameMode)?.TryGrantProp(propType, count) != true)
                Log.Warning("[ShopComponent] Primary game cannot receive prop: {0}.", propType);
        }
    }
}
