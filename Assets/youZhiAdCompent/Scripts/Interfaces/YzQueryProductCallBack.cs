using System;
using System.Collections.Generic;


namespace YzAdComponent
{
    /// <summary>
    /// 查询回调
    /// </summary>
    public class YzQueryProductCallBack
    {
        /// <summary>
        /// 查询商品成功回调
        /// </summary>
        public Action<List<ProductInfo>> onSuccess;
        /// <summary>
        /// 查询商品异常回调,参数为异常信息的接收
        /// </summary>
        public Action<string> onFail;

        public YzQueryProductCallBack() { }
        /// <summary>
        /// 查询商品回调
        /// </summary>
        /// <param name="onSuccess">查询成功回调，回调信息为商品列表数组</param>
        /// <param name="onFail">查询商品失败回调，参数为异常信息</param>
        public YzQueryProductCallBack(Action<List<ProductInfo>> onSuccess, Action<string> onFail)
        {
            this.onFail = onFail;
            this.onSuccess = onSuccess;
        }
    }

}