using System;

namespace Lokas
{
    public sealed class HexaAwayGameData : IGameSaveData
    {
        private static class Keys
        {
            public const string TileSkinId = "HexaAwayTileSkinId";
            public const string CurrentLevelIndex = "HexaAwayCurrentLevelIndex";
            public const string HammerCount = "HexaAwayHammerCount";
            public const string DrillCount = "HexaAwayDrillCount";
            public const string TntCount = "HexaAwayTntCount";
            public const string AddMoveCount = "HexaAwayAddMoveCount";
            public const string PropCountsInitialized = "HexaAwayPropCountsInitialized";
        }

        private int m_CurrentLevel;
        private string m_TileSkinId;

        public string TileSkinId
        {
            get => m_TileSkinId;
            set
            {
                m_TileSkinId = value ?? string.Empty;
                PlayerPrefsManager.SetString(Keys.TileSkinId, m_TileSkinId);
                PlayerPrefsManager.Save();
            }
        }
        private int m_HammerCount;
        private int m_DrillCount;
        private int m_TntCount;
        private int m_AddMoveCount;
        private bool m_PropCountsInitialized;

        public event Action<int> OnCurrentLevelChanged;
        public event Action<HexaAwayPropType, int> OnPropCountChanged;

        /// <summary>HexaAway 关卡库中的 0 基索引。</summary>
        public int CurrentLevel
        {
            get => m_CurrentLevel;
            set
            {
                if (m_CurrentLevel == value)
                {
                    return;
                }

                m_CurrentLevel = value;
                PlayerPrefsManager.SetInt(Keys.CurrentLevelIndex, value);
                PlayerPrefsManager.Save();
                OnCurrentLevelChanged?.Invoke(value);
            }
        }

        public int HammerCount
        {
            get => m_HammerCount;
            set => SetHammerCount(value, true);
        }

        public int DrillCount
        {
            get => m_DrillCount;
            set => SetDrillCount(value, true);
        }

        public int TntCount
        {
            get => m_TntCount;
            set => SetTntCount(value, true);
        }

        public int AddMoveCount
        {
            get => m_AddMoveCount;
            set => SetAddMoveCount(value, true);
        }

        /// <summary>是否已经按配置库写入过道具初始数量，避免每次打开 UI 都重复赠送。</summary>
        public bool PropCountsInitialized
        {
            get => m_PropCountsInitialized;
            set
            {
                if (m_PropCountsInitialized == value)
                {
                    return;
                }

                m_PropCountsInitialized = value;
                PlayerPrefsManager.SetInt(Keys.PropCountsInitialized, value ? 1 : 0);
                PlayerPrefsManager.Save();
            }
        }

        public int GetPropCount(HexaAwayPropType propType)
        {
            switch (propType)
            {
                case HexaAwayPropType.AddMove:
                    return AddMoveCount;
                case HexaAwayPropType.Drill:
                    return DrillCount;
                case HexaAwayPropType.Hammer:
                    return HammerCount;
                case HexaAwayPropType.Tnt:
                    return TntCount;
                default:
                    return 0;
            }
        }

        public void SetPropCount(HexaAwayPropType propType, int count)
        {
            switch (propType)
            {
                case HexaAwayPropType.AddMove:
                    AddMoveCount = count;
                    break;
                case HexaAwayPropType.Drill:
                    DrillCount = count;
                    break;
                case HexaAwayPropType.Hammer:
                    HammerCount = count;
                    break;
                case HexaAwayPropType.Tnt:
                    TntCount = count;
                    break;
            }
        }

        /// <summary>尝试消耗 1 个道具，成功时会同步存档并通知 UI。</summary>
        public bool TryConsumeProp(HexaAwayPropType propType)
        {
            int count = GetPropCount(propType);
            if (count <= 0)
            {
                return false;
            }

            SetPropCount(propType, count - 1);
            return true;
        }

        public void AddPropCount(HexaAwayPropType propType, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetPropCount(propType, GetPropCount(propType) + amount);
        }

        public void Load()
        {
            m_TileSkinId = PlayerPrefsManager.GetString(Keys.TileSkinId, string.Empty);
            m_CurrentLevel = PlayerPrefsManager.GetInt(Keys.CurrentLevelIndex, 0);
            m_HammerCount = PlayerPrefsManager.GetInt(Keys.HammerCount, 99);
            m_DrillCount = PlayerPrefsManager.GetInt(Keys.DrillCount, 99);
            m_TntCount = PlayerPrefsManager.GetInt(Keys.TntCount, 99);
            m_AddMoveCount = PlayerPrefsManager.GetInt(Keys.AddMoveCount, 99);
            m_PropCountsInitialized = PlayerPrefsManager.GetInt(Keys.PropCountsInitialized, 99) == 1;
        }

        public void Save()
        {
            PlayerPrefsManager.SetString(Keys.TileSkinId, m_TileSkinId ?? string.Empty);
            PlayerPrefsManager.SetInt(Keys.CurrentLevelIndex, m_CurrentLevel);
            PlayerPrefsManager.SetInt(Keys.HammerCount, m_HammerCount);
            PlayerPrefsManager.SetInt(Keys.DrillCount, m_DrillCount);
            PlayerPrefsManager.SetInt(Keys.TntCount, m_TntCount);
            PlayerPrefsManager.SetInt(Keys.AddMoveCount, m_AddMoveCount);
            PlayerPrefsManager.SetInt(Keys.PropCountsInitialized, m_PropCountsInitialized ? 1 : 0);
        }

        private void SetHammerCount(int value, bool notify)
        {
            SetCount(ref m_HammerCount, Keys.HammerCount, HexaAwayPropType.Hammer, value, notify);
        }

        private void SetDrillCount(int value, bool notify)
        {
            SetCount(ref m_DrillCount, Keys.DrillCount, HexaAwayPropType.Drill, value, notify);
        }

        private void SetTntCount(int value, bool notify)
        {
            SetCount(ref m_TntCount, Keys.TntCount, HexaAwayPropType.Tnt, value, notify);
        }

        private void SetAddMoveCount(int value, bool notify)
        {
            SetCount(ref m_AddMoveCount, Keys.AddMoveCount, HexaAwayPropType.AddMove, value, notify);
        }

        private void SetCount(ref int field, string key, HexaAwayPropType propType, int value, bool notify)
        {
            int normalizedValue = value < 0 ? 0 : value;
            if (field == normalizedValue)
            {
                return;
            }

            field = normalizedValue;
            PlayerPrefsManager.SetInt(key, normalizedValue);
            PlayerPrefsManager.Save();

            if (notify)
            {
                OnPropCountChanged?.Invoke(propType, normalizedValue);
            }
        }
    }
}
