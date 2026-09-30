using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 道具类型和表现预制体的绑定。预制体由 Inspector 配置，避免在代码里硬编码资源路径。
    /// </summary>
    [Serializable]
    public sealed class HexaAwayPropPrefabBinding
    {
        [SerializeField] private HexaAwayPropType m_PropType;
        [SerializeField] private GameObject m_Prefab;

        public HexaAwayPropType PropType => m_PropType;
        public GameObject Prefab => m_Prefab;
    }
}
