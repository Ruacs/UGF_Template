using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System;

namespace YzAdComponent
{

    public class EventLevelInfo
    {
        private int levelID;
        private long time;
        private string levelStatus;
        private string model = "";

        public EventLevelInfo(int level, string levelStatus, string gameName = "")
        {
            this.time = YzUtils.ConvertDateTimeToLong();
            this.levelID = level;
            this.levelStatus = levelStatus;
            this.model = gameName;
        }

        public JsonData toJsonData()
        {
            try
            {
                JsonData data = new JsonData();
                data["time"] = this.time;
                data["level_id"] = this.levelID;
                switch (this.levelStatus)
                {
                    case "start":
                        data["status"] = 1;
                        break;
                    case "complete":
                        data["status"] = 2;

                        break;
                    case "fail":
                        data["status"] = 3;
                        break;
                    case "skip":
                        data["status"] = 4;
                        break;
                }
                if (model != "")
                {
                    data["model"] = model;
                }
                return data;
            }
            catch (Exception ex)
            {
                YzUtils.showLog("EventLevelInfo toJsonData erro msg =" + ex, YzLogType.Error);
            }
            return new JsonData();
        }
    }

}