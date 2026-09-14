namespace Lokas
{
    /// <summary>
    /// 道具按钮的数量展示模式。
    /// </summary>
    public enum PropButtonDisplayMode
    {
        /// <summary>按实际数量显示。</summary>
        Normal,
        /// <summary>永久无限，通常显示无限图标或“∞”。</summary>
        InfiniteForever,
        /// <summary>限时无限，显示外部传入的倒计时文本。</summary>
        InfiniteWithTimer
    }
}
