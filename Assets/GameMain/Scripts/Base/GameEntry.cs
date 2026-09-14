using Ads;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public partial class GameEntry : MonoBehaviour
    {
        public GameObject customs;

        public static bool GMMode = false;

        public int LevelCount = 10;
        public static int MaxLevel = 10;

        public bool isDebug = false;
        public bool adCallbackSuccess = true;
        public bool rewardedAdReady = true;
        public bool networkReachable = true;
        public bool showLog = false;

        public static string GameName => "";
        public static bool ShowLog;
        public static bool AdCallbackSuccess;
        public static bool RewardedAdReady;
        public static bool NetworkReachable;

        private void Awake()
        {
            if (customs != null)
            {
                customs.SetActive(false);
            }
        }

        private void Start()
        {
            GMMode = isDebug;
            MaxLevel = LevelCount;
            SetShowLog(showLog);

            AdCallbackSuccess = adCallbackSuccess;
            RewardedAdReady = rewardedAdReady;
            NetworkReachable = networkReachable;

            InitBuiltinComponents();

            if (customs != null)
            {
                customs.SetActive(true);
            }


            InitCustomComponents();

            AdsManager.LaunchAdsComponent();
        }


        public static void SetShowLog(bool showLog)
        {
            ShowLog = showLog;

            YzAdComponent.YzUtils.show_log_to_console = showLog;
        }

        public static void Vibrate(long milliseconds = 50)
        {
            if (!GameEntry.SaveData.IsVibration) return;

            if (GMMode && Application.platform == RuntimePlatform.Android)
            {
                Debug.Log("震动了： " + milliseconds + "毫秒。");
                AndroidJavaClass vibrationService = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = vibrationService.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                vibrator.Call("vibrate", milliseconds);

            }
            else
            {
                YzAdComponent.YzUtils.Vibrate(milliseconds);
            }

        }
    }
}
