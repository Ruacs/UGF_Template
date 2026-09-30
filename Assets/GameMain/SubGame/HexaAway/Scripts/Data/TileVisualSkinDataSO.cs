using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 单套 HexaAway Tile 皮肤配置。每套皮肤独立成资产，方便后续单独维护。
    /// </summary>
    [CreateAssetMenu(menuName = "GF/SubGame/HexaAway/Tile Visual Skin", fileName = "HexaAway Tile Visual Skin")]
    public sealed class TileVisualSkinDataSO : ScriptableObject
    {
        [SerializeField] private string skinId = "Default";
        [SerializeField] private GameObject tileGamePrefab;
        [SerializeField] private TileDirectionVisualData[] directionVisuals;
        [SerializeField] private TileVisualGroupData[] visualGroups;

        public string SkinId => skinId;
        public GameObject TileGamePrefab => tileGamePrefab;
        public TileDirectionVisualData[] DirectionVisuals => directionVisuals;
        public TileVisualGroupData[] VisualGroups => visualGroups;

        public TileVisualData GetTileVisualData(HexaAwayDirection direction, int groupId)
        {
            if (groupId > 0 && visualGroups != null)
                foreach (TileVisualGroupData group in visualGroups)
                    if (group != null && group.GroupId == groupId) return group;
            if (directionVisuals != null)
                foreach (TileDirectionVisualData visual in directionVisuals)
                    if (visual != null && visual.Direction == direction) return visual;
            Debug.LogError($"Tile skin {skinId} has no visual for {direction}.", this);
            return null;
        }
    }
}
