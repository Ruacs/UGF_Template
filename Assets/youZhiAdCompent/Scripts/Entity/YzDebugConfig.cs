using LitJson;

namespace YzAdComponent
{

    public class YzDebugConfig
    {
        /// <summary>
        /// APPID
        /// </summary>
        public string appId;

        /// <summary>
        /// 当前游戏版本号
        /// </summary>
        public string version = "1.0.0";

        public bool init_config(JsonData data)
        {
            return true;
        }

    }

}