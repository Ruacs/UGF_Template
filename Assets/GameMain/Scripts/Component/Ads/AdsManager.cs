using System.Collections;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lokas;
using UnityEngine;
using System.Collections.Generic;
using LitJson;
using System.Globalization;
using Log = UnityGameFramework.Runtime.Log;
using YzAdComponent;

namespace Ads
{
    // public class ProductInfo
    // {
    //     public string pid;
    //     public string formattedPrice;
    // }


    public static class AdsManager
    {
        /// <summary>
        /// 展示间隔?=0 不展示，1 每局? 两局一次）
        /// </summary>
        private static readonly Dictionary<GameMode, int> s_RoundCounters = new();

        private static bool m_isLoaded = false;
        public static int ShopUnlockLevel { get; set; } = 1;

        public static int LevelTimerIdleStopSeconds => AdsServerConfig.Common.LevelTimerIdleStopSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            m_isLoaded = false;
            s_RoundCounters.Clear();
        }


        public static void LaunchAdsComponent()
        {
            YzUtils.LaunchAdCompent();
            YzUtils.registerServerInitEvent(() =>
            {
                YzUtils.showLog("广告组件初始化完成,加载服务器配置");
                LoadServerConfig();

            });

        }


        /// <summary>
        /// 加载服务器配?
        /// </summary>
        public static void LoadServerConfig()
        {
            if (m_isLoaded) return;
            m_isLoaded = true;
            s_RoundCounters.Clear();
            AdsServerConfig.ReloadFromServer();
        }



        /// <summary>
        /// 当前关卡胜利是否展示插屏广告
        /// </summary>
        public static bool ShouldShowAds() =>
            ShouldShowAds(GameEntry.GameManager != null ? GameEntry.GameManager.CurrentGameMode : GameMode.None);

        public static bool ShouldShowAds(GameMode gameMode)
        {
            if (GameEntry.SaveData == null || GameEntry.SaveData.IsNoAds) return false;
            if (!AdsServerConfig.TryGet(gameMode, out var config) || !config.TryGetCurrentLevel(out int currentLevel))
                return false;
            if (config.ShowAdsInterval <= 0) return false;
            if (config.NoAdsBeforeLevelCount > 0 && currentLevel <= config.NoAdsBeforeLevelCount) return false;

            // 每个游戏独立计数，并使用对应配置的间隔。
            s_RoundCounters.TryGetValue(gameMode, out int count);
            count = count % config.ShowAdsInterval + 1;
            s_RoundCounters[gameMode] = count;
            return count == config.ShowAdsInterval;
        }

        public static void ShowBanner() =>
            ShowBanner(GameEntry.GameManager != null ? GameEntry.GameManager.CurrentGameMode : GameMode.None);

        // 菜单需要展示时必须显式传入所属游戏，不猜测 HexaAway 进度。
        public static void ShowBanner(GameMode gameMode)
        {
            if (GameEntry.SaveData == null || GameEntry.SaveData.IsNoAds ||
                !AdsServerConfig.TryGet(gameMode, out var config) || !config.TryGetCurrentLevel(out int level))
            {
                HideBanner();
                return;
            }

            if (config.ShowBannerLevel > 0 && level <= config.ShowBannerLevel)
            {
                HideBanner();
                return;
            }
            YzUtils.adManager?.showBannerAd();
        }

        public static void HideBanner()
        {
            YzUtils.adManager?.hideBannerAd();
        }

        /// <summary>
        /// 插屏
        /// </summary>
        public static void ShowInterstitialAd(int AdLocation = 0)
        {
            YzUtils.adManager.showIntersititialAd(AdLocation);
        }

        /// <summary>
        /// 激励视频
        /// </summary>
        /// <param name="onAdComplete"></param>
        /// <param name="onAdFailed"></param>
        public static void ShowRewardedAd(System.Action onAdComplete, System.Action<string> onAdFailed = null)
        {
            if (onAdFailed == null)
            {
                onAdFailed = (error) =>
                {
                    PromptUIPanel.ShowToast(GameEntry.Localization.GetString("Tips.AdsFail"));

                };
            }

            if (GameEntry.GMMode)
            {
                if (GameEntry.AdCallbackSuccess)
                {
                    onAdComplete?.Invoke();
                    PromptUIPanel.ShowToast("GM模式下激励视频广告成功");
                }
                else
                {
                    onAdFailed?.Invoke("GM mode rewarded ad failed.");
                    PromptUIPanel.ShowToast("GM模式下激励视频广告失败");
                }

                return;
            }

            RewardVideoCallBack rewardVideoCallBack = new RewardVideoCallBack
            {
                onSuccess = onAdComplete,
                onFail = onAdFailed
            };


            YzUtils.adManager.showReardVideoAd(rewardVideoCallBack);

        }



        /// <summary>
        /// 激励视频是否准备完成
        /// </summary>
        /// <returns></returns>
        public static bool IsRewardedReady()
        {
            if (GameEntry.GMMode)
            {
                return GameEntry.RewardedAdReady;
            }

            return false;
        }


        /// <summary>
        /// 获取网络状态
        /// </summary>
        /// <returns></returns>
        public static bool IsNetworkReachable()
        {
            if (GameEntry.GMMode)
            {
                return GameEntry.NetworkReachable;
            }

            NetworkReachability netType = Application.internetReachability;
            if (netType == NetworkReachability.NotReachable)
            {
                Log.Info("无网络");
                return false;
            }
            return true;

        }

        public static bool checkIsNetwork()
        {
            return IsNetworkReachable();
        }

        public static void HideSplash()
        {
            Log.Info("Hide Splash");

        }



        public static void GetAllProducedInfo(Action<List<ProductInfo>> onSuccess = null, Action<string> onFail = null)
        {
            YzUtils.queryAllProductDetail(new YzQueryProductCallBack()
            {
                onSuccess = (List<ProductInfo> productInfos) =>
                {
                    Log.Info("查询商品成功：{0}", productInfos == null ? "null" : string.Join(", ", productInfos));
                    onSuccess?.Invoke(productInfos);
                },
                onFail = (string msg) =>
                {
                    Log.Info("查询商品失败：{0}", msg);
                    onFail?.Invoke(msg);
                }
            });
        }
        private const string RemoveAdsProductId = "10001";
        private const string RemoveAdsProductName = "no_ad";


        public static void PurchaseRemoveAds(Action onSuccess = null, Action<string> onFail = null)
        {
            PurchaseRemoveAds(RemoveAdsProductId, onSuccess, onFail);
        }

        public static void PurchaseRemoveAds(string productId, Action onSuccess = null, Action<string> onFail = null)
        {
            PurchaseProduct(productId, RemoveAdsProductName, () =>
            {
                YzUtils.removeAdTime();
                GameEntry.SaveData.IsNoAds = true;
                HideBanner();
                onSuccess?.Invoke();
            }, onFail);
        }

        public static void PurchaseProduct(string productId, string productName, Action onSuccess = null, Action<string> onFail = null)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                const string msg = "Product id is empty.";
                Log.Info("支付失败：{0}", msg);
                onFail?.Invoke(msg);
                return;
            }

            YzUtils.purchase(new ProductInfo(productId, productName), new YzPayCallBack()
            {
                onSuccess = () =>
                {
                    Log.Info("支付成功");
                    onSuccess?.Invoke();
                },
                onFail = (string msg) =>
                {
                    Log.Info("支付失败：{0}", msg);
                    onFail?.Invoke(msg);
                }
            });
        }

        public static void OpenAdsUnavailablePanel(System.Action onRetrySuccess, System.Action onCountdownFallback = null, System.Action onClosed = null)
        {
            PromptUIPanel.ShowToast(GameEntry.Localization.GetString("Tips.AdsFail"));

            bool isNetworkReachable = IsNetworkReachable();
            if (!isNetworkReachable && !AdsServerConfig.Common.EnableAdsUnavailablePanel)
            {
                onClosed?.Invoke();
                return;
            }

            if (!AdsServerConfig.Common.EnableAdsUnavailablePanel)
            {
                onClosed?.Invoke();
                return;
            }

            GameEntry.UI.OpenUIForm(UIFormId.AdsUnavailableUIPanel, new AdsUnavailableUIData(
                onRetrySuccess,
                onCountdownFallback,
                onClosed));
        }

    }

}

