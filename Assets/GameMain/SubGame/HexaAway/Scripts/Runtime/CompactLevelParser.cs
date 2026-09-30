using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Lokas
{
    public static class CompactLevelParser
    {
        private const int TileFamily = 10;
        private const int PlatformId = 2;
        private const int CollapseId = 210;
        private const int StopperId = 220;
        private const int ChangeDirectionFamily = 230;
        private const int WormHoleId = 240;
        private const int WoodBoxId = 310;
        private const int WallId = 510;
        private const int ReverseFamily = 520;
        private const int TripleReverseFamily = 530;
        private const int SquareReverseFamily = 540;
        private const int TripleSquareReverseFamily = 550;
        private const int SawId = 560;
        private const int BombId = 570;
        private const int AreaChangeDirectionId = 580;

        public static CompactLevelConfig ParseConfigText(string json)
        {
            return JsonConvert.DeserializeObject<CompactLevelConfig>(json);
        }

        public static RuntimeLevelData Parse(CompactLevelLib level)
        {
            if (level == null)
            {
                Debug.LogError("Compact level config is null.");
                return null;
            }

            Vector2Int size = ResolveSize(level);
            if (size.x <= 0 || size.y <= 0)
            {
                Debug.LogError($"Compact level {level.lib_id} has invalid size {size}.");
                return null;
            }

            LevelCellData[] cells = new LevelCellData[size.x * size.y];
            int wormHoleIndex = 0;

            for (int y = 0; y < size.y; y++)
            {
                int[] row = level.rows != null && y < level.rows.Length ? level.rows[y] : null;
                for (int x = 0; x < size.x; x++)
                {
                    int code = row != null && x < row.Length ? row[x] : 0;
                    cells[y * size.x + x] = ParseCell(level.lib_id, new Vector2Int(x, y), code, ref wormHoleIndex);
                }
            }

            return new RuntimeLevelData(level.lib_id, size, Mathf.Max(0, level.moves), level.difficulty, cells);
        }

        private static Vector2Int ResolveSize(CompactLevelLib level)
        {
            Vector2Int size = level.Size;
            if (size.x > 0 && size.y > 0)
            {
                return size;
            }

            int height = level.rows != null ? level.rows.Length : 0;
            int width = 0;
            if (level.rows != null)
            {
                for (int i = 0; i < level.rows.Length; i++)
                {
                    if (level.rows[i] != null && level.rows[i].Length > width)
                    {
                        width = level.rows[i].Length;
                    }
                }
            }

            return new Vector2Int(width, height);
        }

        private static LevelCellData ParseCell(int libId, Vector2Int position, int code, ref int wormHoleIndex)
        {
            if (code == 0)
            {
                return new LevelCellData(position, GroundType.None);
            }

            LevelObjectSourceData source = new(code);
            GroundType groundType = GroundType.Normal;
            LevelObjectData groundData = null;
            TileData tile = null;
            List<LevelObjectData> objects = null;
            List<TileEffectData> effects = null;

            if (TryParseTile(code, out HexaAwayDirection tileDirection))
            {
                tile = new TileData(true, tileDirection);
            }
            else if (TryParseTileWithWoodBox(code, out tileDirection))
            {
                tile = new TileData(true, tileDirection);
                effects = new List<TileEffectData> { new(TileEffectType.WoodBox, tileDirection) };
            }
            else
            {
                switch (code)
                {
                    case PlatformId:
                        break;
                    case CollapseId:
                        groundType = GroundType.Collapse;
                        groundData = new LevelObjectData(LevelObjectType.CollapseTrigger, source: source);
                        break;
                    case StopperId:
                        groundType = GroundType.Stopper;
                        groundData = new LevelObjectData(LevelObjectType.Stopper, source: source);
                        break;
                    case WormHoleId:
                        groundType = GroundType.WormHole;
                        int pairId = wormHoleIndex / 2 + 1;
                        wormHoleIndex++;
                        groundData = new LevelObjectData(LevelObjectType.WormHole, pairId: pairId, source: source);
                        break;
                    case WallId:
                        AddObject(ref objects, LevelObjectType.Wall, source: source);
                        break;
                    case SawId:
                        AddObject(ref objects, LevelObjectType.Saw, source: source);
                        break;
                    case BombId:
                        AddObject(ref objects, LevelObjectType.Bomb, source: source);
                        break;
                    case AreaChangeDirectionId:
                        AddObject(ref objects, LevelObjectType.AreaChangeDirection, source: source);
                        break;
                    default:
                        ParseFamilyObjectOrPlatform(libId, position, code, source, ref groundType, ref groundData, ref objects);
                        break;
                }
            }

            return new LevelCellData(
                position,
                groundType,
                groundData,
                tile,
                objects != null ? objects.ToArray() : null,
                effects != null ? effects.ToArray() : null);
        }

        private static void ParseFamilyObjectOrPlatform(
            int libId,
            Vector2Int position,
            int code,
            LevelObjectSourceData source,
            ref GroundType groundType,
            ref LevelObjectData groundData,
            ref List<LevelObjectData> objects)
        {
            int family = code / 10 * 10;
            int directionValue = code - family;

            if (family == ChangeDirectionFamily && TryParseDirection(directionValue, out HexaAwayDirection direction))
            {
                groundType = GroundType.ChangeDirection;
                groundData = new LevelObjectData(LevelObjectType.ChangeDirection, direction: direction, source: source);
                return;
            }

            if (family == ReverseFamily && directionValue >= 1 && directionValue <= 3 && TryParseDirection(directionValue, out direction))
            {
                AddObject(ref objects, LevelObjectType.Reverse, direction, source);
                return;
            }

            if (family == TripleReverseFamily && directionValue >= 1 && directionValue <= 2 && TryParseDirection(directionValue, out direction))
            {
                AddObject(ref objects, LevelObjectType.TripleReverse, direction, source);
                return;
            }

            if (family == SquareReverseFamily && directionValue >= 1 && directionValue <= 3 && TryParseDirection(directionValue, out direction))
            {
                AddObject(ref objects, LevelObjectType.SquareReverse, direction, source);
                return;
            }

            if (family == TripleSquareReverseFamily && directionValue >= 1 && directionValue <= 2 && TryParseDirection(directionValue, out direction))
            {
                AddObject(ref objects, LevelObjectType.TripleSquareReverse, direction, source);
                return;
            }

            Debug.LogWarning($"Compact level {libId} contains unknown cell code {code} at {position}. It will be treated as Platform.");
        }

        private static bool TryParseTile(int code, out HexaAwayDirection direction)
        {
            int directionValue = code - TileFamily;
            return TryParseDirection(directionValue, out direction);
        }

        private static bool TryParseTileWithWoodBox(int code, out HexaAwayDirection direction)
        {
            int tileCode = code / 10000;
            int effectCode = code % 10000;
            direction = HexaAwayDirection.Up;
            return effectCode == WoodBoxId && TryParseTile(tileCode, out direction);
        }

        private static bool TryParseDirection(int directionValue, out HexaAwayDirection direction)
        {
            if (directionValue >= 1 && directionValue <= 6)
            {
                return DirectionHelper.TryGetByCompactCode(TileFamily + directionValue, out direction);
            }

            direction = HexaAwayDirection.Up;
            return false;
        }

        private static void AddObject(
            ref List<LevelObjectData> objects,
            LevelObjectType type,
            HexaAwayDirection direction = HexaAwayDirection.Up,
            LevelObjectSourceData source = null)
        {
            objects ??= new List<LevelObjectData>();
            objects.Add(new LevelObjectData(type, direction: direction, source: source));
        }
    }
}
