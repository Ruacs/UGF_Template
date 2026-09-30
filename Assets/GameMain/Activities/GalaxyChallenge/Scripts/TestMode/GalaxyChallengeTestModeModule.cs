#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Lokas;
using UnityEngine;

namespace Lokas.Activities.GalaxyChallenge
{
    /// <summary>Galaxy Challenge 专属测试页：切换 5/7 步模式并驱动跳跃、淘汰和失败流程。</summary>
    public sealed class GalaxyChallengeTestModeModule : ITestModeModule
    {
        private const int FiveStepEventId = 2;
        private const int SevenStepEventId = 1;
        private const int MaxSimulatedWins = 7;

        private readonly GalaxyChallengeActivityModule m_GalaxyChallenge;
        private readonly string m_SessionId = "galaxy-challenge-debug-" + Guid.NewGuid().ToString("N");
        private long m_Sequence;

        public GalaxyChallengeTestModeModule(GalaxyChallengeActivityModule galaxyChallenge)
        {
            m_GalaxyChallenge = galaxyChallenge ?? throw new ArgumentNullException(nameof(galaxyChallenge));
        }

        public string OwnerId => GalaxyChallengeActivityModule.Id;
        public string PageName => "Galaxy Challenge";
        public string ModuleName => "Progress";
        public int Order => 120;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            GalaxyChallengeSnapshot snapshot = m_GalaxyChallenge.GetSnapshot();

            page.AddItem<TestModeInfoItem>("GalaxyChallengeState")
                .SetLabel("Galaxy Challenge State")
                .SetValueGetter(() => FormatState(m_GalaxyChallenge.GetSnapshot()));

            page.AddItem<TestModeInfoItem>("GalaxyChallengeProgress")
                .SetLabel("Galaxy Challenge Progress")
                .SetValueGetter(() => FormatProgress(m_GalaxyChallenge.GetSnapshot()));

            TestModeDropdownItem mode = page.AddItem<TestModeDropdownItem>("GalaxyChallengeMode");
            mode.SetLabel("Challenge Mode")
                .SetOptions(new[] { "5 Steps (Event 2)", "7 Steps (Event 1)" })
                .SetValue(snapshot.EventId == SevenStepEventId ? 1 : 0);

            page.AddItem<TestModeButtonItem>("ApplyGalaxyChallengeMode")
                .SetLabel("Challenge Mode")
                .SetButtonText("Apply Mode & Reset")
                .OnClick(() => ApplyMode(mode.Value, context));

            page.AddItem<TestModeButtonItem>("OpenGalaxyChallengeMain")
                .SetLabel("Galaxy Challenge UI")
                .SetButtonText("Open Main Page")
                .OnClick(() => OpenMainPage(context));

            page.AddItem<TestModeButtonItem>("JoinGalaxyChallenge")
                .SetLabel("Galaxy Challenge Entry")
                .SetButtonText("Join / Clear Interval")
                .OnClick(() => Join(context));

            TestModeIntStepperItem wins = page.AddItem<TestModeIntStepperItem>("GalaxyChallengeWinCount");
            wins.SetLabel("Wins To Simulate")
                .SetRange(1, MaxSimulatedWins)
                .SetValue(1)
                .SetNotifyOnStepButtonOrInputEnd(true);

            TestModeButtonItem simulateWins = page.AddItem<TestModeButtonItem>("SimulateGalaxyChallengeWins")
                .SetLabel("Galaxy Challenge Progress")
                .SetButtonText("Simulate +1 Win")
                .OnClick(() => SimulateWins(wins.Value, context));
            wins.OnValueChanged(value => simulateWins.SetButtonText($"Simulate +{value} Wins"));

            page.AddItem<TestModeButtonItem>("SimulateGalaxyChallengeLoss")
                .SetLabel("Galaxy Challenge Failure")
                .SetButtonText("Simulate Loss")
                .OnClick(() => SimulateLoss(context));

            page.AddItem<TestModeButtonItem>("ResetGalaxyChallengeData")
                .SetLabel("Galaxy Challenge Data")
                .SetButtonText("Reset Current Mode")
                .OnClick(() => ResetData(context));
        }

        private void ApplyMode(int option, TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            int eventId = option == 1 ? SevenStepEventId : FiveStepEventId;
            ApplyModeAsync(eventId, context).Forget(Debug.LogException);
        }

        private async UniTask ApplyModeAsync(int eventId, TestModeModuleContext context)
        {
            await m_GalaxyChallenge.SetTestEventAsync(eventId);
            context.RequestRefresh();
        }

        private void OpenMainPage(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            m_GalaxyChallenge.OpenMainAsync(context.CancellationToken).Forget(Debug.LogException);
        }

        private void Join(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            JoinAsync(context).Forget(Debug.LogException);
        }

        private async UniTask JoinAsync(TestModeModuleContext context)
        {
            await m_GalaxyChallenge.JoinTestAsync();
            context.RequestRefresh();
        }

        private void SimulateWins(int count, TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            SimulateWinsAsync(Mathf.Clamp(count, 1, MaxSimulatedWins), context).Forget(Debug.LogException);
        }

        private async UniTask SimulateWinsAsync(int count, TestModeModuleContext context)
        {
            GalaxyChallengeSnapshot snapshot = m_GalaxyChallenge.GetSnapshot();
            if (snapshot.CurrentProgress >= snapshot.StepCount) return;
            if (!snapshot.IsJoined)
            {
                await m_GalaxyChallenge.JoinTestAsync();
                snapshot = m_GalaxyChallenge.GetSnapshot();
            }

            string gameId = m_GalaxyChallenge.GetTestModeGameId();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Debug.LogWarning("[GalaxyChallengeTestMode] Galaxy Challenge has no registered game source.");
                return;
            }

            int remaining = snapshot.StepCount - snapshot.CurrentProgress;
            var facts = new List<ActivityLevelCompletedFact>(Mathf.Min(count, remaining));
            for (int index = 0; index < Mathf.Min(count, remaining); index++)
            {
                long sequence = ++m_Sequence;
                string factId = string.Concat(m_SessionId, ":",
                    sequence.ToString(CultureInfo.InvariantCulture));
                facts.Add(new ActivityLevelCompletedFact(factId, gameId, m_SessionId, sequence,
                    DateTimeOffset.UtcNow, sequence.ToString(CultureInfo.InvariantCulture), won: true));
            }
            m_GalaxyChallenge.ProcessTestFacts(facts);
            context.RequestRefresh();
        }

        private void SimulateLoss(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            SimulateLossAsync(context).Forget(Debug.LogException);
        }

        private async UniTask SimulateLossAsync(TestModeModuleContext context)
        {
            GalaxyChallengeSnapshot snapshot = m_GalaxyChallenge.GetSnapshot();
            if (!snapshot.IsJoined) await m_GalaxyChallenge.JoinTestAsync();
            string gameId = m_GalaxyChallenge.GetTestModeGameId();
            if (string.IsNullOrWhiteSpace(gameId)) return;

            long sequence = ++m_Sequence;
            var facts = new List<ActivityLevelCompletedFact>
            {
                new ActivityLevelCompletedFact(
                    string.Concat(m_SessionId, ":", sequence.ToString(CultureInfo.InvariantCulture)),
                    gameId, m_SessionId, sequence, DateTimeOffset.UtcNow, "loss", won: false)
            };
            m_GalaxyChallenge.ProcessTestFacts(facts);
            context.RequestRefresh();
        }

        private void ResetData(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            ResetDataAsync(context).Forget(Debug.LogException);
        }

        private async UniTask ResetDataAsync(TestModeModuleContext context)
        {
            await m_GalaxyChallenge.ResetTestStateAsync();
            context.RequestRefresh();
        }

        private static bool IsActive(TestModeModuleContext context) => context != null && context.IsActive;

        private static string FormatState(GalaxyChallengeSnapshot snapshot)
        {
            if (snapshot == null) return string.Empty;
            return string.Concat(snapshot.State.ToString(), snapshot.IsJoined ? " / Joined" : " / Not Joined",
                snapshot.IsFailed ? " / Failed" : string.Empty,
                snapshot.HasPendingProgress ? " / Animation Pending" : string.Empty);
        }

        private static string FormatProgress(GalaxyChallengeSnapshot snapshot)
        {
            if (snapshot == null) return string.Empty;
            return string.Concat("Event ", snapshot.EventId.ToString(CultureInfo.InvariantCulture), " / ",
                snapshot.StepCount.ToString(CultureInfo.InvariantCulture), " steps / progress ",
                snapshot.CurrentProgress.ToString(CultureInfo.InvariantCulture), "/",
                snapshot.StepCount.ToString(CultureInfo.InvariantCulture), " / users ",
                snapshot.CurrentUserCount.ToString(CultureInfo.InvariantCulture), "/",
                snapshot.JoinedUserCount.ToString(CultureInfo.InvariantCulture), " / shown ",
                snapshot.LastPresentedProgress.ToString(CultureInfo.InvariantCulture));
        }
    }
}
#endif
