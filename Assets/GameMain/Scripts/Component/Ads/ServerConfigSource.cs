using System;
using System.Globalization;
using LitJson;
using UnityGameFramework.Runtime;
using YzAdComponent;

namespace Lokas
{
    /// <summary>保留 SDK 的 openAd 开关；格式错误回退，合法的 0/false 不丢失。</summary>
    public sealed class YzServerConfigSource : IServerConfigSource
    {
        public bool Contains(string key) => YzUtils.openAd && YzUtils.checkConfigHashKey(key);

        public int GetInt(string key, int fallback)
        {
            string raw = GetString(key, null);
            if (raw == null) return fallback;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return value;
            Log.Warning("Invalid integer server config '{0}', using fallback.", key);
            return fallback;
        }

        public bool GetBool(string key, bool fallback)
        {
            string raw = GetString(key, null);
            if (raw == null) return fallback;
            if (bool.TryParse(raw, out bool value)) return value;
            Log.Warning("Invalid boolean server config '{0}', using fallback.", key);
            return fallback;
        }

        public string GetString(string key, string fallback)
        {
            if (!Contains(key)) return fallback;
            try { return YzUtils.getConfigStrValue(key, fallback); }
            catch (Exception exception) when (exception is NullReferenceException || exception is InvalidOperationException)
            {
                Log.Warning("Invalid string server config '{0}', using fallback.", key);
                return fallback;
            }
        }

        public JsonData GetArray(string key)
        {
            if (!Contains(key)) return null;
            JsonData value = YzUtils.getConfigJsonArryValue(key);
            return value != null && value.IsArray ? value : null;
        }
    }

    /// <summary>
    /// 由所属游戏显式启用作用域兼容：GameName.Key 优先，未提供时读取原 Key。
    /// 有作用域的错误值使用该字段默认值，不再回退到另一份旧值。
    /// </summary>
    public sealed class ScopedServerConfigSource : IServerConfigSource
    {
        private readonly IServerConfigSource m_Source;
        private readonly string m_Prefix;

        public ScopedServerConfigSource(IServerConfigSource source, string gameName)
        {
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(gameName)) throw new ArgumentException("Game name is required.");
            m_Prefix = gameName + ".";
        }

        private string Resolve(string key) => m_Source.Contains(m_Prefix + key) ? m_Prefix + key : key;
        public bool Contains(string key) => m_Source.Contains(Resolve(key));
        public int GetInt(string key, int fallback) => m_Source.GetInt(Resolve(key), fallback);
        public bool GetBool(string key, bool fallback) => m_Source.GetBool(Resolve(key), fallback);
        public string GetString(string key, string fallback) => m_Source.GetString(Resolve(key), fallback);
        public JsonData GetArray(string key) => m_Source.GetArray(Resolve(key));
    }
}

