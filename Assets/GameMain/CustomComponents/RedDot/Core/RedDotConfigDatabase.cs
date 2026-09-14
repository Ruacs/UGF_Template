using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime.RedDot
{
    [CreateAssetMenu(menuName = "GameMain/Red Dot/Config Database", fileName = "RedDotConfigDatabase")]
    public sealed class RedDotConfigDatabase : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/GameMain/CustomComponents/RedDot/RedDotConfigDatabase.asset";

        [SerializeField] private List<RedDotConfigEntry> m_Entries = new List<RedDotConfigEntry>();

        public IReadOnlyList<RedDotConfigEntry> Entries => m_Entries;

        public bool TryGetEntry(string key, out RedDotConfigEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                for (int i = 0; i < m_Entries.Count; i++)
                {
                    RedDotConfigEntry item = m_Entries[i];
                    if (item != null && item.Key == key)
                    {
                        entry = item;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        public bool TryGetPath(string key, out string path)
        {
            if (TryGetEntry(key, out RedDotConfigEntry entry))
            {
                path = entry.Path;
                return !string.IsNullOrWhiteSpace(path);
            }

            path = null;
            return false;
        }
    }
}
