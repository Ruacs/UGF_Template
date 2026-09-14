using System;
using System.Globalization;
using LitJson;
using UnityEngine;


namespace YzAdComponent
{
    public class YzAdManager
    {
        public RewardVideoCallBack rewardVideoCallBackObj = null;

        public YzAdManager()
        {
            init();
        }

        private AdAgent adAgent = null;
        public int bannerShowCount;
        public int interstitialShowCount;
        public int rewardVideoShowCount;
        public float bannerHeight = 315;
        public void init()
        {
            YzUtils.showLog("YzAdManager init!");
            if (PlatUtils.isAndroid)
            {
                adAgent = new YzAdAgentNativeAndroid();
            }
            else
            {
                adAgent = new YzAdAgentDebug();
            }

            adAgent.init();

        }


        /// <summary>
        /// 初始化广告配置
        /// </summary>
        public void initAdConfig()
        {
            adAgent.initAdConfig();
        }


        /// <summary>
        /// 展示激励视频广告
        /// </summary>
        /// <param name="rewardVideoCallBack">视频回调</param>
        public void showReardVideoAd(RewardVideoCallBack rewardVideoCallBack = null)
        {
            if (!YzUtils.openAd)
            {
                if (rewardVideoCallBack != null && rewardVideoCallBack.onSuccess != null)
                {
                    rewardVideoCallBack.onSuccess();
                }
                else
                {
                    YzUtils.showLog("激励视频广告 没有成功回调");
                }
                return;
            }

            YzUtils.showLog("展示激励视频广告");
            if (!YzUtils.checkCommonIsInit())
            {
                if (rewardVideoCallBack != null && rewardVideoCallBack.onFail != null)
                {
                    string msg = "未初始化广告！";
                    if (YzUtils.curLanguage == "zh")
                    {
                        msg = "激励视频加载失败！";
                    }
                    else
                    {
                        msg = "Reward video failed to load";
                    }
                    rewardVideoCallBack.onFail(msg);
                }
                return;
            }

            if (rewardVideoCallBack != null) this.rewardVideoCallBackObj = rewardVideoCallBack;
            adAgent.showReardVideo(rewardVideoCallBack);
        }

        /// <summary>激励视频结果回调</summary>
        ///<param name="code">状态值</param>
        ///<param name="msg">异常消息</param>
        public void rewardVideoAdCallBack(string result)
        {
            var resultData = JsonMapper.ToObject(result);
            int code = int.Parse(resultData["code"].ToString(), CultureInfo.InvariantCulture);
            string msg = resultData["msg"].ToString();

            YzUtils.showLog("rewardVideoAdCallBack #code=" + code + " #msg=" + msg);
            if (rewardVideoCallBackObj == null)
            {
                YzUtils.showLog("激励视频广告 回调函数为null");
                return;
            }

            if (code == 1)
            {
                if (rewardVideoCallBackObj.onSuccess != null)
                {
                    YzUtils.showLog("执行激励视频广告成功回调！");
                    rewardVideoCallBackObj.onSuccess();
                }
                else YzUtils.showLog("激励视频广告 没有成功回调");
            }
            else
            {
                if (rewardVideoCallBackObj.onFail != null)
                {
                    YzUtils.showLog("执行激励视频广告失败回调：" + msg);
                    rewardVideoCallBackObj.onFail(msg);
                }
                else YzUtils.showLog("激励视频广告 没有失败回调");
            }
            rewardVideoCallBackObj = null;
        }


        /// <summary>展示Banner广告</summary>
        /// <param name="yzAdLocation">当前调用的位置</param>
        public void showBannerAd(int yzAdLocation = 0)
        {
            if (!YzUtils.openAd) return;

            YzUtils.showLog("展示Banner广告 #location=" + yzAdLocation);
            if (!YzUtils.checkCommonIsInit() || checkIsRemoveAd("Banner") || !checkShowAdTime())
            {
                return;
            }

            YzAdParame adParame = new YzAdParame(yzAdLocation);
            adAgent.showBannerAd(adParame);
        }

        /// <summary>隐藏Banner广告</summary>
        public void hideBannerAd()
        {
            if (!YzUtils.openAd) return;

            if (!YzUtils.checkCommonIsInit()) return;

            YzUtils.showLog("隐藏Banner广告");
            adAgent.hideBannerAd();
        }



        /// <summary>显示当前Banner广告</summary>
        public void ShowCurBannerAd()
        {
            if (!YzUtils.openAd) return;

            if (!YzUtils.checkCommonIsInit()) return;

            YzUtils.showLog("闪烁Banner广告");
            adAgent.showCurBannerAd();
        }

        /// <summary>隐藏当前Banner广告</summary>
        public void HideCurBannerAd()
        {
            if (!YzUtils.openAd) return;

            if (!YzUtils.checkCommonIsInit()) return;

            YzUtils.showLog("闪烁Banner广告");
            adAgent.hideCurBannerAd();
        }




        /// <summary>
        /// 展示插屏广告
        /// </summary>
        /// <param name="yzAdLocation">当前调用的位置</param>
        public void showIntersititialAd(int yzAdLocation = 0)
        {
            if (!YzUtils.openAd) return;

            YzUtils.showLog("展示插屏广告 #location=" + yzAdLocation);
            if (!YzUtils.checkCommonIsInit() || checkIsRemoveAd("插屏") || !checkShowAdTime() || !checkInsertAdTime())
            {
                return;
            }

            YzAdParame adParame = new YzAdParame(yzAdLocation);
            float delay_show_time = YzUtils.getConfigFloatValue("interstitial_delay_show_time", 0);
            if (delay_show_time > 0)
            {
                YzUtils.showLog("延迟：" + delay_show_time + "秒，展示插屏广告 #location=" + yzAdLocation);
                YzTimer.SetTimeout(delay_show_time, () => { adAgent.showIntersititialAd(adParame); });
            }
            else
            {
                adAgent.showIntersititialAd(adParame);
            }
        }


        /// <summary>
        /// 展示插屏视频广告
        /// </summary>
        public void showIntersitialVideoAd()
        {
            if (!YzUtils.openAd) return;

            YzUtils.showLog("展示插屏视频广告");
            if (!YzUtils.checkCommonIsInit() || !checkShowAdTime() || checkIsRemoveAd("插屏视频"))
            {
                return;
            }

            adAgent.showIntersititialVideoAd();
        }


        /// <summary>
        /// 显示原生模版广告
        /// </summary>
        /// <param name="yzAdParame"></param>
        public void showCustomAd(YzAdParame yzAdParame)
        {
            if (!YzUtils.openAd) return;

            if (!YzUtils.checkCommonIsInit()) return;
            if (!checkShowAdTime()) return;
            YzUtils.showLog("显示原生模版广告: " + yzAdParame.location);
            adAgent.showCustomAd(yzAdParame);
        }

        /// <summary>
        /// 隐藏原生模版广告
        /// </summary>
        /// <param name="location">当前位置</param>
        public void hideCustomAd(int location = -1)
        {
            if (!YzUtils.openAd) return;

            if (!YzUtils.checkCommonIsInit()) return;

            YzUtils.showLog("隐藏原生模版广告: " + location);
            adAgent.hideCustomAd(location);
        }


        /// <summary>
        /// 展示原生悬浮ICON广告
        /// </summary>
        /// <param name="yzAdParame">节点配置</param>
        /// <returns></returns>
        public GameObject showNativeIconAd(YzAdParame yzAdParame)
        {
            if (!YzUtils.openAd) return null;

            YzUtils.showLog("展示原生悬浮ICON广告");
            if (PlatUtils.isDebug)
            {
                adAgent.showNativeIcon(yzAdParame);
            }
            else
            {
                if (!YzUtils.checkCommonIsInit() || !checkShowAdTime()) return null;

                if (YzUtils.getConfigIntValue("native_icon_refresh_time", -1) > 0)
                {
                    adAgent.showNativeIcon(yzAdParame);
                }
                else
                {
                    YzUtils.showLog("服务器未开启原生悬浮ICON");
                }
            }

            return null;
        }

        /// <summary>
        /// 隐藏原生悬浮ICON广告
        /// </summary>
        public void hideNativeIcon()
        {
            if (!YzUtils.openAd) return;

            if (!YzUtils.checkCommonIsInit()) return;

            YzUtils.showLog("隐藏原生悬浮ICON广告");
            adAgent.hideNativeIcon();
        }





        /// <summary>
        /// 验证是否购买去除广告功能
        /// </summary>
        /// <returns></returns>
        private bool checkIsRemoveAd(string adType)
        {
            bool isRemoveAd = YzUtils.checkIsRemoveAd();
            if (isRemoveAd)
            {
                YzUtils.showLog("已经购买了去除广告功能，不显示" + adType + "广告！");
            }

            return isRemoveAd;
        }

        /**
         * 验证广告显示时间是否达到
         * @returns true:大于服务器时间  false:小于服务器时间
         */
        private bool checkShowAdTime()
        {
            if (!YzUtils.checkCommonIsInit()) return false;

            if (YzUtils.getConfigFloatValue("first_show_ad_time", 0) > 0)
            {
                long curTime = YzUtils.ConvertDateTimeToLong();
                long interval = (curTime - YzUtils.luanch_time) / 1000;
                float showAdTime = YzUtils.getConfigFloatValue("first_show_ad_time", 0);
                YzUtils.showLog("验证当前广告显示时间：#first_show_ad_time=" + showAdTime + " #interval=" + interval);
                if (interval < showAdTime)
                {
                    return false;
                }
            }

            return true;
        }

        long _insertLastShowTime = 0;

        /**
         * 验证是否能够显示插屏
         * @returns true 可以显示，false 未达到要求不显示
         */
        private bool checkInsertAdTime()
        {
            if (!YzUtils.checkCommonIsInit()) return false;

            if (YzUtils.getConfigFloatValue("first_show_insert_time", 0) > 0)
            {
                long curTime = YzUtils.ConvertDateTimeToLong();
                long interval = (curTime - YzUtils.luanch_time) / 1000;
                float showAdTime = YzUtils.getConfigFloatValue("first_show_insert_time", 0);
                YzUtils.showLog("验证当前广告显示时间：#first_show_insert_time=" + showAdTime + " #interval=" + interval);
                if (interval < showAdTime)
                {
                    return false;
                }
            }

            if (YzUtils.getConfigFloatValue("interstitial_show_interval_time", 0) > 0)
            {
                long curTime = YzUtils.ConvertDateTimeToLong();
                long interval = (curTime - this._insertLastShowTime) / 1000;
                float showAdTime = YzUtils.getConfigFloatValue("interstitial_show_interval_time", 0);
                if (interval < showAdTime)
                {
                    YzUtils.showLog("验证当前插屏广告显示时间：#interstitial_show_interval_time=" + showAdTime + " #interval=" + interval);
                    return false;
                }
                this._insertLastShowTime = curTime;
            }

            return true;
        }


        //移动按钮Banner触发次数
        int moveBtnBannerShowCount = 0;
        //移动按钮Banner调用次数
        int moveBtnBannerRequestCount = 0;

        //当前是否执行移动按钮的Banner广告
        bool isMoveBtnBanner = false;
        //当前需要上移的按钮
        GameObject moveButton = null;

        /// <summary>
        /// 显示移动按钮的Banner广告
        /// 
        /// 执行示例：
        ///    比如打开游戏结算界面，需要移动的按钮为下一关，下一关的按钮位置需要先置底，
        ///    然后这个界面不允许调用showBanner,也不需要调用hideBanner,
        ///    然后直接调用这个方法，把下一关按钮赋值给参数即可
        /// </summary>
        /// <param name="location">当前页面位置</param>
        /// <param name="button">需要移动的按钮</param>
        public void ShowMoveBtnBanner(int location, GameObject button)
        {

            moveButton = button;
            if (PlatUtils.isWechat)
            {
                var move_btn_banner_show_interval = YzUtils.getConfigIntValue("move_btn_banner_show_interval");
                var move_btn_banner_delay_time = YzUtils.getConfigFloatValue("move_btn_banner_delay_time");
                var move_btn_banner_start_count = YzUtils.getConfigIntValue("move_btn_banner_start_count");
                var move_btn_banner_max_count = YzUtils.getConfigIntValue("move_btn_banner_max_count");

                var bannerShowCount = this.bannerShowCount;
                YzUtils.showLog("展示移动按钮的Banner广告： #bannerShowCount:" + bannerShowCount + " #moveBtnBannerShowCount=" + this.moveBtnBannerShowCount + " #move_btn_banner_show_interval=" + move_btn_banner_show_interval + " #move_btn_banner_max_count=" + move_btn_banner_max_count + " #move_btn_banner_delay_time=" + move_btn_banner_delay_time + " #move_btn_banner_start_count=" + move_btn_banner_start_count);
                if (
                    move_btn_banner_delay_time > 0
                    &&
                    (move_btn_banner_start_count == 0 || bannerShowCount > move_btn_banner_start_count)
                    )
                {
                    this.moveBtnBannerRequestCount++;

                    YzUtils.showLog("moveBtnBannerRequestCount: " + moveBtnBannerRequestCount);

                    if (
                        this.moveBtnBannerShowCount < move_btn_banner_max_count
                        &&
                        (this.moveBtnBannerRequestCount - 1) % (move_btn_banner_show_interval + 1) == 0
                        )
                    {
                        this.hideBannerAd();
                        YzUtils.showLog("触发移动Banner的按钮策略！");

                        YzUtils.showLog("YzTimer.SetTimeout move_btn_banner_delay_time: " + move_btn_banner_delay_time);

                        YzTimer.SetTimeout(move_btn_banner_delay_time, () =>
                        {
                            YzUtils.showLog("达到延迟出发展示Banner广告");

                            isMoveBtnBanner = true;//在banner的回调里自动执行moveButton
                            this.showBannerAd(location);
                        });
                        return;
                    }
                }
            }
            isMoveBtnBanner = false;
            YzUtils.showLog("Banner按钮被直接移动上去了");
            moveBannerAdButton();
            this.showBannerAd(location);
        }


        /// <summary>
        /// 移动按钮到Banner上方
        /// </summary>
        /// <param name="location"></param>
        /// <param name="button"></param>
        private void moveBannerAdButton()
        {
            isMoveBtnBanner = false;
            if (moveButton != null)
            {
                RectTransform rectTransform = moveButton.GetComponent<RectTransform>();
                Vector3 position = rectTransform.localPosition;
                if (PlatUtils.isDebug)
                {
                    bannerHeight = 160 + 10;
                }
                else
                {
                    bannerHeight += 10;
                }
                position.y += bannerHeight;
                rectTransform.localPosition = position;
            }
        }

        /// <summary>
        /// 统计广告展示数据
        /// </summary>
        /// <param name="adType"></param>
        /// <param name="ywAdStatus"></param>
        /// <param name="jsonData"></param>
        public void adShowListener(YwAdType adType, YwAdStatus ywAdStatus, JsonData jsonData)
        {
            YzUtils.showLog("AdManager adShowListener #type=" + (int)adType + " #status=" + (int)ywAdStatus);
            //YzUtils.showLog("AdManager 接收到广告消息 #type=" + (int)adType + " #status=" + (int)ywAdStatus + "#jsonData:" + (jsonData !=null ? JsonMapper.ToJson(jsonData):"null"));
            if (adType == YwAdType.BANNER || adType == YwAdType.NATIVE_BANNER || adType == YwAdType.NATIVE_TEMPLATE_BANNER)
            {
                if (ywAdStatus == YwAdStatus.SHOW_SUCCESS)
                {
                    this.bannerShowCount++;
                    if (jsonData != null && jsonData.ContainsKey("height"))
                    {
                        bannerHeight = float.Parse(jsonData["height"].ToString(), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        bannerHeight = 315;
                    }

                    if (isMoveBtnBanner)
                    {
                        moveBannerAdButton();
                    }

                }
            }
        }

        
        /**
        * 激励视频是否准备就绪
        */
        public bool rewardVideoIsReady()  {
            if(PlatUtils.isAndroid){
                return adAgent.rewardVideoIsReady();
            }
            return false;
        }

    }

}