using System;
using UnityEngine;

namespace Lokas
{
    [Serializable]
    public sealed class LevelObjectSourceData
    {
        [SerializeField] private int rawCellCode;
        [SerializeField] private int sourceTypeCode;
        [SerializeField] private int categoryCode;
        [SerializeField] private int typeCode;
        [SerializeField] private int optionCode;

        public int RawCellCode => rawCellCode;
        public int SourceTypeCode => sourceTypeCode;
        public int CategoryCode => categoryCode;
        public int TypeCode => typeCode;
        public int OptionCode => optionCode;

        public LevelObjectSourceData()
        {
        }

        public LevelObjectSourceData(int rawCellCode, int sourceTypeCode = 0, int categoryCode = 0, int typeCode = 0, int optionCode = 0)
        {
            this.rawCellCode = rawCellCode;
            this.sourceTypeCode = sourceTypeCode;
            this.categoryCode = categoryCode;
            this.typeCode = typeCode;
            this.optionCode = optionCode;
        }
    }

    [Serializable]
    public sealed class LevelObjectData
    {
        [SerializeField] private LevelObjectType type;
        [SerializeField] private LevelObjectLayer layer;
        [SerializeField] private HexaAwayDirection direction;
        [SerializeField] private int value;
        [SerializeField] private int pairId;
        [SerializeField] private int groupId;
        [SerializeField] private LevelObjectSourceData source;

        public LevelObjectType Type => type;
        public LevelObjectLayer Layer => layer;
        public HexaAwayDirection Direction => direction;
        public int Value => value;
        public int PairId => pairId;
        public int GroupId => groupId;
        public LevelObjectSourceData Source => source;

        public LevelObjectData()
        {
        }

        public LevelObjectData(
            LevelObjectType type,
            LevelObjectLayer layer = LevelObjectLayer.Board,
            HexaAwayDirection direction = HexaAwayDirection.Up,
            int value = 1,
            int pairId = 0,
            int groupId = 0,
            LevelObjectSourceData source = null)
        {
            this.type = type;
            this.layer = layer;
            this.direction = direction;
            this.value = value;
            this.pairId = pairId;
            this.groupId = groupId;
            this.source = source;
        }
    }

    /// <summary>
    /// 关卡中的一个格子，包含地面类型和可选的 Tile。
    /// </summary>
    [Serializable]
    public sealed class LevelCellData
    {
        [SerializeField] private Vector2Int position;
        [SerializeField] private GroundType groundType;
        [SerializeField] private LevelObjectData groundData;
        [SerializeField] private TileData tile;
        [SerializeField] private LevelObjectData[] objects;
        [SerializeField] private TileEffectData[] effects;

        public Vector2Int Position => position;
        public GroundType GroundType => groundType;
        public LevelObjectData GroundData => groundData;
        public TileData Tile => tile;
        public LevelObjectData[] Objects => objects;
        public TileEffectData[] Effects => effects;

        public LevelCellData()
        {
        }

        public LevelCellData(
            Vector2Int position,
            GroundType groundType,
            LevelObjectData groundData = null,
            TileData tile = null,
            LevelObjectData[] objects = null,
            TileEffectData[] effects = null)
        {
            this.position = position;
            this.groundType = groundType;
            this.groundData = groundData;
            this.tile = tile;
            this.objects = objects;
            this.effects = effects;
        }
    }
}
