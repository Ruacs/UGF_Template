using System;
using LitJson;
using UnityEngine;
using System.Collections.Generic;
using AssemblyCSharp.Assets.Scripts.Common.Scripts.Interfaces;
using System.Globalization;

namespace YzAdComponent
{
    /// <summary>
    /// 游戏分析，关卡上报
    /// </summary>
    public class YzGameAnalytics
    {
        /// <summary>
        /// 优玩域名
        /// </summary>
        private static string MAIN_URL
        {
            get
            {
                if (false) { }
                else
                {
                    return "http://apps.youlesp.com/";
                }
            }
        }


        /// <summary>
        /// 登录接口
        /// </summary>
        private static string LOGIN_URL
        {
            get
            {
                return MAIN_URL + "as/login/v2";
            }

        }

        /// <summary>
        /// 广告上报接口
        /// </summary>
        private static string EVENT_AD_URL
        {
            get
            {
                return MAIN_URL + "ae/ad";
            }
        }



        /// <summary>
        ///  自定义事件上报接口
        /// </summary>
        private static string EVENT_URL
        {
            get
            {
                return MAIN_URL + "ae/event";
            }
        }


        /**
         * 关卡上报接口
         */
        private static string EVENT_LEVEL_URL
        {
            get
            {
                return MAIN_URL + "ae/glevel";
            }
        }

        /// <summary>
        ///验证是否开启上报
        /// </summary>
        private static bool checkSwitch()
        {
            if (!YzUtils.openAd) return false;

            if (PlatUtils.isDebug)
            {
                YzUtils.showLog("测试模式下，不进行服务器上报！");
                return false;
            }
            if (!PlatUtils.isAndroid && string.IsNullOrEmpty(YzUtils.config.otherConfig.yw_app_id))
            {
                YzUtils.showLog("yw_app_id 未配置，不进行服务器上报！");
                return false;
            }
            return true;
        }


        /// <summary>
        ///优玩登录，拉取配置
        /// </summary>
        public static void login(Action<bool, string> callBackFunc)
        {
            if (!YzUtils.openAd) return;

            YzUtils.showLog("调用登录接口");
            if (PlatUtils.isDebug)
            {
                callBackFunc(true, "");
                // 隐私弹窗（首次进游戏）
                YzUtils.showPrivacyPanel();
                // 监听关闭隐私弹窗
                YzUtils.registerPrivacyPanelCloseEvent(() => { YzUtils.instance.emitServerInit(); });
                return;
            }
            if (!checkSwitch()) return;

            JsonData configJsonData = new JsonData();
            getStaticParame(configJsonData);
            getLuanchParame(configJsonData);
            getDeviceInfo(configJsonData, (ret, msg) =>
            {
                if (ret)
                {
                    YzUtils.showLog("登录开始 >>param:" + configJsonData.ToJson());

                    connect(LOGIN_URL, configJsonData, (res, result) =>
                    {
                        if (res)
                        {
                            YzUtils.showLog("登录成功!");
                            JsonData resultJsonData = JsonMapper.ToObject(result);
                            if (resultJsonData != null && int.Parse(resultJsonData["code"].ToString(), CultureInfo.InvariantCulture) == 0)
                            {
                                YzUtils.showLog("服务器配置:" + result);
                                YzUtils.showLog("#uid=" + resultJsonData["data"]["uid"]);
                                YZLocalStorage.setLongItem(YzConstant.ST_KEY_UID, long.Parse(resultJsonData["data"]["uid"].ToString(), CultureInfo.InvariantCulture));
                                if (resultJsonData["app_data"] != null)
                                {
                                    YzUtils.configJsonData = resultJsonData["app_data"];

                                    // 服务器控制输出到日志框
                                    // YzUtils.is_show_log_view = YzUtils.getConfigBooleanValue("is_show_log_view", false);
                                    // 服务器控制输出到控制台
                                    // YzUtils.show_log_to_console = YzUtils.getConfigBooleanValue("show_log_to_console", false);
                                }
                                startEventAdTask();
                            }
                        }
                        else
                        {
                            YzUtils.showLog("登录失败!");
                        }
                        if (callBackFunc != null)
                        {
                            callBackFunc(res, result);
                        }
                        // 隐私弹窗（首次进游戏）
                        YzUtils.showPrivacyPanel();
                        // 监听关闭隐私弹窗
                        YzUtils.registerPrivacyPanelCloseEvent(() => { YzUtils.instance.emitServerInit(); });
                    });
                }
                else
                {
                    YzUtils.showLog(msg);
                    // 隐私弹窗（首次进游戏）
                    YzUtils.showPrivacyPanel();
                    // 监听关闭隐私弹窗
                    YzUtils.registerPrivacyPanelCloseEvent(() => { YzUtils.instance.emitServerInit(); });
                }
            });
        }


        private static List<EventAdInfo> eventAdList = new List<EventAdInfo>();
        private static bool isProcessQueue = false;  //是否在处理队列

        /**
         * 启用定时任务，每10秒上报一次
         */
        private static void startEventAdTask()
        {
            if (!YzUtils.openAd) return;

            YzTimer.SetInterval(10, () =>
            {
                processEventAdQueue();
            }, -1);
        }


        /// <summary>
        /// 处理广告上报队列
        /// </summary>
        private static void processEventAdQueue()
        {
            if (!YzUtils.openAd) return;

            // YzUtils.showLog("处理广告事件队列：#length=" + eventAdList.Count + " #isProcessQueue=" + isProcessQueue);
            if (!isProcessQueue && eventAdList.Count > 0)
            {

                isProcessQueue = true;
                var queueSize = eventAdList.Count;
                EventAdInfo[] eventAdCopyList = new EventAdInfo[queueSize];
                eventAdList.CopyTo(eventAdCopyList);
                JsonData data = new JsonData();

                JsonData[] datas = new JsonData[queueSize];
                getStaticParame(data);
                for (int i = 0; i < eventAdCopyList.Length; i++)
                {
                    datas[i] = eventAdList[i].toJsonData();
                }
                data["ad_data"] = JsonMapper.ToJson(datas);
                connect(EVENT_AD_URL, data, (bool ret, string result) =>
                {
                    try
                    {
                        if (ret)
                        {
                            JsonData resultJsonData = JsonMapper.ToObject(result);
                            if (resultJsonData != null && int.Parse(resultJsonData["code"].ToString(), CultureInfo.InvariantCulture) == 0)
                            {
                                eventAdList.RemoveRange(0, queueSize);
                                YzUtils.showLog("上报广告事件队列成功!");
                            }
                            else
                            {
                                YzUtils.showLog("上报广告事件队列失败！");
                            }
                        }
                        else
                        {
                            YzUtils.showLog("上报广告事件队列失败！");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        isProcessQueue = false;
                    }

                });
            }
        }

        /// <summary>
        /// 上报广告状态事件
        /// </summary>
        /// <param name="adType">广告类型</param>
        /// <param name="adStatus">广告状态</param>
        public static void EventAd(YwAdType adType, YwAdStatus adStatus)
        {
            if (!YzUtils.openAd) return;

            if (!checkSwitch()) return;

            var adInfo = new EventAdInfo(adType, adStatus, null);
            eventAdList.Add(adInfo);
        }

        /// <summary>
        /// 上报广告状态事件
        /// </summary>
        /// <param name="adType">广告类型</param>
        /// <param name="adStatus">广告状态</param>
        /// <param name="adEventParameter">广告参数（Code , ID , Msg）</param>
        public static void EventAdWithObj(YwAdType adType, YwAdStatus adStatus, AdEventParameter adEventParameter)
        {
            if (!YzUtils.openAd) return;

            if (!checkSwitch()) return;
            var adInfo = new EventAdInfo(adType, adStatus, adEventParameter);
            eventAdList.Add(adInfo);
        }


        /// <summary>
        /// 获取常规参数
        /// </summary>
        /// <param name="data"></param>
        private static void getStaticParame(JsonData data)
        {
            if (!YzUtils.openAd) return;

            data["app_id"] = YzUtils.config.otherConfig.yw_app_id;
            data["sdk_version"] = YzUtils.version;
            data["app_type"] = 3;
            data["country"] = "CN";
            string uuid = YZLocalStorage.getStringItem(YzConstant.ST_KEY_UUID, "");
            if (uuid.Length <= 0)
            {
                uuid = YzUtils.agenerteUUID();
            }
            data["uuid"] = uuid;
            data["uid"] = YZLocalStorage.getLongItem(YzConstant.ST_KEY_UID, -1);
            data["openid"] = YZLocalStorage.getStringItem(YzConstant.ST_KEY_OPENID, "");
            data["is_aes"] = false;
            if (PlatUtils.isAndroid)
            {
                data["channel"] = "android";
                data["app_version"] = YzUtils.version;
                data["kyx_app_id"] = "";
            }
            else if (PlatUtils.isDebug)
            {
                data["channel"] = "test";
                data["app_version"] = "1.0.0";
                data["kyx_app_id"] = "test";
            }

        }

        /// <summary>
        /// 获取登录启动参数
        /// </summary>
        private static void getLuanchParame(JsonData data)
        {
            if (!YzUtils.openAd) return;

            string luanchData = YZLocalStorage.getStringItem(YzConstant.ST_LUANCH_DATA, "");
            if (luanchData != "")
            {
                data["query"] = JsonMapper.ToObject(luanchData);
                YzUtils.showLog("登录获取启动参数:" + JsonMapper.ToJson(data["query"]));
            }
            else
            {
                data["query"] = JsonMapper.ToObject("{}");
                YzUtils.showLog("登录获取启动参数: {}");
            }
        }

        /// <summary>
        /// 获取各平台设备信息
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callBack"></param>
        private static void getDeviceInfo(JsonData data, Action<bool, string> callBack)
        {
            if (!YzUtils.openAd) return;

            if (YzUtils.yzTool != null)
            {
                YzUtils.yzTool.getDeviceInfo(data, (ret, msg) =>
                {
                    if (ret)
                    {
                        YzUtils.showLog("获取设备信息完成！");
                        callBack(true, msg);
                    }
                    else
                    {
                        callBack(false, "获取设备信息失败");
                    }

                });
            }
            else
            {
                callBack(false, "获取设备信息失败");
            }
        }




        /// <summary>
        /// 当前关卡
        /// </summary>
        private static int cur_level;


        /// <summary>
        /// 关卡开始-上报
        /// </summary>
        /// <param name="level">当前关卡数</param>
        public static void GameStart(int level, string gameName = "")
        {
            if (!YzUtils.openAd) return;

            cur_level = level;

            if (gameName == "")
            {
                YzUtils.showLog("GameStart #level=" + level);
            }
            else
            {
                YzUtils.showLog("GameStart 游戏名: " + gameName + ", 关卡数：" + level);
            }

            YzGameAnalytics.EventLevel(level, LevelStatus.GameStart, gameName);
        }

        /// <summary>
        /// 关卡过关成功上报
        /// </summary>
        /// <param name="level">当前关卡数</param>
        public static void GameWin(int level, bool showAd = true, string gameName = "")
        {
            if (!YzUtils.openAd) return;

            int complateCount = YZLocalStorage.getIntItem("levelComplateCount", 0);
            YZLocalStorage.setIntItem("levelComplateCount", complateCount += 1);
            if (gameName == "")
            {
                YzUtils.showLog("GameWin #level=" + level);
            }
            else
            {
                YzUtils.showLog("GameWin 游戏名: " + gameName + ", 关卡数：" + level);
            }

            if (showAd)
            {
                showGameOverAd(LevelStatus.GameWin, level);
            }

            YzGameAnalytics.EventLevel(level, LevelStatus.GameWin, gameName);
        }

        /// <summary>
        /// 关卡过关失败上报
        /// </summary>
        /// <param name="level">当前关卡数</param>
        public static void GameFail(int level, bool showAd = true, string gameName = "")
        {
            if (!YzUtils.openAd) return;

            if (gameName == "")
            {
                YzUtils.showLog("GameFail #level=" + level);
            }
            else
            {
                YzUtils.showLog("GameFail 游戏名: " + gameName + ", 关卡数：" + level);
            }

            if (showAd)
            {
                showGameOverAd(LevelStatus.GameFail, level);
            }

            YzGameAnalytics.EventLevel(level, LevelStatus.GameFail, gameName);
        }

        /**
         * 上报关卡事件
         * @param levelID 关卡ID
         * @param levelStatus 关卡状态
         * @param gameName 游戏名称-非必填
         */
        public static void EventLevel(int levelID, string levelStatus, string gameName = "")
        {
            YzUtils.showLog($"上报关卡事件:#levelID={levelID} #LevelStatus={levelStatus} #gameName={gameName}");
            if (!checkSwitch()) return;

            if (PlatUtils.isAndroid)
            {
                if (gameName == "")
                {
                    AndroidJavaClassHelper.CallStatic("eventLevel", levelID, levelStatus);
                }
                else
                {
                    AndroidJavaClassHelper.CallStatic("eventLevel", levelID, levelStatus, gameName);
                }
            }
        }

        private static int INT_MAX = 2147483647;

        /**
         * 上报关卡事件和时长
         * @param levelID 关卡ID
         * @param levelStatus 关卡状态
         * @param durationSec 关卡时长（单位/秒）关卡结束必填
         * @param gameName 游戏名称-非必填
         */
        public static void EventLevelDuration(int levelID, string levelStatus, long durationSec = 0, string gameName = "")
        {
            YzUtils.showLog($"上报关卡事件和时长:#levelID={levelID} #LevelStatus={levelStatus} #gameName={gameName} #duration={durationSec}");
            if (!checkSwitch()) return;

            // 确保 durationSec 在有效范围内
            if (durationSec < 0)
            {
                durationSec = 0;
            }
            else if (durationSec > INT_MAX)
            {
                YzUtils.showLog($"数值 {durationSec} 超出 int 范围，自动替换为 {INT_MAX}");
                durationSec = INT_MAX;

            }
            int durationSec_int = (int)durationSec;

            if (PlatUtils.isAndroid)
            {

                if (string.IsNullOrEmpty(gameName))
                {
                    AndroidJavaClassHelper.CallStatic("eventLevelDuration", levelID, levelStatus, durationSec_int);
                }
                else
                {
                    AndroidJavaClassHelper.CallStatic("eventLevelDuration", levelID, levelStatus, durationSec_int, gameName);
                }
            }
        }

        /// <summary>
        /// 上报自定义事件
        /// </summary>
        /// <param name="name">事件名称</param>
        public static void EventWithName(string name)
        {
            if (!YzUtils.openAd) return;

            YzUtils.showLog("上报自定义事件:" + name);
            if (!checkSwitch()) return;

            if (PlatUtils.isAndroid)
            {
                AndroidJavaClassHelper.CallStatic("eventWithName", name);
            }
        }

        /// <summary>
        /// 上报带字符串参数自定义事件
        /// </summary>
        /// <param name="name">事件名称</param>
        /// <param name="value">字符串参数</param>
        public static void EventWithNameAndstringValue(string name, string value)
        {
            if (!YzUtils.openAd) return;

            if (!checkSwitch()) return;

            YzUtils.showLog("上报带字符串参数自定义事件:#name=" + name + " #value=" + value);
            if (PlatUtils.isAndroid)
            {
                AndroidJavaClassHelper.CallStatic("eventWithNameAndstringValue", name, value);
            }
        }

        /// <summary>
        /// 上报带数字参数自定义事件
        /// </summary>
        /// <param name="name">事件名称</param>
        /// <param name="value">数字参数</param>
        public static void EventWithNameAndIntValue(string name, float value)
        {
            if (!YzUtils.openAd) return;

            if (!checkSwitch()) return;

            YzUtils.showLog("上报带数字参数自定义事件:#name=" + name + " #value=" + value);
            if (PlatUtils.isAndroid)
            {
                AndroidJavaClassHelper.CallStatic("eventWithNameAndIntValue", name, value);
            }
        }


        /// <summary>
        /// 上报带JSON对象参数自定义事件
        /// </summary>
        /// <param name="name">事件名称</param>
        /// <param name="value">JsonData参数</param>
        public static void EventWithNameAndJsonValue(string name, JsonData value)
        {
            if (!YzUtils.openAd) return;

            if (!checkSwitch()) return;

            YzUtils.showLog("上报带JSON对象参数自定义事件:#name=" + name + " #value=" + value);
            if (PlatUtils.isAndroid)
            {
                AndroidJavaClassHelper.CallStatic("eventWithNameAndJsonValue", name, JsonMapper.ToJson(value));
            }

        }


        /// <summary>
        /// 发起请求
        /// </summary>
        /// <param name="url">请求链接</param>
        /// <param name="data">请求参数</param>
        /// <param name="callBack">请求回调</param>
        public static void connect(string url, JsonData data, Action<bool, string> callBack)
        {
            if (!YzUtils.openAd)
            {
                callBack?.Invoke(false, "");
                return;
            }

            YzUtils.showLog(url + "?data=" + JsonMapper.ToJson(data));

            WWWForm form = new WWWForm();
            form.AddField("data", JsonMapper.ToJson(data));
            YzUtils.sendPostRequest(url, form, new HttpRequestCallBack()
            {
                onSuccess = (string result) =>
                {
                    callBack?.Invoke(true, result);
                },
                onFail = (string result) =>
                {
                    callBack?.Invoke(false, result);
                }
            });

        }


        private static long last_time_moregame = YzUtils.ConvertDateTimeToLong() / 1000;
        private static int last_index_moregame = 0;
        private static long last_time_box = YzUtils.ConvertDateTimeToLong() / 1000;
        private static int last_index_box = 0;
        private static long last_time_video = YzUtils.ConvertDateTimeToLong() / 1000;
        private static int last_index_video = 0;

        // 获取结算广告类型
        public static int GetGameOverAdType(string levelStatus, int level)
        {
            // 当前时间(秒)
            long curTime = YzUtils.ConvertDateTimeToLong() / 1000;
            // 更多游戏
            if (YzUtils.checkConfigHashKey("auto_moreGame_interval"))
            {
                JsonData arr_interval = YzUtils.getConfigJsonArryValue("auto_moreGame_interval");
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
                            return 4;
                        }
                        else
                        {
                            YzUtils.showLog("结算广告 更多游戏 未满足时间间隔 curInterval=" + curInterval + " maxInterval=" + maxInterval);
                        }
                    }
                }
            }
            // 误触宝箱
            if (YzUtils.checkConfigHashKey("auto_box_interval"))
            {
                JsonData arr_interval = YzUtils.getConfigJsonArryValue("auto_box_interval");
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
                            return 3;
                        }
                        else
                        {
                            YzUtils.showLog("结算广告 误触宝箱 未满足时间间隔 curInterval=" + curInterval + " maxInterval=" + maxInterval);
                        }
                    }
                }
            }
            // 结算视频
            if (YzUtils.checkConfigHashKey("auto_video_interval"))
            {
                JsonData arr_interval = YzUtils.getConfigJsonArryValue("auto_video_interval");
                if (arr_interval.IsArray)
                {
                    float maxInterval = (float)arr_interval[arr_interval.Count - 1];
                    if (last_index_video < arr_interval.Count - 1)
                    {
                        maxInterval = (float)arr_interval[last_index_video];
                    }
                    float curInterval = curTime - last_time_video;
                    if (curInterval > maxInterval)
                    {
                        last_time_video = curTime;
                        last_index_video++;
                        return 2;
                    }
                    else
                    {
                        YzUtils.showLog("结算广告 视频 未满足时间间隔 curInterval=" + curInterval + " maxInterval=" + maxInterval);
                    }
                }
            }
            // 结算插屏
            bool is_auto_insert = YzUtils.getConfigBooleanValue("is_auto_insert", true);
            if (is_auto_insert)
            {
                return 1;
            }
            YzUtils.showLog("结算广告 服务器配置不弹插屏！");
            // 无广告 
            return 0;
        }

        /// <summary>结算广告  1：插屏 2：视频  3：宝箱</summary>
        /// <param name="levelStatus">当前关卡状态</param>
        /// <param name="level">当前关卡</param>
        public static void showGameOverAd(string levelStatus, int level)
        {
            if (!YzUtils.openAd || !YzUtils.getConfigBooleanValue("isGameOverAd", true))
            {
                YzUtils.showLog("不显示结算广告！");
                return;
            }
            int type = GetGameOverAdType(levelStatus, level);
            // 根据type执行结算广告
            switch (type)
            {
                case 4:
                    YzUtils.showLog("结算显示更多游戏弹窗！");
                    YzUtils.showMoreGamePage();
                    break;
                case 3:
                    YzUtils.showLog("结算显示误触宝箱弹窗！");
                    YzUtils.showBoxPage();
                    break;
                case 2:
                    string auto_video_type = YzUtils.getConfigStrValue("auto_video_type", "reward_video");
                    if (auto_video_type == "reward_video")
                    {
                        YzUtils.showLog("结算显示激励视频广告！");
                        YzUtils.adManager.showReardVideoAd();
                    }
                    else
                    {
                        YzUtils.showLog("结算显示插屏视频广告！");
                        YzUtils.adManager.showIntersitialVideoAd();
                    }
                    break;
                case 1:
                    YzUtils.showLog("结算显示插屏广告！");
                    YzUtils.adManager.showIntersititialAd(YzAdLocation.Over);
                    break;
                default:
                    YzUtils.showLog("结算无广告！");
                    break;
            }

        }

    }
}