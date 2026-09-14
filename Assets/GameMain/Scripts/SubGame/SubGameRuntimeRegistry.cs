using System;
using System.Collections.Generic;

namespace Lokas
{
    /// <summary>GameManager 拥有的显式注册集合；不扫描程序集或自动猜测已安装游戏。</summary>
    public sealed class SubGameRuntimeRegistry
    {
        private Dictionary<GameMode, SubGameManagerComponent> m_ByMode = new();
        private List<SubGameManagerComponent> m_Installed = new();
        public IReadOnlyList<SubGameManagerComponent> Installed => m_Installed;

        public void Rebuild(IEnumerable<SubGameManagerComponent> managers)
        {
            var byMode = new Dictionary<GameMode, SubGameManagerComponent>();
            var installed = new List<SubGameManagerComponent>();
            foreach (var manager in managers ?? Array.Empty<SubGameManagerComponent>())
            {
                if (manager == null) throw new InvalidOperationException("Installed subgame list contains a missing manager.");
                if (manager.GameMode == GameMode.None || string.IsNullOrEmpty(manager.SceneConfigKey) ||
                    manager.GameProcedureType == null || !typeof(ProcedureGame).IsAssignableFrom(manager.GameProcedureType))
                    throw new InvalidOperationException("Incomplete subgame registration: " + manager.name);
                if (byMode.ContainsKey(manager.GameMode)) throw new InvalidOperationException("Duplicate subgame mode: " + manager.GameMode);
                byMode.Add(manager.GameMode, manager);
                installed.Add(manager);
            }
            m_ByMode = byMode;
            m_Installed = installed;
        }

        public bool TryGet(GameMode mode, out SubGameManagerComponent manager) => m_ByMode.TryGetValue(mode, out manager) && manager != null;
        public T Get<T>(GameMode mode) where T : SubGameManagerComponent => TryGet(mode, out var manager) ? manager as T : null;
        public SubGameManagerComponent Get(GameMode mode) => TryGet(mode, out var manager) ? manager : null;
        public SubGameManagerComponent GetForScene(int sceneId)
        {
            foreach (var manager in m_Installed)
                if (manager != null && GameEntry.Config.GetInt(manager.SceneConfigKey) == sceneId) return manager;
            return null;
        }
    }
}
