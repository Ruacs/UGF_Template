using System;
using System.Collections.Generic;
using ConfigSO;
using Lokas;
using UnityEngine;
[CreateAssetMenu(fileName = "RewardDataSO", menuName = "GF/Common/Reward/Bundle")]
public class RewardDataSO : IdOnlyConfigSO
{
    [SerializeField] private List<RewardEntry> m_Entries = new List<RewardEntry>();
    [SerializeField] private RewardChestStyleSO m_ChestStyle;

    public IReadOnlyList<RewardEntry> Entries => m_Entries;
    public RewardChestStyleSO ChestStyle => m_ChestStyle;

    public void ValidateEntries()
    {
        if (m_Entries == null) throw new InvalidOperationException($"Reward bundle '{name}' has a null list.");
        foreach (RewardEntry entry in m_Entries)
        {
            if (entry == null) throw new InvalidOperationException($"Reward bundle '{name}' has a null entry.");
            entry.Validate();
        }
        if (m_Entries.Count > 1 && (m_ChestStyle == null || m_ChestStyle.sprite == null))
            throw new InvalidOperationException($"Reward bundle '{name}' needs a chest style with an icon.");
    }

#if UNITY_EDITOR
    public void ConfigureEntries(IEnumerable<RewardEntry> entries, RewardChestStyleSO chestStyle = null)
    {
        m_Entries = entries == null ? new List<RewardEntry>() : new List<RewardEntry>(entries);
        m_ChestStyle = chestStyle;
    }
#endif
}
