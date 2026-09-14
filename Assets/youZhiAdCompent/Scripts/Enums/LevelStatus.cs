namespace YzAdComponent
{

    public class LevelStatus
    {
        /**
     * 游戏开始
     */
        public static string GameStart = "start";

        /**
         * 游戏胜利
         */
        public static string GameWin = "complete";

        /**
         * 游戏失败
         */
        public static string GameFail = "fail";

        /**
         * 跳过关卡
         */
        public static string GameSkip = "skip";

    }

    public enum EnumLocation
    {
        None = 0,
        Home,
        Level,
        Skin,
        Game,
        Pause,
        Over
    }

}