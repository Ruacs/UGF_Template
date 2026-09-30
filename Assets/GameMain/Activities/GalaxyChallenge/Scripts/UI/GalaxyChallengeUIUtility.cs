using System;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    internal static class GalaxyChallengeUIUtility
    {
        public static GalaxyChallengeActivityModule ResolveModule(object userData)
        {
            GalaxyChallengeActivityModule module =
                (userData as ActivityPageUserData)?.Request.Arguments as GalaxyChallengeActivityModule;
            if (module == null)
                GameEntry.Activities?.TryGetModule(GalaxyChallengeActivityModule.Id, out module);
            return module;
        }

        public static string FormatCountdown(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "00:00:00";
            return remaining.TotalDays >= 1
                ? string.Format("{0}d {1:00}:{2:00}:{3:00}", remaining.Days, remaining.Hours,
                    remaining.Minutes, remaining.Seconds)
                : string.Format("{0:00}:{1:00}:{2:00}", remaining.Hours, remaining.Minutes,
                    remaining.Seconds);
        }
    }
}
