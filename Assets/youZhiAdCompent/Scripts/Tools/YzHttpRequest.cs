using System;
using System.Collections;
using AssemblyCSharp.Assets.Scripts.Common.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.Networking;


namespace YzAdComponent
{
    public class YzHttpRequest
    {

        public static IEnumerator GetRequest(string url, HttpRequestCallBack callBack)
        {
            YzUtils.showLog("GetRequest #url=" + url);

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();
                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    YzUtils.showLog("GetRequest #url=" + url + " #erro:" + webRequest.error);
                    if (callBack != null)
                    {
                        callBack.onFail(webRequest.error);
                    }
                }
                else
                {
                    // YzUtils.showLog("GetRequest success result:" + webRequest.downloadHandler.text);
                    if (callBack != null)
                    {
                        callBack.onSuccess(webRequest.downloadHandler.text);
                    }
                }
            }
        }

        public static IEnumerator PostRequest(string url, WWWForm form, HttpRequestCallBack callBack)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Post(url, form))
            {
                yield return webRequest.SendWebRequest();
                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    YzUtils.showLog("PostRequest #url=" + url + " #erro:" + webRequest.error);
                    if (callBack != null)
                    {
                        callBack.onFail(webRequest.error);
                    }
                }
                else
                {
                    // YzUtils.showLog("PostRequest success result:" + webRequest.downloadHandler.text);
                    if (callBack != null)
                    {
                        callBack.onSuccess(webRequest.downloadHandler.text);
                    }
                }
            }
        }


    }

}