using System;
using System.Collections.Generic;
using System.Globalization;
using LitJson;
using NUnit.Framework;

namespace Lokas.Editor.Tests
{
    public sealed class SubGameServerConfigTests
    {
        public sealed class Source : IServerConfigSource
        {
            public readonly Dictionary<string, string> Values = new();
            public bool Contains(string key) => Values.ContainsKey(key);
            public string GetString(string key, string fallback) => Values.TryGetValue(key, out var value) ? value : fallback;
            public int GetInt(string key, int fallback) => int.TryParse(GetString(key, null),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
            public bool GetBool(string key, bool fallback) => bool.TryParse(GetString(key, null), out bool value) ? value : fallback;
            public JsonData GetArray(string key) => null;
        }
        private sealed class Config : ISubGameServerConfig
        {
            public GameMode GameMode { get; }
            public Config(GameMode mode) => GameMode = mode;
            public int ShowAdsInterval => 1;
            public int NoAdsBeforeLevelCount { get; private set; }
            public int AdRewardItemCount => 1;
            public int ShowBannerLevel { get; private set; }
            public void Load(IServerConfigSource source)
            {
                ShowBannerLevel = source.GetInt("ShowBannerLevel", 5);
                NoAdsBeforeLevelCount = source.GetInt("NoAdsBeforeLevelCount", 5);
            }
            public bool TryGetCurrentLevel(out int level) { level = 0; return false; }
        }
        [Test] public void EmptyRegistryDoesNotFallback()
        {
            var registry = new SubGameServerConfigRegistry();
            Assert.IsFalse(registry.TryGet(GameMode.None, out _));
            Assert.IsFalse(registry.TryGet((GameMode)100, out _));
            registry.Load(new Source());
            Assert.AreEqual(0, registry.Count);
        }
        [Test] public void DuplicateAndStaleRegistration()
        {
            var registry = new SubGameServerConfigRegistry();
            var one = new Config((GameMode)100);
            var two = new Config((GameMode)101);
            Assert.IsTrue(registry.Register(one));
            Assert.IsFalse(registry.Register(one));
            Assert.Throws<InvalidOperationException>(() => registry.Register(new Config((GameMode)100)));
            registry.Register(two);
            Assert.IsFalse(registry.Unregister(new Config((GameMode)100)));
            Assert.IsTrue(registry.Unregister(one));
            Assert.IsTrue(registry.TryGet((GameMode)101, out var found));
            Assert.AreSame(two, found);
        }
        [Test] public void LateRegistrationAndExplicitServerRefresh()
        {
            var registry = new SubGameServerConfigRegistry();
            var source = new Source();
            source.Values["ShowBannerLevel"] = "17";
            registry.Load(source);
            var config = new Config((GameMode)100);
            registry.Register(config);
            Assert.AreEqual(17, config.ShowBannerLevel);
            source.Values["ShowBannerLevel"] = "18";
            registry.Load(source);
            Assert.AreEqual(17, config.ShowBannerLevel);
            registry.Load(source, force: true);
            Assert.AreEqual(18, config.ShowBannerLevel);
        }
        [Test] public void ScopedKeysDoNotMixGames()
        {
            var source = new Source();
            source.Values["Value"] = "12";
            source.Values["One.Value"] = "18";
            Assert.AreEqual(18, new ScopedServerConfigSource(source, "One").GetInt("Value", 5));
            Assert.AreEqual(12, new ScopedServerConfigSource(source, "Two").GetInt("Value", 5));
        }
        [Test] public void ZeroFalseAndMalformedScopedValues()
        {
            var source = new Source();
            source.Values["One.Value"] = "0";
            source.Values["One.Flag"] = "false";
            var scoped = new ScopedServerConfigSource(source, "One");
            Assert.AreEqual(0, scoped.GetInt("Value", 5));
            Assert.IsFalse(scoped.GetBool("Flag", true));
            source.Values["Value"] = "20";
            source.Values["One.Value"] = "invalid";
            Assert.AreEqual(5, scoped.GetInt("Value", 5));
        }
        [Test] public void IdleHintAliasesAndMalformedValues()
        {
            var json = JsonMapper.ToObject("[{\"startLevel\":4,\"endLevel\":8,\"idleHintSeconds\":15},{\"seconds\":\"bad\"}]");
            var stages = SubGameServerConfigParsing.ParseIdleHintStages(json, new List<IdleHintStage>());
            Assert.AreEqual(1, stages.Count);
            Assert.IsTrue(stages[0].ContainsLevel(4));
            Assert.IsFalse(stages[0].ContainsLevel(9));
            Assert.AreEqual(15, stages[0].Seconds);
        }
        [Test] public void SevenFeatureFieldsStayCommon()
        {
            var source = new Source();
            source.Values["RankUnlockLevel"] = "12";
            source.Values["Other.RankUnlockLevel"] = "99";
            source.Values["ShowFirstWinClaim2Button"] = "false";
            var common = new CommonAdsServerConfig();
            Assert.AreEqual(10, common.TaskUnlockLevel);
            common.Load(source);
            Assert.AreEqual(6, common.TaskUnlockLevel);
            Assert.AreEqual(12, common.RankUnlockLevel);
            Assert.IsFalse(common.ShowFirstWinClaim2Button);
            Assert.IsTrue(common.EnableShop);
            foreach (string name in new[] { "LevelTimerIdleStopSeconds", "TaskUnlockLevel", "RankUnlockLevel",
                "ShopUnlockLevel", "FirstWinUnlockLevel", "ShowFirstWinClaim2Button", "EnableShop" })
            {
                Assert.IsNotNull(typeof(CommonAdsServerConfig).GetProperty(name));
                Assert.IsNull(typeof(ISubGameServerConfig).GetProperty(name));
            }
        }
        [Test] public void GlobalAdQuotaIgnoresGameOverrides()
        {
            var source = new Source();
            source.Values["AdsUnavailableCountdownRewardDailyLimit"] = "0";
            source.Values["Other.AdsUnavailableCountdownRewardDailyLimit"] = "10";
            source.Values["EnableAdsUnavailablePanel"] = "false";
            var common = new CommonAdsServerConfig();
            common.Load(source);
            Assert.AreEqual(0, common.AdsUnavailableCountdownRewardDailyLimit);
            Assert.IsFalse(common.EnableAdsUnavailablePanel);
        }
    }
}
