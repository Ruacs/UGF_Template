using System;
using UnityEngine;

namespace Lokas
{
    [CreateAssetMenu(menuName = "GF/SubGame/HexaAway/Element Registry", fileName = "ElementRegistry")]
    public sealed class ElementRegistrySO : ScriptableObject
    {
        [SerializeField] private PlatformPrefabData[] platforms;
        [SerializeField] private ObjectPrefabData[] objects;
        [SerializeField] private EffectPrefabData[] effects;

        public GameObject GetPlatformPrefab(GroundType groundType)
        {
            if (TryGetPlatformPrefab(groundType, out GameObject prefab))
            {
                return prefab;
            }

            if (groundType != GroundType.Normal && TryGetPlatformPrefab(GroundType.Normal, out prefab))
            {
                Debug.LogWarning($"Platform prefab for {groundType} is not configured. Normal platform prefab will be used.", this);
                return prefab;
            }

            Debug.LogError($"Platform prefab for {groundType} is not configured.", this);
            return null;
        }

        public bool TryGetPlatformPrefab(GroundType groundType, out GameObject prefab)
        {
            if (platforms != null)
            {
                for (int i = 0; i < platforms.Length; i++)
                {
                    if (platforms[i] != null && platforms[i].GroundType == groundType && platforms[i].Prefab != null)
                    {
                        prefab = platforms[i].Prefab;
                        return true;
                    }
                }
            }

            prefab = null;
            return false;
        }

        public bool TryGetObjectPrefab(LevelObjectType objectType, out GameObject prefab)
        {
            if (objects != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i] != null && objects[i].Type == objectType && objects[i].Prefab != null)
                    {
                        prefab = objects[i].Prefab;
                        return true;
                    }
                }
            }

            prefab = null;
            return false;
        }

        public TileEffectBehavior GetEffectBehavior(TileEffectType effectType)
        {
            if (effects != null)
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    if (effects[i] != null && effects[i].Type == effectType)
                    {
                        return effects[i].Behavior;
                    }
                }
            }

            Debug.LogError($"Tile effect behavior for {effectType} is not configured.", this);
            return null;
        }
    }

    [Serializable]
    public sealed class PlatformPrefabData
    {
        [SerializeField] private int id;
        [SerializeField] private GroundType groundType;
        [SerializeField] private GameObject prefab;

        public int Id => id;
        public GroundType GroundType => groundType;
        public GameObject Prefab => prefab;
    }

    [Serializable]
    public sealed class ObjectPrefabData
    {
        [SerializeField] private int id;
        [SerializeField] private LevelObjectType type;
        [SerializeField] private GameObject prefab;

        public int Id => id;
        public LevelObjectType Type => type;
        public GameObject Prefab => prefab;
    }

    [Serializable]
    public sealed class EffectPrefabData
    {
        [SerializeField] private int id;
        [SerializeField] private TileEffectType type;
        [SerializeField] private TileEffectBehavior behavior;

        public int Id => id;
        public TileEffectType Type => type;
        public TileEffectBehavior Behavior => behavior;
    }
}
