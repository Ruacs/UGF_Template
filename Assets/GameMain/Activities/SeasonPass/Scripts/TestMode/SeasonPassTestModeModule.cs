#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using Lokas;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Lokas.Activities.SeasonPass
{
    /// <summary>通行证专属测试页；由活动模块初始化时注册，停用时自动注销。</summary>
    public sealed class SeasonPassTestModeModule : ITestModeModule
    {
        private const int MaxSimulatedWins = 99;

        private readonly SeasonPassActivityModule m_SeasonPass;
        private readonly string m_SessionId = "season-pass-debug-" + Guid.NewGuid().ToString("N");
        private long m_Sequence;

        public SeasonPassTestModeModule(SeasonPassActivityModule seasonPass)
        {
            m_SeasonPass = seasonPass;
        }

        public string OwnerId => SeasonPassActivityModule.Id;
        public string PageName => "Season Pass";
        public string ModuleName => "Progress";
        public int Order => 100;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            page.AddItem<TestModeInfoItem>("CurrentCharge")
                .SetLabel("Current Charge")
                .SetValueGetter(() => m_SeasonPass.GetSnapshot().Charge.ToString());

            TestModeIntStepperItem wins = page.AddItem<TestModeIntStepperItem>("SimulateWinCount");
            wins
                .SetLabel("Wins To Simulate")
                .SetRange(1, MaxSimulatedWins)
                .SetValue(1)
                .SetNotifyOnStepButtonOrInputEnd(true);

            TestModeButtonItem simulateWins = page.AddItem<TestModeButtonItem>("SimulateWins")
                .SetLabel("Season Pass Progress")
                .SetButtonText($"Simulate +{wins.Value} Win")
                .OnClick(() => SimulateWins(wins.Value, context));
            wins.OnValueChanged(value => simulateWins.SetButtonText($"Simulate +{value} Win"));

            page.AddItem<TestModeButtonItem>("ResetPassData")
                .SetLabel("Season Pass Data")
                .SetButtonText("Reset Pass Data")
                .OnClick(() => ResetPassData(context));
        }

        private void SimulateWins(int count, TestModeModuleContext context)
        {
            if (context == null || !context.IsActive) return;
            if (GameEntry.Event == null)
            {
                Debug.LogWarning("[SeasonPassTestMode] Game Framework event component is unavailable.");
                return;
            }

            string gameId = m_SeasonPass.GetTestModeGameId();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Debug.LogWarning("[SeasonPassTestMode] The season pass has no registered game source.");
                return;
            }

            GameMode gameMode = GameEntry.GameManager?.PrimaryGameMode ?? GameMode.None;
            string levelId = (GameEntry.SubGames?.Get(gameMode)?.CurrentLevel ?? 0).ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < count; i++)
            {
                long sequence = ++m_Sequence;
                string factId = string.Concat(m_SessionId, ":", sequence.ToString(CultureInfo.InvariantCulture));
                var fact = new ActivityLevelCompletedFact(factId, gameId, m_SessionId, sequence,
                    DateTimeOffset.UtcNow, levelId, won: true);
                GameEntry.Event.Fire(this, ActivityGameFactEventArgs.Create(fact));
                GameEntry.Event.Fire(this, LevelPassedEventArgs.Create(levelId));
            }
            context.RequestRefresh();
        }

        private void ResetPassData(TestModeModuleContext context)
        {
            if (context == null || !context.IsActive) return;
            m_SeasonPass.ResetTestStateAsync().Forget(Debug.LogException);
            context.RequestRefresh();
        }
    }
}
#endif
