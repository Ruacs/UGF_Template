using System;
using Ads;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class AdsUnavailableUIData
    {
        public Action OnRetrySuccess;
        public Action OnCountdownFallback;
        public Action OnClosed;

        public AdsUnavailableUIData()
        {
        }

        public AdsUnavailableUIData(Action onRetrySuccess, Action onCountdownFallback = null, Action onClosed = null)
        {
            OnRetrySuccess = onRetrySuccess;
            OnCountdownFallback = onCountdownFallback;
            OnClosed = onClosed;
        }
    }

    public class AdsUnavailableUIPanel : UGuiForm
    {

        [SerializeField] private Button m_CloseBtn;
        [SerializeField] private Button m_CancelBtn;
        [SerializeField] private Button m_RetryBtn;
        [SerializeField] private ButtonStateController m_RetryBtnStateCtrl;

        [SerializeField] private TMP_Text m_countdownTMP;

        [SerializeField] private bool m_EnableCountdown;

        [SerializeField] private float timer = 30f;

        private const int CountdownSeconds = 30;
        private const float RewardedReadyCheckInterval = 3f;
        private int m_countdownRemainingSeconds;
        private bool m_isCountdownRunning;
        private float m_nextRewardedReadyCheckTime;
        private bool m_isShowingRewardedAd;

        private AdsUnavailableUIData m_UIData;


        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            m_CloseBtn.AddSafeClick(OnCloseClick);
            m_CancelBtn.AddSafeClick(OnCancelClick);
            m_RetryBtn.AddSafeClick(OnRetryClick);

        }

        protected override void OnOpen(object userData)
        {
            m_UIData = userData as AdsUnavailableUIData ?? new AdsUnavailableUIData();
            m_RetryBtnStateCtrl.SetState(ButtonState.Normal);
            ResetCountdownTimer();

            base.OnOpen(userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (!m_EnableCountdown)
            {
                return;
            }

            if (!m_isCountdownRunning)
            {
                StartCountdown();
            }

            if (Time.unscaledTime >= m_nextRewardedReadyCheckTime)
            {
                m_nextRewardedReadyCheckTime = Time.unscaledTime + RewardedReadyCheckInterval;
                if (TryShowRewardedAd())
                {
                    return;
                }
            }

            timer -= elapseSeconds;
            int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(timer));
            if (remainingSeconds != m_countdownRemainingSeconds)
            {
                m_countdownRemainingSeconds = remainingSeconds;
                UpdateCountdownText();
            }

            if (timer <= 0f)
            {
                OnCountdownFinished();
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {

            m_UIData = null;
            base.OnClose(isShutdown, userData);
        }

        // ── Button Handlers ──
        private void OnCancelClick()
        {
            PlayUISound(SoundId.UI_Click);
            m_UIData?.OnClosed?.Invoke();
            // TODO: 实现按钮逻辑
            Close();
        }

        private void OnRetryClick()
        {
            PlayUISound(SoundId.UI_Click);

            //没有网络情况下，直接提示失败，无保底
            if (!AdsManager.IsNetworkReachable())
            {
                ShowAdsFailToast();
                m_UIData?.OnClosed?.Invoke();
                Close();
                return;
            }

            // TODO: 实现按钮逻辑

            if (!TryShowRewardedAd())
            {
                if (!CanUseCountdownReward())
                {
                    ShowAdsFailToast();
                    m_UIData?.OnClosed?.Invoke();
                    Close();
                    return;
                }

                m_RetryBtnStateCtrl.SetState(ButtonState.Disabled);
                m_EnableCountdown = true;

            }

        }

        private void OnCloseClick()
        {
            PlayUISound(SoundId.UI_Close);
            m_UIData?.OnClosed?.Invoke();
            Close();
        }

        private void StartCountdown()
        {
            timer = CountdownSeconds;
            m_countdownRemainingSeconds = CountdownSeconds;
            m_isCountdownRunning = true;
            m_nextRewardedReadyCheckTime = Time.unscaledTime + RewardedReadyCheckInterval;
            UpdateCountdownText();
        }

        private void ResetCountdownTimer()
        {
            m_EnableCountdown = false;
            timer = CountdownSeconds;
            m_countdownRemainingSeconds = CountdownSeconds;
            m_isCountdownRunning = false;
            m_nextRewardedReadyCheckTime = 0f;
            m_isShowingRewardedAd = false;
            UpdateCountdownText();
        }

        private void UpdateCountdownText()
        {
            if (m_countdownTMP != null)
            {
                m_countdownTMP.text = m_countdownRemainingSeconds.ToString();
            }
        }

        private void OnCountdownFinished()
        {
            m_EnableCountdown = false;
            m_isCountdownRunning = false;
            UpdateCountdownText();

            if (!GameEntry.SaveData.TryClaimAdsUnavailableCountdownReward(AdsServerConfig.Common.AdsUnavailableCountdownRewardDailyLimit))
            {
                ShowAdsFailToast();
                m_UIData?.OnClosed?.Invoke();
                Close();
                return;
            }
            int level = GameEntry.GameManager != null ? GameEntry.GameManager.GetCurrentLevel() : 0;
            int ramainingCount = 0;

      
            AdsAnalytics.EventWithName("CountdownFinished", ("mode", $"{GameEntry.GameManager.CurrentGameMode.ToString()}"), ("lv", level), ("scene", "AdsUnavailable"));


            m_UIData?.OnCountdownFallback?.Invoke();
            Close();
        }

        private bool TryShowRewardedAd()
        {
            if (m_isShowingRewardedAd)
            {
                return true;
            }

            if (!AdsManager.IsRewardedReady())
            {
                return false;
            }

            m_isShowingRewardedAd = true;
            m_EnableCountdown = false;

            Action onSuccess = () =>
            {
                m_UIData?.OnRetrySuccess?.Invoke();
                Close();
            };

            Action<string> onFail = (error) =>
            {
                m_isShowingRewardedAd = false;

                if (timer > 0f)
                {
                    m_EnableCountdown = true;
                    m_RetryBtnStateCtrl.SetState(ButtonState.Disabled);
                }
                else
                {
                    OnCountdownFinished();
                }
            };

            AdsManager.ShowRewardedAd(onSuccess, onFail);
            return true;
        }

        private bool CanUseCountdownReward()
        {
            return GameEntry.SaveData.CanClaimAdsUnavailableCountdownReward(AdsServerConfig.Common.AdsUnavailableCountdownRewardDailyLimit);
        }

        private void ShowAdsFailToast()
        {
            PromptUIPanel.ShowToast(GameEntry.Localization.GetString("Tips.AdsFail"));
        }

    }
}
