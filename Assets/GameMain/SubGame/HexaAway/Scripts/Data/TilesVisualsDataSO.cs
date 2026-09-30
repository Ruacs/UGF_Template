using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// HexaAway 棋盘视觉数据库，负责登记可用皮肤并提供当前皮肤查询。
    /// </summary>
    [CreateAssetMenu(menuName = "GF/SubGame/HexaAway/Tiles Visuals Data", fileName = "HexaAway Tiles Visuals Data")]
    public sealed class TilesVisualsDataSO : ScriptableObject
    {
        [SerializeField] private string defaultSkinId = "Default";
        [SerializeField] private TileVisualSkinDataSO[] skins;

        public string DefaultSkinId => defaultSkinId;
        public TileVisualSkinDataSO[] Skins => skins;
        public TileVisualSkinDataSO CurrentSkin => GetSkinData(defaultSkinId);
        public GameObject TileGamePrefab => CurrentSkin?.TileGamePrefab;
        public TileDirectionVisualData[] DirectionVisuals => CurrentSkin?.DirectionVisuals;
        public TileVisualGroupData[] VisualGroups => CurrentSkin?.VisualGroups;

        /// <summary>
        /// 获取指定皮肤配置。找不到时回退到第一个皮肤，避免运行时直接空配置。
        /// </summary>
        public TileVisualSkinDataSO GetSkinData(string skinId)
        {
            if (skins == null || skins.Length == 0)
            {
                Debug.LogError("HexaAway tile visual skin is not configured.", this);
                return null;
            }

            if (!string.IsNullOrEmpty(skinId))
            {
                for (int i = 0; i < skins.Length; i++)
                {
                    if (skins[i] != null && skins[i].SkinId == skinId)
                    {
                        return skins[i];
                    }
                }
            }

            return skins[0];
        }

        /// <summary>
        /// 获取当前皮肤下指定 Tile 的视觉配置。visualGroupId 为 0 时按方向取默认视觉。
        /// </summary>
        public TileVisualData GetTileVisualData(HexaAwayDirection direction, int visualGroupId)
        {
            if (visualGroupId > 0)
            {
                TileVisualGroupData groupData = GetVisualGroupData(visualGroupId);
                if (groupData != null)
                {
                    return groupData;
                }
            }

            return GetDirectionVisualData(direction);
        }

        /// <summary>
        /// 获取当前皮肤下指定方向对应的默认视觉配置。
        /// </summary>
        public TileDirectionVisualData GetDirectionVisualData(HexaAwayDirection direction)
        {
            TileDirectionVisualData[] directionVisuals = DirectionVisuals;
            if (directionVisuals != null)
            {
                for (int i = 0; i < directionVisuals.Length; i++)
                {
                    if (directionVisuals[i] != null && directionVisuals[i].Direction == direction)
                    {
                        return directionVisuals[i];
                    }
                }
            }

            Debug.LogError($"HexaAway tile direction visual data for {direction} is not configured.", this);
            return null;
        }

        /// <summary>
        /// 获取当前皮肤下指定共享视觉组配置。
        /// </summary>
        public TileVisualGroupData GetVisualGroupData(int visualGroupId)
        {
            TileVisualGroupData[] visualGroups = VisualGroups;
            if (visualGroups != null)
            {
                for (int i = 0; i < visualGroups.Length; i++)
                {
                    if (visualGroups[i] != null && visualGroups[i].GroupId == visualGroupId)
                    {
                        return visualGroups[i];
                    }
                }
            }

            Debug.LogWarning($"HexaAway tile visual group {visualGroupId} is not configured. Direction visual will be used.", this);
            return null;
        }
    }
}
