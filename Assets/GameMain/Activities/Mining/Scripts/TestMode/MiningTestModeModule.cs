#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    /// <summary>Mining 专属测试页：选择期次与关卡直接开局，并可重置持久化进度。</summary>
    public sealed class MiningTestModeModule : ITestModeModule
    {
        private const int MaxTestPickaxes = 9999;
        private readonly MiningActivityModule m_Mining;

        public MiningTestModeModule(MiningActivityModule mining)
        {
            m_Mining = mining ?? throw new ArgumentNullException(nameof(mining));
        }

        public string OwnerId => MiningActivityModule.Id;
        public string PageName => "Mining";
        public string ModuleName => "Progress";
        public int Order => 115;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            MiningBoardSnapshot snapshot = m_Mining.GetTestSnapshot();
            int currentEventId = snapshot?.EventId ?? m_Mining.Config.ScheduleConfig.EventId;
            int currentStepId = snapshot?.StepId ?? 1;
            int currentPickaxes = snapshot?.PickaxeCount ?? 99;

            page.AddItem<TestModeInfoItem>("MiningProgress")
                .SetLabel("Mining Progress")
                .SetValueGetter(() => FormatProgress(m_Mining.GetTestSnapshot()));

            TestModeDropdownItem eventSelector = page.AddItem<TestModeDropdownItem>("MiningEvent");
            eventSelector.SetLabel("Event")
                .SetOptions(new[] { "Event 1", "Event 2" })
                .SetValue(Mathf.Clamp(currentEventId - 1, 0, 1));

            TestModeIntStepperItem stepSelector = page.AddItem<TestModeIntStepperItem>("MiningStep");
            stepSelector.SetLabel("Step")
                .SetRange(1, 5)
                .SetValue(currentStepId)
                .SetNotifyOnStepButtonOrInputEnd(true);

            TestModeIntStepperItem pickaxeSelector = page.AddItem<TestModeIntStepperItem>("MiningPickaxes");
            pickaxeSelector.SetLabel("Pickaxes (Immediate)")
                .SetRange(0, MaxTestPickaxes)
                .SetValue(currentPickaxes)
                .SetNotifyOnStepButtonOrInputEnd(true);
            pickaxeSelector.OnValueChanged(value => SetPickaxeCount(value, context));

            page.AddItem<TestModeButtonItem>("OpenMiningStage")
                .SetLabel("Mining Stage")
                .SetButtonText("Open Selected Stage")
                .OnClick(() => OpenSelectedStage(eventSelector.Value + 1, stepSelector.Value, context));

            page.AddItem<TestModeButtonItem>("ResetMiningData")
                .SetLabel("Mining Data")
                .SetButtonText("Reset Mining Data")
                .OnClick(() => ResetData(context));
        }

        private void OpenSelectedStage(int eventId, int stepId, TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            OpenSelectedStageAsync(eventId, stepId, context).Forget(Debug.LogException);
        }

        private async UniTask OpenSelectedStageAsync(int eventId, int stepId,
            TestModeModuleContext context)
        {
            await m_Mining.OpenTestStageAsync(eventId, stepId, context.CancellationToken);
            context.RequestRefresh();
        }

        private void SetPickaxeCount(int pickaxeCount, TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            m_Mining.SetTestPickaxeCount(pickaxeCount);
        }

        private void ResetData(TestModeModuleContext context)
        {
            if (!IsActive(context)) return;
            ResetDataAsync(context).Forget(Debug.LogException);
        }

        private async UniTask ResetDataAsync(TestModeModuleContext context)
        {
            await m_Mining.ResetTestStateAsync(context.CancellationToken);
            context.RequestRefresh();
        }

        private static bool IsActive(TestModeModuleContext context) =>
            context != null && context.IsActive;

        private static string FormatProgress(MiningBoardSnapshot snapshot)
        {
            if (snapshot == null) return "Not initialized";
            return string.Concat(
                "Event ", snapshot.EventId.ToString(CultureInfo.InvariantCulture),
                " / Step ", snapshot.StepId.ToString(CultureInfo.InvariantCulture),
                " / Pickaxes ", snapshot.PickaxeCount.ToString(CultureInfo.InvariantCulture),
                " / Cells ", snapshot.OpenCellIds.Count.ToString(CultureInfo.InvariantCulture),
                " / Gems ", snapshot.CollectedGemIds.Count.ToString(CultureInfo.InvariantCulture));
        }
    }
}
#endif
