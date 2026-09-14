using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    internal static class TestModePropCountBuilder
    {
        private const int MaxPropCount = 999;

        public static void AddPropCountStepper(
            TestModePage page,
            string itemName,
            string label,
            Func<int> getCount,
            Action<int> setCount)
        {
            var stepper = page.AddItem<TestModeIntStepperItem>(itemName);
            stepper
                .SetLabel(label)
                .SetRange(0, MaxPropCount)
                .SetValue(Mathf.Clamp(getCount(), 0, MaxPropCount))
                .SetNotifyOnStepButtonOrInputEnd(true)
                .OnValueChanged(value =>
                {
                    int count = Mathf.Clamp(value, 0, MaxPropCount);
                    setCount(count);
                    Debug.Log($"{label}: {count}");
                });
        }
    }

    public sealed class AdsTestModeModule : ITestModeModule
    {
        public string OwnerId => TestModeModuleRegistry.CommonOwnerId;
        public string PageName => "Ads";

        public string ModuleName => "Ads";

        public int Order => 8;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            var adCallbackSuccess = page.AddItem<TestModeToggleItem>("AdCallbackSuccess");
            adCallbackSuccess
                .SetLabel("Ad Callback Success")
                .SetValue(GameEntry.AdCallbackSuccess)
                .OnValueChanged(value =>
                {
                    GameEntry.AdCallbackSuccess = value;
                    Debug.Log("Ad Callback Success: " + value);
                });

            var rewardedAdReady = page.AddItem<TestModeToggleItem>("RewardedAdReady");
            rewardedAdReady
                .SetLabel("Rewarded Ad Ready")
                .SetValue(GameEntry.RewardedAdReady)
                .OnValueChanged(value =>
                {
                    GameEntry.RewardedAdReady = value;
                    Debug.Log("Rewarded Ad Ready: " + value);
                });

            var networkReachable = page.AddItem<TestModeToggleItem>("NetworkReachable");
            networkReachable
                .SetLabel("Network Reachable")
                .SetValue(GameEntry.NetworkReachable)
                .OnValueChanged(value =>
                {
                    GameEntry.NetworkReachable = value;
                    Debug.Log("Network Reachable: " + value);
                });
        }
    }

    public sealed class RuntimeTestModeModule : ITestModeModule
    {
        public string OwnerId => TestModeModuleRegistry.CommonOwnerId;
        public string PageName => "Runtime";

        public string ModuleName => "Runtime";

        public int Order => 30;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            if (GameEntry.Debugger != null)
            {
                GameEntry.Debugger.ActiveWindow = false;
            }

            page.AddItem<TestModeInfoItem>("CurrentLevelLogicTime")
                .SetLabel("CurrentLevelLogicTime")
                .SetValueGetter(GetCurrentLevelLogicTimeText);

            var debuggerWindow = page.AddItem<TestModeToggleItem>("DebuggerWindow");
            debuggerWindow
                .SetLabel("Debugger Window")
                .SetValue(false)
                .OnValueChanged(value =>
                {
                    if (GameEntry.Debugger != null)
                    {
                        GameEntry.Debugger.ActiveWindow = value;
                    }

                    Debug.Log("Debugger Window: " + value);
                });

            var GMMode = page.AddItem<TestModeToggleItem>("GMMode");
            GMMode
                .SetLabel("GM Mode")
                .SetValue(GameEntry.GMMode)
                .OnValueChanged(value =>
                {
                    GameEntry.GMMode = value;
                    Debug.Log("GM Mode: " + value);
                });

            var showLog = page.AddItem<TestModeToggleItem>("ShowLog");
            showLog
                .SetLabel("Show Log")
                .SetValue(GameEntry.ShowLog)
                .OnValueChanged(value =>
                {
                    GameEntry.SetShowLog(value);
                    Debug.Log("Show Log: " + value);
                });

            var shopVisible = page.AddItem<TestModeToggleItem>("ShopVisible");
            shopVisible
                .SetLabel("Shop Visible")
                .SetValue(GameEntry.Shop != null && GameEntry.Shop.IsAvailable)
                .OnValueChanged(value =>
                {
                    if (GameEntry.Shop != null)
                    {
                        GameEntry.Shop.EnableShop = value;
                        if (value)
                        {
                            GameEntry.Shop.RefreshProductInfo(true);
                        }
                    }

                    Debug.Log("Shop Visible: " + value);
                });
        }

        private static string GetCurrentLevelLogicTimeText()
        {
            return GameEntry.GameManager != null
                ? GameEntry.GameManager.CurrentLevelLogicTime.ToString("0.0")
                : "0.0";
        }
    }

}
