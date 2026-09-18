using System;
using UnityEngine;

namespace Lokas.Activities.Race
{
    [CreateAssetMenu(fileName = "RaceActivityDefinition", menuName = "GF/Activity/Race/Definition")]
    public sealed class RaceActivityDefinition : ActivityModuleDefinition
    {
        [SerializeField] private RaceActivityConfig m_Config;

        public RaceActivityConfig Config => m_Config;

        public override IActivityModule CreateModule()
        {
            if (m_Config == null) throw new InvalidOperationException("Race definition has no configuration asset.");
            return new RaceActivityModule(m_Config, GameIds);
        }

#if UNITY_EDITOR
        public void SetInstallationData(RaceActivityConfig config, string[] gameIds, ActivityPageDefinition[] pages)
        {
            m_Config = config;
            SetInstallation(RaceActivityModule.Id, gameIds, pages);
        }
#endif
    }
}
