using System;
using UnityEngine;

namespace Lokas.Activities.SeasonPass
{
    [CreateAssetMenu(fileName = "SeasonPassActivityDefinition", menuName = "GF/Activity/Season Pass/Definition")]
    public sealed class SeasonPassActivityDefinition : ActivityModuleDefinition
    {
        [SerializeField] private SeasonPassActivityConfig m_Config;

        public SeasonPassActivityConfig Config => m_Config;

        public override IActivityModule CreateModule()
        {
            if (m_Config == null) throw new InvalidOperationException("Season pass definition has no configuration asset.");
            return new SeasonPassActivityModule(m_Config, GameIds);
        }

#if UNITY_EDITOR
        public void SetInstallationData(SeasonPassActivityConfig config, string[] gameIds, ActivityPageDefinition[] pages)
        {
            m_Config = config;
            SetInstallation(SeasonPassActivityModule.Id, gameIds, pages);
        }
#endif
    }
}
