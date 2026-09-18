#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Lokas;
using UnityEngine;

namespace Lokas.Activities.Collector
{
    /// <summary>Collector 专属测试页：模拟目标游戏通关事实，验证首页与任务列表刷新。</summary>
    public sealed class CollectorTestModeModule : ITestModeModule
    {
        private const int MaxSimulatedWins = 99;

        private readonly CollectorActivityModule m_Collector;
        private readonly string m_SessionId = "collector-debug-" + Guid.NewGuid().ToString("N");
        private long m_Sequence;

        public CollectorTestModeModule(CollectorActivityModule collector)
        {
            m_Collector = collector ?? throw new ArgumentNullException(nameof(collector));
        }

        public string OwnerId => CollectorActivityModule.Id;
        public string PageName => "Collector";
        public string ModuleName => "Progress";
        public int Order => 105;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            page.AddItem<TestModeInfoItem>("CollectorProgress")
                .SetLabel("Collector Progress")
                .SetValueGetter(() => FormatProgress(m_Collector.GetSnapshot()));

            TestModeIntStepperItem wins = page.AddItem<TestModeIntStepperItem>("SimulateWinCount");
            wins.SetLabel("Wins To Simulate")
                .SetRange(1, MaxSimulatedWins)
                .SetValue(1)
                .SetNotifyOnStepButtonOrInputEnd(true);

            TestModeButtonItem simulateWins = page.AddItem<TestModeButtonItem>("SimulateWins")
                .SetLabel("Collector Progress")
                .SetButtonText("Simulate +1 Win")
                .OnClick(() => SimulateWins(wins.Value, context));
            wins.OnValueChanged(value => simulateWins.SetButtonText($"Simulate +{value} Win"));

            page.AddItem<TestModeButtonItem>("ResetCollectorData")
                .SetLabel("Collector Data")
                .SetButtonText("Reset Collector Data")
                .OnClick(() => ResetCollectorData(context));
        }

        private void SimulateWins(int count, TestModeModuleContext context)
        {
            if (!IsActive(context)) return;

            string gameId = m_Collector.GetTestModeGameId();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Debug.LogWarning("[CollectorTestMode] Collector has no registered game source.");
                return;
            }

            GameMode gameMode = GameEntry.GameManager?.PrimaryGameMode ?? GameMode.None;
            string levelId = (GameEntry.SubGames?.Get(gameMode)?.CurrentLevel ?? 0)
                .ToString(CultureInfo.InvariantCulture);
            int amount = Mathf.Clamp(count, 1, MaxSimulatedWins);
            var facts = new List<ActivityLevelCompletedFact>(amount);
            for (int index = 0; index < amount; index++)
            {
                long sequence = ++m_Sequence;
                string factId = string.Concat(m_SessionId, ":", sequence.ToString(CultureInfo.InvariantCulture));
                facts.Add(new ActivityLevelCompletedFact(factId, gameId, m_SessionId, sequence,
                    DateTimeOffset.UtcNow, levelId, won: true));
            }
            // Apply the whole test batch through the module so it persists and notifies once.
            // Firing one framework event per simulated win made the home widget restart its
            // tweens for every item and caused visible stutter in the test panel.
            m_Collector.ProcessTestFacts(facts);
            context.RequestRefresh();
        }

        private void ResetCollectorData(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            m_Collector.ResetTestStateAsync().Forget(Debug.LogException);
            context.RequestRefresh();
        }

        private static bool IsActive(TestModeModuleContext context) =>
            context != null && context.IsActive;

        private static string FormatProgress(CollectorSnapshot snapshot)
        {
            if (snapshot == null) return string.Empty;
            string current = snapshot.CurrentTask == null
                ? "Complete"
                : string.Concat("Tier ", snapshot.CurrentTask.Tier.ToString(CultureInfo.InvariantCulture));
            return string.Concat(snapshot.CollectedCount.ToString(CultureInfo.InvariantCulture), "/",
                snapshot.TargetCount.ToString(CultureInfo.InvariantCulture), "  ", current);
        }
    }
}
#endif
