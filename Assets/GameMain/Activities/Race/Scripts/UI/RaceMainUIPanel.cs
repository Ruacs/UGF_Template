using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Race.UI
{
    /// <summary>赛道页读取序列化车道、目标、奖励和结果节点，不在运行时生成可见 UI。</summary>
    public sealed class RaceMainUIPanel : UGuiForm
    {
        [SerializeField] private RaceActivityModule m_Module;
        [SerializeField] private TMP_Text m_Countdown;
        [SerializeField] private TMP_Text[] m_RewardTexts;
        [SerializeField] private GameObject m_ResultPanel; 
        [SerializeField] private Button m_ClaimButton;
        [SerializeField] private Button m_DetailsButton;
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private RaceLaneView[] m_Lanes;
        private bool m_IsClaiming;
        private float m_NextRefreshAt;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            InitializeLanes();
            if (m_DetailsButton != null) m_DetailsButton.AddSafeClick(OnClickDetails);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
            if (m_ClaimButton != null) m_ClaimButton.AddSafeClick(OnClickClaim);
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
            m_IsClaiming = false;
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
            RaceSnapshot snapshot = m_Module.GetSnapshot();
            if (m_Countdown != null)
                m_Countdown.text = RaceUiFactory.FormatCountdown(snapshot.EndUtc - DateTimeOffset.UtcNow);

            if (m_Lanes != null)
            {
                bool showRanks = snapshot.CompletedStages > 0 || snapshot.IsFinished;
                for (int index = 0; index < m_Lanes.Length; index++)
                {
                    RaceLaneView lane = m_Lanes[index];
                    if (lane == null) continue;
                    RaceRacerSnapshot racer = FindRacerForLane(snapshot.Racers, index);
                    lane.gameObject.SetActive(racer != null);
                    if (racer != null) lane.Bind(racer, showRanks);
                }
            }

            if (m_RewardTexts != null)
            {
                for (int index = 0; index < m_RewardTexts.Length; index++)
                {
                    TMP_Text rewardText = m_RewardTexts[index];
                    if (rewardText == null) continue;
                    bool visible = index < snapshot.RankRewards.Count;
                    rewardText.gameObject.SetActive(visible);
                    if (visible) rewardText.text = DescribeRewards(snapshot.RankRewards[index].Rewards);
                }
            }

            if (m_ResultPanel != null) m_ResultPanel.SetActive(snapshot.IsFinished);
            if (m_ClaimButton != null)
                m_ClaimButton.interactable = snapshot.IsFinished && snapshot.CanClaim && !m_IsClaiming;
        }

        private void InitializeLanes()
        {
            if (m_Lanes == null) return;
            for (int index = 0; index < m_Lanes.Length; index++)
            {
                if (m_Lanes[index] != null) m_Lanes[index].SetLaneIndex(index);
            }
        }

        private static RaceRacerSnapshot FindRacerForLane(IReadOnlyList<RaceRacerSnapshot> racers, int laneIndex)
        {
            if (racers == null) return null;
            string racerId = GetRacerIdForLane(laneIndex);
            for (int index = 0; index < racers.Count; index++)
            {
                RaceRacerSnapshot racer = racers[index];
                if (racer != null && string.Equals(racer.RacerId, racerId, StringComparison.Ordinal)) return racer;
            }
            return null;
        }

        private static string GetRacerIdForLane(int laneIndex)
        {
            if (laneIndex == 2) return "player";
            return laneIndex < 2 ? $"npc-{laneIndex + 1}" : $"npc-{laneIndex}";
        }

        private void OnClickDetails()
        {
            if (m_Module != null) m_Module.OpenRulesAsync().Forget(Debug.LogException);
        }

        private void OnClickClaim()
        {
            ClaimAsync().Forget(Debug.LogException);
        }

        private async UniTask ClaimAsync()
        {
            if (m_IsClaiming || m_Module == null) return;
            m_IsClaiming = true;
            Refresh();
            try
            {
                await m_Module.ClaimRewardAsync();
            }
            finally
            {
                m_IsClaiming = false;
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
