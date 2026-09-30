using System;
using UnityEngine;

namespace Lokas
{
    [Serializable]
    public sealed class CompactLevelConfig
    {
        public int version = 1;
        public CompactLevelLib[] level_libs;

        public int Version => version;
        public CompactLevelLib[] LevelLibs => level_libs;
        public int AmountOfLevels => level_libs != null ? level_libs.Length : 0;

        public CompactLevelLib GetLevelByIndex(int index)
        {
            if (level_libs == null || index < 0 || index >= level_libs.Length)
            {
                return null;
            }

            return level_libs[index];
        }

        public CompactLevelLib GetLevelByLibId(int libId)
        {
            if (level_libs == null)
            {
                return null;
            }

            for (int i = 0; i < level_libs.Length; i++)
            {
                if (level_libs[i] != null && level_libs[i].lib_id == libId)
                {
                    return level_libs[i];
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class CompactLevelLib
    {
        public int lib_id;
        public CompactLevelSize size;
        public int moves;
        public int difficulty;
        public int[][] rows;

        public int LibId => lib_id;
        public Vector2Int Size => size != null ? new Vector2Int(size.x, size.y) : Vector2Int.zero;
        public int Moves => moves;
        public int Difficulty => difficulty;
        public int[][] Rows => rows;
    }

    [Serializable]
    public sealed class CompactLevelSize
    {
        public int x;
        public int y;
    }
}
