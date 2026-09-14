using System;
using UnityEngine;

namespace UnityGameFramework.Runtime.RedDot
{
    [Serializable]
    public sealed class RedDotConfigEntry
    {
        [SerializeField] private string m_Key;
        [SerializeField] private string m_DisplayName;
        [SerializeField] private string m_Path;
        [SerializeField] private string m_Description;

        public string Key => m_Key;
        public string DisplayName => string.IsNullOrWhiteSpace(m_DisplayName) ? m_Key : m_DisplayName;
        public string Path => m_Path;
        public string Description => m_Description;

        public RedDotConfigEntry()
        {
        }

        public RedDotConfigEntry(string key, string displayName, string path, string description)
        {
            m_Key = key;
            m_DisplayName = displayName;
            m_Path = path;
            m_Description = description;
        }
    }
}
