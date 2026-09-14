using LitJson;

namespace Lokas
{
    /// <summary>公共广告和 UI 只读取此能力视图，不引用具体游戏配置。</summary>
    public interface ISubGameServerConfig
    {
        GameMode GameMode { get; }
        int ShowAdsInterval { get; }
        int NoAdsBeforeLevelCount { get; }
        int AdRewardItemCount { get; }
        int ShowBannerLevel { get; }
        void Load(IServerConfigSource source);
        // 沿用各游戏已有的关卡计数，不在公共广告层猜测存档类型或 +1。
        bool TryGetCurrentLevel(out int level);
    }

    public interface IServerConfigSource
    {
        bool Contains(string key);
        int GetInt(string key, int fallback);
        bool GetBool(string key, bool fallback);
        string GetString(string key, string fallback);
        JsonData GetArray(string key);
    }
}
