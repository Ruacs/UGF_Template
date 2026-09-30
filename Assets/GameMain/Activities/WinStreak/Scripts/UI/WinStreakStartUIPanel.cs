using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.WinStreak.UI
{
    /// <summary>首次进入连胜活动的报名弹窗。</summary>
    public sealed class WinStreakStartUIPanel : UGuiForm
    {
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_Countdown;
        [SerializeField] private TMP_Text m_Description;
        [SerializeField] private Button m_StartButton;
        [SerializeField] private Button m_CloseButton;

        private WinStreakActivityModule m_Module;
        private bool m_IsStarting;
        private float m_NextRefreshAt;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_StartButton != null) m_StartButton.AddSafeClick(OnClickStart);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
        }

        protected override void OnOpen(object userData)
        {
            m_Module = ExtractModule(userData);
            if (m_Module == null) GameEntry.Activities?.TryGetModule(WinStreakActivityModule.Id, out m_Module);
            if (m_Module != null) m_Module.StateChanged += Refresh;
            base.OnOpen(userData);
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            if (m_Module != null) m_Module.StateChanged -= Refresh;
            m_Module = null;
            m_IsStarting = false;
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
            WinStreakSnapshot snapshot = m_Module.GetSnapshot();
            if (m_Title != null) m_Title.SetText(snapshot.DisplayName);
            if (m_Description != null)
                m_Description.SetText("Win levels in a row to unlock rewards!\nA loss resets your current streak.");
            if (m_Countdown != null) m_Countdown.SetText(FormatCountdown(snapshot.EndUtc - DateTimeOffset.UtcNow));
            if (m_StartButton != null) m_StartButton.interactable = snapshot.IsActive && snapshot.IsUnlocked &&
                !snapshot.IsJoined && !m_IsStarting;
        }

        private void OnClickStart()
        {
            StartAsync().Forget(Debug.LogException);
        }

        private async UniTask StartAsync()
        {
            if (m_Module == null || m_IsStarting) return;
            m_IsStarting = true;
            Refresh();
            try
            {
                await m_Module.JoinAsync();
                await m_Module.OpenMainAsync();
                Close();
            }
            finally
            {
                m_IsStarting = false;
                if (m_Module != null) Refresh();
            }
        }

#if UNITY_EDITOR
        public void BindSerializedReferences(TMP_Text title, TMP_Text countdown, TMP_Text description,
            Button startButton, Button closeButton)
        {
            m_Title = title;
            m_Countdown = countdown;
            m_Description = description;
            m_StartButton = startButton;
            m_CloseButton = closeButton;
        }
#endif

        private static WinStreakActivityModule ExtractModule(object userData) =>
            (userData as ActivityPageUserData)?.Request.Arguments as WinStreakActivityModule;

        private static string FormatCountdown(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "00:00:00";
            return remaining.TotalDays >= 1
                ? string.Format("{0}d {1:00}:{2:00}:{3:00}", remaining.Days, remaining.Hours, remaining.Minutes,
                    remaining.Seconds)
                : string.Format("{0:00}:{1:00}:{2:00}", remaining.Hours, remaining.Minutes, remaining.Seconds);
        }
    }
}
