using System.Collections.Generic;

namespace Lokas
{
    /// <summary>HexaAway 自有服务器配置。初始值与 Load 缺省值按旧工程分别保留。</summary>
    public sealed class HexaAwayServerConfig : ISubGameServerConfig
    {
        public GameMode GameMode => GameMode.HexaAway;
        public int ShowAdsInterval { get; private set; } = 1;
        public int NoAdsBeforeLevelCount { get; private set; } = 0;
        public int AdRewardItemCount { get; private set; } = 3;
        public bool EnableBehaviorAdaptationRecommendation { get; private set; } = true;
        public int PointsPerHour { get; private set; } = 4;
        public bool EnableFirstLaunchEnterGame { get; private set; } = true;
        public int ShowBannerLevel { get; private set; } = 8;
        public bool ShowFailLevelAds { get; private set; } = false;
        public int FailLevelAdInterval { get; private set; } = -1;
        public int NoFailLevelAdsBeforeLevel { get; private set; } = 10;
        public string LevelConfigName { get; private set; } = "LevelConfig_A";
        public List<IdleHintStage> IdleHintStages { get; private set; } = CreateDefaultIdleHintStages();

        public void Load(IServerConfigSource source)
        {
            // 可选 HexaAway.Key 优先；保留未带前缀的线上 Key 兼容，不要求服务端同步改名。
            var scoped = new ScopedServerConfigSource(source, nameof(GameMode.HexaAway));
            ShowAdsInterval = scoped.GetInt("ShowAdsInterval", 1);
            NoAdsBeforeLevelCount = scoped.GetInt("NoAdsBeforeLevelCount", 5);
            AdRewardItemCount = scoped.GetInt("AdRewardItemCount", 1);

            IdleHintStages = SubGameServerConfigParsing.ParseIdleHintStages(scoped.GetArray("IdleHintStages"), CreateDefaultIdleHintStages());
            EnableBehaviorAdaptationRecommendation = scoped.GetBool("EnableBehaviorAdaptationRecommendation", true);
            PointsPerHour = scoped.GetInt("PointsPerHour", 4);
            EnableFirstLaunchEnterGame = scoped.GetBool("EnableFirstLaunchEnterGame", true);
            ShowBannerLevel = scoped.GetInt("ShowBannerLevel", 5);
            ShowFailLevelAds = scoped.GetBool("ShowFailLevelAds", false);
            FailLevelAdInterval = scoped.GetInt("FailLevelAdInterval", -1);
            NoFailLevelAdsBeforeLevel = scoped.GetInt("NoFailLevelAdsBeforeLevel", 5);
            LevelConfigName = SubGameServerConfigParsing.ParseLevelConfigName(scoped.GetString("LevelConfigName", "LevelConfig_A"));


            // 非负计数校验；间隔/门槛的 <=0 禁用语义沿用原实现。
            AdRewardItemCount = System.Math.Max(0, AdRewardItemCount);
            PointsPerHour = System.Math.Max(0, PointsPerHour);
        }

        public bool TryGetCurrentLevel(out int level)
        {
            var data = GameEntry.SaveData?.Get<HexaAwayGameData>();
            level = data?.CurrentLevel ?? 0;
            return data != null;
        }

        public string SetLevelConfigName(string value)
        {
            LevelConfigName = SubGameServerConfigParsing.ParseLevelConfigName(value);
            return LevelConfigName;
        }

        public int GetIdleHintSeconds(int level)
        {
            if (level <= 0 || IdleHintStages == null) return 0;
            foreach (var stage in IdleHintStages)
                if (stage != null && stage.ContainsLevel(level)) return System.Math.Max(0, stage.Seconds);
            return 0;
        }

        private static List<IdleHintStage> CreateDefaultIdleHintStages() => new()
        {
            new IdleHintStage { MinLevel = 1, MaxLevel = 3, Seconds = 5 },
            new IdleHintStage { MinLevel = 4, MaxLevel = 10, Seconds = 30 },
            new IdleHintStage { MinLevel = 11, MaxLevel = 0, Seconds = 50 },
        };
    }
}
