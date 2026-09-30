using System;
using UnityEngine;

namespace Lokas.Activities.WinStreak
{
    [CreateAssetMenu(fileName = "WinStreakActivityDefinition", menuName = "GF/Activity/Win Streak/Definition")]
    public sealed class WinStreakActivityDefinition : ActivityModuleDefinition
    {
        [SerializeField] private WinStreakActivityConfig m_Config;

        public WinStreakActivityConfig Config => m_Config;

        public override IActivityModule CreateModule()
        {
            if (m_Config == null)
                throw new InvalidOperationException("Win Streak definition has no configuration asset.");
            return new WinStreakActivityModule(m_Config, GameIds);
        }

#if UNITY_EDITOR
        public void SetInstallationData(WinStreakActivityConfig config, string[] gameIds,
            ActivityPageDefinition[] pages)
        {
            m_Config = config;
            SetInstallation(WinStreakActivityModule.Id, gameIds, pages);
        }
#endif
    }
}
