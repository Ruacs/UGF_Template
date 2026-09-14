using System;
using UnityEngine;

namespace YzAdComponent
{
    /// <summary>
    /// 广告接口类
    /// </summary>
    public interface AdAgent
    {
        void init();

        ///<summary>
        ///展示Banner广告
        /// </summary>
        void showBannerAd(YzAdParame yzAdParame);


        ///<summary>
        ///隐藏Banner广告
        /// </summary>
        void hideBannerAd();


        ///<summary>
        ///显示当前Banner广告
        /// </summary>
        void showCurBannerAd();
        ///<summary>
        ///隐藏当前Banner广告
        /// </summary>
        void hideCurBannerAd();

        ///<summary>
        ///展示插屏广告
        /// </summary>
        void showIntersititialAd(YzAdParame yzAdParame);

        ///<summary>
        ///展示激励视频广告
        /// </summary>
        /// <param name="rewardVideoCallBack">视频回调</param>
        void showReardVideo(RewardVideoCallBack rewardVideoCallBack);


        ///<summary>
        ///展示原生悬浮ICON广告
        /// </summary>
        /// <param name="transform"></param>
        GameObject showNativeIcon(YzAdParame yzAdParame);


        /// <summary>
        /// 隐藏原生悬浮ICON
        /// </summary>
        /// <param name="yzAdLocationEnum"></param>
        void hideNativeIcon();


        /// <summary>
        /// 展示插屏视频广告
        /// </summary>
        void showIntersititialVideoAd();

        /// <summary>
        /// 初始化广告配置
        /// </summary>
        void initAdConfig();

        ///<summary>
        ///展示模版广告
        /// </summary>
        void showCustomAd(YzAdParame yzAdParame);

        ///<summary>
        ///隐藏模版广告
        /// </summary>
        void hideCustomAd(int location);

        /// <summary>激励视频是否准备就绪</summary>
        bool rewardVideoIsReady();
    }
}
