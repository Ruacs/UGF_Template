using System;
using System.Collections.Generic;

namespace Lokas
{
    /// <summary>由 AdsServerConfig 持有；只聚合实例，不创建具体玩法配置。</summary>
    public sealed class SubGameServerConfigRegistry
    {
        private readonly Dictionary<GameMode, ISubGameServerConfig> m_Configs = new();
        private IServerConfigSource m_Source;

        public int Count => m_Configs.Count;
        public bool IsLoaded => m_Source != null;

        public bool Register(ISubGameServerConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.GameMode == GameMode.None) throw new ArgumentException("A game config needs an explicit game mode.");
            if (m_Configs.TryGetValue(config.GameMode, out var existing))
            {
                if (ReferenceEquals(existing, config)) return false;
                throw new InvalidOperationException($"Duplicate server config for '{config.GameMode}'.");
            }

            // SDK/预加载先完成时，后注册模块也要加载，不受全局一次性标记阻塞。
            if (m_Source != null) config.Load(m_Source);
            m_Configs.Add(config.GameMode, config);
            return true;
        }

        public bool Unregister(ISubGameServerConfig config)
        {
            if (config == null || !m_Configs.TryGetValue(config.GameMode, out var existing) ||
                !ReferenceEquals(existing, config)) return false;
            return m_Configs.Remove(config.GameMode);
        }

        public bool TryGet(GameMode gameMode, out ISubGameServerConfig config) =>
            m_Configs.TryGetValue(gameMode, out config);

        public bool TryGet<T>(GameMode gameMode, out T config) where T : class, ISubGameServerConfig
        {
            config = m_Configs.TryGetValue(gameMode, out var value) ? value as T : null;
            return config != null;
        }

        public void Load(IServerConfigSource source, bool force = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (IsLoaded && !force) return;
            foreach (var config in m_Configs.Values) config.Load(source);
            m_Source = source;
        }
    }
}

