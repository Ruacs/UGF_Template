namespace Lokas
{
    /// <summary>
    /// 道具补充方式。按钮只展示入口，具体购买或广告流程由业务层处理。
    /// </summary>
    public enum PropButtonPurchaseMode
    {
        /// <summary>不可补充。</summary>
        None,
        /// <summary>通过激励广告补充。</summary>
        Ad,
        /// <summary>通过游戏内货币购买。</summary>
        Currency,
        /// <summary>业务自定义补充方式。</summary>
        Custom
    }
}
