using System;
using UnityEngine;

namespace Lokas.Activities.GalaxyChallenge
{
    [CreateAssetMenu(fileName = "GalaxyChallengeActivityDefinition",
        menuName = "GF/Activity/Galaxy Challenge/Definition")]
    public sealed class GalaxyChallengeActivityDefinition : ActivityModuleDefinition
    {
        [SerializeField] private GalaxyChallengeActivityConfig m_Config;

        public GalaxyChallengeActivityConfig Config => m_Config;

        public override IActivityModule CreateModule()
        {
            if (m_Config == null)
                throw new InvalidOperationException("Galaxy Challenge definition has no configuration asset.");
            return new GalaxyChallengeActivityModule(m_Config, GameIds);
        }

#if UNITY_EDITOR
        public void SetInstallationData(GalaxyChallengeActivityConfig config, string[] gameIds,
            ActivityPageDefinition[] pages)
        {
            m_Config = config;
            SetInstallation(GalaxyChallengeActivityModule.Id, gameIds, pages);
        }
#endif
    }
}
