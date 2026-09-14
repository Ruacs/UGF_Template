using System.Collections;
using LitJson;
using UnityEngine;

namespace YzAdComponent
{

    /// <summary> Yz组件配置 </summary>
    public class YzCommonConfig
    {
        public JsonData youwanConfig = null;
        public YzOtherConfig otherConfig = new YzOtherConfig();

        public YzDebugConfig debugConfig = null;
        public YzNativeAndroidConfig androidConfig = null;

        /// <summary> 读取本地平台配置 </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool init_local_config(JsonData data)
        {
            if (data == null)
            {
                YzUtils.showLog("本地配置为空，验证失败！", YzLogType.Error);
                return false;
            }
            if (PlatUtils.isAndroid)
            {
                androidConfig = new YzNativeAndroidConfig();
                return androidConfig.init_config(data);
            }
            else
            {
                debugConfig = new YzDebugConfig();
                return debugConfig.init_config(data);
            }
        }

    }

}
