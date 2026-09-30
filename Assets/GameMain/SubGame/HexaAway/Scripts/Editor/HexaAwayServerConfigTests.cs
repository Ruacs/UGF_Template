using NUnit.Framework;
namespace Lokas.Editor.Tests
{
    public sealed class HexaAwayServerConfigTests
    {
        [Test] public void InitialAndLoadDefaultsRemainDistinct()
        {
            var config = new HexaAwayServerConfig();
            Assert.AreEqual(3, config.AdRewardItemCount);
            Assert.AreEqual(0, config.NoAdsBeforeLevelCount);
            Assert.AreEqual(8, config.ShowBannerLevel);
            config.Load(new SubGameServerConfigTests.Source());
            Assert.AreEqual(1, config.AdRewardItemCount);
            Assert.AreEqual(5, config.NoAdsBeforeLevelCount);
            Assert.AreEqual(5, config.ShowBannerLevel);
            Assert.IsNull(typeof(HexaAwayServerConfig).GetProperty("FirstWinUnlockLevel"));
        }
        [Test] public void ScopedValuesMalformedAndZeroAreHandled()
        {
            var source = new SubGameServerConfigTests.Source();
            source.Values["ShowBannerLevel"] = "20";
            source.Values["HexaAway.ShowBannerLevel"] = "wrong";
            source.Values["HexaAway.AdRewardItemCount"] = "-3";
            source.Values["HexaAway.ShowAdsInterval"] = "0";
            source.Values["HexaAway.ShowFailLevelAds"] = "false";
            var config = new HexaAwayServerConfig();
            config.Load(source);
            Assert.AreEqual(5, config.ShowBannerLevel);
            Assert.AreEqual(0, config.AdRewardItemCount);
            Assert.AreEqual(0, config.ShowAdsInterval);
            Assert.IsFalse(config.ShowFailLevelAds);
        }
        [Test] public void ListsAndLegacyNameArePerInstance()
        {
            var one = new HexaAwayServerConfig();
            var two = new HexaAwayServerConfig();
            one.IdleHintStages[0].Seconds = 99;
            Assert.AreEqual(5, two.GetIdleHintSeconds(1));
            one.Load(new SubGameServerConfigTests.Source());
            Assert.AreEqual(5, one.GetIdleHintSeconds(1));
            one.SetLevelConfigName("B.json");
            Assert.AreEqual("LevelConfig_B", one.LevelConfigName);
            Assert.AreEqual("LevelConfig_A", two.LevelConfigName);
        }
    }
}
