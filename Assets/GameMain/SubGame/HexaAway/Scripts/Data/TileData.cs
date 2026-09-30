using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// Cell 上的可�?Tile 数据，与 Ground 不再互斥�?    /// </summary>
    [Serializable]
    public sealed class TileData
    {
        [SerializeField] private bool exists;
        [SerializeField] private HexaAwayDirection direction;
        [SerializeField] private int visualGroupId;

        public bool Exists => exists;
        public HexaAwayDirection HexaAwayDirection => direction;
        public int VisualGroupId => visualGroupId;

        public TileData()
        {
        }

        public TileData(bool exists, HexaAwayDirection direction = HexaAwayDirection.Up, int visualGroupId = 0)
        {
            this.exists = exists;
            this.direction = direction;
            this.visualGroupId = visualGroupId;
        }
    }
}
