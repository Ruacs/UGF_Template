using System;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 单个 Tile 上附加效果的关卡内参数。
    /// </summary>
    [Serializable]
    public sealed class TileEffectData
    {
        // 不同效果只会读取其中一部分参数，例如 Ice 读取回合数，Key 读取钥匙数。
        [SerializeField] private TileEffectType type;
        [SerializeField] private HexaAwayDirection direction;
        [SerializeField] private int iceTurnsAmount = 1;
        [SerializeField] private int keysAmount = 1;
        [SerializeField] private int targetMovesCount = 10;

        public TileEffectType Type => type;
        public HexaAwayDirection Direction => direction;
        public int IceTurnsAmount => iceTurnsAmount;
        public int KeysAmount => keysAmount;
        public int TargetMovesCount => targetMovesCount;

        public TileEffectData()
        {
        }

        public TileEffectData(
            TileEffectType type,
            HexaAwayDirection direction = HexaAwayDirection.Up,
            int iceTurnsAmount = 1,
            int keysAmount = 1,
            int targetMovesCount = 10)
        {
            this.type = type;
            this.direction = direction;
            this.iceTurnsAmount = iceTurnsAmount;
            this.keysAmount = keysAmount;
            this.targetMovesCount = targetMovesCount;
        }
    }
}
