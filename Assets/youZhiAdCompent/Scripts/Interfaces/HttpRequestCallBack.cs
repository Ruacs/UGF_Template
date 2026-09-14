using System;
namespace AssemblyCSharp.Assets.Scripts.Common.Scripts.Interfaces
{
    public class HttpRequestCallBack
    {
        /// <summary>
        /// 请求成功回调
        /// </summary>
        public Action<string> onSuccess;
        /// <summary>
        /// 请求失败回调,参数为异常信息的接收
        /// </summary>
        public Action<string> onFail;

        public HttpRequestCallBack() { }
        /// <summary>
        /// Http请求回调
        /// </summary>
        /// <param name="onSuccess">请求成功回调</param>
        /// <param name="onFail">请求失败回调,参数为异常信息的接收</param>
        public HttpRequestCallBack(Action<string> onSuccess, Action<string> onFail)
        {
            this.onFail = onFail;
            this.onSuccess = onSuccess;
        }
    }
}
