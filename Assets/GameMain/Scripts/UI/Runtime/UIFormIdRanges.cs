namespace Lokas
{
    /// <summary>
    /// UIForm ID allocation policy. Keep each activity inside its own block so
    /// adding a page to one activity never consumes another activity's IDs.
    /// </summary>
    public static class UIFormIdRanges
    {
        public const int CommonStart = 100;
        public const int CommonEnd = 199;

        // Non-activity shared/sample block. Package manifests allocate available IDs here.
        public const int SampleSubGameStart = 200;
        public const int SampleSubGameEnd = 299;

        public const int ActivityBlockSize = 20;
        public const int ActivityStart = 300;

        public const int SeasonPassStart = 300;
        public const int SeasonPassEnd = SeasonPassStart + ActivityBlockSize - 1;
        public const int SeasonPassMain = 300;
        public const int SeasonPassRules = 301;
        public const int SeasonPassGoldPassPurchase = 302;

        public const int RaceStart = 320;
        public const int RaceEnd = RaceStart + ActivityBlockSize - 1;
        public const int RaceStartPage = 320;
        public const int RaceMain = 321;
        public const int RaceDetails = 322;

        public const int CollectorStart = 340;
        public const int CollectorEnd = CollectorStart + ActivityBlockSize - 1;
        public const int CollectorMain = 340;

        public const int WinStreakStart = 360;
        public const int WinStreakEnd = WinStreakStart + ActivityBlockSize - 1;
        public const int WinStreakStartPage = 360;
        public const int WinStreakMain = 361;
        public const int WinStreakDetails = 362;
        public const int WinStreakEndPage = 363;

        public const int MiningStart = 380;
        public const int MiningEnd = MiningStart + ActivityBlockSize - 1;
        public const int MiningMain = 380;
        public const int MiningStartPage = 381;
        public const int MiningDetails = 382;
        public const int MiningEndPage = 383;

        public const int GalaxyChallengeStart = 400;
        public const int GalaxyChallengeEnd = GalaxyChallengeStart + ActivityBlockSize - 1;
        public const int GalaxyChallengeStartPage = 400;
        public const int GalaxyChallengeMain = 401;
        public const int GalaxyChallengeDetails = 402;
        public const int GalaxyChallengeEndPage = 403;
        public const int GalaxyChallengeInterval = 404;

        public static bool IsValid(int id)
        {
            return id > 0;
        }

        public static bool IsActivityId(int id)
        {
            return id >= ActivityStart;
        }

        public static bool IsInRange(int id, int start, int end)
        {
            return id >= start && id <= end;
        }
    }
}
