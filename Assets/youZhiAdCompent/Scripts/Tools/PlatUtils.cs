using UnityEngine;


namespace YzAdComponent
{
    /// <summary>
    /// 平台辅助类
    /// </summary>
    public class PlatUtils

    {
        /// <summary> 是否iOS设备（iPhone或iPad）在WebGL环境下 </summary>
        public static bool isWebglIphone
        {
            get
            {
                if (isDebug) return false;

                return false;
            }
        }


        /// <summary> 是否开发测试平台 </summary>
        public static bool isDebug
        {
            get
            {
                return Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor;
            }
        }

        /// <summary> 是否安卓平台 </summary>
        public static bool isAndroid
        {
            get
            {
                if (isDebug) return false;
                return Application.platform == RuntimePlatform.Android;
            }
        }

        /// <summary> 是否iOS平台 </summary>
        public static bool isIOS
        {
            get
            {
                if (isDebug) return false;
                return Application.platform == RuntimePlatform.IPhonePlayer;
            }
        }


        /// <summary> 是否微信小游戏平台 </summary>
        public static bool isWechat
        {
            get
            {
                return false;
            }
        }

        /// <summary> 是否Oppo快游戏平台 </summary>
        public static bool isOppoKyx
        {
            get
            {
                return false;
            }
        }

        /// <summary> 是否VIVO快游戏平台 </summary>
        public static bool isVivoKyx
        {
            get
            {
                return false;
            }
        }

        /// <summary> 是否抖音小游戏平台 </summary>
        public static bool isDouyinKyx
        {
            get
            {
                return false;
            }
        }

        /// <summary> 是否快手小游戏平台 </summary>
        public static bool isKwaiKyx
        {
            get
            {
                return false;
            }
        }

        /// <summary> 是否华为快游戏平台 </summary>
        public static bool isHuaweiKyx
        {
            get
            {
                return false;
            }
        }

        /// <summary> 是否荣耀快游戏平台 </summary>
        public static bool isHonorKyx
        {
            get
            {
                return false;
            }
        }

        /// <summary> 是否tiktok快游戏平台 </summary>
        public static bool isTiktokKyx
        {
            get
            {
                return false;
            }
        }


        /// <summary> 原生平台渠道号 </summary>
        public static string native_channel = "";


    }


}