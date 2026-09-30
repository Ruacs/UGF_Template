using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public enum TileMoveStepType
    {
        Move = 0,
        TeleportEnter = 1,
        TeleportExit = 2
    }

    public readonly struct TileMoveStep
    {
        public TileMoveStep(Vector2Int position, TileMoveStepType type = TileMoveStepType.Move)
        {
            Position = position;
            Type = type;
        }

        public Vector2Int Position { get; }
        public TileMoveStepType Type { get; }
    }

    /// <summary>
    /// 单个关卡的运行时表现层，负责根据 SO 生成棋盘对象并维护坐标索引
    /// </summary>
    public sealed class LevelRuntime
    {
        // 当前使用 flat-top 六边形布局，Prefab 尺寸应与半径配置保持一致
        public const float HexRadius = 0.52f;
        public const float TileYPosition = 0.0f;
        public const float PlatformYPosition = -0.25f;
        private static readonly Vector3 DefaultPlatformColliderSize = new(0.95f, 0.6f, 0.85f);
        private static readonly Vector3 DefaultPlatformColliderCenter = new(0f, 0.3f, 0f);

        private readonly LevelContext context;
        private float minZ = int.MaxValue;
        private float maxZ = int.MinValue;
        private float minX = int.MaxValue;
        private float maxX = int.MinValue;

        public RuntimeLevelData LevelData { get; }
        public Vector2Int Size => LevelData.Size;
        public Transform LevelTransform { get; private set; }
        public SpatialGrid<TileBehavior> TilesGrid { get; private set; }
        public SpatialGrid<GimmickBehavior> InteractablesGrid { get; private set; }
        public SpatialGrid<PlatformBehavior> PlatformsGrid { get; private set; }
        public List<PlatformBehavior> Platforms { get; private set; }
        public List<TileBehavior> Tiles { get; private set; }
        public List<GimmickBehavior> Interactables { get; private set; }
        public bool LevelCleared => Tiles != null && Tiles.Count == 0;
        public Bounds LevelBounds => GetBounds();

        /// <summary>
        /// 创建关卡表现根节点并保存运行上下文
        /// </summary>
        public LevelRuntime(LevelContext runtimeContext, RuntimeLevelData level)
        {
            context = runtimeContext;
            LevelData = level;
            GameObject levelObject = new("[HexaAway Level]");
            LevelTransform = levelObject.transform;
        }

        /// <summary>
        /// 根据关卡配置实例化平台、Tile 和交互物
        /// </summary>
        public void SpawnLevel()
        {
            Vector2Int size = LevelData.Size;
            TilesGrid = new SpatialGrid<TileBehavior>(size.x, size.y);
            InteractablesGrid = new SpatialGrid<GimmickBehavior>(size.x, size.y);
            PlatformsGrid = new SpatialGrid<PlatformBehavior>(size.x, size.y);
            Interactables = new List<GimmickBehavior>();
            Tiles = new List<TileBehavior>();
            Platforms = new List<PlatformBehavior>();

            List<Vector3> positions = GenerateFlatTopPositions(size.x, size.y, HexRadius);
            LevelCellData[] cells = LevelData.Cells;

            for (int row = 0; row < size.y; row++)
            {
                for (int col = 0; col < size.x; col++)
                {
                    int index = LevelData.GetIndex(row, col);
                    LevelCellData cell = cells != null && index < cells.Length ? cells[index] : null;
                    if (cell == null || cell.GroundType == GroundType.None)
                    {
                        continue;
                    }

                    Vector3 local = positions[index];
                    Vector3 world = LevelTransform.TransformPoint(new Vector3(local.x, 0, local.z));
                    Vector2Int matrixPosition = new(col, row);
                    UpdateBounds(world);
                    SpawnPlatform(matrixPosition, world + new Vector3(0, PlatformYPosition, 0), cell.GroundType, cell.GroundData);

                    if (cell.Tile != null && cell.Tile.Exists)
                    {
                        SpawnTile(matrixPosition, world + new Vector3(0, TileYPosition, 0), cell.Tile.HexaAwayDirection, cell.Tile.VisualGroupId, cell.Effects);
                    }

                    SpawnLevelObjects(matrixPosition, world + new Vector3(0, TileYPosition, 0), cell.Objects);
                }
            }

            for (int i = 0; i < Interactables.Count; i++)
            {
                Interactables[i]?.OnLevelSpawned();
            }

            for (int i = 0; i < Platforms.Count; i++)
            {
                Platforms[i]?.OnLevelSpawned();
            }
        }

        private void SpawnLevelObjects(Vector2Int matrixPosition, Vector3 position, IEnumerable<LevelObjectData> objects)
        {
            if (objects == null)
            {
                return;
            }

            foreach (LevelObjectData objectData in objects)
            {
                if (objectData == null || objectData.Type == LevelObjectType.None || objectData.Layer != LevelObjectLayer.Board)
                {
                    continue;
                }

                SpawnLevelObject(matrixPosition, position, objectData);
            }
        }

        public void SpawnLevelObject(Vector2Int matrixPosition, Vector3 position, LevelObjectData data)
        {
            if (data == null || data.Type == LevelObjectType.None)
            {
                return;
            }

            if (!context.TryGetInteractableObjectPrefab(data.Type, out GameObject prefab) || prefab == null)
            {
                return;
            }

            bool isPlatformObject = prefab.GetComponent<PlatformBehavior>() != null;
            Vector3 spawnPosition = position;
            if (isPlatformObject)
            {
                spawnPosition.y += PlatformYPosition - TileYPosition;
            }

            GameObject interactableObject = Object.Instantiate(prefab, spawnPosition, Quaternion.identity, LevelTransform);
            EnsurePlatformCollider(interactableObject, isPlatformObject);
            GimmickBehavior interactable = interactableObject.GetComponent<GimmickBehavior>();
            if (interactable == null)
            {
                Debug.LogError("HexaAway interactable prefab requires GimmickBehavior.", interactableObject);
                return;
            }

            interactable.Init(context, data, matrixPosition);

            if (interactable is PlatformBehavior platformBehavior)
            {
                PlatformBehavior previousPlatform = PlatformsGrid.Get(matrixPosition.x, matrixPosition.y);
                if (previousPlatform != null && previousPlatform != platformBehavior)
                {
                    Platforms.Remove(previousPlatform);
                    Object.Destroy(previousPlatform.gameObject);
                }

                Platforms.Add(platformBehavior);
                PlatformsGrid.Set(matrixPosition.x, matrixPosition.y, platformBehavior);
            }
            else
            {
                Interactables.Add(interactable);
                InteractablesGrid.Set(matrixPosition.x, matrixPosition.y, interactable);
            }
        }

        /// <summary>
        /// 移除平台型机关及其坐标索引。
        /// </summary>
        public void RemovePlatform(PlatformBehavior platform)
        {
            if (platform == null)
            {
                return;
            }

            Vector2Int position = platform.Position;
            PlatformsGrid?.Remove(position.x, position.y);
            Platforms?.Remove(platform);
            Object.Destroy(platform.gameObject);
        }

        /// <summary>
        /// 根据当前选择的道具刷新棋盘上的可点击目标准星。
        /// </summary>
        public void RefreshPropTargetCrosshairs(HexaAwayPropType? propType)
        {
            ClearPropTargetCrosshairs();

            if (!propType.HasValue)
            {
                return;
            }

            switch (propType.Value)
            {
                case HexaAwayPropType.Hammer:
                    ShowTileCrosshairs();
                    break;
                case HexaAwayPropType.Drill:
                    ShowNormalPlatformCrosshairs();
                    break;
                case HexaAwayPropType.Tnt:
                    ShowTntPlatformCrosshairs();
                    break;
            }
        }

        /// <summary>
        /// 清理所有道具目标准星，避免取消选择、关闭面板或关卡销毁时残留。
        /// </summary>
        public void ClearPropTargetCrosshairs()
        {
            if (Platforms != null)
            {
                for (int i = 0; i < Platforms.Count; i++)
                {
                    Platforms[i]?.SetPlatformCrosshairState(false);
                    Platforms[i]?.SetTileCrosshairState(false);
                }
            }
        }

        private void ShowTileCrosshairs()
        {
            if (Tiles == null)
            {
                return;
            }

            for (int i = 0; i < Tiles.Count; i++)
            {
                TileBehavior tile = Tiles[i];
                if (tile != null && !tile.IsCollected)
                {
                    Vector2Int position = tile.MatrixPosition;
                    if (PlatformsGrid != null && PlatformsGrid.TryGet(position.x, position.y, out PlatformBehavior platform))
                    {
                        platform.SetTileCrosshairState(true);
                    }
                }
            }
        }

        private void ShowNormalPlatformCrosshairs()
        {
            if (Platforms == null)
            {
                return;
            }

            for (int i = 0; i < Platforms.Count; i++)
            {
                PlatformBehavior platform = Platforms[i];
                if (IsNormalPlatformPropTarget(platform))
                {
                    platform.SetPlatformCrosshairState(true);
                }
            }
        }

        private void ShowTntPlatformCrosshairs()
        {
            if (Platforms == null)
            {
                return;
            }

            for (int i = 0; i < Platforms.Count; i++)
            {
                PlatformBehavior platform = Platforms[i];
                if (IsTntPlatformPropTarget(platform))
                {
                    platform.SetPlatformCrosshairState(true);
                }
            }
        }

        /// <summary>
        /// 检查当前关卡是否存在 Drill 可以选择的空普通平台。
        /// </summary>
        public bool HasNormalPlatformPropTarget()
        {
            if (Platforms == null)
            {
                return false;
            }

            for (int i = 0; i < Platforms.Count; i++)
            {
                if (IsNormalPlatformPropTarget(Platforms[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查当前关卡是否存在 TNT 可以选择的平台目标。
        /// </summary>
        public bool HasTntPlatformPropTarget()
        {
            if (Platforms == null)
            {
                return false;
            }

            for (int i = 0; i < Platforms.Count; i++)
            {
                if (IsTntPlatformPropTarget(Platforms[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 普通平台目标必须没有 Tile、没有独立机关，也不能是会改变移动规则的交互平台。
        /// </summary>
        public bool IsNormalPlatformPropTarget(PlatformBehavior platform)
        {
            if (platform == null || platform.IsInteractivePlatform)
            {
                return false;
            }

            Vector2Int position = platform.Position;
            return (TilesGrid == null || !TilesGrid.Has(position.x, position.y))
                && (InteractablesGrid == null || !InteractablesGrid.Has(position.x, position.y));
        }

        /// <summary>
        /// TNT 可放在空普通平台，也可放在空 Stopper 平台。
        /// </summary>
        public bool IsTntPlatformPropTarget(PlatformBehavior platform)
        {
            if (platform == null)
            {
                return false;
            }

            if (IsNormalPlatformPropTarget(platform))
            {
                return true;
            }

            Vector2Int position = platform.Position;
            return platform.GroundType == GroundType.Stopper
                && (TilesGrid == null || !TilesGrid.Has(position.x, position.y))
                && (InteractablesGrid == null || !InteractablesGrid.Has(position.x, position.y));
        }

        /// <summary>
        /// 将矩阵坐标转换为关卡根节点下的世界坐标
        /// </summary>
        public Vector3 GetPosition(Vector2Int position)
        {
            float horizStep = 1.5f * HexRadius;
            float vertStep = Mathf.Sqrt(3f) * HexRadius;
            float x = position.x * horizStep;
            float z = vertStep * (position.y + ((position.x & 1) == 1 ? 0.5f : 0f));
            return LevelTransform.TransformPoint(new Vector3(x, TileYPosition, z));
        }

        /// <summary>
        /// 生成一个平台格并写入平台网格
        /// </summary>
        public void SpawnPlatform(Vector2Int matrixPosition, Vector3 worldPosition, GroundType groundType = GroundType.Normal, LevelObjectData groundData = null)
        {
            GameObject prefab = GetPlatformPrefab(groundType);
            if (prefab == null)
            {
                Debug.LogError("HexaAway platform prefab is not configured.");
                return;
            }

            GameObject platformObject = Object.Instantiate(prefab, worldPosition, Quaternion.identity, LevelTransform);
            EnsurePlatformCollider(platformObject, true);
            PlatformBehavior platform = platformObject.GetComponent<PlatformBehavior>();
            if (platform == null)
            {
                Debug.LogError("HexaAway platform prefab requires PlatformBehavior.", platformObject);
                return;
            }

            platform.Init(context, groundData, matrixPosition, groundType);
            Platforms.Add(platform);
            PlatformsGrid.Set(matrixPosition.x, matrixPosition.y, platform);
        }

        private GameObject GetPlatformPrefab(GroundType groundType)
        {
            return context.GetPlatformPrefab(groundType);
        }

        /// <summary>
        /// 生成一个 Tile，并绑定方向视觉和附加效果。
        /// </summary>
        public TileBehavior SpawnTile(Vector2Int matrixPosition, Vector3 position, HexaAwayDirection direction, int visualGroupId, IEnumerable<TileEffectData> effects)
        {
            GameObject prefab = context.TileSkin != null ? context.TileSkin.TileGamePrefab : null;
            if (prefab == null)
            {
                Debug.LogError("HexaAway tile prefab is not configured.");
                return null;
            }

            GameObject tileObject = Object.Instantiate(prefab, position, Quaternion.identity, LevelTransform);
            TileBehavior tile = EnsureTileRuntimeComponents(tileObject);

            if (tile == null) { Object.Destroy(tileObject); return null; }
            tile.Init(context, matrixPosition, direction);
            tile.SetVisualData(context.GetTileVisualData(direction, visualGroupId));

            if (effects != null)
            {
                foreach (TileEffectData effect in effects)
                {
                    if (effect == null || effect.Type == TileEffectType.None)
                    {
                        continue;
                    }

                    TileEffectBehavior effectBehavior = context.GetEffectBehavior(effect.Type);
                    if (effectBehavior != null)
                    {
                        effectBehavior.ApplyEffect(tile, effect);
                    }
                }
            }

            Tiles.Add(tile);
            TilesGrid.Set(matrixPosition.x, matrixPosition.y, tile);
            return tile;
        }

        /// <summary>
        /// 平台点击依赖 3D Collider；显式引用 BoxCollider 可避免 Android Engine Code Stripping 裁掉 AssetBundle 中的平台碰撞体。
        /// </summary>
        private static void EnsurePlatformCollider(GameObject platformObject, bool isPlatformObject)
        {
            if (!isPlatformObject || platformObject == null || platformObject.GetComponentInChildren<Collider>(true) != null)
            {
                return;
            }

            BoxCollider collider = platformObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = DefaultPlatformColliderSize;
            collider.center = DefaultPlatformColliderCenter;
            Debug.LogWarning("HexaAway platform prefab has no Collider. A runtime BoxCollider was added for input raycast.", platformObject);
        }

        public bool HasMovingTiles => Tiles != null && Tiles.Exists(tile => tile != null && tile.IsMoving);
        public bool IsTileInputBlocked =>
            (Interactables != null && Interactables.Exists(item => item != null && item.BlocksTileInput)) ||
            (Platforms != null && Platforms.Exists(item => item != null && item.BlocksTileInput));

        private static TileBehavior EnsureTileRuntimeComponents(GameObject tileObject)
        {
            TileBehavior tile = tileObject.GetComponent<TileBehavior>();
            if (tile == null || tileObject.GetComponent<TileVisuals>() == null || tileObject.GetComponent<TileMovement>() == null)
            {
                Debug.LogError("Tile prefab must declare TileBehavior, TileVisuals and TileMovement.", tileObject);
                return null;
            }
            return tile;
        }

        /// <summary>
        /// Tile 被收集销毁后，从运行时列表和网格中移除引用
        /// </summary>
        public void OnTileDestructed(TileBehavior tile)
        {
            Vector2Int position = tile.MatrixPosition;
            if (TilesGrid.Get(position.x, position.y) == tile) TilesGrid.Remove(position.x, position.y);
            Tiles.Remove(tile);
        }

        /// <summary>
        /// 广播 Tile 收集事件给当前关卡内的所有 Tile 效果。
        /// </summary>
        public void NotifyTileCollectedGlobal(TileBehavior collectedTile)
        {
            if (collectedTile == null)
            {
                return;
            }

            NotifyTileCollectedGlobal(collectedTile, collectedTile.MatrixPosition);
        }

        /// <summary>
        /// 广播 Tile 收集事件，并携带实际触发收集的矩阵位置。
        /// </summary>
        public void NotifyTileCollectedGlobal(TileBehavior collectedTile, Vector2Int collectionPosition)
        {
            if (collectedTile == null || Tiles == null)
            {
                return;
            }

            for (int i = 0; i < Tiles.Count; i++)
            {
                TileBehavior tile = Tiles[i];
                if (tile == null || tile.Effects == null)
                {
                    continue;
                }

                for (int j = 0; j < tile.Effects.Count; j++)
                {
                    TileEffectBehavior effect = tile.Effects[j];
                    if (effect != null && effect.IsActive)
                    {
                        effect.OnTileCollectedGlobal(collectedTile, collectionPosition);
                    }
                }
            }
        }

        /// <summary>
        /// 计算 Tile 沿指定方向的可移动路径
        /// </summary>
        /// <returns>
        /// 路径状态、路径坐标列表，以及移动过程中方向是否被交互物改写
        /// returns>
        public (PathStatus, List<TileMoveStep>, bool) IsMovementAllowed(TileBehavior tile, Vector2Int position, HexaAwayDirection direction)
        {
            List<TileMoveStep> path = new();
            Vector2Int size = Size;
            Vector2Int startPosition = position;
            Vector2Int currentPosition = startPosition;
            bool directionChanged = false;

            for (int steps = 0; steps < 100; steps++)
            {
                Vector2Int nextPoint = DirectionHelper.Step(currentPosition, direction);
                if (nextPoint.x < 0 || nextPoint.x >= size.x || nextPoint.y < 0 || nextPoint.y >= size.y || !PlatformsGrid.Has(nextPoint.x, nextPoint.y))
                {
                    path.Add(new TileMoveStep(nextPoint));
                    return (PathStatus.Allowed, path, directionChanged);
                }

                PlatformBehavior platform = PlatformsGrid.Get(nextPoint.x, nextPoint.y);
                if (platform != null && platform.IsBusy)
                {
                    return (PathStatus.Busy, path, directionChanged);
                }

                if (nextPoint != startPosition && TilesGrid.TryGet(nextPoint.x, nextPoint.y, out TileBehavior nextTile))
                {
                    if (nextTile.IsMoving) return (PathStatus.Busy, path, directionChanged);
                    if (!nextTile.TileCanGoThrough(tile))
                    {
                        break;
                    }
                }

                // Board gimmicks take precedence when sharing a cell with an interactive platform.
                // Reverse can move gimmicks (for example Saw) onto platform gimmicks (for example Stopper).
                InteractablesGrid.TryGet(nextPoint.x, nextPoint.y, out GimmickBehavior interactable);
                if (interactable == null
                    && PlatformsGrid.TryGet(nextPoint.x, nextPoint.y, out PlatformBehavior platform2)
                    && platform2.IsInteractivePlatform)
                {
                    interactable = platform2;
                }

                if (interactable != null)
                {
                    if (!interactable.TileCanGoThrough(tile))
                    {
                        break;
                    }

                    if (interactable.ShouldStopMovementHere(tile))
                    {
                        path.Add(new TileMoveStep(nextPoint));
                        return (PathStatus.Stopped, path, directionChanged);
                    }

                    if (interactable.TryGetTeleportExit(tile, out Vector2Int exitPoint))
                    {
                        if (!CanTeleportTo(tile, exitPoint, startPosition))
                        {
                            break;
                        }

                        path.Add(new TileMoveStep(nextPoint, TileMoveStepType.TeleportEnter));
                        path.Add(new TileMoveStep(exitPoint, TileMoveStepType.TeleportExit));
                        currentPosition = exitPoint;
                        continue;
                    }

                    HexaAwayDirection overridedDirection = interactable.GetOverridedDirection(direction);
                    if (direction != overridedDirection)
                    {
                        direction = overridedDirection;
                        directionChanged = true;
                    }
                }

                path.Add(new TileMoveStep(nextPoint));
                currentPosition = nextPoint;
            }

            return (PathStatus.Blocked, path, directionChanged);
        }

        private bool CanTeleportTo(TileBehavior tile, Vector2Int exitPoint, Vector2Int startPosition)
        {
            Vector2Int size = Size;
            if (exitPoint.x < 0 || exitPoint.x >= size.x || exitPoint.y < 0 || exitPoint.y >= size.y || !PlatformsGrid.Has(exitPoint.x, exitPoint.y))
            {
                return false;
            }

            PlatformBehavior platform = PlatformsGrid.Get(exitPoint.x, exitPoint.y);
            if (platform != null && platform.IsBusy)
            {
                return false;
            }

            if (exitPoint != startPosition && TilesGrid.TryGet(exitPoint.x, exitPoint.y, out TileBehavior exitTile))
            {
                return exitTile.TileCanGoThrough(tile);
            }

            return true;
        }

        /// <summary>
        /// 销毁关卡根节点，释放本关实例化对象
        /// </summary>
        public void Dispose()
        {
            if (LevelTransform != null)
            {
                Object.Destroy(LevelTransform.gameObject);
                LevelTransform = null;
            }
        }

        /// <summary>
        /// 生成 flat-top 六边形网格的本地坐标列表
        /// </summary>
        public static List<Vector3> GenerateFlatTopPositions(int width, int height, float radius)
        {
            List<Vector3> list = new(width * height);
            float horizStep = 1.5f * radius;
            float vertStep = Mathf.Sqrt(3f) * radius;

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    float x = col * horizStep;
                    float z = vertStep * (row + ((col & 1) == 1 ? 0.5f : 0f));
                    list.Add(new Vector3(x, 0f, z));
                }
            }

            return list;
        }

        /// <summary>
        /// 扩展关卡包围盒边界
        /// </summary>
        private void UpdateBounds(Vector3 position)
        {
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minZ = Mathf.Min(minZ, position.z);
            maxZ = Mathf.Max(maxZ, position.z);
        }

        /// <summary>
        /// 获取当前关卡的世界包围盒
        /// </summary>
        private Bounds GetBounds()
        {
            if (Platforms == null || Platforms.Count == 0)
            {
                return new Bounds(LevelTransform != null ? LevelTransform.position : Vector3.zero, Vector3.zero);
            }

            Bounds bounds = new();
            bounds.center = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
            bounds.size = new Vector3(maxX - minX, 0f, maxZ - minZ);
            return bounds;
        }
    }
}
