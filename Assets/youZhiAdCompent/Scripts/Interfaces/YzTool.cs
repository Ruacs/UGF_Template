using System;
using LitJson;

namespace YzAdComponent
{
    public interface YzTool
    {
        void init(JsonData configJsonData);
        //void ReportLevel(String levelStatus, int level);
        void jumpMoreGame();
        void jumpPravicy();
        void exitGame();
        void getDeviceInfo(JsonData jsonData, Action<bool, string> callBack);

        void showYzRealNameAuthPanel();

        void sendPay(ProductInfo productInfo, YzPayCallBack payCallBack);
        void queryAllProductDetail(YzQueryProductCallBack queryProductCallBack);

        void vibrateToTime(int msTime);

        void showGoodReview();


        void platLogin(Action suc, Action fail);


        bool canShortcut();
        void createShortcut(Action<bool> callback);
        void claimShortcut(Action<bool> callback);


        void checkCanSidebar(Action<bool> callback);
        void claimSidebar(Action<bool> callback);


        void login();
        void copyToClipboard(string content);
        void hideSplash();
    }

}