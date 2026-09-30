using System.Runtime.CompilerServices;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// HexaAway 六边形棋盘方向工具，当前使用 flat-top、奇偶列偏移坐标�?    /// </summary>
    public static class DirectionHelper
    {
        /// <summary>
        /// 六个方向的基础描述。偶数列的斜向偏移会�?<see cref="GetOffset"/> 中修正�?        /// </summary>
        public static readonly DirectionData[] Directions =
        {
            new(HexaAwayDirection.Up,        11,   0f,   new Vector2Int(0, +1)),
            new(HexaAwayDirection.UpRight,   12,  60f,  new Vector2Int(+1, +1)),
            new(HexaAwayDirection.DownRight, 13, 120f, new Vector2Int(+1, 0)),
            new(HexaAwayDirection.Down,      14, 180f, new Vector2Int(0, -1)),
            new(HexaAwayDirection.DownLeft,  15, 240f, new Vector2Int(-1, 0)),
            new(HexaAwayDirection.UpLeft,    16, -60f,  new Vector2Int(-1, +1))
        };

        /// <summary>
        /// 获取方向描述�?        /// </summary>
        public static DirectionData Get(HexaAwayDirection direction)
        {
            return Directions[(int)direction];
        }

        public static bool TryGetByCompactCode(int compactCode, out HexaAwayDirection direction)
        {
            for (int i = 0; i < Directions.Length; i++)
            {
                if (Directions[i].CompactCode == compactCode)
                {
                    direction = Directions[i].Type;
                    return true;
                }
            }

            direction = HexaAwayDirection.Up;
            return false;
        }

        /// <summary>
        /// 获取相反方向
        /// </summary>
        public static HexaAwayDirection Opposite(HexaAwayDirection direction)
        {
            return (HexaAwayDirection)(((int)direction + 3) % 6);
        }

        /// <summary>
        /// 获取指定坐标周围六个相邻格�?        /// </summary>
        public static Vector2Int[] GetNeighbors(Vector2Int position)
        {
            Vector2Int[] neighbors = new Vector2Int[Directions.Length];
            for (int i = 0; i < neighbors.Length; i++)
            {
                neighbors[i] = Step(position, Directions[i].Type);
            }

            return neighbors;
        }

        /// <summary>
        /// 按列奇偶获取方向偏移。HexaAway 当前以列为偏移轴�?        /// </summary>
        public static Vector2Int GetOffset(HexaAwayDirection direction, int col)
        {
            Vector2Int offset = Directions[(int)direction].Offset;
            if (col % 2 == 0)
            {
                switch (direction)
                {
                    case HexaAwayDirection.UpRight:
                        offset = new Vector2Int(+1, 0);
                        break;
                    case HexaAwayDirection.UpLeft:
                        offset = new Vector2Int(-1, 0);
                        break;
                    case HexaAwayDirection.DownLeft:
                        offset = new Vector2Int(-1, -1);
                        break;
                    case HexaAwayDirection.DownRight:
                        offset = new Vector2Int(+1, -1);
                        break;
                }
            }

            return offset;
        }

        /// <summary>
        /// 根据相邻两个坐标推导方向，无法推导时返回默认方向�?        /// </summary>
        public static HexaAwayDirection GetDirection(Vector2Int firstPosition, Vector2Int secondPosition, HexaAwayDirection defaultDirection = HexaAwayDirection.Up)
        {
            if (firstPosition == secondPosition)
            {
                return defaultDirection;
            }

            Vector2Int delta = secondPosition - firstPosition;
            foreach (DirectionData directionData in Directions)
            {
                Vector2Int offset = GetOffset(directionData.Type, firstPosition.x);
                if (offset == delta)
                {
                    return directionData.Type;
                }
            }

            return defaultDirection;
        }

        /// <summary>
        /// 从指定坐标沿方向前进一步�?        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int Step(Vector2Int from, HexaAwayDirection direction)
        {
            return from + GetOffset(direction, from.x);
        }
    }
}
