using System.Collections.Generic;
using System.Globalization;
using LitJson;
using UnityEngine;

namespace YzAdComponent
{

    /// <summary>
    /// 广告测试平台,用于开发测试
    /// </summary>
    public class YzAdAgentDebug : AdAgent
    {

        Dictionary<int, YzAdConfig> adConfigs = new Dictionary<int, YzAdConfig>();
        int refreshIconTimer = -1;

        int refreshBannerTimer = -1;
        public void init()
        {
            //YzUtils.registerServerInit(() =>
            //{
            //    YzUtils.showLog("组件初始化成功，执行初始化广告类！");

            //    new AsyncCallback(initAdConfig).Invoke(null);

            //}
            //);
            //Invok();

        }

        public void initAdConfig()
        {
            adConfigs = new Dictionary<int, YzAdConfig>();
            JsonData config = YzUtils.getConfigJsonArryValue("ad_location_configs");
            if (JsonMapper.ToJson(config) == "")
            {
                return;
            }

            for (int i = 0; i < config.Count; i++)
            {
                JsonData item = config[i];
                YzAdConfig yzAdConfig = new YzAdConfig();
                yzAdConfig.show_banner_ad = bool.Parse(item["show_banner_ad"].ToString());
                yzAdConfig.show_intersitial_ad = bool.Parse(item["show_intersitial_ad"].ToString());
                yzAdConfig.location = int.Parse(item["location"].ToString(), CultureInfo.InvariantCulture);
                adConfigs.Add(yzAdConfig.location, yzAdConfig);
            }

            float intervalTimeShowIntersititialAd = YzUtils.getConfigFloatValue("interval_time_show_cp");
            if (intervalTimeShowIntersititialAd > 0)
            {
                YzTimer.SetInterval(intervalTimeShowIntersititialAd, () =>
                {
                    YzUtils.showLog("定时调用插屏广告！");
                    YzUtils.adManager.showIntersititialAd(99999);
                }, -1);
            }
        }

        public void showBannerAd(YzAdParame yzAdParame)
        {
            //initAdConfig();
            YzAdConfig adConfig = new YzAdConfig();

            if (adConfigs.ContainsKey(yzAdParame.location))
            {
                adConfig = adConfigs[yzAdParame.location];
            }
            YzUtils.showLog("当前位置服务器Banner的配置：" + adConfig.ToString());

            if (!adConfig.show_banner_ad)
            {
                YzUtils.showLog("当前位置不显示Banner！");
                hideBannerAd();
                return;
            }


            showBannerTimerTask(yzAdParame.location, false);
            if (refreshBannerTimer > -1) YzTimer.ClearTimer(refreshBannerTimer);

            int refreshBannerTime = YzUtils.getConfigIntValue("banner_refresh_time", -1);
            YzUtils.showLog("服务器Banner 配置：#refreshBannerTime:" + refreshBannerTime);

            if (refreshBannerTime > 0)
            {
                refreshBannerTimer = YzTimer.SetInterval(refreshBannerTime, () =>
                {
                    YzUtils.showLog("定时" + refreshBannerTime + "秒刷新Banner！");
                    showBannerTimerTask(yzAdParame.location, true);
                }, -1);
            }
        }

        private void showBannerTimerTask(int yzAdLocationEnum, bool isTimerRefresh)
        {
            YzUtils.instance.createNativeBanner();
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

            YzUtils.instance.createNativeIntersitial();
        }

        public void showIntersititialVideoAd()
        {

        }

        public GameObject showNativeIcon(YzAdParame adParame)
        {
            YzUtils.instance.createNativeIcon(adParame);
            if (refreshIconTimer > -1) YzTimer.ClearTimer(refreshIconTimer);

            int refreshIconTime = YzUtils.getConfigIntValue("native_icon_refresh_time", 15);
            YzUtils.showLog("服务器原生悬浮ICON 配置：#icon_jump_native=" + refreshIconTime);
            if (refreshIconTime <= 3)
            {
                YzUtils.showLog("悬浮ICON必须大于3秒，当前默认为15秒刷新");
                refreshIconTime = 15;
            }

            refreshIconTimer = YzTimer.SetInterval(refreshIconTime, () =>
            {
                YzUtils.showLog("定时" + refreshIconTime + "秒刷新ICON！");
                YzUtils.instance.createNativeIcon(adParame);
            }, -1);
            return null;
        }

        public void showReardVideo(RewardVideoCallBack rewardVideoCallBack)
        {
            if (rewardVideoCallBack != null)
            {
                rewardVideoCallBack.onSuccess();
            }
        }

        public void hideBannerAd()
        {
            if (refreshBannerTimer > -1)
            {
                YzTimer.ClearTimer(refreshBannerTimer);
                refreshBannerTimer = -1;
            }
            YzUtils.instance.destroyNativeBanner();
        }

        public void showCurBannerAd()
        {
            YzUtils.instance.createNativeBanner();
        }

        public void hideCurBannerAd()
        {
            YzUtils.instance.destroyNativeBanner();
        }


        public void hideNativeIcon()
        {
            if (refreshIconTimer > -1)
            {
                YzTimer.ClearTimer(refreshIconTimer);
                refreshIconTimer = -1;
            }
            YzUtils.instance.destroyNativeIcon();
        }

        public void showCustomAd(YzAdParame yzAdParame)
        {

        }

        public void hideCustomAd(int location)
        {

        }

        public bool rewardVideoIsReady()
        {
            return false;
        }

    }

}