using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class TestModeUI : MonoBehaviour
    {
        public static TestModeUI Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private GameObject TestModeRoot;
        [SerializeField] private TestModeRoot m_Root;
        [SerializeField] private TestModeRoot m_RootPrefab;
        [SerializeField] private TestModePage m_PagePrefab;
        [SerializeField] private TestModePrefabCatalog m_ItemPrefabs = new TestModePrefabCatalog();
        [SerializeField] private Button m_FloatingButton;
        [SerializeField] private bool HideFloatingButton = false;

        private TestModeModuleRegistry m_Registry;
        private bool m_RefreshRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureRoot();
            EnsureFloatingButton();
        }

        private void OnDestroy()
        {
            if (m_Registry != null) m_Registry.Changed -= RequestRefresh;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            if (!m_RefreshRequested)
            {
                return;
            }

            m_RefreshRequested = false;
            RefreshUI();
        }

        public void BindModules(TestModeModuleRegistry registry)
        {
            if (ReferenceEquals(m_Registry, registry)) return;
            if (m_Registry != null) m_Registry.Changed -= RequestRefresh;
            m_Registry = registry;
            if (m_Registry != null) m_Registry.Changed += RequestRefresh;
            RequestRefresh();
        }

        public void RequestRefresh()
        {
            m_RefreshRequested = true;
        }

        public void SetTestModeRootActive(bool active)
        {
            if (active) RequestRefresh();
            EnsureRoot();
            if (m_Root != null)
            {
                m_Root.gameObject.SetActive(active);
                return;
            }

            if (TestModeRoot != null)
            {
                TestModeRoot.SetActive(active);
            }
        }

        public void SetAvailable(bool available)
        {
            EnsureRoot();
            EnsureFloatingButton();

            if (m_FloatingButton != null && !HideFloatingButton)
            {
                m_FloatingButton.gameObject.SetActive(available);
            }

            if (!available)
            {
                SetTestModeRootActive(false);
            }
        }

        public void ToggleTestModeRootActive()
        {
            SetTestModeRootActive(!IsTestModeRootActive());
        }

        public bool IsTestModeRootActive()
        {
            EnsureRoot();
            if (m_Root != null)
            {
                return m_Root.gameObject.activeSelf;
            }

            return TestModeRoot != null && TestModeRoot.activeSelf;
        }

        private void RefreshUI()
        {
            EnsureRoot();
            if (m_Root != null && m_Registry != null)
            {
                m_Root.Render(m_Registry);
            }
        }

        private void EnsureRoot()
        {
            if (!IsSceneObject(TestModeRoot))
            {
                TestModeRoot = null;
            }

            if (!IsSceneObject(m_Root))
            {
                m_Root = null;
            }

            if (TestModeRoot == null)
            {
                Transform root = transform.Find("TestModeRoot");
                TestModeRoot = root != null ? root.gameObject : gameObject;
            }

            if (m_Root == null && TestModeRoot != null)
            {
                m_Root = TestModeRoot.GetComponent<TestModeRoot>();
                if (m_Root == null)
                {
                    if (m_RootPrefab != null)
                    {
                        m_Root = Instantiate(m_RootPrefab, TestModeRoot.transform, false);
                        HideLegacyChildren(m_Root.transform);
                    }
                    else
                    {
                        m_Root = TestModeRoot.AddComponent<TestModeRoot>();
                    }
                }

                m_Root.SetPrefabs(m_PagePrefab, m_ItemPrefabs);
            }
        }

        private void EnsureFloatingButton()
        {
            if (HideFloatingButton) return;

            if (!IsSceneObject(m_FloatingButton))
            {
                m_FloatingButton = null;
            }

            if (m_FloatingButton == null)
            {
                Transform buttonTransform = transform.Find("Btn_Show");
                if (buttonTransform != null)
                {
                    m_FloatingButton = buttonTransform.GetComponent<Button>();
                }
            }


            if (m_FloatingButton != null)
            {
                m_FloatingButton.onClick.RemoveListener(ToggleTestModeRootActive);
                m_FloatingButton.onClick.AddListener(ToggleTestModeRootActive);
            }
        }

        private static bool IsSceneObject(Component component)
        {
            return component != null && IsSceneObject(component.gameObject);
        }

        private static bool IsSceneObject(GameObject gameObject)
        {
            return gameObject != null && gameObject.scene.IsValid() && gameObject.scene.isLoaded;
        }

        private void HideLegacyChildren(Transform activeRoot)
        {
            if (TestModeRoot == null)
            {
                return;
            }

            Transform rootTransform = TestModeRoot.transform;
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                Transform child = rootTransform.GetChild(i);
                if (child != activeRoot)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}
