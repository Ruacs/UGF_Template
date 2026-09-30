using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    public sealed class GalaxyChallengeIntervalUIPanel : UGuiForm
    {
        [SerializeField] private TMP_Text m_Countdown;
        [SerializeField] private Button m_ContinueButton;
        [SerializeField] private Button m_CloseButton;
        private GalaxyChallengeActivityModule m_Module;
        private bool m_IsOpening;
        private float m_NextRefreshAt;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_ContinueButton != null)
                m_ContinueButton.AddSafeClick(() => ContinueAsync().Forget(Debug.LogException));
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
        }

        protected override void OnOpen(object userData)
        {
            m_Module = GalaxyChallengeUIUtility.ResolveModule(userData);
            m_NextRefreshAt = 0f;
            base.OnOpen(userData);
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_Module = null;
            m_IsOpening = false;
            base.OnClose(isShutdown, userData);
        }

        private void Update()
        {
            if (m_Module == null || Time.unscaledTime < m_NextRefreshAt) return;
            m_NextRefreshAt = Time.unscaledTime + 1f;
            Refresh();
        }

        private void Refresh()
        {
            if (m_Module == null) return;
            GalaxyChallengeSnapshot snapshot = m_Module.GetSnapshot();
            TimeSpan remaining = snapshot.IntervalEndUtc.HasValue
                ? snapshot.IntervalEndUtc.Value - DateTimeOffset.UtcNow
                : TimeSpan.Zero;
            if (m_Countdown != null)
                m_Countdown.SetText(GalaxyChallengeUIUtility.FormatCountdown(remaining));
            if (m_ContinueButton != null)
                m_ContinueButton.interactable = remaining <= TimeSpan.Zero && !m_IsOpening;
        }
 
        private async UniTask ContinueAsync()
        {
            if (m_Module == null || m_IsOpening) return;
            m_IsOpening = true;
            Refresh();
            try
            {
                await m_Module.JoinAsync();
                await m_Module.OpenMainAsync();
                Close();
            }
            finally
            {
                m_IsOpening = false;
                if (m_Module != null) Refresh();
            }
        }
    }
}
