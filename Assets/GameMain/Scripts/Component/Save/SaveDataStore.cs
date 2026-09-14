using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using LocalizationLanguage = GameFramework.Localization.Language;

namespace Lokas
{
    public sealed class SaveDataStore
    {
        [Serializable]
        private class JsonValueWrapper<T>
        {
            public T value;
        }

        private static class Keys
        {
            public const string Money = "Money";
            public const string IsMusic = "IsMusic";
            public const string IsSound = "IsSound";
            public const string IsVibration = "IsVibration";
            public const string SignedInDays = "SignedInDays";
            public const string LastSignInDay = "LastSignInDay";
            public const string PlayerName = "PlayerName";
            public const string AvatarId = "AvatarId";
            public const string AvatarFrameId = "AvatarFrameId";
            public const string LastExitTime = "LastExitTime";
            public const string IsFirstLaunch = "IsFirstLaunch";
            public const string Language = "Language";
            public const string EventKeyPrefix = "EventKey_";
            public const string JsonDataPrefix = "JsonData_";
            public const string IsNoAds = "IsNoAds";
            public const string FirstWinDate = "FirstWin_Date";
            public const string FirstWinCount = "FirstWin_Count";
            public const string AdsUnavailableCountdownRewardDate = "AdsUnavailableCountdownReward_Date";
            public const string AdsUnavailableCountdownRewardCount = "AdsUnavailableCountdownReward_Count";
        }

        private readonly Dictionary<Type, IGameSaveData> m_Modules = new();

        private int m_Money;
        private bool m_IsSound;
        private bool m_IsMusic;
        private bool m_IsVibration;
        private int m_SignedInDays;
        private string m_LastSignInDay;
        private string m_PlayerName;
        private int m_AvatarId;
        private int m_AvatarFrameId;
        private long m_LastExitTime;
        private bool m_IsFirstLaunch;
        private LocalizationLanguage m_Language;
        private bool m_IsNoAds;
        private string m_FirstWinDate;
        private int m_FirstWinCount;
        private string m_AdsUnavailableCountdownRewardDate;
        private int m_AdsUnavailableCountdownRewardCount;

        private bool m_IsLoaded;

        public bool IsCN => m_Language == LocalizationLanguage.ChineseSimplified || m_Language == LocalizationLanguage.ChineseTraditional;

        public int Money
        {
            get => m_Money;
            set
            {
                m_Money = value;
                PlayerPrefsManager.SetInt(Keys.Money, value);
                PlayerPrefsManager.Save();
            }
        }

        public bool IsSound
        {
            get => m_IsSound;
            set
            {
                m_IsSound = value;
                PlayerPrefsManager.SetBool(Keys.IsSound, value);
                PlayerPrefsManager.Save();
            }
        }

        public bool IsMusic
        {
            get => m_IsMusic;
            set
            {
                m_IsMusic = value;
                PlayerPrefsManager.SetBool(Keys.IsMusic, value);
                PlayerPrefsManager.Save();
            }
        }

        public bool IsVibration
        {
            get => m_IsVibration;
            set
            {
                m_IsVibration = value;
                PlayerPrefsManager.SetBool(Keys.IsVibration, value);
                PlayerPrefsManager.Save();
            }
        }

        public int SignedInDays
        {
            get => m_SignedInDays;
            private set
            {
                m_SignedInDays = value;
                PlayerPrefsManager.SetInt(Keys.SignedInDays, value);
            }
        }

        public string LastSignInDay
        {
            get => m_LastSignInDay;
            private set
            {
                m_LastSignInDay = value;
                PlayerPrefsManager.SetString(Keys.LastSignInDay, value);
            }
        }

        public string PlayerName
        {
            get => m_PlayerName;
            set
            {
                m_PlayerName = value;
                PlayerPrefsManager.SetString(Keys.PlayerName, value);
                PlayerPrefsManager.Save();
            }
        }

        public int AvatarId
        {
            get => m_AvatarId;
            set
            {
                m_AvatarId = value;
                PlayerPrefsManager.SetInt(Keys.AvatarId, value);
                PlayerPrefsManager.Save();
            }
        }

        public int AvatarFrameId
        {
            get => m_AvatarFrameId;
            set
            {
                m_AvatarFrameId = value;
                PlayerPrefsManager.SetInt(Keys.AvatarFrameId, value);
                PlayerPrefsManager.Save();
            }
        }

        public bool IsFirstLaunch
        {
            get => m_IsFirstLaunch;
            set
            {
                m_IsFirstLaunch = value;
                PlayerPrefsManager.SetBool(Keys.IsFirstLaunch, value);
                PlayerPrefsManager.Save();
            }
        }

        public LocalizationLanguage Language
        {
            get => m_Language;
            set
            {
                m_Language = value;
                if (GameEntry.Localization != null)
                {
                    GameEntry.Localization.Language = m_Language;
                }

                PlayerPrefsManager.SetString(Keys.Language, value.ToString());
                PlayerPrefsManager.Save();
            }
        }

        public long LastExitTime
        {
            get => m_LastExitTime;
            set
            {
                m_LastExitTime = value;
                PlayerPrefsManager.SetString(Keys.LastExitTime, value.ToString());
                PlayerPrefsManager.Save();
            }
        }

        public bool IsNoAds
        {
            get => m_IsNoAds;
            set
            {
                m_IsNoAds = value;
                PlayerPrefsManager.SetBool(Keys.IsNoAds, value);
                PlayerPrefsManager.Save();
            }
        }

        public int FirstWinCount
        {
            get => m_FirstWinCount;
            private set
            {
                m_FirstWinCount = value;
                PlayerPrefsManager.SetInt(Keys.FirstWinCount, value);
                PlayerPrefsManager.Save();
            }
        }

        public void Register<T>(T module) where T : class, IGameSaveData
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (m_Modules.TryGetValue(typeof(T), out var existing))
            {
                if (ReferenceEquals(existing, module)) return;
                throw new InvalidOperationException("Duplicate save module: " + typeof(T).FullName);
            }
            if (m_IsLoaded) module.Load();
            m_Modules.Add(typeof(T), module);
        }

        public T Get<T>() where T : class, IGameSaveData
        {
            return m_Modules.TryGetValue(typeof(T), out IGameSaveData module) ? module as T : null;
        }

        public void Load()
        {
            if (m_IsLoaded) return;
            m_Money = PlayerPrefsManager.GetInt(Keys.Money, 0);
            m_IsMusic = PlayerPrefsManager.GetBool(Keys.IsMusic, true);
            m_IsSound = PlayerPrefsManager.GetBool(Keys.IsSound, true);
            m_IsVibration = PlayerPrefsManager.GetBool(Keys.IsVibration, true);
            m_SignedInDays = PlayerPrefsManager.GetInt(Keys.SignedInDays, 0);
            m_LastSignInDay = PlayerPrefsManager.GetString(Keys.LastSignInDay, DateTime.Today.ToString("yyyy-MM-dd"));
            m_PlayerName = PlayerPrefsManager.GetString(Keys.PlayerName, "Player");
            m_AvatarId = PlayerPrefsManager.GetInt(Keys.AvatarId, 1);
            m_AvatarFrameId = PlayerPrefsManager.GetInt(Keys.AvatarFrameId, 1);
            m_LastExitTime = long.TryParse(PlayerPrefsManager.GetString(Keys.LastExitTime, "0"), out long lastExitTime) ? lastExitTime : 0;
            m_IsFirstLaunch = PlayerPrefsManager.GetBool(Keys.IsFirstLaunch, true);
            m_Language = Enum.TryParse(PlayerPrefsManager.GetString(Keys.Language, LocalizationLanguage.Unspecified.ToString()), out LocalizationLanguage language)
                ? language
                : LocalizationLanguage.Unspecified;
            m_IsNoAds = PlayerPrefsManager.GetBool(Keys.IsNoAds, false);
            m_FirstWinDate = PlayerPrefsManager.GetString(Keys.FirstWinDate, string.Empty);
            m_FirstWinCount = PlayerPrefsManager.GetInt(Keys.FirstWinCount, 0);
            m_AdsUnavailableCountdownRewardDate = PlayerPrefsManager.GetString(Keys.AdsUnavailableCountdownRewardDate, string.Empty);
            m_AdsUnavailableCountdownRewardCount = PlayerPrefsManager.GetInt(Keys.AdsUnavailableCountdownRewardCount, 0);

            foreach (IGameSaveData module in m_Modules.Values)
            {
                module.Load();
            }
            m_IsLoaded = true;
        }

        public void SaveAll()
        {
            PlayerPrefsManager.SetInt(Keys.Money, m_Money);
            PlayerPrefsManager.SetBool(Keys.IsMusic, m_IsMusic);
            PlayerPrefsManager.SetBool(Keys.IsSound, m_IsSound);
            PlayerPrefsManager.SetBool(Keys.IsVibration, m_IsVibration);
            PlayerPrefsManager.SetInt(Keys.SignedInDays, m_SignedInDays);
            PlayerPrefsManager.SetString(Keys.LastSignInDay, m_LastSignInDay);
            PlayerPrefsManager.SetString(Keys.PlayerName, m_PlayerName);
            PlayerPrefsManager.SetInt(Keys.AvatarId, m_AvatarId);
            PlayerPrefsManager.SetInt(Keys.AvatarFrameId, m_AvatarFrameId);
            PlayerPrefsManager.SetString(Keys.LastExitTime, m_LastExitTime.ToString());
            PlayerPrefsManager.SetBool(Keys.IsFirstLaunch, m_IsFirstLaunch);
            PlayerPrefsManager.SetString(Keys.Language, m_Language.ToString());
            PlayerPrefsManager.SetBool(Keys.IsNoAds, m_IsNoAds);
            PlayerPrefsManager.SetString(Keys.FirstWinDate, m_FirstWinDate);
            PlayerPrefsManager.SetInt(Keys.FirstWinCount, m_FirstWinCount);
            PlayerPrefsManager.SetString(Keys.AdsUnavailableCountdownRewardDate, m_AdsUnavailableCountdownRewardDate);
            PlayerPrefsManager.SetInt(Keys.AdsUnavailableCountdownRewardCount, m_AdsUnavailableCountdownRewardCount);

            foreach (IGameSaveData module in m_Modules.Values)
            {
                module.Save();
            }

            PlayerPrefsManager.Save();
        }

        public bool CanSignIn()
        {
            return DateTime.Today.ToString("yyyy-MM-dd") != m_LastSignInDay;
        }

        public void SignIn()
        {
            if (!CanSignIn())
            {
                return;
            }

            SignedInDays = m_SignedInDays + 1;
            LastSignInDay = DateTime.Today.ToString("yyyy-MM-dd");
            PlayerPrefsManager.Save();
        }

        public bool TryClaimDailyFirstWin()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (m_FirstWinDate == today)
            {
                return false;
            }

            m_FirstWinDate = today;
            PlayerPrefsManager.SetString(Keys.FirstWinDate, today);
            FirstWinCount = m_FirstWinCount % 7 + 1;
            return true;
        }

        public void ForceClaimFirstWin()
        {
            FirstWinCount = m_FirstWinCount % 7 + 1;
        }

        public bool CanClaimAdsUnavailableCountdownReward(int dailyLimit)
        {
            RefreshAdsUnavailableCountdownRewardDate();
            return dailyLimit > 0 && m_AdsUnavailableCountdownRewardCount < dailyLimit;
        }

        public bool TryClaimAdsUnavailableCountdownReward(int dailyLimit)
        {
            if (!CanClaimAdsUnavailableCountdownReward(dailyLimit))
            {
                return false;
            }

            m_AdsUnavailableCountdownRewardCount++;
            PlayerPrefsManager.SetInt(Keys.AdsUnavailableCountdownRewardCount, m_AdsUnavailableCountdownRewardCount);
            PlayerPrefsManager.Save();
            return true;
        }

        private void RefreshAdsUnavailableCountdownRewardDate()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (m_AdsUnavailableCountdownRewardDate == today)
            {
                return;
            }

            m_AdsUnavailableCountdownRewardDate = today;
            m_AdsUnavailableCountdownRewardCount = 0;
            PlayerPrefsManager.SetString(Keys.AdsUnavailableCountdownRewardDate, today);
            PlayerPrefsManager.SetInt(Keys.AdsUnavailableCountdownRewardCount, m_AdsUnavailableCountdownRewardCount);
            PlayerPrefsManager.Save();
        }

        public bool GetEventData(string key)
            => PlayerPrefsManager.GetBool(Keys.EventKeyPrefix + key, false);

        public int GetEventData(string key, int defaultValue)
            => PlayerPrefsManager.GetInt(Keys.EventKeyPrefix + key, defaultValue);

        public void SetEventData(string key)
        {
            PlayerPrefsManager.SetBool(Keys.EventKeyPrefix + key, true);
            PlayerPrefsManager.Save();
        }

        public void SetEventData(string key, int value)
        {
            PlayerPrefsManager.SetInt(Keys.EventKeyPrefix + key, value);
            PlayerPrefsManager.Save();
        }

        public void SetData<T>(string key, T data)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            try
            {
                var wrapper = new JsonValueWrapper<T> { value = data };
                PlayerPrefsManager.SetString(Keys.JsonDataPrefix + key, JsonUtility.ToJson(wrapper));
                PlayerPrefsManager.Save();
            }
            catch (Exception ex)
            {
                Log.Warning($"[SaveData] SetData failed, key={key}, err={ex.Message}");
            }
        }

        public T GetData<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            var payload = PlayerPrefsManager.GetString(Keys.JsonDataPrefix + key, string.Empty);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return defaultValue;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<JsonValueWrapper<T>>(payload);
                return wrapper == null ? defaultValue : wrapper.value;
            }
            catch (Exception ex)
            {
                Log.Warning($"[SaveData] GetData failed, key={key}, err={ex.Message}");
                return defaultValue;
            }
        }
    }
}
