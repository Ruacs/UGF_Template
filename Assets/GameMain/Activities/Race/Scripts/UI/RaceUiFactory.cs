using System;
using UnityEngine;

namespace Lokas.Activities.Race.UI
{
    /// <summary>Race 运行时仅计算展示数值与颜色；页面结构和可见文案均在 Prefab 内维护。</summary>
    internal static class RaceUiFactory
    {
        public static string FormatCountdown(System.TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "00:00:00";
            long totalHours = Math.Max(0L, (long)Math.Floor(remaining.TotalHours));
            return $"{totalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        public static Color RankColor(int rank)
        {
            return rank switch
            {
                1 => new Color(1f, 0.78f, 0.12f),
                2 => new Color(0.7f, 0.82f, 0.94f),
                3 => new Color(0.94f, 0.55f, 0.26f),
                _ => new Color(0.78f, 0.9f, 0.96f)
            };
        }
    }
}
