using System;
using UnityEngine;

namespace Lokas.Activities.Collector
{
    [CreateAssetMenu(fileName = "CollectorActivityDefinition", menuName = "GF/Activity/Collector/Definition")]
    public sealed class CollectorActivityDefinition : ActivityModuleDefinition
    {
        [SerializeField] private CollectorActivityConfig m_Config;

        public CollectorActivityConfig Config => m_Config;

        public override IActivityModule CreateModule()
        {
            if (m_Config == null)
                throw new InvalidOperationException("Collector definition has no configuration asset.");
            return new CollectorActivityModule(m_Config, GameIds);
        }

#if UNITY_EDITOR
        public void SetInstallationData(CollectorActivityConfig config, string[] gameIds, ActivityPageDefinition[] pages)
        {
            m_Config = config;
            SetInstallation(CollectorActivityModule.Id, gameIds, pages);
        }
#endif
    }
}
