using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 六边形方向的运行时描述，包含朝向、显示角度和基础偏移 
    /// </summary>
    public sealed class DirectionData
    {
        public HexaAwayDirection Type { get; }
        /// <summary>Compact level 中的方向编码，范围为 11~16。</summary>
        public int CompactCode { get; }
        public float Angle { get; }
        public Vector2Int Offset { get; }

        /// <summary>
        /// 创建一个方向描述
        /// </summary>
        public DirectionData(HexaAwayDirection type, int compactCode, float angle, Vector2Int offset)
        {
            Type = type;
            CompactCode = compactCode;
            Angle = angle;
            Offset = offset;
        }
    }
}
