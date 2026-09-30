using System;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    [CreateAssetMenu(fileName = "MiningActivityDefinition", menuName = "GF/Activity/Mining/Definition")]
    public sealed class MiningActivityDefinition : ActivityModuleDefinition
    {
        [SerializeField] private MiningActivityConfig m_Config;

        public MiningActivityConfig Config => m_Config;

        public override IActivityModule CreateModule()
        {
            if (m_Config == null)
                throw new InvalidOperationException("Mining definition has no configuration asset.");
            return new MiningActivityModule(m_Config);
        }

#if UNITY_EDITOR
        public void SetInstallationData(MiningActivityConfig config, string[] gameIds,
            ActivityPageDefinition[] pages)
        {
            m_Config = config;
            SetInstallation(MiningActivityModule.Id, gameIds, pages);
        }
#endif
    }
}
