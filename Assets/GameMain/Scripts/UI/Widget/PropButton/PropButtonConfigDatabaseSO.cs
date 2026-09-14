using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 道具按钮配置库，集中管理所有 PropButtonConfigSO，并按 ItemId 提供查询。
    /// </summary>
    [CreateAssetMenu(fileName = "PropButtonConfigDatabase", menuName = "GF/Common/UI/Prop Button Config Database")]
    public class PropButtonConfigDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<PropButtonConfigSO> m_Configs = new List<PropButtonConfigSO>();

        private Dictionary<int, PropButtonConfigSO> m_ConfigMap;

        /// <summary>
        /// 获取全部配置，供外部批量创建按钮或做编辑器检查。
        /// </summary>
        public IReadOnlyList<PropButtonConfigSO> Configs => m_Configs;

        /// <summary>
        /// 按 ItemId 查询配置资产。
        /// </summary>
        public bool TryGetConfig(int itemId, out PropButtonConfigSO config)
        {
            EnsureMap();
            return m_ConfigMap.TryGetValue(itemId, out config);
        }

        /// <summary>
        /// 按 ItemId 创建运行时配置。调用方再结合存档生成 PropButtonState。
        /// </summary>
        public bool TryCreateRuntimeConfig(int itemId, out PropButtonConfig config)
        {
            config = null;
            if (!TryGetConfig(itemId, out PropButtonConfigSO configSO) || configSO == null)
            {
                return false;
            }

            config = configSO.CreateRuntimeConfig();
            return true;
        }

        /// <summary>
        /// 清理缓存，下次查询时重新生成字典。
        /// </summary>
        public void Rebuild()
        {
            m_ConfigMap = null;
            EnsureMap();
        }

        private void EnsureMap()
        {
            if (m_ConfigMap != null)
            {
                return;
            }

            m_ConfigMap = new Dictionary<int, PropButtonConfigSO>();
            if (m_Configs == null)
            {
                return;
            }

            foreach (PropButtonConfigSO config in m_Configs)
            {
                if (config == null)
                {
                    continue;
                }

                if (m_ConfigMap.ContainsKey(config.ItemId))
                {
                    Debug.LogWarning($"[PropButtonConfigDatabaseSO] Duplicate ItemId: {config.ItemId}", this);
                    continue;
                }

                m_ConfigMap.Add(config.ItemId, config);
            }
        }

        private void OnValidate()
        {
            m_ConfigMap = null;
        }
    }
}
