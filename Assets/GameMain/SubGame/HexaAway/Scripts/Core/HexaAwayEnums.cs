namespace Lokas
{
    /// <summary>
    /// 平台地板类型
    /// </summary>
    public enum GroundType
    {
        None = 0, // 空白
        Normal = 1, // 普通地面
        Collapse = 2, // 破碎地面
        Stopper = 3, // 停止地板
        ChangeDirection = 4, // 转向地板
        WormHole = 5 // 虫洞地板
    }

    /// <summary>
    /// 方向类型
    /// </summary>
    public enum HexaAwayDirection
    {
        Up = 0, // 上
        UpRight = 1, // 右上
        DownRight = 2, // 右下
        Down = 3, // 下
        DownLeft = 4, // 左下
        UpLeft = 5 // 左上
    }

    /// <summary>
    /// 关卡格子内容类型
    /// </summary>
    public enum PathStatus
    {
        Allowed = 1, // 可通过
        Blocked = 2, // 被阻挡
        Stopped = 3, // 停止
        Busy = 4 // 忙碌
    }

    /// <summary>
    /// 关卡难度/展示类型
    /// </summary>
    public enum LevelType
    {
        Normal = 0, // 普通关
        Hard = 1 // 困难关
    }

    public enum LevelObjectType
    {
        None = 0, // 无
        Stopper = 1, // 停止器
        Wall = 2, // 墙

        Reverse = 10, // 反转
        TripleReverse = 11, // 三重反转
        ChangeDirection = 12, // 改变方向
        Saw = 13, // 锯子
        Bomb = 14, // 炸弹

        CollapseTrigger = 20, // 破碎触发
        SquareReverse = 21, // 地块反转
        TripleSquareReverse = 22, // 三重地块反转
        AreaChangeDirection = 23, // 区域改变方向
        WormHole = 24, // 虫洞
        Tunnel = 25, // 隧道

        CustomStart = 1000 // 自定义起始值
    }

    public enum LevelObjectLayer
    {
        Board = 0, // 棋盘对象层
        Cover = 1, // 覆盖层
        Modifier = 2, // 修改器层
        Meta = 3 // 元数据层
    }

    /// <summary>
    /// Tile 附加效果类型，运行时行为由效果配置映射
    /// </summary>
    public enum TileEffectType
    {
        None = 0, // 无
        Ice = 1, // 冰
        Unknown = 2, // 未知
        Chain = 3, // 锁链

        WoodBox = 4
    }


     public enum ReverseType { Double, Tripple }
}
