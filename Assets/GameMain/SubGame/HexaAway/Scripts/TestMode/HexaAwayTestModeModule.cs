using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public sealed class HexaAwayTestModeModule : ITestModeModule, ITestModeModuleLifecycle
    {
        private readonly HexaAwayGameManagerComponent m_Manager;
        private readonly HexaAwayGameData m_Data;
        private readonly HexaAwayRemoteLevelConfigTestModeModule m_Remote;
        private TestModeModuleContext m_Context;

        public HexaAwayTestModeModule(HexaAwayGameManagerComponent manager, HexaAwayGameData data)
        {
            m_Manager = manager;
            m_Data = data;
            m_Remote = new HexaAwayRemoteLevelConfigTestModeModule(manager, data);
        }

        public string OwnerId => nameof(GameMode.HexaAway);
        private bool CanUse => m_Context != null && m_Context.IsActive && m_Manager != null
            && m_Manager.IsResourcesInitialized && m_Data != null;

        public void OnRegistered(TestModeModuleContext context)
        {
            m_Context = context;
            if (m_Data != null) m_Data.OnCurrentLevelChanged += OnLevelChanged;
            if (m_Manager != null) m_Manager.LevelLoaded += OnLevelLoaded;
        }

        public void OnUnregistered()
        {
            if (m_Data != null) m_Data.OnCurrentLevelChanged -= OnLevelChanged;
            if (m_Manager != null) m_Manager.LevelLoaded -= OnLevelLoaded;
            m_Context = null;
        }

        private void OnLevelChanged(int level) => m_Context?.RequestRefresh();
        private void OnLevelLoaded() => m_Context?.RequestRefresh();

        public string PageName => "Hexa";

        public string ModuleName => "Main";

        public int Order => 0;

        public void Build(TestModePage page, TestModeModuleContext context)
        {
            if (!CanUse)
            {
                page.AddItem<TestModeInfoItem>("HexaAwayUnavailable").SetLabel("HexaAway").SetValue("Not ready");
                return;
            }
            int levelCount = Mathf.Max(1, m_Manager.AmountOfLevels);
            var levelStepper = page.AddItem<TestModeIntStepperItem>("HexaAwayCurrentLevel");
            levelStepper
                .SetLabel("Level")
                .SetRange(1, levelCount)
                .SetValue(Mathf.Clamp(m_Data.CurrentLevel + 1, 1, levelCount))
                .SetNotifyOnStepButtonOrInputEnd(true)
                .OnValueChanged(level =>
                {
                    if (!CanUse) return;
                    int selectedLevel = Mathf.Clamp(level, 1, Mathf.Max(1, m_Manager.AmountOfLevels));
                    int previousLevel = m_Data.CurrentLevel;
                    m_Data.CurrentLevel = selectedLevel - 1;
                    try
                    {
                        if (!m_Manager.BuildLevel(selectedLevel - 1)) m_Data.CurrentLevel = previousLevel;
                    }
                    catch
                    {
                        m_Data.CurrentLevel = previousLevel;
                        throw;
                    }
                    context.RequestRefresh();
                });

            List<string> skinIds = GetTileSkinIds();
            var skinDropdown = page.AddItem<TestModeDropdownItem>("HexaAwayTileSkin");
            skinDropdown
                .SetLabel("Tile Skin")
                .SetOptions(skinIds.Count > 0 ? skinIds : new[] { "No Skins" })
                .SetValue(GetCurrentSkinIndex(skinIds))
                .OnValueChanged(index => SelectTileSkin(index));
            m_Remote.Build(page, context);
        }

        private List<string> GetTileSkinIds()
        {
            var result = new List<string>();
            TileVisualSkinDataSO[] skins = m_Manager != null ? m_Manager.VisualsData?.Skins : null;
            if (skins == null)
            {
                return result;
            }

            for (int i = 0; i < skins.Length; i++)
            {
                string skinId = skins[i] != null ? skins[i].SkinId : null;
                if (!string.IsNullOrEmpty(skinId))
                {
                    result.Add(skinId);
                }
            }

            return result;
        }

        private int GetCurrentSkinIndex(IReadOnlyList<string> skinIds)
        {
            if (skinIds == null || skinIds.Count == 0)
            {
                return 0;
            }

            string selectedSkinId = m_Data?.TileSkinId;
            if (string.IsNullOrEmpty(selectedSkinId) && m_Manager != null)
            {
                selectedSkinId = m_Manager.VisualsData?.DefaultSkinId;
            }

            for (int i = 0; i < skinIds.Count; i++)
            {
                if (skinIds[i] == selectedSkinId)
                {
                    return i;
                }
            }

            return 0;
        }

        private void SelectTileSkin(int index)
        {
            if (!CanUse) return;
            HexaAwayGameManagerComponent manager = m_Manager;
            List<string> skinIds = GetTileSkinIds();
            if (manager == null || skinIds.Count == 0)
            {
                return;
            }

            string skinId = skinIds[Mathf.Clamp(index, 0, skinIds.Count - 1)];
            if (!manager.SelectTileSkin(skinId))
            {
                Debug.LogWarning($"HexaAway tile skin {skinId} is not available.");
                return;
            }

            if (manager.IsLevelLoaded && m_Data != null)
            {
                int currentLevel = m_Data.CurrentLevel;
                manager.ClearLevel();
                manager.BuildLevel(currentLevel);
            }

            Debug.Log($"HexaAway Tile Skin: {skinId}");
        }
    }
}
