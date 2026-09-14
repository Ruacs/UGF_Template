using System;
using System.Collections;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace YzAdComponent
{

    public class YzOtherConfig
    {
        // 优玩id
        public string yw_app_id = "";
        // 游戏名称
        public string game_name = "";
        // 软著登记号
        public string game_code = "";
        // 游戏著作人
        public string game_author = "";
        // 公司主体
        public string company_subject = "";
        // 公司联系方式
        public string company_contact = "";
        // 公司联系地址
        public string company_address = "";
        // logo图
        public Sprite logo_spr = null;
        // 游戏信息图
        public Sprite gameInfo_spr = null;

        /// <summary> 读取本地other配置 </summary>
        /// <param name="localOtherConfig"></param>
        public void init_local_config(JsonData localOtherConfig)
        {
            if (localOtherConfig != null)
            {
                // 优玩id
                if (localOtherConfig.ContainsKey("yw_app_id")) yw_app_id = localOtherConfig["yw_app_id"].ToString();
                else YzUtils.showLog("本地other配置 yw_app_id: 未配置");
                // 游戏名称
                if (localOtherConfig.ContainsKey("game_name")) game_name = localOtherConfig["game_name"].ToString();
                else YzUtils.showLog("本地other配置 game_name: 未配置");
                // 软著登记号
                if (localOtherConfig.ContainsKey("game_code")) game_code = localOtherConfig["game_code"].ToString();
                else YzUtils.showLog("本地other配置 game_code: 未配置");
                // 游戏著作人
                if (localOtherConfig.ContainsKey("game_author")) game_author = localOtherConfig["game_author"].ToString();
                else YzUtils.showLog("本地other配置 game_author: 未配置");
                // 公司信息
                if (localOtherConfig.ContainsKey("company"))
                {
                    JsonData company = localOtherConfig["company"];
                    // 公司主体
                    if (company.ContainsKey("subject")) company_subject = company["subject"].ToString();
                    else YzUtils.showLog("本地other配置 subject: 未配置");
                    // 联系方式
                    if (company.ContainsKey("contact")) company_contact = company["contact"].ToString();
                    else YzUtils.showLog("本地other配置 contact: 未配置");
                    // 联系地址
                    if (company.ContainsKey("address")) company_address = company["address"].ToString();
                    else YzUtils.showLog("本地other配置 address: 未配置");
                }
                else YzUtils.showLog("本地other配置 company: 未配置");
            }
        }

        /// <summary> 读取快游戏平台的优玩配置 </summary>
        /// <param name="youwanConfig"></param>
        public void init_youwan_cofig(JsonData youwanConfig)
        {
            if (youwanConfig != null)
            {
                // 优玩id
                if (youwanConfig.ContainsKey("yw_app_id")) yw_app_id = youwanConfig["yw_app_id"].ToString();
                else YzUtils.showLog("快游戏平台配置 yw_app_id: 未配置");
                // 游戏名称
                if (youwanConfig.ContainsKey("game_name")) game_name = youwanConfig["game_name"].ToString();
                else YzUtils.showLog("快游戏平台配置 game_name: 未配置");
                // 软著登记号
                if (youwanConfig.ContainsKey("game_code")) game_code = youwanConfig["game_code"].ToString();
                else YzUtils.showLog("快游戏平台配置 game_code: 未配置");
                // 游戏著作人
                if (youwanConfig.ContainsKey("game_author")) game_author = youwanConfig["game_author"].ToString();
                else YzUtils.showLog("快游戏平台配置 game_author: 未配置");
                // logo
                if (youwanConfig.ContainsKey("logo_base64")) logo_spr = Base64ToSprite(youwanConfig["logo_base64"].ToString());
                else YzUtils.showLog("快游戏平台配置 logo_base64: 未配置");
                // 游戏信息图
                if (youwanConfig.ContainsKey("gameInfo_base64")) gameInfo_spr = Base64ToSprite(youwanConfig["gameInfo_base64"].ToString());
                else YzUtils.showLog("快游戏平台配置 gameInfo_base64: 未配置");
                // 公司信息
                if (youwanConfig.ContainsKey("company"))
                {
                    JsonData company = youwanConfig["company"];
                    // 公司主体
                    if (company.ContainsKey("subject")) company_subject = company["subject"].ToString();
                    else YzUtils.showLog("快游戏平台配置 subject: 未配置");
                    // 联系方式
                    if (company.ContainsKey("contact")) company_contact = company["contact"].ToString();
                    else YzUtils.showLog("快游戏平台配置 contact: 未配置");
                    // 联系地址
                    if (company.ContainsKey("address")) company_address = company["address"].ToString();
                    else YzUtils.showLog("快游戏平台配置 address: 未配置");
                }
                else YzUtils.showLog("快游戏平台配置 company: 未配置");
            }
        }

        public Texture2D Base64ToTexture(string base64String)
        {
            // 移除Base64字符串前缀（如果有）
            if (base64String.Contains(","))
            {
                base64String = base64String.Substring(base64String.IndexOf(',') + 1);
            }

            byte[] imgBytes = System.Convert.FromBase64String(base64String);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imgBytes);
            return tex;
        }

        public Sprite Base64ToSprite(string base64String)
        {
            Texture2D tex = Base64ToTexture(base64String);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }

    }

}