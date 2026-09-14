using Lokas;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameEntry = Lokas.GameEntry;

namespace Ads
{
    /// <summary>公共配置入口：管理已安装游戏的实例，不持有具体玩法类型/字段。</summary>
    public static class AdsServerConfig
    {
        private static SubGameServerConfigRegistry s_Games = new();
        private static readonly IServerConfigSource s_Source = new YzServerConfigSource();
        public static CommonAdsServerConfig Common { get; private set; } = new();

        // 主玩法来自 GameManager 的明确配置；不根据当前进入的副玩法或安装顺序推断。
        public static GameMode PrimaryGameMode =>
            GameEntry.GameManager != null ? GameEntry.GameManager.PrimaryGameMode : GameMode.None;

        public static bool Register(ISubGameServerConfig config) => s_Games.Register(config);
        public static bool Unregister(ISubGameServerConfig config) => s_Games.Unregister(config);

        public static bool TryGet(GameMode gameMode, out ISubGameServerConfig config) => s_Games.TryGet(gameMode, out config);
        public static bool TryGet<T>(GameMode gameMode, out T config) where T : class, ISubGameServerConfig =>
            s_Games.TryGet(gameMode, out config);

        public static bool TryGetPrimaryLevel(out int level)
        {
            level = 0;
            return TryGet(PrimaryGameMode, out var config) && config.TryGetCurrentLevel(out level);
        }

        public static void Load()
        {
            if (s_Games.IsLoaded) return;
            Common.Load(s_Source);
            s_Games.Load(s_Source);
        }

        // SDK 初始化回调允许刷新预加载阶段的缺省值；不再被先前的 Load() 吞掉。
        public static void ReloadFromServer()
        {
            Common.Load(s_Source);
            s_Games.Load(s_Source, force: true);
            Log.Info("Common and registered subgame server configs loaded.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            s_Games = new SubGameServerConfigRegistry();
            Common = new CommonAdsServerConfig();
        }
    }
}
