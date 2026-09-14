namespace Lokas
{
    public enum GameState
    {
        None,
        Menu,
        Playing,
        Paused,
        Restart,
        TimeOver,
        GameOver
    }

    public enum GameResult
    {
        None,
        Win,
        Fail
    }
    
    public enum GameMode
    {
        None = 0,
        Game = 1,
        // 稳定协议 ID，卸载示例不删除或重排枚举值。
        HexaAway = 2,
        RectMatch = 3
    }


    public enum Direction
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    /// <summary>
    /// 难度类型
    /// </summary>
    public enum DifficultyType
    {
        Normal,
        Hard
    }

    /// <summary>
    /// 线索类型
    /// </summary>
    public enum ClueType
    {
        None = 0,
        Shape = 1,          //形状线索：竖向、正方形或横向
        LockStep = 2,       //锁步线索：锁步 1-9
        Unkown = 3          //未知数字：推断大小
    }
}
