using GF_Mahjong.RedDot;
using GameEntry = Lokas.GameEntry;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime.RedDot;

namespace GF_Mahjong.UI
{
    public class RedDotCountBinder : MonoBehaviour
    {
        [SerializeField] private string m_Path;
        [SerializeField] private Text m_Text;
        [SerializeField] private TextMeshProUGUI m_TMPText;
        [SerializeField] private int m_MaxDisplayCount = 99;
        [SerializeField] private string m_OverflowSuffix = "+";

        public string Path
        {
            get => m_Path;
            set
            {
                if (m_Path == value)
                    return;

                Unsubscribe();
                m_Path = value;
                Subscribe();
                Refresh();
            }
        }

        private void Awake()
        {
            if (m_Text == null)
                m_Text = GetComponent<Text>();
            if (m_TMPText == null)
                m_TMPText = GetComponent<TextMeshProUGUI>();
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
            if (GameEntry.RedDot == null || string.IsNullOrWhiteSpace(m_Path))
                return;

            string text = FormatCount(GameEntry.RedDot.GetCount(m_Path));
            if (m_Text != null)
                m_Text.text = text;
            if (m_TMPText != null)
                m_TMPText.text = text;
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
            if (change.Path == m_Path)
                Refresh();
        }

        private string FormatCount(int count)
        {
            if (count > m_MaxDisplayCount)
                return $"{m_MaxDisplayCount}{m_OverflowSuffix}";

            return count.ToString();
        }
    }
}

