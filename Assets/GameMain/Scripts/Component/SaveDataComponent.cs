using LocalizationLanguage = GameFramework.Localization.Language;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public sealed class SaveDataComponent : GameFrameworkComponent
    {
        public SaveDataStore Store { get; private set; }
        public bool IsInitialized { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Store = new SaveDataStore();
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            Store ??= new SaveDataStore();
            Store.Load();
            IsInitialized = true;
        }

        public bool IsCN => Store.IsCN;

        public int Money
        {
            get => Store.Money;
            set => Store.Money = value;
        }

        public bool IsSound
        {
            get => Store.IsSound;
            set => Store.IsSound = value;
        }

        public bool IsMusic
        {
            get => Store.IsMusic;
            set => Store.IsMusic = value;
        }

        public bool IsVibration
        {
            get => Store.IsVibration;
            set => Store.IsVibration = value;
        }

        public int SignedInDays => Store.SignedInDays;

        public string LastSignInDay => Store.LastSignInDay;

        public string PlayerName
        {
            get => Store.PlayerName;
            set => Store.PlayerName = value;
        }

        public int AvatarId
        {
            get => Store.AvatarId;
            set => Store.AvatarId = value;
        }

        public int AvatarFrameId
        {
            get => Store.AvatarFrameId;
            set => Store.AvatarFrameId = value;
        }

        public bool IsFirstLaunch
        {
            get => Store.IsFirstLaunch;
            set => Store.IsFirstLaunch = value;
        }

        public LocalizationLanguage Language
        {
            get => Store.Language;
            set => Store.Language = value;
        }

        public long LastExitTime
        {
            get => Store.LastExitTime;
            set => Store.LastExitTime = value;
        }

        public bool IsNoAds
        {
            get => Store.IsNoAds;
            set => Store.IsNoAds = value;
        }

        public int FirstWinCount => Store.FirstWinCount;

        public T Get<T>() where T : class, IGameSaveData
        {
            Initialize();
            return Store.Get<T>();
        }

        public bool TryClaimDailyFirstWin()
        {
            Initialize();
            return Store.TryClaimDailyFirstWin();
        }

        public void ForceClaimFirstWin()
        {
            Initialize();
            Store.ForceClaimFirstWin();
        }

        public bool CanClaimAdsUnavailableCountdownReward(int dailyLimit)
        {
            Initialize();
            return Store.CanClaimAdsUnavailableCountdownReward(dailyLimit);
        }

        public bool TryClaimAdsUnavailableCountdownReward(int dailyLimit)
        {
            Initialize();
            return Store.TryClaimAdsUnavailableCountdownReward(dailyLimit);
        }

        public bool CanSignIn()
        {
            Initialize();
            return Store.CanSignIn();
        }

        public void SignIn()
        {
            Initialize();
            Store.SignIn();
        }

        public bool GetEventData(string key)
        {
            Initialize();
            return Store.GetEventData(key);
        }

        public int GetEventData(string key, int defaultValue)
        {
            Initialize();
            return Store.GetEventData(key, defaultValue);
        }

        public void SetEventData(string key)
        {
            Initialize();
            Store.SetEventData(key);
        }

        public void SetEventData(string key, int value)
        {
            Initialize();
            Store.SetEventData(key, value);
        }

        public void SetData<T>(string key, T data)
        {
            Initialize();
            Store.SetData(key, data);
        }

        public T GetData<T>(string key, T defaultValue = default)
        {
            Initialize();
            return Store.GetData(key, defaultValue);
        }

        public void SaveAll()
        {
            Initialize();
            Store.SaveAll();
        }
    }
}
