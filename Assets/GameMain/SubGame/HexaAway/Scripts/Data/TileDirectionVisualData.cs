using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// Tile 可复用视觉数据，包含材质与平台反馈色。
    /// </summary>
    [Serializable]
    public abstract class TileVisualData
    {
        [SerializeField] private Material mat_tile;
        [SerializeField] private Material mat_Arrow;
        [SerializeField] private Color feedbackColor = Color.white;

        public Material Mat_Tile => mat_tile;
        public Material Mat_Arrow => mat_Arrow;
        public Color FeedbackColor => feedbackColor;
    }

    /// <summary>
    /// 默认方向视觉配置。Tile 未指定视觉组时按方向使用它。
    /// </summary>
    [Serializable]
    public sealed class TileDirectionVisualData : TileVisualData
    {
        [SerializeField] private HexaAwayDirection direction;

        public HexaAwayDirection Direction => direction;
    }

    /// <summary>
    /// 共享视觉组配置。不同方向的 Tile 可以通过同一个组 ID 共用外观。
    /// </summary>
    [Serializable]
    public sealed class TileVisualGroupData : TileVisualData
    {
        [SerializeField] private int groupId;

        public int GroupId => groupId;
    }
}
