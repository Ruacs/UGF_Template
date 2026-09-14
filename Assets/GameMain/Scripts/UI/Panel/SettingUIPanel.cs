using Ads;
using DG.Tweening;
using GameFramework.Event;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public enum SettingType
    {
        Main,
        Game
    }

    public struct SettingUIData
    {
        public SettingType settingType;
        public object userData;
        public GameMode gameMode;

        public SettingUIData(SettingType type, object data = null)
        {
            settingType = type;
            userData = data;
            gameMode = GameMode.None;
        }
        public SettingUIData(GameMode mode, object data)
        {
            settingType = SettingType.Game;
            gameMode = mode;
            userData = data;
        }
    }

    public class SettingUIPanel : UGuiForm
    {
        [SerializeField] private RectTransform m_closeBtnRT;
        [SerializeField] private ToggleHandle m_MusicToggle;
        [SerializeField] private ToggleHandle m_SoundToggle;
        [SerializeField] private ToggleHandle m_VibrationToggle;
        [SerializeField] private Transform m_PrivacyRoot;
        [SerializeField] private Button m_VersionBtn;
        [SerializeField] private RectTransform m_LanguageBtnRT;
        [SerializeField] private ToggleHandle m_CountToggle;
        [SerializeField] private CanvasGroup m_CountToggleLightCG;
        [SerializeField] private RectTransform m_ButtonGroups;
        [SerializeField] private Button m_HomeBtn;
        [SerializeField] private Button m_RestartBtn;
        [SerializeField] private TMP_Text m_versionTMP;
        [SerializeField] private GameObject m_GMMode;
        [SerializeField] private Toggle m_DebugToggle;

        private bool m_IsIniting;

        private SettingUIData m_settingUIData;
        private SubGameManagerComponent m_GameSettings;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            m_closeBtnRT?.GetComponent<Button>()?.AddSafeClick(OnClickClose);
            m_MusicToggle?.toggle.onValueChanged.AddListener(OnChangeMusic);
            m_SoundToggle?.toggle.onValueChanged.AddListener(OnChangeSound);
            m_VibrationToggle?.toggle.onValueChanged.AddListener(OnChangeVibration);
            m_VersionBtn?.onClick.AddListener(OnVersionClick);
            m_LanguageBtnRT?.GetComponent<Button>()?.AddSafeClick(OnLanguageClick);
            m_CountToggle?.toggle.onValueChanged.AddListener(OnChangeBlockCount);


            m_HomeBtn?.AddSafeClick(OnHomeClick);
            m_RestartBtn?.AddSafeClick(OnRestartClick);

            if (m_GMMode != null) m_GMMode.SetActive(GameEntry.GMMode);
            if (m_DebugToggle != null) m_DebugToggle.onValueChanged.AddListener(OnDebugClick);
            if (m_PrivacyRoot != null) m_PrivacyRoot.gameObject.SetActive(false);
            if (m_versionTMP != null) m_versionTMP.SetText($"v{Application.version}");

            if(GameEntry.GMMode)
            {
                OnDebugOpened();
            }

        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_CountToggleLightCG.gameObject.SetActive(false);
            AdsAnalytics.EventWithName("Setting_Open");
            GameEntry.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);

            m_settingUIData = (SettingUIData)userData;

            bool isInGame = m_settingUIData.settingType != SettingType.Main;
            m_GameSettings = isInGame ? GameEntry.SubGames?.Get(m_settingUIData.gameMode) : null;
            m_ButtonGroups.gameObject.SetActive(isInGame);
            m_CountToggle.gameObject.SetActive(m_GameSettings != null && m_GameSettings.SupportsBlockCountSetting);
            InitToggles();

            if (m_DebugToggle != null)
                m_DebugToggle.SetIsOnWithoutNotify(GameEntry.TestMode != null && GameEntry.TestMode.IsWindowVisible);

            if (m_GameSettings != null && m_GameSettings.TryConsumeBlockCountHint())
            {
                m_CountToggleLightCG.gameObject.SetActive(true);
                m_CountToggleLightCG?.DOKill();
                m_CountToggleLightCG?.DOFade(0, 0.5f).From(1).SetLoops(7, LoopType.Yoyo).SetEase(Ease.Linear);
            }

        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            AdsAnalytics.EventWithName("Setting_Close");
            GameEntry.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
            base.OnClose(isShutdown, userData);
        }

        private void InitToggles()
        {
            m_IsIniting = true;
            m_MusicToggle?.Init(GameEntry.SaveData.IsMusic);
            m_SoundToggle?.Init(GameEntry.SaveData.IsSound);
            m_VibrationToggle?.Init(GameEntry.SaveData.IsVibration);
            if (m_GameSettings != null && m_GameSettings.SupportsBlockCountSetting)
            {
                m_CountToggle?.Init(m_GameSettings.ShowBlockCount);
            }
            m_IsIniting = false;
        }

        private void OnLanguageClick()
        {
            PlayUISound(SoundId.UI_Click);
            Action action = ReloadLanguage;
            GameEntry.UI.OpenUIForm(UIFormId.LanguageUIPanel, userData: action);
        }

        private void ReloadLanguage()
        {
            GameEntry.Localization.RemoveAllRawStrings();
            GameEntry.Localization.LoadLanguage(this);
        }

        private void OnLanguageReloaded(object sender, GameEventArgs e)
        {
            GameEntry.UI.UpdateLocalizationTexts();
        }

        private void OnChangeMusic(bool isOn)
        {
            if (!m_IsIniting) PlayUISound(SoundId.UI_Click);
            GameEntry.Sound.Mute("Music", !isOn);
            GameEntry.SaveData.IsMusic = isOn;
        }

        private void OnChangeSound(bool isOn)
        {
            if (!m_IsIniting) PlayUISound(SoundId.UI_Click);
            GameEntry.Sound.Mute("Sound", !isOn);
            GameEntry.Sound.Mute("UISound", !isOn);
            GameEntry.SaveData.IsSound = isOn;
        }

        private void OnChangeVibration(bool isOn)
        {
            if (!m_IsIniting) PlayUISound(SoundId.UI_Click);
            GameEntry.SaveData.IsVibration = isOn;
        }

        private void OnChangeBlockCount(bool isOn)
        {
            if (!m_IsIniting) PlayUISound(SoundId.UI_Click);

            if (m_GameSettings != null && m_GameSettings.SupportsBlockCountSetting)
            {
                m_GameSettings.ShowBlockCount = isOn;
            }
        }


        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }


        private void OnRestartClick()
        {
            PlayUISound(SoundId.UI_Click);
            (m_settingUIData.userData as ProcedureGame).Restart();
            Close();
        }

        private void OnHomeClick()
        {
            PlayUISound(SoundId.UI_Click);
            (m_settingUIData.userData as ProcedureGame).ReturnMenu();
            Close();
        }

        private const float interval = 3f;
        private const int clicksPerCombo = 3;
        private const int requiredCombos = 3;

        public static bool startClick = false;

        [SerializeField] private float lastTime = 0;

        private static int versionClickCount = 0;
        private static int versionComboCount = 0;
        private void OnVersionClick()
        {
            return;
            if (GameEntry.GMMode)
            {
                return;
            }

            if (startClick)
            {
                // Each three-click combo must be completed within the interval.
                if (Time.time - lastTime > interval)
                {
                    versionClickCount = 0;
                    versionComboCount = 0;
                    startClick = false;
                }
            }
            else
            {
                // After each combo, wait for the full interval before starting the next one.
                if (versionComboCount > 0 && Time.time - lastTime < interval)
                {
                    versionClickCount = 0;
                    versionComboCount = 0;
                    return;
                }

                lastTime = Time.time;
                versionClickCount = 0;
                startClick = true;
            }

            versionClickCount++;
            if (versionClickCount == clicksPerCombo)
            {
                versionClickCount = 0;
                versionComboCount++;
                startClick = false;
                lastTime = Time.time;

                if (versionComboCount == requiredCombos)
                {
                    versionComboCount = 0;

                    OnDebugOpened();
                }
            }

        }
        private void OnDebugOpened()
        {
            if (m_GMMode != null) m_GMMode.SetActive(true);
            GameEntry.TestMode?.SetEnabled(true);
            PromptUIPanel.ShowToast("Debug Mode Open");
        }

        public void OnDebugClick(bool value)
        {
            if (GameEntry.TestMode == null || !GameEntry.TestMode.IsEnabled) return;
            if (value) GameEntry.TestMode.Show();
            else GameEntry.TestMode.Hide();
            if (value)
            {
                Close();
            }
        }





    }
}
