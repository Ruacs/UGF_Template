using UnityGameFramework.Runtime.RedDot;
using GameEntry = Lokas.GameEntry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GF_Mahjong.RedDot;

namespace GF_Mahjong.UI
{
    public enum RedDotDisplayType
    {
        DotOnly,
        CountOnly,
        BackgroundAndCount,
        DotAndBackgroundAndCount,
        Custom
    }

    public enum RedDotPathInputMode
    {
        Custom,
        Preset
    }

    public enum RedDotPathPreset
    {
        Main,
        MainTask,
        MainTaskDaily,
        MainTaskAchievement,
        MainShop,
        MainShopFree,
        MainRank,
        MainRankSeasonReward,
        Game,
        GameItem
    }

    public class RedDotBinder : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private RedDotConfigDatabase m_ConfigDatabase;
        [SerializeField] private string m_ConfigKey;
        [SerializeField, HideInInspector] private RedDotPathInputMode m_PathInputMode = RedDotPathInputMode.Custom;
        [SerializeField, HideInInspector] private RedDotPathPreset m_PathPreset = RedDotPathPreset.Main;
        [SerializeField, HideInInspector] private string m_Path;
        [SerializeField] private GameObject m_RedDotRoot;

        [Header("Display")]
        [SerializeField] private RedDotDisplayType m_DisplayType = RedDotDisplayType.DotOnly;
        [Tooltip("Small dot object. Optional for DotOnly when Red Dot Root itself is the dot.")]
        [SerializeField] private GameObject m_DotRoot;
        [Tooltip("Text object root for count display.")]
        [SerializeField] private GameObject m_CountRoot;
        [Tooltip("Background object used by BackgroundAndCount modes.")]
        [SerializeField] private GameObject m_CountBackground;
        [SerializeField] private Text m_CountText;
        [SerializeField] private TextMeshProUGUI m_TMPCountText;

        [Header("Count")]
        [SerializeField] private int m_MaxDisplayCount = 99;
        [SerializeField] private string m_OverflowSuffix = "+";

        [Header("Visibility")]
        [SerializeField] private bool m_HideWhenZero = true;

        public string Path
        {
            get => GetResolvedPath();
            set
            {
                if (m_Path == value)
                    return;

                Unsubscribe();
                m_ConfigDatabase = null;
                m_ConfigKey = string.Empty;
                m_PathInputMode = RedDotPathInputMode.Custom;
                m_Path = value;
                Subscribe();
                Refresh();
            }
        }

        public string ConfigKey => m_ConfigKey;

        public RedDotDisplayType DisplayType
        {
            get => m_DisplayType;
            set
            {
                if (m_DisplayType == value)
                    return;

                m_DisplayType = value;
                Refresh();
            }
        }

        public void ApplyConfig(RedDotConfigDatabase database, string key, string path)
        {
            Unsubscribe();
            m_ConfigDatabase = database;
            m_ConfigKey = key;
            m_PathInputMode = RedDotPathInputMode.Custom;
            m_Path = path;
            Subscribe();
            Refresh();
        }

        private void Awake()
        {
            AutoBindReferences();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Refresh()
        {
            string path = GetResolvedPath();
            if (m_RedDotRoot == null || GameEntry.RedDot == null || string.IsNullOrWhiteSpace(path))
                return;

            int count = GameEntry.RedDot.GetCount(path);
            bool active = count > 0;
            bool rootActive = m_HideWhenZero ? active : true;
            bool rootIsBinderObject = m_RedDotRoot == gameObject;

            if (!rootIsBinderObject)
                m_RedDotRoot.SetActive(rootActive);
            if (!rootActive)
            {
                if (rootIsBinderObject)
                    ApplyDisplayObjects(false, false, false);
                return;
            }

            ApplyDisplay(count, active);
        }

        private void Subscribe()
        {
            if (GameEntry.RedDot == null)
                return;

            GameEntry.RedDot.Changed -= OnRedDotChanged;
            GameEntry.RedDot.Changed += OnRedDotChanged;
        }

        private void Unsubscribe()
        {
            if (GameEntry.RedDot == null)
                return;

            GameEntry.RedDot.Changed -= OnRedDotChanged;
        }

        private void OnRedDotChanged(RedDotChange change)
        {
            if (change.Path == GetResolvedPath())
                Refresh();
        }

        private string GetResolvedPath()
        {
            if (m_ConfigDatabase != null && m_ConfigDatabase.TryGetPath(m_ConfigKey, out string configPath))
                return configPath;

            if (!string.IsNullOrWhiteSpace(m_Path))
                return m_Path;

            if (m_PathInputMode == RedDotPathInputMode.Preset)
                return GetPresetPath(m_PathPreset);

            return string.Empty;
        }

        private static string GetPresetPath(RedDotPathPreset preset)
        {
            switch (preset)
            {
                case RedDotPathPreset.Main:
                    return RedDotPath.Main;
                case RedDotPathPreset.MainTask:
                    return RedDotPath.MainTask;
                case RedDotPathPreset.MainTaskDaily:
                    return RedDotPath.MainTaskDaily;
                case RedDotPathPreset.MainTaskAchievement:
                    return RedDotPath.MainTaskAchievement;
                case RedDotPathPreset.MainShop:
                    return RedDotPath.MainShop;
                case RedDotPathPreset.MainShopFree:
                    return RedDotPath.MainShopFree;
                case RedDotPathPreset.MainRank:
                    return RedDotPath.MainRank;
                case RedDotPathPreset.MainRankSeasonReward:
                    return RedDotPath.MainRankSeasonReward;
                case RedDotPathPreset.Game:
                    return RedDotPath.Game;
                case RedDotPathPreset.GameItem:
                    return RedDotPath.GameItem;
                default:
                    return string.Empty;
            }
        }

        private void ApplyDisplay(int count, bool active)
        {
            bool showDot = false;
            bool showCount = false;
            bool showBackground = false;

            switch (m_DisplayType)
            {
                case RedDotDisplayType.DotOnly:
                    showDot = active;
                    break;
                case RedDotDisplayType.CountOnly:
                    showCount = active;
                    break;
                case RedDotDisplayType.BackgroundAndCount:
                    showCount = active;
                    showBackground = active;
                    break;
                case RedDotDisplayType.DotAndBackgroundAndCount:
                    showDot = active;
                    showCount = active;
                    showBackground = active;
                    break;
                case RedDotDisplayType.Custom:
                    SetCountText(FormatCount(count));
                    return;
            }

            SetActiveIfNotNull(m_DotRoot, showDot);
            SetActiveIfNotNull(m_CountRoot, showCount);
            SetActiveIfNotNull(m_CountBackground, showBackground);

            if (showCount)
                SetCountText(FormatCount(count));
        }

        private void ApplyDisplayObjects(bool showDot, bool showCount, bool showBackground)
        {
            SetActiveIfNotNull(m_DotRoot, showDot);
            SetActiveIfNotNull(m_CountRoot, showCount);
            SetActiveIfNotNull(m_CountBackground, showBackground);
        }

        private void SetCountText(string text)
        {
            if (m_CountText != null)
                m_CountText.text = text;
            if (m_TMPCountText != null)
                m_TMPCountText.text = text;
        }

        private string FormatCount(int count)
        {
            if (count > m_MaxDisplayCount)
                return $"{m_MaxDisplayCount}{m_OverflowSuffix}";

            return count.ToString();
        }

        private void AutoBindReferences()
        {
            if (m_RedDotRoot == null)
                m_RedDotRoot = gameObject;
            if (m_DotRoot == null)
                m_DotRoot = FindChildGameObject("Dot", "RedDot", "Point");
            if (m_CountRoot == null)
                m_CountRoot = FindChildGameObject("Count", "Num", "Number", "Text");
            if (m_CountBackground == null)
                m_CountBackground = FindChildGameObject("Bg", "BG", "Background", "Num_bg", "CountBg");
            if (m_CountText == null)
                m_CountText = GetComponentInChildren<Text>(true);
            if (m_TMPCountText == null)
                m_TMPCountText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private GameObject FindChildGameObject(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = transform.Find(names[i]);
                if (child != null)
                    return child.gameObject;
            }

            return null;
        }

        private void SetActiveIfNotNull(GameObject target, bool active)
        {
            if (target != null && target != m_RedDotRoot)
                target.SetActive(active);
        }

#if UNITY_EDITOR
        public void ApplyEditorPreview()
        {
            AutoBindReferences();

            bool showDot = false;
            bool showCount = false;
            bool showBackground = false;

            switch (m_DisplayType)
            {
                case RedDotDisplayType.DotOnly:
                    showDot = true;
                    break;
                case RedDotDisplayType.CountOnly:
                    showCount = true;
                    break;
                case RedDotDisplayType.BackgroundAndCount:
                    showCount = true;
                    showBackground = true;
                    break;
                case RedDotDisplayType.DotAndBackgroundAndCount:
                    showDot = true;
                    showCount = true;
                    showBackground = true;
                    break;
                case RedDotDisplayType.Custom:
                    showDot = true;
                    showCount = true;
                    showBackground = true;
                    break;
            }

            ApplyDisplayObjects(showDot, showCount, showBackground);
            if (showCount)
                SetCountText(FormatCount(m_MaxDisplayCount + 1));
            else
                SetCountText(string.Empty);
        }

        private void OnValidate()
        {
            AutoBindReferences();
            if (!Application.isPlaying)
                ApplyEditorPreview();
        }
#endif
    }
}

