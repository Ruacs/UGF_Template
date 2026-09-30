using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    /// <summary>首次进入页；所有节点、图片和适配参数均由 Prefab 与 Inspector 提供。</summary>
    public sealed class GalaxyChallengeStartUIPanel : UGuiForm
    {
        [SerializeField] private TMP_Text m_Countdown;
        [SerializeField] private TMP_Text m_Description;
        [SerializeField] private Button m_StartButton;
        [SerializeField] private Button m_CloseButton;

        private GalaxyChallengeActivityModule m_Module;
        private bool m_IsOpening;
        private float m_NextRefreshAt;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_StartButton != null) m_StartButton.AddSafeClick(OnStartClicked);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
        }

        protected override void OnOpen(object userData)
        {
            m_Module = GalaxyChallengeUIUtility.ResolveModule(userData);
            if (m_Module != null) m_Module.StateChanged += Refresh;
            base.OnOpen(userData);
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            if (m_Module != null) m_Module.StateChanged -= Refresh;
            m_Module = null;
            m_IsOpening = false;
            base.OnClose(isShutdown, userData);
        }

        private void Update()
        {
            if (m_Module == null || Time.unscaledTime < m_NextRefreshAt) return;
            m_NextRefreshAt = Time.unscaledTime + 1f;
            RefreshCountdown(m_Module.GetSnapshot());
        }

        private void Refresh()
        {
            if (m_Module == null) return;
            GalaxyChallengeSnapshot snapshot = m_Module.GetSnapshot();
            RefreshCountdown(snapshot);
            if (m_StartButton != null)
                m_StartButton.interactable = snapshot.State == GalaxyChallengeState.Ready && !m_IsOpening;
        }

        private void RefreshCountdown(GalaxyChallengeSnapshot snapshot)
        {
            if (m_Countdown != null)
                m_Countdown.SetText(GalaxyChallengeUIUtility.FormatCountdown(snapshot.EndUtc - DateTimeOffset.UtcNow));
        }

        private void OnStartClicked() => OpenMainAsync().Forget(Debug.LogException);

        private async UniTask OpenMainAsync()
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
