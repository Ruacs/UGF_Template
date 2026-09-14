using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class CustomConfigComponent : GameFrameworkComponent
    {
        private readonly Dictionary<string, object> m_JsonConfigs = new Dictionary<string, object>();

        [SerializeField] private TMP_ColorGradient m_ColorGradient;
        public TMP_ColorGradient ColorGradient => m_ColorGradient;

        [SerializeField] private PropDataBaseSO m_PropDataBaseSO;
        public PropDataBaseSO PropDataBaseSO => m_PropDataBaseSO;

        [SerializeField] private AvatarDatabaseSO m_AvatarConfig;
        public AvatarDatabaseSO AvatarConfig => m_AvatarConfig;

        [SerializeField] private RewardDataBaseSO m_RewardConfig;
        public RewardDataBaseSO RewardConfig => m_RewardConfig;


        public void SetRewardConfig(RewardDataBaseSO rewardDataBaseSO)
        {
            m_RewardConfig = rewardDataBaseSO;
        }

        public void SetAvatarConfig(AvatarDatabaseSO avatarDatabaseSO)
        {
            m_AvatarConfig = avatarDatabaseSO;
        }

        public void SetPropDataConfig(PropDataBaseSO propDataBaseSO)
        {
            m_PropDataBaseSO = propDataBaseSO;
        }

        public void LoadJsonConfig<T>(string configName, bool fromBytes, Action<T> onSuccess = null, Action<string> onFailure = null)
        {
            CustomJsonConfigLoader.Load<T>(
                configName,
                fromBytes,
                config =>
                {
                    m_JsonConfigs[GetJsonConfigKey<T>(configName)] = config;
                    onSuccess?.Invoke(config);
                },
                onFailure);
        }

        public bool TryGetJsonConfig<T>(string configName, out T config)
        {
            if (m_JsonConfigs.TryGetValue(GetJsonConfigKey<T>(configName), out object value) && value is T typedConfig)
            {
                config = typedConfig;
                return true;
            }

            config = default;
            return false;
        }

        public T GetJsonConfig<T>(string configName)
        {
            return TryGetJsonConfig(configName, out T config) ? config : default;
        }

        private static string GetJsonConfigKey<T>(string configName)
        {
            return $"{typeof(T).FullName}:{configName}";
        }

    }
}
