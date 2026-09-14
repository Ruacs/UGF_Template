using LitJson;

namespace Ads
{
    /// <summary>
    /// Central analytics entry for game code, keeping SDK calls behind the Ads layer.
    /// </summary>
    public static class AdsAnalytics
    {
        public static void GameStart(int level, string gameName = "")
        {
            EventLevel(level, YzAdComponent.LevelStatus.GameStart, gameName);
            // YzAdComponent.YzGameAnalytics.GameStart(level, gameName);
        }


        public static void GameWin(int level, bool showAd = true, string gameName = "")
        {
            YzAdComponent.YzGameAnalytics.GameWin(level, showAd, gameName);
        }

        public static void GameFail(int level, bool showAd = true, string gameName = "")
        {
            if (showAd)
            {
                YzAdComponent.YzUtils.adManager.showIntersititialAd(YzAdComponent.YzAdLocation.Over);
            }
        }

        public static void GameSkip(int level, string gameName = "")
        {
            EventLevel(level, YzAdComponent.LevelStatus.GameSkip, gameName);
        }

        public static void EventLevel(int level, string levelStatus, string gameName = "")
        {
            YzAdComponent.YzGameAnalytics.EventLevel(level, levelStatus, gameName);
        }

        public static void EventLevelDuration(int level, string levelStatus, long duration, string gameName = "")
        {
            YzAdComponent.YzGameAnalytics.EventLevelDuration(level, levelStatus, duration, gameName);
        }

        public static void EventWithName(string eventName)
        {
            YzAdComponent.YzGameAnalytics.EventWithName(eventName);
        }

        public static void EventWithName(string eventName, params (string key, object value)[] param)
        {
            if (param == null || param.Length == 0)
            {
                EventWithName(eventName);
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder(eventName);

            foreach (var (key, value) in param)
            {
                sb.Append("|");
                sb.Append(key);
                sb.Append("=");
                sb.Append(value);
            }

            EventWithName(sb.ToString());
        }

        public static void EventWithStringValue(string eventName, string value)
        {
        }

        public static void EventWithIntValue(string eventName, float value)
        {
        }

        public static void EventWithJsonValue(string eventName, JsonData value)
        {

        }
    }
}
