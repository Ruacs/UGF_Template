using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lokas
{
    /// <summary>Composed by HexaAwayTestModeModule and bound to the same entry session.</summary>
    public sealed class HexaAwayRemoteLevelConfigTestModeModule : ITestModeModule
    {
        private readonly HexaAwayGameManagerComponent m_Manager;
        private readonly HexaAwayGameData m_Data;
        private readonly List<HexaAwayRemoteLevelConfigItem> m_ConfigItems = new List<HexaAwayRemoteLevelConfigItem>();
        private readonly string m_BaseUrl = HexaAwayRemoteLevelConfigLoader.DefaultBaseUrl;
        private TestModeModuleContext m_Context;
        private string m_Status = "Not loaded";
        private int m_SelectedIndex;
        private bool m_IsBusy;

        public HexaAwayRemoteLevelConfigTestModeModule(HexaAwayGameManagerComponent manager, HexaAwayGameData data)
        {
            m_Manager = manager;
            m_Data = data;
        }

        public string OwnerId => nameof(GameMode.HexaAway);
        public string PageName => "Hexa";
        public string ModuleName => "RemoteConfig";
        public int Order => 1;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            m_Context = context;
            page.AddItem<TestModeInfoItem>("HexaAwayRemoteServer").SetLabel("Remote Server")
                .SetValueGetter(() => m_BaseUrl);
            page.AddItem<TestModeInfoItem>("HexaAwayRemoteStatus").SetLabel("Remote Status")
                .SetValueGetter(() => m_Status);
            page.AddItem<TestModeButtonItem>("HexaAwayRefreshRemoteConfigs").SetLabel("Remote Configs")
                .SetButtonText(m_IsBusy ? "Loading..." : "Refresh").OnClick(() => RefreshConfigsAsync().Forget());
            page.AddItem<TestModeDropdownItem>("HexaAwayRemoteConfigDropdown").SetLabel("Config")
                .SetOptions(GetConfigOptionLabels())
                .SetValue(Mathf.Clamp(m_SelectedIndex, 0, Mathf.Max(0, m_ConfigItems.Count - 1)))
                .OnValueChanged(index =>
                {
                    if (!CanUse(context) || m_IsBusy) return;
                    m_SelectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, m_ConfigItems.Count - 1));
                    m_Status = m_ConfigItems.Count > 0 ? $"Selected {GetConfigName(m_ConfigItems[m_SelectedIndex])}" : "No remote configs";
                });
            page.AddItem<TestModeButtonItem>("HexaAwayLoadRemoteConfig").SetLabel("Apply Remote")
                .SetButtonText(m_IsBusy ? "Loading..." : "Load").OnClick(() => LoadSelectedConfigAsync().Forget());
        }

        private bool CanUse(TestModeModuleContext context)
        {
            return context != null && context.IsActive && ReferenceEquals(context, m_Context)
                && m_Manager != null && m_Manager.IsResourcesInitialized && m_Data != null;
        }

        private async UniTaskVoid RefreshConfigsAsync()
        {
            TestModeModuleContext context = m_Context;
            if (m_IsBusy || !CanUse(context)) return;
            m_IsBusy = true;
            m_Status = "Refreshing remote configs...";
            context.RequestRefresh();
            try
            {
                var manifest = await HexaAwayRemoteLevelConfigLoader.LoadManifestAsync(m_BaseUrl, context.CancellationToken);
                if (!CanUse(context)) return;
                m_ConfigItems.Clear();
                foreach (var item in manifest.Configs)
                    if (item != null && !string.IsNullOrWhiteSpace(item.File)) m_ConfigItems.Add(item);
                m_SelectedIndex = Mathf.Clamp(m_SelectedIndex, 0, Mathf.Max(0, m_ConfigItems.Count - 1));
                m_Status = m_ConfigItems.Count > 0 ? $"Loaded {m_ConfigItems.Count} configs" : "No remote configs";
            }
            catch (OperationCanceledException) when (!context.IsActive || context.CancellationToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                if (CanUse(context))
                {
                    m_Status = exception.Message;
                    Debug.LogError(exception);
                }
            }
            finally
            {
                m_IsBusy = false;
                if (CanUse(context)) context.RequestRefresh();
            }
        }

        private async UniTaskVoid LoadSelectedConfigAsync()
        {
            TestModeModuleContext context = m_Context;
            if (m_IsBusy || !CanUse(context)) return;
            if (m_ConfigItems.Count == 0)
            {
                m_Status = "Refresh remote configs first";
                context.RequestRefresh();
                return;
            }

            var item = m_ConfigItems[Mathf.Clamp(m_SelectedIndex, 0, m_ConfigItems.Count - 1)];
            m_IsBusy = true;
            m_Status = $"Loading {GetConfigName(item)}...";
            context.RequestRefresh();
            try
            {
                CompactLevelConfig config = await HexaAwayRemoteLevelConfigLoader.LoadConfigAsync(m_BaseUrl, item, context.CancellationToken);
                // A completed download must not mutate a game after its Procedure has left.
                if (!CanUse(context)) return;
                if (config.AmountOfLevels <= 0 || CompactLevelParser.Parse(config.GetLevelByIndex(0)) == null)
                    throw new InvalidOperationException("The remote config has no valid first level.");

                CompactLevelConfig previousConfig = m_Manager.LevelConfig;
                int previousLevel = m_Data.CurrentLevel;
                try
                {
                    m_Manager.SetLevelConfig(config);
                    m_Data.CurrentLevel = 0;
                    if (!m_Manager.BuildLevel(0))
                        throw new InvalidOperationException("The remote level could not be built.");
                }
                catch
                {
                    m_Manager.SetLevelConfig(previousConfig);
                    m_Data.CurrentLevel = previousLevel;
                    throw;
                }
                m_Status = $"Applied {GetConfigName(item)} ({config.AmountOfLevels} levels)";
            }
            catch (OperationCanceledException) when (!context.IsActive || context.CancellationToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                if (CanUse(context))
                {
                    m_Status = exception.Message;
                    Debug.LogError(exception);
                }
            }
            finally
            {
                m_IsBusy = false;
                if (CanUse(context)) context.RequestRefresh();
            }
        }

        private IEnumerable<string> GetConfigOptionLabels()
        {
            if (m_ConfigItems.Count == 0)
            {
                yield return "No remote configs";
                yield break;
            }
            foreach (var item in m_ConfigItems) yield return $"{item.Name} ({item.LevelCount})";
        }

        private static string GetConfigName(HexaAwayRemoteLevelConfigItem item)
        {
            return string.IsNullOrWhiteSpace(item.Name) ? item.Id : item.Name;
        }
    }
}
