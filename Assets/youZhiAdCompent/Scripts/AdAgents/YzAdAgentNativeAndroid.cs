using System;
using System.Collections.Generic;
using System.Globalization;
using LitJson;
using UnityEngine;

namespace YzAdComponent
{


    /**
     * 原生安卓
     */
    public class YzAdAgentNativeAndroid : AdAgent
    {

        private static AndroidJavaClass androidJavaClass = null;
        private Dictionary<int, YzAdConfig> adConfigs = null;
        int refreshBannerTimer = -1;
        int refreshIconTimer = -1;
        public void init()
        {
            try
            {
                androidJavaClass = new AndroidJavaClass(YzConstant.JniClassName);
            }
            catch (Exception e)
            {
                YzUtils.showLog("erro JNI类没有找到！ #msg=" + e.Message);
            }
        }

        public void initAdConfig()
        {
            adConfigs = new Dictionary<int, YzAdConfig>();
            JsonData config = YzUtils.getConfigJsonArryValue("ad_location_configs");
            Debug.Log("ad_location_configs:" + YzUtils.getConfigStrValue("ad_location_configs", "no value"));
            if (JsonMapper.ToJson(config) != "")
            {
                for (int i = 0; i < config.Count; i++)
                {
                    JsonData item = config[i];
                    YzAdConfig yzAdConfig = new YzAdConfig();
                    yzAdConfig.show_banner_ad = bool.Parse(item["show_banner_ad"].ToString());
                    yzAdConfig.show_intersitial_ad = bool.Parse(item["show_intersitial_ad"].ToString());
                    yzAdConfig.location = int.Parse(item["location"].ToString(), CultureInfo.InvariantCulture);
                    adConfigs.Add(yzAdConfig.location, yzAdConfig);
                }
            }
            int intervalTimeShowIntersititialAd = YzUtils.getConfigIntValue("interval_time_show_cp");
            if (intervalTimeShowIntersititialAd > 0)
            {
                YzTimer.SetInterval(intervalTimeShowIntersititialAd, () =>
                {
                    YzUtils.showLog("定时调用插屏广告！");
                    YzUtils.adManager.showIntersititialAd(99999);
                }, -1);
            }
        }

        public void showBannerAd(YzAdParame adParame)
        {

            YzAdConfig adConfig = new YzAdConfig();

            if (adConfigs.ContainsKey(adParame.location))
            {
                adConfig = adConfigs[adParame.location];
            }
            YzUtils.showLog("当前位置服务器Banner的配置：" + adConfig.ToString());

            if (!adConfig.show_banner_ad)
            {
                YzUtils.showLog("当前位置不显示Banner！");
                hideBannerAd();
                return;
            }

            showBannerTimerTask(adParame.location, false);
            if (refreshBannerTimer > -1) YzTimer.ClearTimer(refreshBannerTimer);

            int refreshBannerTime = YzUtils.getConfigIntValue("banner_refresh_time", -1);
            YzUtils.showLog("服务器Banner 配置：#refreshBannerTime" + refreshBannerTime);

            if (refreshBannerTime > 0)
            {
                refreshBannerTimer = YzTimer.SetInterval(refreshBannerTime, () =>
                {
                    YzUtils.showLog("定时" + refreshBannerTime + "秒刷新Banner！");

                    showBannerTimerTask(adParame.location, true);
                }, -1);
            }
        }

        public void showIntersititialAd(YzAdParame adParame)
        {
            YzAdConfig adConfig = new YzAdConfig();

            if (adConfigs.ContainsKey(adParame.location))
            {
                adConfig = adConfigs[adParame.location];
            }
            YzUtils.showLog("当前位置服务器插屏的配置：" + adConfig.ToString());

            if (!adConfig.show_intersitial_ad)
            {
                YzUtils.showLog("当前位置不显示插屏！");
                return;
            }
            androidJavaClass.CallStatic("showInterstitial");
        }

        private void showBannerTimerTask(int yzAdLocation, bool isTimerRefresh)
        {
            YzAdParame adParame = new YzAdParame(yzAdLocation, isTimerRefresh);
            androidJavaClass.CallStatic("showBanner", JsonMapper.ToJson(adParame));
        }


        public void showIntersititialVideoAd()
        {
            androidJavaClass.CallStatic("showFullScreenVideo");
        }


        public GameObject showNativeIcon(YzAdParame adParame)
        {
            adParame.winSizeWidth = Screen.width;
            adParame.winSizeHeight = Screen.height;
            adParame.parent = null;
            androidJavaClass.CallStatic("showNativeIcon", JsonMapper.ToJson(adParame));
            if (refreshIconTimer > -1) YzTimer.ClearTimer(refreshIconTimer);

            int refreshIconTime = YzUtils.getConfigIntValue("native_icon_refresh_time", 15);
            YzUtils.showLog("服务器原生悬浮ICON 配置：#native_icon_refresh_time=" + refreshIconTime);

            if (refreshIconTime <= 3)
            {
                YzUtils.showLog("悬浮ICON必须大于3秒，当前默认为15秒刷新");
                refreshIconTime = 15;
            }

            refreshIconTimer = YzTimer.SetInterval(refreshIconTime, () =>
            {
                YzUtils.showLog("定时" + refreshIconTime + "秒刷新ICON！");
                androidJavaClass.CallStatic("showNativeIcon", JsonMapper.ToJson(adParame));
            }, -1);

            return null;
        }

        public void showReardVideo(RewardVideoCallBack rewardVideoCallBack)
        {
            androidJavaClass.CallStatic("showVideo");
        }

        public void hideBannerAd()
        {
            if (refreshBannerTimer > -1)
            {
                YzTimer.ClearTimer(refreshBannerTimer);
                refreshBannerTimer = -1;
            }
            androidJavaClass.CallStatic("hideBanner", "");
        }
        public void showCurBannerAd()
        {
        }
        public void hideCurBannerAd()
        {
        }

        public void hideNativeIcon()
        {
            if (refreshIconTimer > -1)
            {
                YzTimer.ClearTimer(refreshIconTimer);
                refreshIconTimer = -1;
            }
            androidJavaClass.CallStatic("hideNativeIcon");
        }

        public void showCustomAd(YzAdParame yzAdParame) { }

        public void hideCustomAd(int location) { }

        /**
         * 激励视频广告是否准备就绪
         * @returns true:准备就绪，false：没有加载成功的广告
         */
        public bool rewardVideoIsReady()
        {
            return AndroidJavaClassHelper.CallStatic<bool>("rewardVideoIsReady");
        }

    }

}