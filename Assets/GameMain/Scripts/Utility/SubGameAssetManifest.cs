using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>公共数据结构，资产实例必须位于所属游戏的 ScriptableObjects/Registry/。</summary>
    [CreateAssetMenu(fileName = "GameAssetRegistryConfig", menuName = "GF/SubGame/Asset Manifest")]
    public sealed class SubGameAssetManifest : ScriptableObject
    {
        [SerializeField] private SubGameAssetRegistryEntry m_Entry = new();
        public SubGameAssetRegistryEntry Entry => m_Entry;
        public string GameName => m_Entry?.GameName;

        public void Initialize(SubGameAssetRegistryEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            // 拷贝序列化数据，迁移后不保留与旧总表共享的可变列表。
            m_Entry = JsonUtility.FromJson<SubGameAssetRegistryEntry>(JsonUtility.ToJson(entry));
        }
    }
}

