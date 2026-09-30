using System.Runtime.CompilerServices;

namespace Lokas
{
    /// <summary>
    /// 简单二维引用网格，用于按棋盘坐标查询 Tile、平台和交互物。
    /// </summary>
    public sealed class SpatialGrid<T> where T : class
    {
        public readonly int Width;
        public readonly int Height;

        private readonly T[,] grid;

        /// <summary>
        /// 创建指定尺寸的二维网格。
        /// </summary>
        public SpatialGrid(int width, int height)
        {
            Width = width;
            Height = height;
            grid = new T[width, height];
        }

        /// <summary>
        /// 判断坐标是否位于网格范围内。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInside(int x, int y)
        {
            return (uint)x < (uint)Width && (uint)y < (uint)Height;
        }

        /// <summary>
        /// 获取坐标上的对象，越界或为空时返回 null。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int x, int y)
        {
            return IsInside(x, y) ? grid[x, y] : null;
        }

        /// <summary>
        /// 尝试获取坐标上的非空对象。
        /// </summary>
        public bool TryGet(int x, int y, out T value)
        {
            if (IsInside(x, y))
            {
                value = grid[x, y];
                return value != null;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// 判断坐标上是否存在对象。
        /// </summary>
        public bool Has(int x, int y)
        {
            return IsInside(x, y) && grid[x, y] != null;
        }

        /// <summary>
        /// 设置坐标上的对象，越界时返回 false。
        /// </summary>
        public bool Set(int x, int y, T value)
        {
            if (!IsInside(x, y))
            {
                return false;
            }

            grid[x, y] = value;
            return true;
        }

        /// <summary>
        /// 清空坐标上的对象引用，越界时返回 false。
        /// </summary>
        public bool Remove(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return false;
            }

            grid[x, y] = null;
            return true;
        }
    }
}
