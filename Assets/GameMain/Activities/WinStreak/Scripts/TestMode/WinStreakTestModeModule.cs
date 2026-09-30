#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Lokas;
using UnityEngine;

namespace Lokas.Activities.WinStreak
{
    /// <summary>Win Streak 专属测试页：报名、批量模拟胜场、模拟失败和清空本地状态。</summary>
    public sealed class WinStreakTestModeModule : ITestModeModule
    {
        private const int MaxSimulatedWins = 99;

        private readonly WinStreakActivityModule m_WinStreak;
        private readonly string m_SessionId = "win-streak-debug-" + Guid.NewGuid().ToString("N");
        private long m_Sequence;

        public WinStreakTestModeModule(WinStreakActivityModule winStreak)
        {
            m_WinStreak = winStreak ?? throw new ArgumentNullException(nameof(winStreak));
        }

        public string OwnerId => WinStreakActivityModule.Id;
        public string PageName => "Win Streak";
        public string ModuleName => "Progress";
        public int Order => 115;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            page.AddItem<TestModeInfoItem>("WinStreakState")
                .SetLabel("Win Streak State")
                .SetValueGetter(() => FormatState(m_WinStreak.GetSnapshot()));

            page.AddItem<TestModeInfoItem>("WinStreakProgress")
                .SetLabel("Win Streak Progress")
                .SetValueGetter(() => FormatProgress(m_WinStreak.GetSnapshot()));

            page.AddItem<TestModeButtonItem>("JoinWinStreak")
                .SetLabel("Win Streak Entry")
                .SetButtonText("Join Win Streak")
                .OnClick(() => JoinWinStreak(context));

            TestModeIntStepperItem wins = page.AddItem<TestModeIntStepperItem>("SimulateWinCount");
            wins.SetLabel("Wins To Simulate")
                .SetRange(1, MaxSimulatedWins)
                .SetValue(1)
                .SetNotifyOnStepButtonOrInputEnd(true);

            TestModeButtonItem simulateWins = page.AddItem<TestModeButtonItem>("SimulateWins")
                .SetLabel("Win Streak Progress")
                .SetButtonText("Simulate +1 Win")
                .OnClick(() => SimulateWins(wins.Value, context));
            wins.OnValueChanged(value => simulateWins.SetButtonText($"Simulate +{value} Wins"));

            page.AddItem<TestModeButtonItem>("SimulateLoss")
                .SetLabel("Win Streak Reset")
                .SetButtonText("Simulate Loss")
                .OnClick(() => SimulateLoss(context));

            page.AddItem<TestModeButtonItem>("ResetWinStreakData")
                .SetLabel("Win Streak Data")
                .SetButtonText("Reset Win Streak Data")
                .OnClick(() => ResetData(context));
        }

        private void JoinWinStreak(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            m_WinStreak.JoinTestAsync().Forget(Debug.LogException);
            context.RequestRefresh();
        }

        private void SimulateWins(int count, TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            SimulateWinsAsync(Mathf.Clamp(count, 1, MaxSimulatedWins), context).Forget(Debug.LogException);
        }

        private async UniTask SimulateWinsAsync(int count, TestModeModuleContext context)
        {
            WinStreakSnapshot snapshot = m_WinStreak.GetSnapshot();
            if (!snapshot.IsJoined)
            {
                await m_WinStreak.JoinTestAsync();
                snapshot = m_WinStreak.GetSnapshot();
            }
            string gameId = m_WinStreak.GetTestModeGameId();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Debug.LogWarning("[WinStreakTestMode] Win Streak has no registered game source.");
                return;
            }

            var facts = new List<ActivityLevelCompletedFact>(count);
            for (int index = 0; index < count; index++)
            {
                long sequence = ++m_Sequence;
                string factId = string.Concat(m_SessionId, ":", sequence.ToString(CultureInfo.InvariantCulture));
                facts.Add(new ActivityLevelCompletedFact(factId, gameId, m_SessionId, sequence,
                    DateTimeOffset.UtcNow, sequence.ToString(CultureInfo.InvariantCulture), won: true));
            }
            m_WinStreak.ProcessTestFacts(facts);
            context.RequestRefresh();
        }

        private void SimulateLoss(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            SimulateLossAsync(context).Forget(Debug.LogException);
        }

        private async UniTask SimulateLossAsync(TestModeModuleContext context)
        {
            WinStreakSnapshot snapshot = m_WinStreak.GetSnapshot();
            if (!snapshot.IsJoined) await m_WinStreak.JoinTestAsync();
            string gameId = m_WinStreak.GetTestModeGameId();
            if (string.IsNullOrWhiteSpace(gameId)) return;
            long sequence = ++m_Sequence;
            var facts = new List<ActivityLevelCompletedFact>
            {
                new ActivityLevelCompletedFact(
                    string.Concat(m_SessionId, ":", sequence.ToString(CultureInfo.InvariantCulture)),
                    gameId, m_SessionId, sequence, DateTimeOffset.UtcNow, "loss", won: false)
            };
            m_WinStreak.ProcessTestFacts(facts);
            context.RequestRefresh();
        }

        private void ResetData(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            m_WinStreak.ResetTestStateAsync().Forget(Debug.LogException);
            context.RequestRefresh();
        }

        private static bool IsActive(TestModeModuleContext context) => context != null && context.IsActive;

        private static string FormatState(WinStreakSnapshot snapshot) =>
            snapshot == null ? string.Empty : snapshot.State.ToString();

        private static string FormatProgress(WinStreakSnapshot snapshot)
        {
            if (snapshot == null) return string.Empty;
            return string.Concat(snapshot.CurrentProgress.ToString(CultureInfo.InvariantCulture), "/",
                snapshot.MaxProgress.ToString(CultureInfo.InvariantCulture), "  best ",
                snapshot.BestProgress.ToString(CultureInfo.InvariantCulture), "  claimed ",
                snapshot.RewardReceiveProgress.ToString(CultureInfo.InvariantCulture));
        }
    }
}
#endif
