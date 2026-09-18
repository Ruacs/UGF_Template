using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Race.UI
{
    /// <summary>报名页只绑定文档化的 Prefab 节点；标题、按钮和说明文案由 Prefab 数据维护。</summary>
    public sealed class RaceStartUIPanel : UGuiForm
    {
        private RaceActivityModule m_Module;
        private TMP_Text m_Countdown;
        private TMP_Text[] m_RewardTexts;
        private Button m_StartButton;
        private Button m_CloseButton;
        private bool m_IsStarting;
        private float m_NextCountdownRefresh;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (!BindPrefab())
            {
                enabled = false;
                return;
            }
            m_StartButton.AddSafeClick(OnClickStart);
            m_CloseButton.AddSafeClick(() => Close());
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_Module = ExtractModule(userData);
            if (m_Module != null) m_Module.StateChanged += Refresh;
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
            if (m_Module == null || Time.unscaledTime < m_NextCountdownRefresh) return;
            m_NextCountdownRefresh = Time.unscaledTime + 1f;
            RefreshCountdown(m_Module.GetSnapshot());
        }

        private bool BindPrefab()
        {
            m_Countdown = RaceUiLookup.Text(transform, "Root/Content/RemainTimeArea_back/RemainTimeArea/RemainTime");
            m_RewardTexts = new[]
            {
                RaceUiLookup.Text(transform, "Root/Content/RewardArea/Top/RewardValue"),
                RaceUiLookup.Text(transform, "Root/Content/RewardArea/Second/RewardValue"),
                RaceUiLookup.Text(transform, "Root/Content/RewardArea/Third/RewardValue")
            };
            m_StartButton = RaceUiLookup.Button(transform, "Root/Content/BtnArea/FreeStartBtn");
            m_CloseButton = RaceUiLookup.Button(transform, "Root/Btn_Close");
            if (m_Countdown != null && m_StartButton != null && m_CloseButton != null)
            {
                foreach (TMP_Text rewardText in m_RewardTexts)
                    if (rewardText == null) return false;
                return true;
            }
            return false;
        }

        private void Refresh()
        {
            if (m_Module == null) return;
            RaceSnapshot snapshot = m_Module.GetSnapshot();
            RefreshCountdown(snapshot);
            for (int index = 0; index < m_RewardTexts.Length; index++)
            {
                bool visible = index < snapshot.RankRewards.Count;
                m_RewardTexts[index].gameObject.SetActive(visible);
                if (visible) m_RewardTexts[index].text = DescribeRewards(snapshot.RankRewards[index].Rewards);
            }
            m_StartButton.interactable = snapshot.CanJoin && !m_IsStarting;
        }

        private void RefreshCountdown(RaceSnapshot snapshot)
        {
            if (m_Countdown != null)
                m_Countdown.text = RaceUiFactory.FormatCountdown(snapshot.EndUtc - DateTimeOffset.UtcNow);
        }

        private void OnClickStart()
        {
            StartAsync().Forget(Debug.LogException);
        }

        private async UniTask StartAsync()
        {
            if (m_IsStarting || m_Module == null) return;
            m_IsStarting = true;
            Refresh();
            try
            {
                if (await m_Module.StartRaceAsync())
                {
                    await m_Module.OpenMainAsync();
                    Close();
                }
            }
            finally
            {
                m_IsStarting = false;
                if (m_Module != null) Refresh();
            }
        }

        private static RaceActivityModule ExtractModule(object userData) =>
            (userData as ActivityPageUserData)?.Request.Arguments as RaceActivityModule;

        private static string DescribeRewards(IReadOnlyList<RewardEntry> rewards)
        {
            if (rewards == null || rewards.Count == 0) return string.Empty;
            var values = new List<string>();
            foreach (RewardEntry reward in rewards)
            {
                if (reward == null) continue;
                var builder = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(reward.DisplayName)) builder.Append(reward.DisplayName).Append(' ');
                builder.Append(reward.RequestAmount);
                values.Add(builder.ToString());
            }
            return string.Join(" ", values);
        }
    }
}
