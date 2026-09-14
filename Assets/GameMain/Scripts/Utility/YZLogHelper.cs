using System.Collections;
using System.Collections.Generic;
using GameFramework; 
using Lokas;
using UnityEngine;
using YzAdComponent;

public class YZLogHelper : GameFrameworkLog.ILogHelper
    {
        /// <summary>
        /// 记录日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(GameFrameworkLogLevel level, object message)
        {
            if(!GameEntry.ShowLog)
            {
                return;
            }

            switch (level)
            {
                case GameFrameworkLogLevel.Debug:
                    YzUtils.showLog(Utility.Text.Format("<color=#888888>{0}</color>", message.ToString()), YzLogType.Info);
                    break;

                case GameFrameworkLogLevel.Info:
                    YzUtils.showLog(message.ToString(),YzLogType.Info);
                    break;

                case GameFrameworkLogLevel.Warning:
                    YzUtils.showLog(message.ToString(),YzLogType.Warn);
                    break;

                case GameFrameworkLogLevel.Error:
                    YzUtils.showLog(message.ToString(),YzLogType.Error);
                    break;

                default:
                    throw new GameFrameworkException(message.ToString());
            }
        }
    }
