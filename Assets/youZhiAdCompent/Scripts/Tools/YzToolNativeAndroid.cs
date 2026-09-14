using UnityEngine;
using System;
using LitJson;



namespace YzAdComponent
{
    public class YzToolNativeAndroid : YzTool
    {

        public YzToolNativeAndroid()
        {

        }

        //JsonData config = null;
        //string ST_ServerUrl = "http://apps.youlesp.com/gss?";
        //string POST_ServerUrl = "http://report.youlesp.com/gss?";
        //private int uid = 0; //用户ID
        //private string appId = "";
        //private string version = "";
        //private string channel = "";
        //private string deviceInfo = "";

        public void init(JsonData configJsonData)
        {
            if (!YzUtils.openAd) return;

            try
            {
                YzUtils.instance.emitServerInit();
            }
            catch (Exception e)
            {
                YzUtils.showLog("erro 初始化安卓端工具类异常！ #msg=" + e.Message);
            }
        }


        public void ReportLevel(string levelStatus, int level)
        {
            YzUtils.showLog("关卡上报：#levelStatus=" + levelStatus + " #level=" + level);
            AndroidJavaClassHelper.CallStatic("eventLevel", level, levelStatus);
        }

        public void jumpMoreGame()
        {
            YzUtils.showLog("点击了更多游戏挂件");
            AndroidJavaClassHelper.CallStatic("showNativeMoreGame");
        }

        public void jumpPravicy()
        {
            YzUtils.showLog("点击了隐私协议挂件");
            AndroidJavaClassHelper.CallStatic("showPrivacyAgreement");
        }

        /// <summary> 平台登录 </summary>
        public void platLogin(Action suc, Action fail)
        {
            YzUtils.showLog("调用平台登录功能");
            if (suc != null)
            {
                suc();
            }
        }


        /// <summary>
        /// 退出游戏功能
        /// </summary>
        public void exitGame()
        {
            YzUtils.showLog("调用退出游戏功能");
            AndroidJavaClassHelper.CallStatic("gameExit");
        }

        public void sendEvent(string key)
        {
            //string url = POST_ServerUrl + "m=revent&event=" + System.Net.WebUtility.UrlEncode(key);
            //url = buildHttpUrl(url);
            //YzUtils.sendGetRequest(url, new HttpRequestCallBack()
            //{
            //    onSuccess = (string result) =>
            //    {
            //        YzUtils.showLog("上报自定义事件成功 result:" + result);

            //    },
            //    onFail = (string result) =>
            //    {
            //        YzUtils.showLog("上报自定义事件失败 result:" + result);
            //    }
            //});
        }

        public void getDeviceInfo(JsonData deviceInfo, Action<bool, string> callBack)
        {

        }

        public void showYzRealNameAuthPanel()
        {
            YzUtils.showLog("调用原生实名制认证弹窗");
            AndroidJavaClassHelper.CallStatic("showRealNameAuthPanel", "");
        }

        public void sendPay(ProductInfo productInfo, YzPayCallBack payCallBack)
        {
            YzUtils.showLog("调用原生支付接口");
            AndroidJavaClassHelper.CallStatic("purchaseIntentReq", JsonMapper.ToJson(productInfo));
        }


        public void queryAllProductDetail(YzQueryProductCallBack queryProductCallBack)
        {
            YzUtils.showLog("调用原生查询商品接口");
            AndroidJavaClassHelper.CallStatic("queryAllProductDetail");
        }


        public void vibrateToTime(int msTime)
        {
            YzUtils.showLog("原生安卓震动:" + msTime + "毫秒");
            AndroidJavaClassHelper.CallStatic("vibrateToTime", msTime);
        }

        /**
         * 展示好评
         */
        public void showGoodReview()
        {
            YzUtils.showLog("展示好评弹窗");
            AndroidJavaClassHelper.CallStatic("jumpToHp");
        }

        public bool canShortcut()
        {
            return false;
        }
        public void createShortcut(Action<bool> callback)
        {
            if (YzUtils.callback_desktop != null)
            {
                YzUtils.callback_desktop(false);
                if (callback != null) callback(false);
            }
        }
        /** 检测能否领取桌面奖励 */
        public void claimShortcut(Action<bool> callback)
        {
            if (callback != null) callback(false);
        }

        public void checkCanSidebar(Action<bool> callback) { callback(false); }
        public void claimSidebar(Action<bool> callback) { callback(false); }

        /** 登录 */
        public void login()
        {
            AndroidJavaClassHelper.CallStatic("login");
        }
        
        public void copyToClipboard(string content)
        {
            AndroidJavaClassHelper.CallStatic("copyToClipboard", content);
        }

        /// <summary>隐藏开屏图</summary>
        public void hideSplash()
        {
            AndroidJavaClassHelper.CallStatic("hideSplash");
        }

    }

}

