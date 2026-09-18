#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Lokas;
using UnityEngine;

namespace Lokas.Activities.Race
{
    /// <summary>Race 专属测试页；仅在编辑器和开发包中注册。</summary>
    public sealed class RaceTestModeModule : ITestModeModule
    {
        private const int MaxSimulatedWins = 99;

        private readonly RaceActivityModule m_Race;
        private readonly string m_SessionId = "race-debug-" + Guid.NewGuid().ToString("N");
        private long m_Sequence;

        public RaceTestModeModule(RaceActivityModule race)
        {
            m_Race = race;
        }

        public string OwnerId => RaceActivityModule.Id;
        public string PageName => "Race";
        public string ModuleName => "Progress";
        public int Order => 110;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            page.AddItem<TestModeInfoItem>("RaceState")
                .SetLabel("Race State")
                .SetValueGetter(() => FormatState(m_Race.GetSnapshot()));

            page.AddItem<TestModeInfoItem>("RaceProgress")
                .SetLabel("Race Progress")
                .SetValueGetter(() => FormatProgress(m_Race.GetSnapshot()));

            page.AddItem<TestModeButtonItem>("StartRace")
                .SetLabel("Race Entry")
                .SetButtonText("Start Race")
                .OnClick(() => StartRace(context));

            TestModeIntStepperItem wins = page.AddItem<TestModeIntStepperItem>("SimulateWinCount");
            wins.SetLabel("Wins To Simulate")
                .SetRange(1, MaxSimulatedWins)
                .SetValue(1)
                .SetNotifyOnStepButtonOrInputEnd(true);

            TestModeButtonItem simulateWins = page.AddItem<TestModeButtonItem>("SimulateWins")
                .SetLabel("Race Progress")
                .SetButtonText("Simulate +1 Win")
                .OnClick(() => SimulateWins(wins.Value, context));
            wins.OnValueChanged(value => simulateWins.SetButtonText($"Simulate +{value} Win"));

            page.AddItem<TestModeButtonItem>("ResetRaceData")
                .SetLabel("Race Data")
                .SetButtonText("Reset Race Data")
                .OnClick(() => ResetRaceData(context));
        }

        private void StartRace(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            StartRaceAsync(context).Forget(Debug.LogException);
        }

        private async UniTask StartRaceAsync(TestModeModuleContext context)
        {
            await m_Race.StartRaceAsync(context.CancellationToken);
            context.RequestRefresh();
        }

        private void SimulateWins(int count, TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            SimulateWinsAsync(Mathf.Clamp(count, 1, MaxSimulatedWins), context).Forget(Debug.LogException);
        }

        private async UniTask SimulateWinsAsync(int count, TestModeModuleContext context)
        {
            RaceSnapshot snapshot = m_Race.GetSnapshot();
            if (!snapshot.IsJoined)
            {
                if (!await m_Race.StartRaceAsync(context.CancellationToken)) return;
                snapshot = m_Race.GetSnapshot();
            }

            if (snapshot.IsFinished) return;

            if (GameEntry.Event == null)
            {
                Debug.LogWarning("[RaceTestMode] Game Framework event component is unavailable.");
                return;
            }

            string gameId = m_Race.GetTestModeGameId();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Debug.LogWarning("[RaceTestMode] The race has no registered game source.");
                return;
            }

            int startLevel = snapshot.StartLevel + snapshot.CompletedStages;
            int remaining = snapshot.TargetStages - snapshot.CompletedStages;
            for (int index = 0; index < Mathf.Min(count, remaining); index++)
            {
                long sequence = ++m_Sequence;
                string factId = string.Concat(m_SessionId, ":", sequence.ToString(CultureInfo.InvariantCulture));
                string levelId = (startLevel + index).ToString(CultureInfo.InvariantCulture);
                var fact = new ActivityLevelCompletedFact(factId, gameId, m_SessionId, sequence,
                    DateTimeOffset.UtcNow, levelId, won: true);
                GameEntry.Event.Fire(this, ActivityGameFactEventArgs.Create(fact));
            }

            context.RequestRefresh();
        }

        private void ResetRaceData(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            m_Race.ResetTestStateAsync().Forget(Debug.LogException);
            context.RequestRefresh();
        }

        private static bool IsActive(TestModeModuleContext context) =>
            context != null && context.IsActive;

        private static string FormatState(RaceSnapshot snapshot) =>
            snapshot == null ? string.Empty : snapshot.State.ToString();

        private static string FormatProgress(RaceSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsJoined) return "0/0";
            return string.Concat(snapshot.CompletedStages.ToString(CultureInfo.InvariantCulture), "/",
                snapshot.TargetStages.ToString(CultureInfo.InvariantCulture), "  #",
                snapshot.PlayerRank.ToString(CultureInfo.InvariantCulture));
        }
    }
}
#endif
