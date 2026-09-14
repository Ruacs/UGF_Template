namespace Lokas
{
    /// <summary>
    /// UIFramework 中预设的界面分组，枚举名称必须与 UIComponent 的 UI Group Name 保持一致。
    /// </summary>
    public enum UIGroupType
    {
        Default = 0,            /// 默认分组
        GamePlayOverlay = 1,    /// 游戏中覆盖分组  伤害、奖励、飘字
        Dialog = 2,             /// 对话框分组  设置、签到等
        SystemOverlay = 3,      /// 系统覆盖分组 Toast等
    }
}
