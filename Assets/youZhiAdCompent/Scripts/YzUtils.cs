using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LitJson;
using AssemblyCSharp.Assets.Scripts.Common.Scripts.Interfaces;
using System.Runtime.InteropServices;
using System.Globalization;
using UnityEngine.EventSystems;
namespace YzAdComponent
{
    /// <summary>
    /// 正确的广告组件启动方式：YzUtils.LaunchAdCompent()详情见
    /// </summary>
    public class YzUtils : MonoBehaviour
    {
        // 组件版本号
        public static string version = "andriod_haiwai v1.0.4";

        #region 广告组件可手动设置选项
        // 是否开启广告
        public static bool openAd = true;
        // 是否输出到控制台
        public static bool show_log_to_console = false;
        // 是否输出到日志框
        public static bool is_show_log_view = false;
        // 是否开启测试代码
        public static bool openTestCode = false;
        /// 当前打包是否是 微信转oppo小游戏
        public static bool isPackWeChatToOppo = false;
        // UI设计分辨率的宽度 
        public static int designWidth = 1080;
        // UI设计分辨率的高度
        public static int designHeight = 1920;
        #endregion
        /// 当前屏幕分辨率与标准屏幕分辨率的比值，用来适配广告ui
        public static float screenResolutionRate = 1;
        public static float screenHeightRate = 1;
        public static float screenWidthRate = 1;
        private static bool kyxConfigIsInit = false;
        private static bool commonIsInit = false;
        public static long luanch_time = 0;
        public static YzUtils instance;
        public static YzAdManager adManager;
        public static YzTool yzTool;
        public static YzCommonConfig config = null;
        public static JsonData configJsonData;
        public static JsonData luanchData = null;
        public static JsonData onShowDate = null;
        private bool canClick_Btn_Pay_RemoveAd = true;
        private static ProductInfo curProductInfo = null;
        private static YzPayCallBack curPayCallBack = null;
        private static YzQueryProductCallBack curQueryProductCallBack = null;
        [Header("隐私协议"), Tooltip("请把Common/Prefabs/PrivacyPrefabs 挂到当前节点上！")]
        public GameObject privacyWidgetPrefabs;
        [Header("更多游戏"), Tooltip("请把Common/Prefabs/MoreGamesWidgetPrefabs 挂到当前节点上！")]
        public GameObject moreGamesWidgetPrefabs;
        // [Header("去除广告挂件"), Tooltip("请把Common/Prefabs/RemoveAdWidgetPrefabs 挂到当前节点上！")]
        // public GameObject removeAdWidgetPrefabs;
        // [Header("日志弹窗"), Tooltip("请把Common/Prefabs/LogViewPrefabs 挂到当前节点上！")]
        // public GameObject LogViewPrefabs;
        // [Header("隐私协议弹窗"), Tooltip("请把Common/Prefabs/PrvicyPanel 挂到当前节点上！")]
        // public GameObject privacyPanelPrefabs;
        // [Header("原生Banner"), Tooltip("请把Common/Prefabs/NativeBannerPrefabs 挂到当前节点上！")]
        // public GameObject nativeBannerAdPrefabs;
        // [Header("原生插屏"), Tooltip("请把Common/Prefabs/NativeIntersitialPrefabs 挂到当前节点上！")]
        // public GameObject nativeIntersitialAdPrefabs;
        // [Header("原生悬浮ICON"), Tooltip("请把Common/Prefabs/NativeIconPrefabs 挂到当前节点上！")]
        // public GameObject nativeIconAdPrefabs;
        // [Header("快捷桌面挂件"), Tooltip("请把Common/Prefabs/shortcutWidgetPrefabs 挂到当前节点上！")]
        // public GameObject shortcutWidgetPrefabs;
        // [Header("桌面弹窗"), Tooltip("请把Common/Prefabs/desktopPanelPrefabs 挂到当前节点上！")]
        // public GameObject desktopPanelPrefabs;
        // [Header("侧边栏挂件"), Tooltip("请把Common/Prefabs/sideBarWidgetPrefabs 挂到当前节点上！")]
        // public GameObject sideBarWidgetPrefabs;
        // [Header("侧边栏弹窗"), Tooltip("请把Common/Prefabs/sideBarPanelPrefabs 挂到当前节点上！")]
        // public GameObject sideBarPanelPrefabs;
        // [Header("宝箱弹窗"), Tooltip("请把Common/Prefabs/boxPagePrefabs 挂到当前节点上！")]
        // public GameObject boxPagePrefabs;
        // [Header("更多游戏弹窗"), Tooltip("请把Common/Prefabs/moreGamePagePrefabs 挂到当前节点上！")]
        // public GameObject moreGamePagePrefabs;


        /// <summary> 启动广告组件
        /// 首场景创建空物体YzUtilsPar，将组件预制体放入，取消激活显示。
        /// 在项目启动时第一句代码执行，不管关闭还是开启广告都要执行
        /// </summary>
        public static void LaunchAdCompent()
        {
            showLog("启动组件!");
            // 适配屏幕比例
            if (Screen.width < Screen.height)//竖屏
            {
                screenResolutionRate = (float)Screen.width / designWidth;
                screenHeightRate = (float)Screen.height / designHeight;
                screenWidthRate = (float)Screen.width / designWidth;
            }
            else//横屏
            {
                if (designWidth == 1080 && designHeight == 1920)
                {
                    designWidth = 1920;
                    designHeight = 1080;
                }
                screenResolutionRate = (float)Screen.height / designHeight;
                screenHeightRate = (float)Screen.height / designHeight;
                screenWidthRate = (float)Screen.width / designWidth;
            }
            showLog("设计分辨率：" + designWidth + "x" + designHeight);
            showLog("屏幕分辨率：" + Screen.width + "x" + Screen.height);
            showLog("屏幕分辨率比例：" + screenResolutionRate + " 屏幕高比例：" + screenHeightRate + " 屏幕宽比例：" + screenWidthRate);
            GameObject YzUtilsPar = GameObject.Find("YzUtilsPar");
            GameObject yzUtilsObj = YzUtilsPar.transform.Find("YzUtils").gameObject;
            if (YzUtils.instance == null)
            {
                DontDestroyOnLoad(YzUtilsPar);
                YzUtilsPar.name = "YzUtilsPar_Instance";
            }
            if (YzUtils.openAd)
            {
                if (YzUtils.instance == null)
                {
                    yzUtilsObj.SetActive(true);
                }
                else
                {
                    Destroy(YzUtilsPar);
                }
            }
            else
            {
                if (YzUtils.instance == null)
                {
                    Debug.Log("广告已关闭，启动关闭广告模式");
                    YzUtils.instance = YzUtilsPar.transform.Find("YzUtils").GetComponent<YzUtils>();
                    YzUtils.initAdManagerAndTool();
                }
                else
                {
                    Destroy(YzUtilsPar);
                }
            }
        }
        private static void initAdManagerAndTool()
        {
            YzUtils.adManager = new YzAdManager();
            if (PlatUtils.isAndroid)
            {
                showLog("当前平台为： Android");
                YzUtils.yzTool = new YzToolNativeAndroid();
            }
            else if (PlatUtils.isDebug)
            {
                showLog("当前平台为： 编辑器");
                YzUtils.yzTool = new YzToolDebug();
            }
            else
            {
                showLog("当前平台为： 未知");
            }
        }
        private void QuickGame_UnityInstance()
        {
        }
        private JsonData loadConfig()
        {
            if (PlatUtils.isAndroid)
            {
                var result = AndroidJavaClassHelper.CallStatic<string>("getLocalConfig");
                YzUtils.showLog("获取到原生端服务器配置:" + result);
                if (result != null && !result.Equals(""))
                {
                    configJsonData = JsonMapper.ToObject(result);
                }
                return configJsonData;
            }
            // else if (config.youwanConfig != null)
            // {
            //     YzUtils.showLog("获取快游戏优玩配置");
            //     // 返回快游戏平台的配置(用于出马甲包)
            //     return config.youwanConfig;
            // }
            else
            {
                //获取Json文件
                TextAsset jsonData = Resources.Load<TextAsset>("YzConfig/configs");
                configJsonData = JsonMapper.ToObject(jsonData.text);
                JsonData otherConfig = configJsonData["other"];
                YzUtils.showLog("本地other配置" + JsonMapper.ToJson(otherConfig));
                config.otherConfig.init_local_config(otherConfig);
                if (PlatUtils.isAndroid)
                {
                    return configJsonData;
                }
            }
            return configJsonData["test"];
        }
        //private LocalConfigModel configModel;
        void Awake()
        {
            // 广告canvas
            GetAdCanvas();
            // 初始化
            init();
        }
        private GameObject GetAdCanvas()
        {
            // 检查是否场景有 EventSystem
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
            GameObject canvasObj = GameObject.Find("AdCanvas");
            if (canvasObj != null)
            {
                return canvasObj;
            }
            // 创建根对象
            canvasObj = new GameObject("AdCanvas");
            // 添加核心组件
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 默认渲染模式
            canvas.sortingOrder = 1000;
            // 添加缩放适配组件
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            if (Screen.height >= Screen.width)//竖屏时
            {
                // scaler.referenceResolution = new Vector2(1080, 2400);
                scaler.referenceResolution = new Vector2(Screen.width, Screen.height);
                scaler.matchWidthOrHeight = 0;
            }
            else//横屏时
            {
                // scaler.referenceResolution = new Vector2(2400, 1080);
                scaler.referenceResolution = new Vector2(Screen.width, Screen.height);
                scaler.matchWidthOrHeight = 1;
            }
            // 添加射线检测组件（用于UI交互）
            canvasObj.AddComponent<GraphicRaycaster>();
            // 配置RectTransform（覆盖全屏）
            RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            // 设置为UI层
            canvasObj.layer = LayerMask.NameToLayer("UI");
            // 常驻
            DontDestroyOnLoad(canvasObj);
            return canvasObj;
        }
        private void init()
        {
            luanch_time = ConvertDateTimeToLong();
            if (instance == null)
            {
                showLog("优智广告组件， 版本：" + version);
                // 实例组件对象
                instance = this;
                // 初始化配置
                config = new YzCommonConfig();
                configJsonData = loadConfig();
                if (configJsonData != null && config.init_local_config(configJsonData))
                {
                    YzUtils.showLog("本地配置文件：" + JsonMapper.ToJson(configJsonData));
                    // 创建广告和工具
                    YzUtils.initAdManagerAndTool();
                    // 初始化平台和登录
                    YzUtils.yzTool.init(configJsonData);
                    // 注册服务器初始化
                    YzUtils.registerServerInitEvent(() =>
                    {
                        showLog("组件配置初始化完成，执行广告配置初始化！");
                        adManager.initAdConfig();
                        // 服务器自动控制自动弹广告
                        try
                        {
                            // 插屏：是否在进入游戏时展示
                            bool enterGameInterstitial = getConfigBooleanValue("enter_game_interstitial", false);
                            if (enterGameInterstitial)
                            {
                                showLog("服务器控制进游戏弹插屏");
                                YzUtils.adManager.showIntersititialAd(99999);
                            }
                            // 插屏间隔定时
                            float t1 = getConfigFloatValue("interstitial_interval_time", 0f);
                            if (t1 > 0f)
                            {
                                YzTimer.SetInterval(t1, () =>
                                {
                                    showLog("服务器控制自动弹插屏");
                                    YzUtils.adManager.showIntersititialAd(99999);
                                }, -1);
                            }
                            // 视频：进入游戏时展示
                            bool enterGameVideo = getConfigBooleanValue("enter_game_video", false);
                            if (enterGameVideo)
                            {
                                showLog("服务器控制进游戏弹视频");
                                YzUtils.adManager.showReardVideoAd();
                            }
                            // 视频间隔定时
                            float t2 = getConfigFloatValue("video_interval_time", 0f);
                            if (t2 > 0f)
                            {
                                YzTimer.SetInterval(t2, () =>
                                {
                                    showLog("服务器控制自动弹视频");
                                    YzUtils.adManager.showReardVideoAd();
                                }, -1);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            showLog("服务器自动控制自动弹广告 error: " + ex.Message);
                        }
                    });
                    // });
                }
                else
                {
                    YzUtils.showLog("本地配置验证失败，请先检查本地配置！", YzLogType.Error);
                    // 创建广告和工具
                    YzUtils.initAdManagerAndTool();
                }
            }
        }
        #region 注册初始化
        private static Action KyxConfigEvents;
        public static void registerKyxManagerConfigEvent(Action action)
        {
            if (kyxConfigIsInit)
            {
                action?.Invoke();
            }
            else
            {
                KyxConfigEvents += action;
            }
        }
        public void emitKyxConfigInit()
        {
            showLog("快游戏平台配置初始化！");
            kyxConfigIsInit = true;
            KyxConfigEvents?.Invoke();
            KyxConfigEvents = null;
        }
        public static bool checkKyxConfigIsInit()
        {
            if (!kyxConfigIsInit) showLog("快游戏平台配置未获取成功！！");
            return kyxConfigIsInit;
        }
        private static Action serverInitEvents;
        public static void registerServerInitEvent(Action action)
        {
            if (!openAd)
            {
                action?.Invoke();
                return;
            }
            if (yzTool == null)
            {
                showLog("组件未完成基础初始化，不执行回调监听");
                return;
            }
            if (commonIsInit)
            {
                action?.Invoke();
            }
            else
            {
                serverInitEvents += action;
            }
        }
        public void emitServerInit()
        {
            showLog("组件初始化成功！");
            commonIsInit = true;
            serverInitEvents?.Invoke();
            serverInitEvents = null;
        }

        public static bool checkCommonIsInit()
        {
            if (!commonIsInit) showLog("组件未初始化成功！");
            return commonIsInit;
        }
        #endregion
        #region 获取字段配置
        /// <summary> 验证配置中是否包含当前Key </summary>
        public static bool checkConfigHashKey(string key)
        {
            return configJsonData != null && configJsonData.ContainsKey(key);
        }
        /// <summary> 通过字段名称获取服务器字符串配置 </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        public static string getConfigStrValue(string key, string defaultValue = "")
        {
            if (!openAd)
            {
                return defaultValue;
            }
            if (checkConfigHashKey(key))
            {
                string val = configJsonData[key].ToString();
                YzUtils.showLog("服务器包含字段: " + key + "，返回值: " + val);
                return val;
            }
            else
            {
                YzUtils.showLog("服务器不包含字段: " + key + "，返回默认值: " + defaultValue);
                return defaultValue;
            }
        }
        /// <summary> 通过字段名称获取服务器整型配置 </summary>
        public static int getConfigIntValue(string key, int defaultValue = -1)
        {
            if (!openAd)
            {
                return defaultValue;
            }
            if (checkConfigHashKey(key))
            {
                int val = int.Parse(configJsonData[key].ToString(), CultureInfo.InvariantCulture);
                YzUtils.showLog("服务器包含字段: " + key + "，返回值: " + val);
                return val;
            }
            YzUtils.showLog("服务器不包含字段: " + key + "，返回默认值: " + defaultValue);
            return defaultValue;
        }
        /// <summary> 通过字段名称获取服务器浮点型配置 </summary>
        public static float getConfigFloatValue(string key, float defaultValue = -1f)
        {
            if (!openAd)
            {
                return defaultValue;
            }
            if (checkConfigHashKey(key))
            {
                float val = float.Parse(configJsonData[key].ToString(), CultureInfo.InvariantCulture);
                YzUtils.showLog("服务器包含字段: " + key + "，返回值: " + val);
                return val;
            }
            YzUtils.showLog("服务器不包含字段: " + key + "，返回默认值: " + defaultValue);
            return defaultValue;
        }
        /// <summary> 根据Key获取配置中的bool值 </summary>
        public static bool getConfigBooleanValue(string key, bool defaultValue = false)
        {
            if (!openAd)
            {
                return defaultValue;
            }
            if (checkConfigHashKey(key))
            {
                if (configJsonData[key].ToString() == "true")
                {
                    YzUtils.showLog("服务器包含字段: " + key + "，返回值: " + true);
                    return true;
                }
                if (configJsonData[key].ToString() == "false")
                {
                    YzUtils.showLog("服务器包含字段: " + key + "，返回值: " + false);
                    return false;
                }
                bool val = bool.Parse(configJsonData[key].ToString());
                YzUtils.showLog("服务器包含字段: " + key + "，返回值: " + val);
                return val;
            }
            YzUtils.showLog("服务器不包含字段: " + key + "，返回默认值: " + defaultValue);
            return defaultValue;
        }
        /// <summary> 根据Key获取配置中的JSON数组值 </summary>
        public static JsonData getConfigJsonArryValue(string key)
        {
            if (!openAd)
            {
                return new JsonData { };
            }
            if (checkConfigHashKey(key))
            {
                JsonData val = configJsonData[key];
                YzUtils.showLog("服务器包含字段: " + key + "，返回值: " + val);
                return val;
            }
            YzUtils.showLog("服务器不包含字段: " + key + "，返回默认值json: {}");
            return new JsonData { };
        }
        #endregion
        #region 调用js
        /// <summary> 平台登录 </summary>
        public static void platLogin(Action suc, Action fail)
        {
            if (yzTool != null) yzTool.platLogin(suc, fail);
        }
        /// <summary> 退出游戏功能 </summary>
        public static void exitGame()
        {
            if (!openAd)
            {
                return;
            }
            if (yzTool != null) yzTool.exitGame();
        }
        public static void sendGetRequest(string url, HttpRequestCallBack callBack)
        {
            instance.StartCoroutine(YzHttpRequest.GetRequest(url, callBack));
        }
        public static void sendPostRequest(string url, WWWForm form, HttpRequestCallBack callBack)
        {
            instance.StartCoroutine(YzHttpRequest.PostRequest(url, form, callBack));
        }
        /// <summary> 快游戏端JS脚本上报广告事件 </summary>
        /// <param name="adType"></param>
        /// <param name="adStatus"></param>
        public void KyxEventAd(string _jsonData)
        {
        }
        /// <summary> 快游戏端JS脚本上报广告带参数事件 </summary>
        /// <param name="adType"></param>
        /// <param name="adStatus"></param>
        public void KyxEventAdWithObj(string _jsonData)
        {
        }
        /// <summary> 调用快游戏端JS脚本展示激励视频广告 </summary>
        /// <param name="param">位置</param>
        public static void showKyxRewardVideo()
        {
        }
        /// <summary> 调用快游戏端JS脚本展示插屏广告 </summary>
        /// <param name="param">位置</param>
        public static void showKyxIntersititialAd(int param)
        {
        }
        /// <summary> 调用快游戏端JS脚本展示Banner广告 </summary>
        /// <param name="param">位置</param>
        public static void showKyxBannerAd(int param)
        {
        }
        /// <summary> 调用快游戏端JS脚本隐藏Banner广告 </summary>
        public static void hideKyxBannerAd()
        {
        }
        /// <summary> 调用快游戏端JS脚本Banner广告显示 </summary>
        public static void showCurKyxBannerAd()
        {
        }
        /// <summary> 调用快游戏端JS脚本Banner广告隐藏 </summary>
        public static void hideCurKyxBannerAd()
        {
        }
        /// <summary> 调用快游戏端JS脚本展示原生模版广告 </summary>
        /// <param name="param">位置</param>
        public static void showKyxCustomAd(string param)
        {
        }
        /// <summary> 调用快游戏端JS脚本隐藏原生模版广告 </summary>
        /// <param name="location">位置:-1表示隐藏所有</param>
        public static void hideKyxCustomAd(int location = -1)
        {
        }
        /// <summary> 跳转到小游戏 </summary>
        /// <param name="gameId">游戏ID</param>
        public static void goMiniGame(string gameId)
        {
            showLog("跳转到小游戏 #gameId：" + gameId);
        }
        /// <summary> 跳转到小游戏 </summary>
        public static string kyxSystemInfo()
        {
            return "";
        }
        #endregion
        #region 回调
        /// <summary> ManagerConfig回调 </summary>
        public void onManagerConfigCallBack(string result)
        {
            showLog("快游戏平台优玩配置回调:" + result);
            JsonData data = JsonMapper.ToObject(result);
            if (data.ContainsKey("YouwanConfig"))
            {
                config.youwanConfig = data["YouwanConfig"];
                config.otherConfig.init_youwan_cofig(config.youwanConfig);
                emitKyxConfigInit();
            }
        }
        /// <summary> onShow结果回调 </summary>
        public void onShowCallBack(string result)
        {
            showLog("onShowCallBack result：" + result);
            onShowDate = JsonMapper.ToObject(result);
        }
        /// <summary> 激励视频结果回调 </summary>
        public void rewardVideoAdCallBack(string result)
        {
            showLog("rewardVideoAdCallBack result：" + result);
            if (adManager != null) adManager.rewardVideoAdCallBack(result);
        }
        /// <summary> 支付结果回调 </summary>
        public void payCallBack(string result)
        {
            if (!openAd)
            {
                return;
            }
            var resultData = JsonMapper.ToObject(result);
            int code = int.Parse(resultData["code"].ToString(), CultureInfo.InvariantCulture);
            string msg = resultData["msg"].ToString();
            YzUtils.showLog("payCallBack #code=" + code + " #msg=" + msg);
            if (code == 1)
            {
                //YzUtils.hideRemoveAdWidget();
                YzUtils.adManager.hideBannerAd();
                YZLocalStorage.setStringItem("remove_ad", "true");
            }
            else
            {
            }
        }
        #endregion
        #region 协议
        /// <summary> 显示实名认证弹窗，目前只支持原生端，所有UI都是原生端负责展示 </summary>
        public static void showYzRealNameAuthPanel()
        {
            if (!openAd)
            {
                return;
            }
            if (PlatUtils.isAndroid)
            {
                int showPanel = getConfigIntValue("yz_game_real_name", 0);
                if (showPanel < 0)
                {
                    showLog("服务器配置不显示实名弹窗！");
                    return;
                }
                if (yzTool != null) yzTool.showYzRealNameAuthPanel();
            }
            showLog("当前平台不显示实名弹窗！");
        }
        /// <summary> 是否显示用户协议挂件 </summary>
        private static bool isShowPrivacyWidget()
        {
            if (!YzUtils.openAd) return false;
            if (PlatUtils.isDebug || PlatUtils.isTiktokKyx)
            {
                return getConfigBooleanValue("is_privacy", true);
            }
            else
            {
                return getConfigBooleanValue("is_privacy", false);
            }
        }
        private static GameObject _privacyWidget = null;
        /// <summary> 显示隐私协议挂件 </summary>
        public static void showPrivacyWidget(YzAdParame adParame)
        {
            if (!openAd)
            {
                return;
            }
            showLog("ShowPrivacyWidget #adParame=" + (adParame != null ? adParame.ToString() : "null"));
            if (adParame == null)
            {
                showLog("erro , 传入节点的参数异常");
                return;
            }
            if (!isShowPrivacyWidget())
            {
                return;
            }
            if (instance.privacyWidgetPrefabs != null)
            {
                Destroy(_privacyWidget);
                _privacyWidget = Instantiate(instance.privacyWidgetPrefabs, adParame.parent);
                setNodeTransform(_privacyWidget, adParame);
                Text textComponent = _privacyWidget.GetComponentInChildren<Text>();
                if (textComponent != null)
                {
                    // 使用 adParame.color 设置文字颜色
                    if (adParame.color != default)
                    {
                        textComponent.color = adParame.color;
                    }
                    else
                    {
                        textComponent.color = Color.black;
                    }
                    // 根据系统语言设置文字
                    if (curLanguage == "zh")
                    {
                        textComponent.text = "《隐私协议》";
                    }
                    else
                    {
                        textComponent.text = "<<Privacy Policy>>";
                    }
                }
                else
                {
                    Debug.LogWarning("未找到隐私协议Text组件");
                }
            }
            else
            {
                showLog("erro , 隐私协议挂件预制体不存在！");
            }
        }
        /// <summary> 隐藏隐私协议挂件 </summary>
        public static void hidePrivacyWidget()
        {
            if (!openAd)
            {
                return;
            }
            showLog("隐藏隐私协议挂件！");
            if (_privacyWidget != null)
            {
                Destroy(_privacyWidget);
                _privacyWidget = null;
            }
        }
        /// <summary> 是否显示用户协议弹窗 </summary>
        private static bool isShowPrivacyPanel()
        {
            if (!YzUtils.openAd) return false;
            if (false)
            {
                // 默认显示
                if (getConfigBooleanValue("is_privacy_panel", true))
                {
                    return true;
                }
            }
            else
            {
                // 默认不显示
                if (getConfigBooleanValue("is_privacy_panel", false))
                {
                    return true;
                }
            }
            showLog("warn:" + "用户协议弹窗不显示！");
            return false;
        }
        private static GameObject _privacyPanel = null;
        /// <summary> 展示隐私协议弹窗 </summary>
        public static void showPrivacyPanel(bool isWidgetClick = false)
        {
            // if (!YzUtils.openAd) return;
            // // 非手动开启弹窗
            // if (!isWidgetClick)
            // {
            //     if (!YzUtils.isShowPrivacyPanel())
            //     {
            //         return;
            //     }
            //     var privacyIsOk = YZLocalStorage.getIntItem(YzConstant.ST_KEY_PRIVACY_PANEL, -1);
            //     showLog("[showPrivacyPanel] #privacyIsOk=" + privacyIsOk);
            //     if (privacyIsOk > 0)
            //     {
            //         emitPrivacyEvent();
            //         return;
            //     }
            // }
            // else
            // {
            //     showLog("手动点击隐私挂件,展示隐私协议内容");
            // }
            // if (instance.privacyPanelPrefabs != null)
            // {
            //     if (_privacyPanel != null)
            //     {
            //         Destroy(_privacyPanel);
            //     }
            //     _privacyPanel = Instantiate(instance.privacyPanelPrefabs);
            //     _privacyPanel.GetComponent<PrivacyPanel>().isWidgetClick = isWidgetClick;
            //     GameObject canvas = YzUtils.instance.GetAdCanvas();
            //     RectTransform parentRectTransform = canvas.gameObject.GetComponent<RectTransform>();
            //     _privacyPanel.transform.SetParent(canvas.transform, false);
            //     RectTransform rectTransform = _privacyPanel.GetComponent<RectTransform>();
            //     float ratio = 1;
            //     float width = 1080;
            //     if (Screen.height < Screen.width)
            //     {
            //         ratio = Screen.width / 1920 * 0.7f;
            //     }
            //     else
            //     {
            //         ratio = Screen.width / 1080;
            //         width *= ratio;
            //     }
            //     width *= ratio;
            //     float height = rectTransform.rect.height * ratio;
            //     if (ratio <= 0)
            //     {
            //         ratio = 1;
            //     }
            //     ratio = 1;
            //     rectTransform.localScale = new Vector3(ratio, ratio, ratio);
            //     Vector3 position = new Vector3();
            //     position.y = 0;
            //     //rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, nodeConfig.bottom, height);
            //     position.x = 0;
            //     rectTransform.localPosition = position;
            // }
            // else
            // {
            //     showLog("隐私协议弹窗预制体不存在！");
            // }
        }
        private static Action privacyPanelCloseEvents;
        /// <summary> 添加隐私协议弹窗关闭事件监听 </summary>
        public static void registerPrivacyPanelCloseEvent(Action action)
        {
            if (YzUtils.isShowPrivacyPanel())
            {
                var privacyIsOk = YZLocalStorage.getIntItem(YzConstant.ST_KEY_PRIVACY_PANEL, -1);
                showLog("[registerPrivacyPanelCloseEvent] #privacyIsOk=" + privacyIsOk);
                if (privacyIsOk > 0)
                {
                    action?.Invoke();
                }
                else
                {
                    privacyPanelCloseEvents += action;
                }
            }
            else
            {
                action?.Invoke();
            }
        }
        /// <summary> 发送隐私协议弹窗关闭事件 </summary>
        public static void emitPrivacyEvent()
        {
            showLog("隐私弹窗关闭！ ");
            privacyPanelCloseEvents?.Invoke();
            privacyPanelCloseEvents = null;
        }
        #endregion
        #region 原生广告
        private GameObject _nativeBannerAd = null;
        /// <summary> 创建原生Banner节点 </summary>
        public void createNativeBanner()
        {
            showLog("[createNativeBanner]");
            //             if (instance.nativeBannerAdPrefabs != null)
            //             {
            //                 if (this._nativeBannerAd != null)
            //                 {
            //                     Destroy(this._nativeBannerAd);
            //                 }
            //                 _nativeBannerAd = Instantiate(nativeBannerAdPrefabs);
            //                 GameObject canvas = YzUtils.instance.GetAdCanvas();
            //                 RectTransform parentRectTransform = canvas.gameObject.GetComponent<RectTransform>();
            //                 _nativeBannerAd.transform.SetParent(canvas.transform, false);
            //                 RectTransform rectTransform = _nativeBannerAd.GetComponent<RectTransform>();
            //                 float ratio = 1;
            //                 if (canvas.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace)//当Canvas使用世界坐标模式时，即3d ui 模式
            //                 {
            //                     if (parentRectTransform.rect.height < parentRectTransform.rect.width)//横屏
            //                     {
            //                         ratio = parentRectTransform.rect.width / 1920f;
            //                     }
            //                     else//竖屏
            //                     {
            //                         ratio = parentRectTransform.rect.width / 1080f;
            //                     }
            //                 }
            //                 else//Canvas为常规模式时
            //                 {
            // #if UNITY_EDITOR
            //                     float width = UnityEditor.Handles.GetMainGameViewSize().x;
            //                     float height = UnityEditor.Handles.GetMainGameViewSize().y;
            //                     if (height < width)//横屏
            //                     {
            //                         ratio = width / 1920f;
            //                     }
            //                     else//竖屏
            //                     {
            //                         ratio = width / 1080f;
            //                     }
            // #endif
            //                 }
            //                 if (ratio <= 0)
            //                 {
            //                     ratio = 1;
            //                 }
            //                 if (Screen.height < Screen.width)//横屏
            //                 {
            //                     rectTransform.localScale = new Vector3(ratio, ratio * 1.47f, ratio);
            //                 }
            //                 else//竖屏
            //                 {
            //                     rectTransform.localScale = new Vector3(ratio, ratio * 1.8f, ratio);
            //                 }
            //                 return _nativeBannerAd.GetComponent<NativeBannerAd>();
            //             }
            //             else
            //             {
            //                 showLog("原生Banner预制体不存在！");
            //                 return null;
            //             }
        }
        public void destroyNativeBanner()
        {
            if (_nativeBannerAd != null)
            {
                Destroy(_nativeBannerAd);
            }
        }
        private GameObject _nativeIntersitialAd = null;
        /// <summary> 创建原生插屏节点 </summary>
        public void createNativeIntersitial()
        {
            showLog("[createNativeIntersitial]");

            // if (instance.nativeIntersitialAdPrefabs != null)
            // {
            //     if (this._nativeIntersitialAd != null)
            //     {
            //         Destroy(this._nativeIntersitialAd);
            //     }
            //     _nativeIntersitialAd = Instantiate(nativeIntersitialAdPrefabs);
            //     GameObject canvas = YzUtils.instance.GetAdCanvas();
            //     RectTransform parentRectTransform = canvas.gameObject.GetComponent<RectTransform>();
            //     _nativeIntersitialAd.transform.SetParent(canvas.transform, false);
            //     RectTransform rectTransform = _nativeIntersitialAd.GetComponent<RectTransform>();
            //     float ratio = 1;
            //     float width = 1080;
            //     if (Screen.height < Screen.width)//横屏
            //     {
            //         ratio = (float)Screen.width / 1920f * 0.7f;
            //     }
            //     else//竖屏
            //     {
            //         ratio = (float)Screen.width / 1080f;
            //         width *= ratio;
            //     }
            //     width *= ratio;
            //     float height = rectTransform.rect.height * ratio;
            //     if (ratio <= 0)
            //     {
            //         ratio = 1;
            //     }
            //     rectTransform.localScale = new Vector3(ratio, ratio, ratio);
            //     Vector3 position = new Vector3();
            //     position.y = 0;
            //     //rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, nodeConfig.bottom, height);
            //     position.x = 0;
            //     rectTransform.localPosition = position;
            //     return _nativeIntersitialAd.GetComponent<NativeIntersititialAd>();
            // }
            // else
            // {
            //     showLog("原生插屏预制体不存在！");
            //     return null;
            // }
        }
        private GameObject _nativeIconAd = null;
        /// <summary> 创建原生悬浮ICON广告 </summary>
        public void createNativeIcon(YzAdParame YzAdParame)
        {
            showLog("[createNativeIcon]");

            // if (instance.nativeIconAdPrefabs != null)
            // {
            //     if (this._nativeIconAd != null)
            //     {
            //         Destroy(this._nativeIconAd);
            //         this._nativeIconAd = null;
            //     }
            //     this._nativeIconAd = Instantiate(nativeIconAdPrefabs);
            //     setNodeTransform(this._nativeIconAd, YzAdParame);
            //     return this._nativeIconAd.GetComponent<NativeIconAd>();
            // }
            // else
            // {
            //     showLog("原生插屏预制体不存在！");
            //     return null;
            // }
        }
        public void destroyNativeIcon()
        {
            if (_nativeIconAd != null)
            {
                Destroy(_nativeIconAd);
            }
        }
        public static GameObject _boxPage = null;
        /// <summary> 宝箱弹窗 </summary>
        public static void showBoxPage(Action closeCallFunc = null, Action rewardCallFunc = null)
        {
            // if (PlatUtils.isDebug || YzUtils.getConfigBooleanValue("isBoxPage", false))
            // {
            //     // 回调
            //     if (instance.boxPagePrefabs != null)
            //     {
            //         if (_boxPage != null)
            //         {
            //             Destroy(_boxPage);
            //             _boxPage = null;
            //         }
            //         _boxPage = Instantiate(instance.boxPagePrefabs);
            //         _boxPage.GetComponent<BoxPage>().closeCallFunc = closeCallFunc;
            //         _boxPage.GetComponent<BoxPage>().rewardCallFunc = rewardCallFunc;
            //         GameObject canvas = YzUtils.instance.GetAdCanvas();
            //         _boxPage.transform.SetParent(canvas.transform, false);
            //     }
            //     else
            //     {
            //         showLog("宝箱预制体不存在！");
            //         if (closeCallFunc != null)
            //         {
            //             closeCallFunc();
            //         }
            //     }
            // }
            // else
            // {
            //     showLog("服务器控制不显示宝箱弹窗！");
            //     if (closeCallFunc != null)
            //     {
            //         closeCallFunc();
            //     }
            // }
        }
        public static GameObject _moreGamePage = null;
        /// <summary> 更多游戏弹窗 </summary>
        public static void showMoreGamePage(Action closeCallFunc = null, Action rewardCallFunc = null)
        {
            // if (PlatUtils.isDebug || YzUtils.getConfigBooleanValue("isMoreGamePage", false))
            // {
            //     if (instance.moreGamePagePrefabs != null)
            //     {
            //         if (_moreGamePage != null)
            //         {
            //             Destroy(_moreGamePage);
            //             _moreGamePage = null;
            //         }
            //         _moreGamePage = Instantiate(instance.moreGamePagePrefabs);
            //         _moreGamePage.GetComponent<MoreGamePage>().closeCallFunc = closeCallFunc;
            //         _moreGamePage.GetComponent<MoreGamePage>().rewardCallFunc = rewardCallFunc;
            //         GameObject canvas = YzUtils.instance.GetAdCanvas();
            //         _moreGamePage.transform.SetParent(canvas.transform, false);
            //     }
            //     else
            //     {
            //         showLog("宝箱预制体不存在！");
            //         if (closeCallFunc != null)
            //         {
            //             closeCallFunc();
            //         }
            //     }
            // }
            // else
            // {
            //     showLog("服务器控制不显示宝箱弹窗！");
            //     if (closeCallFunc != null)
            //     {
            //         closeCallFunc();
            //     }
            // }
        }
        #endregion
        #region 去广告

        /// <summary> 是否支付过去除广告功能 </summary>
        public static bool Has_Pay_RemoveAd
        {
            get
            {
                return YZLocalStorage.getStringItem("HasPayRemoveAd", "false") == "true";
            }
        }
        //支付去除广告功能使用示例：
        //1，先完成支付去除广告的ui，
        //2，项目启动时，判断存档 YzUtils.Has_Pay_RemoveAd 来检测是否未支付过，以及是否有服务器字段控制开启显示，
        //   都满足则开启ui,否则隐藏
        //3, 将YzUtils.instance.Btn_Pay_RemoveAd（）支付去除广告功能添加到去除广告按钮的方法里。
        //4, 当支付成功时，在Btn_Pay_RemoveAd（）成功回调里隐藏去除广告相关的ui

        /// <summary> 支付去除广告功能
        /// 将该方法绑定到ui上的支付去除广告按钮,
        /// 然后当支付成功时，在成功回调里隐藏去除广告相关的ui即可
        /// </summary>
        /// <param name="onSuccess"></param>
        public void Btn_Pay_RemoveAd(Action onSuccess)
        {
            if (!YzUtils.openAd) return;
            if (Has_Pay_RemoveAd)
            {
                YzUtils.showLog("已支付过去除广告。");
                return;
            }
            if (canClick_Btn_Pay_RemoveAd)
            {
                canClick_Btn_Pay_RemoveAd = false;
                YzUtils.registerServerInitEvent(() =>
                {
                    //发起支付，示例是根据指定去除广告商品，
                    //重点注意商品名称必须固定传入"no_ad",组件做了相关去除广告的逻辑。
                    //需要在回调中隐藏“去除广告的挂件按钮”，之后不再展示
                    ProductInfo productInfo = new ProductInfo();
                    productInfo.pid = "10001";
                    productInfo.name = "no_ad";
                    YzPayCallBack yzPayCallBack = new YzPayCallBack();
                    yzPayCallBack.onFail = (msg) =>
                    {
                        canClick_Btn_Pay_RemoveAd = true;
                        YzUtils.showMsg("支付失败：" + msg);
                    };
                    yzPayCallBack.onSuccess = () =>
                    {
                        //隐藏去除广告挂件
                        YzUtils.showMsg("支付成功，发送道具");
                        YZLocalStorage.setStringItem("HasPayRemoveAd", "true");
                        onSuccess?.Invoke();
                    };
                    YzUtils.purchase(productInfo, yzPayCallBack);
                });
            }
        }

        /** 是否显示去除广告挂件  */
        private static bool isShowRemoveAdWidget()
        {
            if (!checkCommonIsInit()) return false;
            if (YZLocalStorage.getStringItem("remove_ad", "false") == "true")
            {
                showLog("已经购买了去除广告功能，去除广告挂件组件不显示！");
                return false;
            }
            if (PlatUtils.isDebug || getConfigBooleanValue("is_remove_ad", false))
            {
                return true;
            }
            else
            {
                showLog("warn:" + "去除广告挂件组件不显示！");
            }
            return false;
        }
        private static GameObject _removeAdWidget = null;
        /// <summary> 显示去除广告挂件 </summary>
        public static void showRemoveAdWidget(YzAdParame adParame)
        {
            if (!openAd)
            {
                return;
            }
            showLog("showRemoveAdWidget #adParame=" + (adParame != null ? adParame.ToString() : "null"));
            if (adParame == null)
            {
                showLog("erro , 传入节点的参数异常");
                return;
            }
            if (!isShowRemoveAdWidget())
            {
                return;
            }
            if (PlatUtils.isAndroid)
            {
                adParame.parent = null;
                AndroidJavaClassHelper.CallStatic("showRemoveAdWiget", JsonMapper.ToJson(adParame));
            }
            // else
            // {
            //     if (instance.removeAdWidgetPrefabs != null)
            //     {
            //         Destroy(_removeAdWidget);
            //         _removeAdWidget = Instantiate(instance.removeAdWidgetPrefabs, adParame.parent);
            //         setNodeTransform(_removeAdWidget, adParame);
            //     }
            //     else
            //     {
            //         showLog("erro , 去除广告挂件预制体不存在！");
            //     }
            // }
        }
        /// <summary> 隐藏去除广告挂件 </summary>
        public static void hideRemoveAdWidget()
        {
            if (!openAd)
            {
                return;
            }
            showLog("隐藏去除广告挂件！");
            if (PlatUtils.isAndroid)
            {
                AndroidJavaClassHelper.CallStatic("hideRemoveAdWiget");
            }
            // else
            // {
            //     if (_removeAdWidget != null)
            //     {
            //         Destroy(_removeAdWidget);
            //     }
            // }
        }
        #endregion
        #region 支付

        /**
         * 检测是否内购开启
         */
        public static bool checkPurchaseEnabled()
        {
            if (YzUtils.checkConfigHashKey("product_configs"))
            {
                var productConfigs = YzUtils.getConfigJsonArryValue("product_configs");
                if (productConfigs != null && productConfigs.IsArray && productConfigs.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary> 查询所有商品信息，在组件初始化成功后立即调用，获取成功后本地商品列表数据要通过ID匹配，最终显示给用户的价格更新成查询结果中的价格 </summary>
        /// <param name="queryProductCallBack">onSuccess 查询成功回调，返回Product数组；onFail 查询失败回调(返回查询失败信息msg，不用展示给用户)</param>
        public static void queryAllProductDetail(YzQueryProductCallBack queryProductCallBack)
        {
            if (commonIsInit)
            {
                curQueryProductCallBack = queryProductCallBack;
                yzTool.queryAllProductDetail(queryProductCallBack);
            }
            else
            {
                if (queryProductCallBack != null)
                {
                    queryProductCallBack.onFail("Product Not Found!");
                }
            }
        }

        /// <summary> 查询所有商品结果回调 </summary>
        /// <param name="result"></param>
        public void queryProductCallBack(string result)
        {
            YzUtils.showLog("purchaseCallBack #result=" + result);
            var resultData = JsonMapper.ToObject(result);
            int resultType = int.Parse(resultData["code"].ToString(), CultureInfo.InvariantCulture);
            string msg = resultData.ContainsKey("msg") ? resultData["msg"].ToString() : "";
            YzUtils.showLog("purchaseCallBack #resultType=" + resultType + " #msg=" + msg);
            if (resultType == 1)
            {
                if (resultData.ContainsKey("data"))
                {
                    List<ProductInfo> productInfos = new List<ProductInfo>();
                    JsonData datas = resultData["data"];
                    foreach (JsonData productObj in datas)
                    {
                        ProductInfo productInfo = new ProductInfo();
                        productInfo.name = productObj["name"].ToString();
                        productInfo.pid = productObj["id"].ToString();
                        productInfo.formattedPrice = productObj["formattedPrice"].ToString();
                        productInfos.Add(productInfo);
                        YzUtils.showLog("queryProductInfo: " + productInfo.ToString());
                    }
                    if (curQueryProductCallBack != null)
                    {
                        curQueryProductCallBack.onSuccess(productInfos);
                        curQueryProductCallBack = null;
                    }
                }
                else
                {
                    curQueryProductCallBack.onFail(msg == null ? "Product Not Fount" : msg);
                    curQueryProductCallBack = null;
                }
            }
            else
            {
                if (curQueryProductCallBack != null)
                {
                    curQueryProductCallBack.onFail(msg == null ? "Product Not Fount!" : msg);
                    curQueryProductCallBack = null;
                }
            }
        }


        /// <summary> 发起支付 </summary>
        /// <param name="productInfo">商品信息 ProductInfo {"id": "商品ID","name":商品名称 ....} 去除广告的商品名称统一传入“no_ad”，方便组件统一做广告限制</param>
        /// <param name="payCallBack">onSuccess 支付成功回调，收到回调后展示对应支付成功或者获取奖励的UI；onFail 支付失败回调(返回支付失败详细信息，必须展示给用户)</param>
        public static void purchase(ProductInfo productInfo, YzPayCallBack payCallBack)
        {
            if (!YzUtils.checkCommonIsInit())
            {
                Debug.LogWarning("广告组件未初始化成功");
                if (payCallBack != null)
                {
                    payCallBack.onFail("Product Not Found!");
                }
                return;
            }
            if (productInfo == null || !openAd || yzTool == null)
            {
                if (!openAd)
                {
                    YzTimer.SetTimeout(3, () =>
                    {
                        if (payCallBack != null)
                        {
                            payCallBack.onSuccess();
                        }
                    });
                    YzUtils.showLog("测试模式延迟3秒返回支付成功");
                }
                else
                {
                    if (payCallBack != null)
                    {
                        payCallBack.onFail("Product Not Found!");
                    }
                }
                return;
            }
            YzUtils.showLog("purchase: #payInfo=" + productInfo.ToString());
            curProductInfo = productInfo;
            curPayCallBack = payCallBack;
            yzTool.sendPay(productInfo, payCallBack);
        }

        /// <summary> 内购支付原生段结果回调 </summary>
        ///<param name="code">状态值</param>
        ///<param name="msg">异常消息</param>
        public void purchaseCallBack(string result)
        {
            var resultData = JsonMapper.ToObject(result);
            int resultType = int.Parse(resultData["code"].ToString(), CultureInfo.InvariantCulture);
            string msg = resultData["msg"].ToString();
            YzUtils.showLog("purchaseCallBack #resultType=" + resultType + " #msg=" + msg);
            if (resultType == 1)
            {
                if (curProductInfo != null)
                {
                    if (curProductInfo.name == "no_ad")
                    {
                        YzUtils.adManager.hideBannerAd();
                        YZLocalStorage.setStringItem("remove_ad", "true");
                    }
                }
                if (curPayCallBack != null)
                {
                    curPayCallBack.onSuccess();
                    curPayCallBack = null;
                }
            }
            else
            {
                if (curPayCallBack != null)
                {
                    curPayCallBack.onFail(msg);
                    curPayCallBack = null;
                }
            }
        }


        /// <summary> 移除广告时间(本地存储状态)
        /// newAdTime: 移除广告功能时长（单位：分） 
        /// ** 永久移除: newAdTime<=0
        /// ** 临时移除24小时: 24*60
        /// </summary>
        public static void removeAdTime(int newAdTime = 0)
        {
            try
            {
                long currentAdTime = YZLocalStorage.getLongItem(YzConstant.ST_REMOVE_AD, 0);
                if (currentAdTime < 0)
                {
                    YzUtils.showLog("已经永久移除广告了！");
                    return;
                }

                if (newAdTime <= 0)
                {
                    YZLocalStorage.setLongItem(YzConstant.ST_REMOVE_AD, -1);
                }
                else
                {
                    newAdTime = newAdTime * 60 * 1000; // 转换为毫秒
                    long now = ConvertDateTimeToLong();
                    if (now < currentAdTime)
                    {
                        YZLocalStorage.setLongItem(YzConstant.ST_REMOVE_AD, currentAdTime + newAdTime);
                    }
                    else
                    {
                        YZLocalStorage.setLongItem(YzConstant.ST_REMOVE_AD, now + newAdTime);
                    }
                }

                // 隐藏Banner
                adManager.hideBannerAd();

            }
            catch (System.Exception error)
            {
                YzUtils.showLog(error.Message);
            }
        }

        /// <summary> 验证是否购买移除广告功能 </summary>
        /// <returns>true:已购买 false：未购买</returns>
        public static bool checkIsRemoveAd()
        {
            // 兼容旧版本: 固定永久移除
            bool isRemoveAd = YZLocalStorage.getStringItem("remove_ad", "false") == "false" ? false : true;
            if (isRemoveAd)
            {
                return true;
            }

            // 新版本：可以控制去广告时长
            long currentAdTime = YZLocalStorage.getLongItem(YzConstant.ST_REMOVE_AD, 0);
            long now = ConvertDateTimeToLong();
            return currentAdTime < 0 || currentAdTime > now;
        }


        /// <summary> 消息提示 </summary>
        public static void showMsg(string msg)
        {
            if (!openAd)
            {
                return;
            }
            GameObject canvas = YzUtils.instance.GetAdCanvas();
            GameObject gameObject = new GameObject();
            gameObject.AddComponent<RectTransform>();
            if (Screen.height < Screen.width)
            {
                gameObject.GetComponent<Transform>().GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width * 0.7f, 80);
            }
            else
            {
                gameObject.GetComponent<Transform>().GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, 80);
            }
            gameObject.transform.SetParent(canvas.transform, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 122);
            GameObject textObject = new GameObject();
            textObject.AddComponent<RectTransform>();
            textObject.GetComponent<Transform>().GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, 60);
            Text text = textObject.AddComponent<Text>();
            text.text = msg;
            text.font = Resources.Load<Font>("Fonts/SIMHEI");
            text.fontSize = 45;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            textObject.transform.SetParent(gameObject.transform, false);
            gameObject.transform.localPosition = new Vector3(0, 0);
            YzTimer.SetTimeout(3, () =>
            {
                Destroy(gameObject);
            });
        }
        #endregion


        #region 手机震动
#if UNITY_ANDROID
        /// <summary>
        /// 保留接口（用于设置Android手机震动权限），不可删除，不可调用
        /// </summary>
        private static void PreserveAndroidPhoneVibrationPermission()
        {
            Handheld.Vibrate();
        }
#endif
        /// <summary> 手机震动,部分安卓手机开启省电模式时会关闭震动 </summary>
        /// <param name="milliseconds">
        /// 震动时间，单位毫秒，1秒等于1000毫秒，即输入1000L,实现效果会感觉震动很长时间
        /// 实际震动效果： 短震：20L  中震：100L  长震：300L
        /// 推荐使用 中震：100L
        /// </param>
        public static void Vibrate(long milliseconds)
        {
            if (YzUtils.openAd)
            {
#if UNITY_ANDROID
                if (PlatUtils.isAndroid && yzTool != null)
                {
                    yzTool.vibrateToTime((int)milliseconds);
                }
#endif
            }
            else
            {
#if UNITY_ANDROID
                if (PlatUtils.isAndroid)
                {
                    AndroidJavaClass vibrationService = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject currentActivity = vibrationService.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    vibrator.Call("vibrate", milliseconds);
                    Debug.Log("震动了： " + milliseconds + "毫秒。");
                }
#endif
            }
        }
        #endregion
        #region 游戏好评功能
        /// <summary>
        /// 根据过关次数（存档）是否大于指定过关次数（运营字段参数），结算界面来展示自己游戏的好评弹窗
        /// 只展示一次
        /// 
        /// 结算界面结算完关卡数据后调用，如果返回true则打开自己游戏的好评弹窗
        /// </summary>
        /// <returns></returns>
        public static bool isShowGoodReview()
        {
            if (!YzUtils.openAd || !PlatUtils.isAndroid)
            {
                return false;
            }
            int interval = getConfigIntValue("show_good_review_interval");
            if (interval > 0)
            {
                int complateCount = YZLocalStorage.getIntItem("levelComplateCount", 0);
                bool canShow = YZLocalStorage.getStringItem("can_show_good_review", "") == "";
                if (complateCount >= interval && canShow)
                {
                    YZLocalStorage.setStringItem("can_show_good_review", "show_good_review");
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 好评弹窗打五星时调用,前往应用商店打分,调用后关闭好评弹窗
        /// 打1-3星则不调用而直接关闭好评弹窗
        /// </summary>
        public static void GoToAppShopGoodReview()
        {
            if (!YzUtils.openAd || !PlatUtils.isAndroid)
            {
                return;
            }
            if (yzTool != null)
            {
                yzTool.showGoodReview();
            }
        }
        #endregion
        #region 快捷桌面
        /// <summary> 检测能否创建快捷桌面 </summary>
        public static bool canShortcut()
        {
            if (!checkCommonIsInit()) { return false; }
            if (YzUtils.yzTool != null)
            {
                return YzUtils.yzTool.canShortcut();
            }
            else
            {
                return false;
            }
        }
        /// <summary> 是否显示创建快捷方式控件 </summary>
        public static bool isShowShortcutWidget()
        {
            if (!checkCommonIsInit()) { return false; }
            if (false)
            {
                return YzUtils.getConfigBooleanValue("is_desktop", true);
            }
            else
            {
                return YzUtils.getConfigBooleanValue("is_desktop", false);
            }
        }
        /// <summary> 检测是否领取快捷桌面今日奖励 </summary>
        public static bool checkShortcutClaimed()
        {
            string today = DateTime.Now.ToShortDateString();
            string claimedDate = YZLocalStorage.getStringItem(YzConstant.ST_SHORTCUT_REWARD, "");
            Debug.Log("today " + today);
            Debug.Log("claimedDate " + claimedDate);
            if (today == claimedDate)
            {
                return true;
            }
            return false;
        }
        /// <summary> 检测能否领取快捷桌面奖励 </summary>
        public static void claimShortcut(Action<bool> callback)
        {
            if (YzUtils.yzTool != null)
            {
                YzUtils.yzTool.claimShortcut(callback);
            }
            else if (callback != null)
            {
                callback(false);
            }
        }
        public static Action<bool> callback_desktop = null;
        private static GameObject _shortcutWidget = null;
        /// <summary> 创建快捷方式挂件 </summary>
        public static void ShowShortcutWidget(YzAdParame parame = null, Action<bool> callback = null)
        {
            // if (YzUtils.isShowShortcutWidget())
            // {
            //     // 回调
            //     YzUtils.callback_desktop = (ret) =>
            //     {
            //         if (ret)
            //         {
            //             if (!YzUtils.checkShortcutClaimed())
            //             {
            //                 // 存储日期
            //                 YZLocalStorage.setStringItem(YzConstant.ST_SHORTCUT_REWARD, DateTime.Now.ToShortDateString());
            //                 // 更新红点
            //                 if (_shortcutWidget != null && _shortcutWidget.GetComponent<ShortCutWidget>() != null)
            //                 {
            //                     _shortcutWidget.GetComponent<ShortCutWidget>().updateRedPoint();
            //                 }
            //                 // 成功回调
            //                 callback?.Invoke(true);
            //             }
            //         }
            //         else
            //         {
            //             // 失败回调
            //             callback?.Invoke(false);
            //         }
            //     };
            //     // 直接用平台自带的创建图标
            //     if (instance.shortcutWidgetPrefabs != null)
            //     {
            //         GameObject newNode = GameObject.Instantiate(instance.shortcutWidgetPrefabs);
            //         if (newNode != null)
            //         {
            //             if (_shortcutWidget != null)
            //             {
            //                 GameObject.Destroy(_shortcutWidget);
            //                 _shortcutWidget = null;
            //             }
            //             _shortcutWidget = newNode;
            //             setNodeTransform(_shortcutWidget, parame);
            //         }
            //     }
            //     else
            //     {
            //         YzUtils.showLog("warn:未找到预制体 ShortcutWidget, 请查看CommonUtils组件上是否赋值！");
            //     }
            // }
            // else
            // {
            //     YzUtils.showLog("warn:不显示创建桌面图标");
            // }
        }
        /// <summary> 隐藏快捷方式挂件 </summary>
        public static void HideShortcutWidget()
        {
            if (_shortcutWidget != null)
            {
                GameObject.Destroy(_shortcutWidget);
                _shortcutWidget = null;
            }
        }
        private static GameObject _desktopPanel = null;
        public static void showDesktopPanel()
        {
            // if (!YzUtils.openAd) return;
            // showLog("调用桌面弹窗");
            // if (instance.desktopPanelPrefabs)
            // {
            //     GameObject newNode = Instantiate(instance.desktopPanelPrefabs);
            //     if (newNode)
            //     {
            //         if (_desktopPanel != null)
            //         {
            //             Destroy(_desktopPanel);
            //         }
            //         _desktopPanel = newNode;
            //         GameObject canvas = YzUtils.instance.GetAdCanvas();
            //         RectTransform parentRectTransform = canvas.gameObject.GetComponent<RectTransform>();
            //         _desktopPanel.transform.SetParent(parentRectTransform, false);
            //     }
            // }
            // else
            // {
            //     showLog("warn:" + "未找到预制体 desktopPanelPrefabs, 请查看CommonUtils组件上是否赋值!");
            // }
        }
        /** 创建快捷桌面 */
        public static void createShortcut(Action<bool> callback = null)
        {
            if (YzUtils.yzTool != null)
            {
                YzUtils.yzTool.createShortcut(callback);
            }
        }
        #endregion
        #region 抖音侧边栏
        /** 平台能否显示侧边栏 */
        public static void checkCanSidebar(Action<bool> callback)
        {
            if (YzUtils.yzTool != null)
            {
                YzUtils.yzTool.checkCanSidebar(callback);
            }
        }
        /// <summary> 检测是否领取今日奖励 </summary>
        public static bool checkSidebarClaimed()
        {
            string today = DateTime.Now.ToShortDateString();
            string claimedDate = YZLocalStorage.getStringItem(YzConstant.ST_SIDEBAR_REWARD, "");
            Debug.Log("today " + today);
            Debug.Log("claimedDate " + claimedDate);
            if (today == claimedDate)
            {
                return true;
            }
            return false;
        }
        /** 检测能否领取侧边栏奖励 */
        public static void claimSidebar(Action<bool> callback)
        {
            if (YzUtils.yzTool != null)
            {
                YzUtils.yzTool.claimSidebar(callback);
            }
            else if (callback != null)
            {
                callback(false);
            }
        }
        /** 是否显示侧边栏挂件 */
        public static bool isShowSidebarWidget()
        {
            bool b = false;
            if (false)
            {
                b = getConfigBooleanValue("is_sidebar", true);
            }
            else
            {
                showLog("当前平台没有侧边栏功能");
            }
            return b;
        }
        public static Action callback_sidebar = null;
        private static GameObject _sideBarWidget = null;
        public static void showSidebarWidget(YzAdParame parame, Action callback = null)
        {
            if (!YzUtils.openAd) return;
            showLog("调用侧边栏挂件");
            // if (isShowSidebarWidget())
            // {
            //     // 回调
            //     callback_sidebar = () =>
            //     {
            //         if (!YzUtils.checkSidebarClaimed())
            //         {
            //             // 存储日期
            //             YZLocalStorage.setStringItem(YzConstant.ST_SIDEBAR_REWARD, DateTime.Now.ToShortDateString());
            //             // 更新红点
            //             if (_sideBarWidget != null && _sideBarWidget.GetComponent<SidebarWidget>() != null)
            //             {
            //                 _sideBarWidget.GetComponent<SidebarWidget>().updateRedPoint();
            //             }
            //             // 成功回调
            //             callback?.Invoke();
            //         }
            //     };
            //     // 创建挂件节点
            //     if (instance.sideBarWidgetPrefabs)
            //     {
            //         GameObject newNode = Instantiate(instance.sideBarWidgetPrefabs);
            //         if (newNode)
            //         {
            //             if (_sideBarWidget != null)
            //             {
            //                 Destroy(_sideBarWidget);
            //                 _sideBarWidget = null;
            //             }
            //             _sideBarWidget = newNode;
            //             setNodeTransform(_sideBarWidget, parame);
            //         }
            //     }
            //     else
            //     {
            //         showLog("warn:" + "未找到预制体 SideBarWidet, 请查看CommonUtils组件上是否赋值!");
            //     }
            // }
        }
        /** 隐藏侧边栏挂件 */
        public static void hideSidebarWidget()
        {
            if (_sideBarWidget != null)
            {
                GameObject.Destroy(_sideBarWidget);
                _sideBarWidget = null;
            }
        }
        private static GameObject _sideBarPanel = null;
        public static void showSidebarPanel()
        {
            if (!YzUtils.openAd) return;
            showLog("调用侧边栏弹窗");
            // if (isShowSidebarWidget())
            // {
            //     if (instance.sideBarPanelPrefabs)
            //     {
            //         GameObject newNode = Instantiate(instance.sideBarPanelPrefabs);
            //         if (newNode)
            //         {
            //             if (_sideBarPanel != null)
            //             {
            //                 Destroy(_sideBarPanel);
            //             }
            //             _sideBarPanel = newNode;
            //             GameObject canvas = YzUtils.instance.GetAdCanvas();
            //             RectTransform parentRectTransform = canvas.gameObject.GetComponent<RectTransform>();
            //             _sideBarPanel.transform.SetParent(parentRectTransform, false);
            //         }
            //     }
            //     else
            //     {
            //         showLog("warn:" + "未找到预制体 sideBarPanel, 请查看CommonUtils组件上是否赋值!");
            //     }
            // }
        }
        /// <summary> 调用快游戏端侧边栏 </summary>
        public static void showKyxSidebar()
        {
        }
        #endregion
        #region 更多游戏
        private static GameObject _moreGamesWidget = null;
        /** 是否显示更多游戏挂件 */
        private static bool isShowMoreGamesWidget()
        {
            if (!checkCommonIsInit()) return false;
            if (PlatUtils.isDebug || (PlatUtils.isAndroid && getConfigBooleanValue("is_more_game", false)))
            {
                return true;
            }
            else
            {
                showLog("warn:" + "更多游戏挂件组件不显示！");
            }
            return false;
        }
        /// <summary> 显示更多游戏挂件 </summary>
        public static void showMoreGamesWidget(YzAdParame adParame)
        {
            if (!YzUtils.openAd) return;
            showLog("showMoreGamesWidget #adParame=" + (adParame != null ? adParame.ToString() : "null"));
            if (adParame == null)
            {
                showLog("erro , 传入节点的参数异常");
                return;
            }
            if (!isShowMoreGamesWidget())
            {
                return;
            }
            if (instance.moreGamesWidgetPrefabs != null)
            {
                Destroy(_moreGamesWidget);
                _moreGamesWidget = Instantiate(instance.moreGamesWidgetPrefabs, adParame.parent);
                setNodeTransform(_moreGamesWidget, adParame);
            }
            else
            {
                showLog("erro , 更多游戏挂件预制体不存在！");
            }
        }
        ///<summary> 隐藏更多游戏挂件 </summary>
        public static void hideMoreGamesWidget()
        {
            if (!openAd)
            {
                return;
            }
            showLog("warn:" + "隐藏更多游戏挂件！");
            if (_moreGamesWidget)
            {
                Destroy(_moreGamesWidget);
                _moreGamesWidget = null;
            }
        }
        #endregion
        #region 结算前广告
        private static long last_time_moregame = YzUtils.ConvertDateTimeToLong() / 1000;
        private static int last_index_moregame = 0;
        private static long last_time_box = YzUtils.ConvertDateTimeToLong() / 1000;
        private static int last_index_box = 0;
        // 获取结算广告类型
        public static int GetBeforeGameOverAdType()
        {
            // 当前时间(秒)
            long curTime = YzUtils.ConvertDateTimeToLong() / 1000;
            // 更多游戏
            if (YzUtils.checkConfigHashKey("beforeOver_moreGame_interval"))
            {
                JsonData arr_interval = YzUtils.getConfigJsonArryValue("beforeOver_moreGame_interval");
                if (arr_interval != null)
                {
                    if (arr_interval.IsArray)
                    {
                        float maxInterval = (float)arr_interval[arr_interval.Count - 1];
                        if (last_index_moregame < arr_interval.Count - 1)
                        {
                            maxInterval = (float)arr_interval[last_index_moregame];
                        }
                        float curInterval = curTime - last_time_moregame;
                        if (curInterval > maxInterval)
                        {
                            last_time_moregame = curTime;
                            last_index_moregame++;
                            return 2;
                        }
                        else
                        {
                            YzUtils.showLog("结算前广告 更多游戏 未满足时间间隔 curInterval=" + curInterval + " maxInterval=" + maxInterval);
                        }
                    }
                }
            }
            // 误触宝箱
            if (YzUtils.checkConfigHashKey("beforeOver_box_interval"))
            {
                JsonData arr_interval = YzUtils.getConfigJsonArryValue("beforeOver_box_interval");
                if (arr_interval != null)
                {
                    if (arr_interval.IsArray)
                    {
                        float maxInterval = (float)arr_interval[arr_interval.Count - 1];
                        if (last_index_box < arr_interval.Count - 1)
                        {
                            maxInterval = (float)arr_interval[last_index_box];
                        }
                        float curInterval = curTime - last_time_box;
                        if (curInterval > maxInterval)
                        {
                            last_time_box = curTime;
                            last_index_box++;
                            return 1;
                        }
                        else
                        {
                            YzUtils.showLog("结算前广告 误触宝箱 未满足时间间隔 curInterval=" + curInterval + " maxInterval=" + maxInterval);
                        }
                    }
                }
            }
            // 无广告 
            return 0;
        }

        public static void ShowBeforeGameOverAd(Action closeCallFunc, Action rewardCallFunc = null, int level = 0)
        {
            YzUtils.showLog("不显示结算前广告！");
            if (closeCallFunc != null) closeCallFunc();
        }
        #endregion

        #region 分享
        public static void share(Action callback = null) { }
        #endregion

        #region 平台特殊接口
        public static bool isShowTrySkin(int curLevel)
        {
            int count = 5;
            if (YzUtils.checkConfigHashKey("try_skin_level_count"))
            {
                count = YzUtils.getConfigIntValue("try_skin_level_count", 5);
            }
            if ((count > 0) && (curLevel % count == 0))
            {
                if (YzUtils.checkConfigHashKey("try_skin_show_ad_interval"))
                {
                    int ad_count = YzUtils.getConfigIntValue("try_skin_show_ad_interval", 0);
                    if ((ad_count > 0) && (curLevel % ad_count == 0))
                    {
                        showLog("服务器配置间隔" + ad_count + "关试用皮肤展示插屏！");
                        adManager.showIntersititialAd();
                    }
                }
                return true;
            }
            return false;
        }
        private static bool isReportScene = true;
        /// <summary> 上报场景就绪(抖音获客) </summary>
        public static void reportScene()
        {
            if (isReportScene)
            {
                // 开关关闭
                isReportScene = false;
            }
        }
        /// <summary> 上报抖音复访 </summary>
        /// scene: 1离线收益场景 2体力恢复场景 3重要事件掉落
        /// timeMS: 间隔多久触发复访(毫秒)
        /// 例如1分钟后重要事件则scene=3,timeMS=60000
        public static void reportRevisit(int scene = 3, long timeMS = 60000)
        {
        }
        /// <summary> 获取启动信息(抖音) 
        /// 0:非推荐流直出复访
        /// 1:复访离线收益场景
        /// 2:复访体力恢复场景 
        /// 3:复访重要事件掉落
        /// </summary>
        public static int getFeedGameScene()
        {
            showLog("是否为推荐流直出场景:0");
            return 0;
        }
        private static GameObject _gameClubWidget;
        /// <summary> 显示微信游戏圈按钮挂件 </summary>
        public static void ShowGameClubWidget(YzAdParame parame)
        {
            if (!YzUtils.openAd) return;
            // 隐藏旧的
            HideGameClubWidget();
            if (PlatUtils.isDebug || YzUtils.getConfigBooleanValue("isGameClub", true))
            {
                if (PlatUtils.isDebug)
                {
                    if (parame == null)
                    {
                        YzUtils.showLog("ShowGameClubWidget debug: parame is null");
                        return;
                    }
                    // 创建根节点
                    _gameClubWidget = new GameObject("GameClubWidget");
                    _gameClubWidget.layer = LayerMask.NameToLayer("UI");
                    // 添加UI组件
                    RectTransform rt = _gameClubWidget.AddComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(100f, 100f);
                    // 图像
                    Image img = _gameClubWidget.AddComponent<Image>();
                    // 文本标识
                    GameObject txtObj = new GameObject("Label");
                    RectTransform txtRt = txtObj.AddComponent<RectTransform>();
                    txtRt.sizeDelta = rt.sizeDelta;
                    txtRt.localScale = Vector3.one;
                    txtObj.transform.SetParent(_gameClubWidget.transform, false);
                    Text txt = txtObj.AddComponent<Text>();
                    txt.font = Resources.Load<Font>("Fonts/SIMHEI");
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = Color.black;
                    txt.fontSize = 30;
                    txt.text = "游戏圈";
                    // 设置父节点并按parame定位（复用已有工具方法）
                    setNodeTransform(_gameClubWidget, parame);
                    showLog("显示编辑器游戏圈挂件");
                }
                else if (false)
                {
                }
            }
            else
            {
                showLog("服务器控制不显示微信游戏圈！");
            }
        }
        /// <summary> 隐藏微信游戏圈按钮挂件 </summary>
        public static void HideGameClubWidget()
        {
            if (PlatUtils.isDebug)
            {
                if (_gameClubWidget != null)
                {
                    Destroy(_gameClubWidget);
                    _gameClubWidget = null;
                }
            }
            else if (false)
            {
            }
        }
        /// <summary> 显示微信游戏圈页面 </summary>
        public static void ShowGameClubPage()
        {
            if (!YzUtils.openAd) return;
            if (YzUtils.getConfigBooleanValue("isGameClub", true))
            {
                if (PlatUtils.isWechat && !YzUtils.isPackWeChatToOppo)
                {
                }
            }
            else
            {
                showLog("服务器控制不显示微信游戏圈！");
            }
        }
        /// <summary> 显示微信推荐页面(只能显示一次) </summary>
        public static void ShowRecommendPage()
        {
            if (!YzUtils.openAd) return;
            if (PlatUtils.isWechat && !YzUtils.isPackWeChatToOppo)
            {
                // 检测时间
                if (YzUtils.checkConfigHashKey("recommend_time"))
                {
                    long curTime = YzUtils.ConvertDateTimeToLong();
                    long interval = (curTime - YzUtils.luanch_time) / 1000;
                    float showAdTime = YzUtils.getConfigFloatValue("recommend_time", 180);
                    if (interval < showAdTime)
                    {
                        YzUtils.showLog("验证当前广告显示时间：#recommend_time=" + showAdTime + " #interval=" + interval);
                        return;
                    }
                }
            }
        }
        /// <summary> 显示微信加载页插屏广告，用于Loading页面广告逻辑
        /// 注意事项：一个appid对应一个优玩id,对应一个加载插屏广告id,如果重复使用加载插屏广告id,会导致卡住加载界面
        /// 如果没有有效的加载插屏广告id,可以使用输入"xxxxx",直接执行加载插屏广告失败回调
        /// </summary>
        /// <param name="id">插屏广告ID</param>
        /// <param name="closeAdOrLoadFailAction">插屏展示成功后的关闭事件或者加载失败的回调</param>
        public static void showLoadPageWechatInterstitialAd(string id, Action closeAdOrLoadFailAction)
        {
            Debug.Log("showLoadPageWechatInterstitialAd: " + id);
            closeAdOrLoadFailAction?.Invoke();
            closeAdOrLoadFailAction = null;
            return;
        }
        /// <summary> 修复微信小游戏平台多点触控问题，在项目启动时手动调用 </summary>
        public static void FixedWeChatInputSystem()
        {
        }
        #endregion
        #region 日志输出
        /// <summary> js回调日志  </summary>
        public void showLogCall(string message)
        {
            YzUtils.showLog(JsonMapper.ToObject(message).ToString());
        }
        private static List<string> logArray = new List<string>();
        public static void showLog(string message, YzLogType logType = YzLogType.Info)
        {
            if (YzUtils.show_log_to_console)
            {
                if (logType == YzLogType.Info)
                {
                    Debug.Log("[YzCommon]:" + message.ToString());
                }
                else
                {
                    Debug.LogError("[YzCommon]:" + message.ToString());
                }
            }
            // if (YzUtils.is_show_log_view)
            // {
            //     // 之前存的信息
            //     if (logArray.Count > 0)
            //     {
            //         for (var i = 0; i < logArray.Count; i++)
            //         {
            //             printLogsToView(logArray[i]);
            //         }
            //         logArray.Clear();
            //     }
            //     printLogsToView(message.ToString());
            // }
            else if (!commonIsInit)
            {
                // 未初始化缓存信息
                logArray.Add(message.ToString());
            }
        }
        public static GameObject logOutView = null;
        private static void printLogsToView(string str)
        {
            // if (logOutView == null)
            // {
            //     if (YzUtils.instance != null && YzUtils.instance.LogViewPrefabs != null)
            //     {
            //         // 查找AdCanvas，没有就创建一个新的
            //         var CanvasLog = GameObject.Find("CanvasLog");
            //         if (CanvasLog == null)
            //         {
            //             CanvasLog = new GameObject("CanvasLog");
            //             CanvasLog.AddComponent<Canvas>();
            //             CanvasLog.AddComponent<CanvasScaler>();
            //             CanvasLog.AddComponent<GraphicRaycaster>();
            //         }
            //         // 设置层级
            //         var canvasComp = CanvasLog.GetComponent<Canvas>();
            //         if (canvasComp != null)
            //         {
            //             canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            //             canvasComp.sortingOrder = 32767;
            //         }
            //         // 设置为常驻
            //         GameObject.DontDestroyOnLoad(CanvasLog);
            //         // 设置日志尺寸
            //         logOutView = GameObject.Instantiate(YzUtils.instance.LogViewPrefabs, CanvasLog.transform);
            //         var rect = logOutView.GetComponent<RectTransform>().sizeDelta;
            //         if (Screen.width > Screen.height)
            //         {
            //             logOutView.GetComponent<RectTransform>().sizeDelta = new Vector2(rect.x, 600);
            //         }
            //         else
            //         {
            //             logOutView.GetComponent<RectTransform>().sizeDelta = new Vector2(rect.x, 1000);
            //         }
            //     }
            // }
            // if (logOutView != null)
            // {
            //     logOutView.GetComponent<LogView>().showLog(str);
            // }
        }
        #endregion
        #region js http
        // 回调字典
        private static System.Collections.Generic.Dictionary<string, System.Action<bool, string>> _jsCallbackDict;
        public static void SendWebRequestByJs(string url, string jsonData, System.Action<bool, string> callBack)
        {
            // 1. 定义 JSBridge 回调唯一标识
            string callbackId = "YzHttpRequestCb_" + System.Guid.NewGuid().ToString();
            // 2. 注册回调到全局（可用静态字典管理）
            if (_jsCallbackDict == null)
            {
                _jsCallbackDict = new System.Collections.Generic.Dictionary<string, System.Action<bool, string>>();
            }
            _jsCallbackDict.Add(callbackId, callBack);
            // 3. 调用 jslib 导出方法
        }
        // JS 回调入口（js 侧通过 SendMessage 调用）
        public void OnJsWebRequestCallback(string cbObj)
        {
            JsonData obj = JsonMapper.ToObject(cbObj);
            string callbackId = obj["callbackId"].ToString();
            if (_jsCallbackDict != null && _jsCallbackDict.TryGetValue(callbackId, out var cb))
            {
                string success = obj["success"].ToString();
                string result = obj["result"].ToString();
                bool isSuccess = success.ToLower() == "true";
                cb?.Invoke(isSuccess, result);
                _jsCallbackDict.Remove(callbackId);
            }
            else
            {
                YzUtils.showLog("OnJsWebRequestCallback: 没有回调函数");
            }
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="adParame"></param>
        /// <param name="needAutoAdaptationScale">是否需要自动根据屏幕分辨率调整scale</param>
        private static void setNodeTransform(GameObject gameObject, YzAdParame adParame, bool needAutoAdaptationScale = false)
        {
            if (adParame.parent == null)
            {
                adParame.parent = YzUtils.instance.GetAdCanvas().transform;
            }
            gameObject.transform.SetParent(adParame.parent, false);
            RectTransform parentRectTransform = adParame.parent.GetComponent<RectTransform>();
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            float targetScale = adParame.scale;
            if (needAutoAdaptationScale)
            {
                targetScale = adParame.scale * YzUtils.screenResolutionRate;
            }
            // 设置坐标
            Vector3 position = new Vector3();
            float width = rectTransform.rect.width * targetScale;
            float height = rectTransform.rect.height * targetScale;
            if (adParame.top >= 0)
            {
                position.y = (parentRectTransform.rect.height / 2) - height / 2 - adParame.top;
            }
            else if (adParame.bottom >= 0)
            {
                position.y = -((parentRectTransform.rect.height / 2) - height / 2 - adParame.bottom);
            }
            if (adParame.left >= 0)
            {
                position.x = -(parentRectTransform.rect.width / 2) + width / 2 + adParame.left;
            }
            else if (adParame.right >= 0)
            {
                position.x = parentRectTransform.rect.width / 2 - width / 2 - adParame.right;
            }
            rectTransform.localScale = new Vector3(targetScale, targetScale, targetScale);
            rectTransform.localPosition = position;
        }
        /// <summary> 获取当前设备总运行内存,单位GB
        /// 测试数据：
        ///   pc平台：16g运存输出 15.87   
        ///   安卓手机：8g运存输出 7.3 ,6g运存输出 5.4  ,4g运存输出 3.75 ,3g运存输出 2.73  ,2g运存输出 1.86  ,1.5g运存输出 1.36 ,1g运存输出 0.87
        /// </summary>
        /// <returns></returns>
        public static float GetSystemTotalMemorySize()
        {
            float resFloat = 8;// 默认8GB
#if UNITY_EDITOR || UNITY_ANDROID
            resFloat = (float)UnityEngine.SystemInfo.systemMemorySize / (float)1024;
            string formattedNumber = resFloat.ToString("F2", CultureInfo.InvariantCulture);
            resFloat = float.Parse(formattedNumber, CultureInfo.InvariantCulture);
#endif
            //Debug.Log("当前设备总运行内存:" + resFloat + "GB");
            return resFloat;
        }
        /// <summary> 获取当前时间戳(毫秒) </summary>
        public static long ConvertDateTimeToLong()
        {
            return (long)(DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }
        /// <summary> 生成32位UUID </summary>
        public static String agenerteUUID()
        {
            return System.Guid.NewGuid().ToString("N");
        }
        /// <summary> 返回当前语言 </summary>
        public static string curLanguage
        {
            get
            {
                return "en";
                // if (PlatUtils.isDebug || PlatUtils.isTiktokKyx)
                // {
                //     return "en";
                // }
                // string lang;
                // switch (Application.systemLanguage)
                // {
                //     case SystemLanguage.Chinese:
                //         lang = "zh";
                //         break;
                //     case SystemLanguage.ChineseSimplified:
                //         lang = "zh";
                //         break;
                //     case SystemLanguage.ChineseTraditional:
                //         lang = "zh";
                //         break;
                //     case SystemLanguage.English:
                //         lang = "en";
                //         break;
                //     case SystemLanguage.Unknown:
                //         lang = "en";
                //         break;
                //     default:
                //         lang = "en";
                //         break;
                // }
                // return lang;
            }
        }



        /// <summary>复制指定内容到剪贴板</summary>
        public static void copyToClipboard(string content)
        {
            if (yzTool != null) yzTool.copyToClipboard(content);
        }

        /// <summary>隐藏开屏图</summary>
        public static void hideSplash()
        {
            if (yzTool != null) yzTool.hideSplash();
        }



        private static Action<JsonData> _loginSuccessCallback;
        private static Action<string> _loginFailCallback;

        /// <summary> 登录 </summary>
        public static void login(Action<JsonData> successCallFunc = null, Action<string> failCallFunc = null)
        {
            showLog("=====login====");
            _loginSuccessCallback = successCallFunc;
            _loginFailCallback = failCallFunc;
            if (yzTool != null)
            {
                yzTool.login();
            }
            else
            {
                _loginSuccessCallback = null;
                _loginFailCallback = null;
                successCallFunc?.Invoke("Login failed！");
            }
        }
        /// <summary> Android登录回调，包含用户GAID等数据的JSON字符串 </summary>
        public void loginCallBack(string loginInfo)
        {
            showLog("登录回调函数 ------>result=" + loginInfo);
            Action<JsonData> suc = _loginSuccessCallback;
            Action<string> fail = _loginFailCallback;
            _loginSuccessCallback = null;
            _loginFailCallback = null;
            if (!string.IsNullOrEmpty(loginInfo))
            {
                JsonData loginData = JsonMapper.ToObject(loginInfo);
                int code = int.Parse(loginData["code"].ToString(), CultureInfo.InvariantCulture);
                if (code == 1)
                {
                    suc?.Invoke(loginData);
                }
                else
                {
                    fail?.Invoke("Login failed！");
                }
            }
            else
            {
                fail?.Invoke("Login failed！");
            }
        }


        private static InterstitialAdListener _interstitialAdListener;
        /// <summary> 注册插屏广告监听 </summary>
        public static void registerInterstitialAdListener(InterstitialAdListener adListener)
        {
            _interstitialAdListener = adListener;
        }
        /// <summary> Android事件回传，JSON字符串 </summary>
        public void sendEvent(string eventMsg)
        {
            if (string.IsNullOrEmpty(eventMsg)) return;
            showLog("收到Android事件 ------>result=" + eventMsg);
            JsonData evt = JsonMapper.ToObject(eventMsg);
            if (!evt.ContainsKey("type")) return;
            string eventType = evt["type"].ToString();
            showLog("#EventType=" + eventType);
            switch (eventType)
            {
                case "interstitial_ad_impression":
                    showLog("插屏展示成功");
                    _interstitialAdListener?.onShowed();
                    break;
                case "interstitial_ad_close":
                    showLog("插屏关闭");
                    _interstitialAdListener?.onClosed();
                    break;
                case "interstitial_ad_display_fail":
                    showLog("插屏展示失败");
                    _interstitialAdListener?.onDisplayFailed();
                    break;
            }
        }


    }
}