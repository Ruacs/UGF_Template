using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    [Serializable]
    public class RankTargetItemConfig
    {
        public int Id;
        public Sprite Icon;
        public string TitleKey;
        public string DescriptionKey;
    }

    [CreateAssetMenu(fileName = "RankTargetConfig", menuName = "GameMain/Rank/Target Config")]
    public class RankTargetConfigSO : ScriptableObject
    {
        [SerializeField] private List<RankTargetItemConfig> m_Items = new();

        public IReadOnlyList<RankTargetItemConfig> Items => m_Items;

        public void AppendValidIds(List<int> targetIds)
        {
            if (targetIds == null || m_Items == null) return;

            for (int i = 0; i < m_Items.Count; i++)
            {
                RankTargetItemConfig item = m_Items[i];
                if (item == null || item.Id <= 0) continue;
                if (targetIds.Contains(item.Id)) continue;

                targetIds.Add(item.Id);
            }
        }

        public bool TryGetItem(int id, out RankTargetItemConfig item)
        {
            if (m_Items != null)
            {
                for (int i = 0; i < m_Items.Count; i++)
                {
                    RankTargetItemConfig current = m_Items[i];
                    if (current == null || current.Id != id) continue;

                    item = current;
                    return true;
                }
            }

            item = null;
            return false;
        }

        public Sprite GetIcon(int id)
        {
            return TryGetItem(id, out RankTargetItemConfig item) ? item.Icon : null;
        }

        public string GetTitleKey(int id)
        {
            return TryGetItem(id, out RankTargetItemConfig item) ? item.TitleKey : null;
        }

        public string GetDescriptionKey(int id)
        {
            return TryGetItem(id, out RankTargetItemConfig item) ? item.DescriptionKey : null;
        }
    }
}
