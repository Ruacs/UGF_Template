using System;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class TestModeComponent : GameFrameworkComponent
    {
        [SerializeField] private TestModeUI m_TestModeUI;
        [SerializeField] private GameObject m_TestModeRoot;
        [SerializeField] private KeyCode m_ToggleKey = KeyCode.BackQuote;

        private bool m_LastVisible;

        private TestModeModuleRegistry m_Modules;
        public TestModeModuleRegistry Modules
        {
            get
            {
                if (m_Modules == null)
                {
                    m_Modules = new TestModeModuleRegistry(SimulateRankReward);
                    m_Modules.Register(new AdsTestModeModule());
                    m_Modules.Register(new RuntimeTestModeModule());
                }
                return m_Modules;
            }
        }

        public bool IsWindowVisible => IsEnabled && m_TestModeUI != null && m_TestModeUI.IsTestModeRootActive();

        public bool RegisterModule(ITestModeModule module)
        {
            bool registered = Modules.Register(module);
            BindUI();
            return registered;
        }

        public bool UnregisterModule(ITestModeModule module) => m_Modules != null && m_Modules.Unregister(module);

        private void OnDestroy()
        {
            m_Modules?.Dispose();
            m_Modules = null;
        }

        private bool m_IsEnabled = false;
        public bool IsEnabled { get => m_IsEnabled; set => m_IsEnabled = value; }

        public void SetEnabled(bool enabled)
        {
            m_IsEnabled = enabled;
            RefreshVisibility();
        }

        protected override void Awake()
        {
            base.Awake();
            if (!IsSceneObject(m_TestModeUI))
            {
                m_TestModeUI = null;
            }

            if (!IsSceneObject(m_TestModeRoot))
            {
                m_TestModeRoot = null;
            }

            if (m_TestModeUI == null)
            {
                m_TestModeUI = GetComponentInChildren<TestModeUI>(true);
            }

            if (m_TestModeRoot == null && m_TestModeUI != null)
            {
                m_TestModeRoot = m_TestModeUI.gameObject;
            }
        }

        private void Start()
        {
            BindUI();
            RefreshVisibility();
        }

        private void Update()
        {
            if (m_LastVisible != IsEnabled)
            {
                RefreshVisibility();
            }

            if (IsEnabled && m_ToggleKey != KeyCode.None && Input.GetKeyDown(m_ToggleKey))
            {
                ToggleVisible();
            }
        }

        public void RefreshVisibility()
        {
            BindUI();

            if (m_TestModeUI != null)
            {
                m_TestModeUI.SetAvailable(IsEnabled);
            }

            if (m_TestModeRoot != null)
            {
                m_TestModeRoot.SetActive(IsEnabled);
            }

            m_LastVisible = IsEnabled;
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void ToggleVisible()
        {
            if (!IsEnabled) return;
            BindUI();
            if (m_TestModeUI != null)
            {
                m_TestModeUI.ToggleTestModeRootActive();
                return;
            }

            if (m_TestModeRoot != null)
            {
                m_TestModeRoot.SetActive(!m_TestModeRoot.activeSelf);
            }
        }

        private void SetVisible(bool visible)
        {
            visible = visible && IsEnabled;
            BindUI();
            if (m_TestModeUI != null)
            {
                m_TestModeUI.SetAvailable(IsEnabled);
                m_TestModeUI.SetTestModeRootActive(visible);
            }

            if (m_TestModeRoot != null)
            {
                m_TestModeRoot.SetActive(visible);
            }
        }

        private void BindUI()
        {
            if (!IsSceneObject(m_TestModeUI))
            {
                m_TestModeUI = null;
            }

            if (!IsSceneObject(m_TestModeRoot))
            {
                m_TestModeRoot = null;
            }

            if (m_TestModeUI == null)
            {
                m_TestModeUI = GetComponentInChildren<TestModeUI>(true);
            }

            if (m_TestModeUI == null)
            {
                if (m_TestModeRoot != null)
                {
                    m_TestModeRoot.SetActive(IsEnabled);
                }

                return;
            }

            if (m_TestModeRoot == null)
            {
                m_TestModeRoot = m_TestModeUI.gameObject;
            }

            m_TestModeUI.BindModules(Modules);
        }

        public void SimulateRankReward(int rank)
        {
            if (GameEntry.Rank == null)
            {
                return;
            }

            int clampedRank = Mathf.Clamp(rank, 1, RankComponent.MaxRankRewardRank);
            GameEntry.Rank.DEV_ForceRankReward(clampedRank);

            if (GameEntry.UI == null || !GameEntry.UI.HasUIForm(UIFormId.MainUIPanel))
            {
                return;
            }

            MainUIPanel mainUI = GameEntry.UI.GetUIForm(UIFormId.MainUIPanel) as MainUIPanel;
            mainUI?.TryOpenRankReward().Forget();
        }

        private static bool IsSceneObject(Component component)
        {
            return component != null && IsSceneObject(component.gameObject);
        }

        private static bool IsSceneObject(GameObject gameObject)
        {
            return gameObject != null && gameObject.scene.IsValid() && gameObject.scene.isLoaded;
        }

    }
}
