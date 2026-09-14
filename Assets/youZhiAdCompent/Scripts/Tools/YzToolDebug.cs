using LitJson;
using System;
using System.Collections.Generic;

namespace YzAdComponent
{
    public class YzToolDebug : YzTool
    {

        public YzToolDebug() { }



        public void init(JsonData configJsonData)
        {
            if (!YzUtils.openAd) return;

            YzGameAnalytics.login((ret, msg) =>
            {
                if (ret)
                {
                    YzUtils.showLog("测试模式拉取配置成功");
                }
                else
                {
                    YzUtils.showLog("测试模式拉取配置失败", YzLogType.Error);
                }
            });

        }
        public void ReportLevel(string levelStatus, int level)
        {
            YzUtils.showLog("关卡上报：#levelStatus=" + levelStatus + " #level=" + level + "，成功！");
        }

        public void jumpMoreGame()
        {
            YzUtils.showLog("点击了更多游戏挂件");
        }

        public void jumpPravicy()
        {
            YzUtils.showLog("点击了隐私协议挂件");
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

        /// <summary> 退出游戏功能 </summary>
        public void exitGame()
        {
            YzUtils.showLog("调用退出游戏功能");
        }

        public void sendEvent(string key)
        {

        }


        public void getDeviceInfo(JsonData deviceInfo, Action<bool, string> callBack)
        {
            callBack(true, "微信获取设备信息完成");
        }

        public void showYzRealNameAuthPanel()
        {
        }

        public void sendPay(ProductInfo productInfo, YzPayCallBack payCallBack)
        {
            if (payCallBack != null)
            {
                YzTimer.SetTimeout(3, () =>
                {
                    YzUtils.showLog("测试模式下，延迟3秒返回支付成功！");
                    payCallBack.onSuccess();
                });
            }
        }

        public void queryAllProductDetail(YzQueryProductCallBack queryProductCallBack)
        {
            List<ProductInfo> productInfos = new List<ProductInfo>();
            ProductInfo productInfo = new ProductInfo("10001", "去除广告", "HK 100.00");
            productInfos.Add(productInfo);
            queryProductCallBack.onSuccess(productInfos);
            YzUtils.showLog("测试模式下，返回测试商品信息！");
        }

        public void vibrateToTime(int msTime) { }

        public void showGoodReview()
        {
        }


        public bool canShortcut()
        {
            return true;
        }
        public void createShortcut(Action<bool> callback)
        {
            YzUtils.showLog("创建快捷桌面");
            if (YzUtils.callback_desktop != null)
            {
                YzUtils.showLog("创建快捷桌面成功");
                YzUtils.callback_desktop(true);
                if (callback != null) callback(true);
            }
        }
        /** 检测能否领取桌面奖励 */
        public void claimShortcut(Action<bool> callback)
        {
            if (callback != null) callback(true);
        }


        public void checkCanSidebar(Action<bool> callback)
        {
            callback(true);
        }
        public void claimSidebar(Action<bool> callback)
        {
            callback(true);
        }

        /** 登录 */
        public void login() { }

        public void copyToClipboard(string content) { }

        /// <summary>隐藏开屏图</summary>
        public void hideSplash() { }

    }
}
