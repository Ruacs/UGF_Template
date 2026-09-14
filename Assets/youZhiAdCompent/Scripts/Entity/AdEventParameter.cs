using LitJson;

namespace YzAdComponent
{

    public class AdEventParameter
    {

        public int code = -999; //状态码
        public string msg = ""; //状态信息
        public string tag = ""; //标签
        public string adId = ""; //广告id

        public AdEventParameter(string adId)
        {
            this.adId = adId;
        }

        public AdEventParameter(string adId, int code, string msg)
        {
            this.adId = adId;
            this.code = code;
            this.msg = msg;
        }

        public AdEventParameter(string adId, int code, string msg, string tag)
        {
            this.adId = adId;
            this.code = code;
            this.msg = msg;
            this.tag = tag;
        }


        public JsonData toJsonData()
        {
            JsonData data = new JsonData();
            data["adId"] = this.adId;
            if (this.code != -999)
            {
                data["code"] = this.code;
            }
            if (!string.IsNullOrEmpty(msg))
            {
                data["msg"] = this.msg;
            }
            if (!string.IsNullOrEmpty(this.tag))
            {
                data["tag"] = this.tag;
            }
            return data;
        }
    }

}