using UnityEngine;

namespace Lokas
{
    public sealed class RuntimeLevelData
    {
        private readonly LevelCellData[] cells;

        public RuntimeLevelData(int libId, Vector2Int size, int moves, int difficulty, LevelCellData[] cells)
        {
            LibId = libId;
            Size = size;
            Moves = moves;
            Difficulty = difficulty;
            this.cells = cells;
        }

        public int LibId { get; }
        public Vector2Int Size { get; }
        public int Moves { get; }
        public int Difficulty { get; }
        public LevelCellData[] Cells => cells;

        public int GetIndex(int row, int col)
        {
            return row * Size.x + col;
        }
    }
}
