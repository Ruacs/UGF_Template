using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Lokas
{
    public sealed class RewardItemViewData
    {
        public Sprite Icon { get; }
        public string Text { get; }
        public string Name { get; }
        public RewardItemViewData(Sprite icon, string text, string name = null)
        {
            Icon = icon;
            Text = text;
            Name = name;
        }
    }

    /// <summary>展示转换不修改奖励清单，也不触发入账或计时。</summary>
    public static class RewardPresentation
    {
        public static string FormatValue(RewardEntry entry, int multiplier = 1)
        {
            if (multiplier <= 0) throw new ArgumentOutOfRangeException(nameof(multiplier));
            long amount = checked(entry.Amount * multiplier);
            if (entry.GrantMode == RewardGrantMode.AddQuantity)
                return "×" + amount.ToString(CultureInfo.InvariantCulture);
            return "∞ " + FormatDuration(amount);
        }

        public static string FormatDuration(long seconds)
        {
            long hours = seconds / 3600;
            long minutes = seconds % 3600 / 60;
            long remainder = seconds % 60;
            var parts = new List<string>(3);
            if (hours > 0) parts.Add(hours.ToString(CultureInfo.InvariantCulture) + "h");
            if (minutes > 0) parts.Add(minutes.ToString(CultureInfo.InvariantCulture) + "m");
            if (remainder > 0 || parts.Count == 0) parts.Add(remainder.ToString(CultureInfo.InvariantCulture) + "s");
            return string.Join(" ", parts);
        }

        public static IReadOnlyList<RewardItemViewData> Build(RewardDataSO bundle)
        {
            if (bundle == null) return Array.Empty<RewardItemViewData>();
            bundle.ValidateEntries();
            return Build(bundle.Entries);
        }

        public static IReadOnlyList<RewardItemViewData> Build(IReadOnlyList<RewardEntry> rewards)
        {
            var result = new List<RewardItemViewData>();
            if (rewards == null) return result;
            foreach (RewardEntry reward in rewards) result.Add(BuildItem(reward));
            return result.AsReadOnly();
        }

        public static RewardItemViewData BuildItem(RewardEntry entry, int multiplier = 1)
        {
            if (entry == null) throw new InvalidOperationException("Reward entry is missing.");
            entry.Validate();
            Sprite icon = entry.Definition.GetIcon(entry.GrantMode);
            string value = FormatValue(entry, multiplier);
            return new RewardItemViewData(icon, icon != null ? value : entry.DisplayName + "\n" + value, entry.DisplayName);
        }
    }
}
