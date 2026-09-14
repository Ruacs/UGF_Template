using LitJson;

namespace YzAdComponent
{
    public class YzNativeAndroidConfig
    {
        /// <summary>
        /// APPID
        /// </summary>
        public string appId;

        /// <summary>
        /// 当前游戏版本号
        /// </summary>
        public string version = "1.0.0";


        /// <summary>
        /// 当前游戏渠道号
        /// </summary>
        public string channel = "1.0.0";


        public bool init_config(JsonData data)
        {

            if (data.ContainsKey("app_id"))
            {
                appId = data["app_id"].ToString();
            }

            if (data.ContainsKey("version"))
            {
                version = data["version"].ToString();
            }

            if (data.ContainsKey("channel"))
            {
                channel = data["channel"].ToString();
                PlatUtils.native_channel = channel;
            }

            return true;
        }

    }

}