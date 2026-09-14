using System;


namespace YzAdComponent
{
    /// <summary>
    /// 激励视频回调
    /// </summary>
    public class RewardVideoCallBack
    {
        /// <summary>
        /// 获得奖励回调
        /// </summary>
        public Action onSuccess;
        /// <summary>
        /// 视频播放异常回调,参数为异常信息的接收
        /// </summary>
        public Action<string> onFail;

        public RewardVideoCallBack() { }
        /// <summary>
        /// 激励视频回调
        /// </summary>
        /// <param name="onSuccess">获得奖励回调</param>
        /// <param name="onFail">视频播放异常，参数为异常信息，展示给用户</param>
        public RewardVideoCallBack(Action onSuccess, Action<string> onFail)
        {
            this.onFail = onFail;
            this.onSuccess = onSuccess;
        }
    }
}