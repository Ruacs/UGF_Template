namespace Lokas
{
    /// <summary>公共广告、计时和主玩法驱动的功能配置，副玩法无需重复声明。</summary>
    public sealed class CommonAdsServerConfig
    {
        public int AdsUnavailableCountdownRewardDailyLimit { get; private set; } = 3;
        public bool EnableAdsUnavailablePanel { get; private set; } = true;
        public int LevelTimerIdleStopSeconds { get; private set; } = 15;
        public int TaskUnlockLevel { get; private set; } = 10;
        public int RankUnlockLevel { get; private set; } = 8;
        public int ShopUnlockLevel { get; private set; } = 10;
        public int FirstWinUnlockLevel { get; private set; } = 9;
        public bool ShowFirstWinClaim2Button { get; private set; } = true;
        public bool EnableShop { get; private set; } = true;

        public void Load(IServerConfigSource source)
        {
            AdsUnavailableCountdownRewardDailyLimit = source.GetInt("AdsUnavailableCountdownRewardDailyLimit", 3);
            EnableAdsUnavailablePanel = source.GetBool("EnableAdsUnavailablePanel", true);
            LevelTimerIdleStopSeconds = source.GetInt("LevelTimerIdleStopSeconds", 15);
            TaskUnlockLevel = source.GetInt("TaskUnlockLevel", 6);
            RankUnlockLevel = source.GetInt("RankUnlockLevel", 8);
            ShopUnlockLevel = source.GetInt("ShopUnlockLevel", 10);
            FirstWinUnlockLevel = source.GetInt("FirstWinUnlockLevel", 9);
            ShowFirstWinClaim2Button = source.GetBool("ShowFirstWinClaim2Button", true);
            // 原实现不从服务器读取 EnableShop，保留初始 true，不借拆分改变生效规则。
        }
    }
}
