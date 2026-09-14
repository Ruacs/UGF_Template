using System;
using System.Collections.Generic;
using System.Globalization;
using LitJson;
using UnityEngine;

namespace Lokas
{
    // 只复用格式解析，Key 与默认值仍由各游戏配置持有。
    public static class SubGameServerConfigParsing
    {
        public static List<IdleHintStage> ParseIdleHintStages(JsonData jsonData, List<IdleHintStage> fallback)
        {
            if (jsonData == null || !jsonData.IsArray || jsonData.Count <= 0)
                return fallback;

            var stages = new List<IdleHintStage>();
            for (int i = 0; i < jsonData.Count; i++)
            {
                JsonData item = jsonData[i];
                if (item == null || !item.IsObject)
                    continue;

                int minLevel = GetJsonIntAny(item, 1, "minLevel", "MinLevel", "startLevel", "StartLevel");
                int maxLevel = GetJsonIntAny(item, 0, "maxLevel", "MaxLevel", "level", "Level", "endLevel", "EndLevel");
                int seconds = GetJsonIntAny(item, 0, "seconds", "Seconds", "idleHintSeconds", "IdleHintSeconds");

                if (seconds <= 0)
                    continue;

                stages.Add(new IdleHintStage
                {
                    MinLevel = Mathf.Max(1, minLevel),
                    MaxLevel = Mathf.Max(0, maxLevel),
                    Seconds = seconds
                });
            }

            if (stages.Count <= 0)
                return fallback;

            stages.Sort((a, b) =>
            {
                int minCompare = a.MinLevel.CompareTo(b.MinLevel);
                if (minCompare != 0) return minCompare;
                return a.MaxLevel.CompareTo(b.MaxLevel);
            });

            return stages;
        }

        public static string ParseLevelConfigName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "LevelConfig";

            value = value.Trim();

            if (value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(0, value.Length - ".json".Length);

            if (value.Equals("A", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("B", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("C", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("D", StringComparison.OrdinalIgnoreCase))
            {
                return $"LevelConfig_{value.ToUpperInvariant()}";
            }

            return value;
        }

        private static int GetJsonIntAny(JsonData jsonData, int defaultValue, params string[] keys)
        {
            JsonData value = GetJsonDataAny(jsonData, keys);
            return value == null ? defaultValue : ParseJsonInt(value, defaultValue);
        }

        private static JsonData GetJsonDataAny(JsonData jsonData, params string[] keys)
        {
            if (jsonData == null || keys == null)
                return null;

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (!string.IsNullOrEmpty(key) && jsonData.ContainsKey(key))
                    return jsonData[key];
            }

            return null;
        }

        private static int ParseJsonInt(JsonData jsonData, int defaultValue)
        {
            if (jsonData == null)
                return defaultValue;

            return int.TryParse(jsonData.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : defaultValue;
        }


    }

    public sealed class IdleHintStage
    {
        public int MinLevel;
        public int MaxLevel;
        public int Seconds;

        public bool ContainsLevel(int level)
        {
            if (level < MinLevel)
                return false;

            return MaxLevel <= 0 || level <= MaxLevel;
        }
    }
}

