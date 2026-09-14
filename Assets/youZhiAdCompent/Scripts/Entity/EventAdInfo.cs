using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System;

namespace YzAdComponent
{

    public class EventAdInfo
    {
        private YwAdType adType;
        private YwAdStatus adStatus;
        private AdEventParameter adEventParameter;
        private long time;

        public EventAdInfo(YwAdType _adType, YwAdStatus _adStatus, AdEventParameter _adEventParameter = null)
        {
            time = YzUtils.ConvertDateTimeToLong();
            adType = _adType;
            adStatus = _adStatus;
            adEventParameter = _adEventParameter;
        }

        public JsonData toJsonData()
        {
            try
            {
                JsonData data = new JsonData();

                data["ad_type"] = (int)adType;
                data["ad_status"] = (int)adStatus;
                if (adEventParameter != null)
                {
                    if (!string.IsNullOrEmpty(adEventParameter.adId))
                    {
                        data["ad_id"] = adEventParameter.adId;
                    }
                    data["ad_info"] = adEventParameter.toJsonData();
                }
                data["time"] = time;
                return data;
            }
            catch (Exception ex)
            {
                YzUtils.showLog("EventAdInfo toJsonData erro msg =" + ex.ToString(), YzLogType.Error);
            }
            return new JsonData();
        }

        // update =dt {}
    }

}