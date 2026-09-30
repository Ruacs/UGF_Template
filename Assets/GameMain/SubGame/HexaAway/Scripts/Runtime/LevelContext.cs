using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 单局关卡运行上下文，避免 Tile/交互物直接散落引用多个配置入口。
    /// </summary>
    public sealed class LevelContext
    {
        public HexaAwayGameManagerComponent Manager { get; }
        public ElementRegistrySO ElementRegistry { get; }
        public TilesVisualsDataSO VisualsData { get; }
        public TileVisualSkinDataSO TileSkin { get; }

        /// <summary>
        /// 创建关卡上下文。
        /// </summary>
        public LevelContext(HexaAwayGameManagerComponent manager, ElementRegistrySO elementRegistry, TilesVisualsDataSO visualsData, string skinId = null)
        {
            Manager = manager;
            ElementRegistry = elementRegistry;
            VisualsData = visualsData;
            TileSkin = visualsData != null ? visualsData.GetSkinData(string.IsNullOrEmpty(skinId) ? visualsData.DefaultSkinId : skinId) : null;
        }

        /// <summary>
        /// 从元素注册表查询效果行为配置。
        /// </summary>
        public TileEffectBehavior GetEffectBehavior(TileEffectType effectType)
        {
            return ElementRegistry != null ? ElementRegistry.GetEffectBehavior(effectType) : null;
        }

        /// <summary>
        /// 从元素注册表查询交互物 Prefab。
        /// </summary>
        public bool TryGetInteractableObjectPrefab(LevelObjectType objectType, out GameObject prefab)
        {
            if (ElementRegistry != null)
            {
                return ElementRegistry.TryGetObjectPrefab(objectType, out prefab);
            }

            prefab = null;
            return false;
        }

        public GameObject GetPlatformPrefab(GroundType groundType)
        {
            return ElementRegistry != null ? ElementRegistry.GetPlatformPrefab(groundType) : null;
        }

        /// <summary>
        /// 从视觉配置查询 Tile 视觉数据。
        /// </summary>
        public TileVisualData GetTileVisualData(HexaAwayDirection direction, int visualGroupId)
        {
            return TileSkin != null ? TileSkin.GetTileVisualData(direction, visualGroupId) : null;
        }
    }
}
