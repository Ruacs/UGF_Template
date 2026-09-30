using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// HexaAway Tile 核心行为，负责点击、路径移动、收集和附加效果调度�?    /// </summary>
    public sealed class TileBehavior : MonoBehaviour
    {
        [SerializeField] private float movementDuration = 0.6f;
        [SerializeField] private float movementReverseDuration = 0.3f;
        [SerializeField] private float teleportAnimationDuration = 0.18f;

        private readonly List<TileEffectBehavior> effects = new();
        private LevelContext context;
        private TileVisualData visualData;
        private HexaAwayDirection direction;
        private DirectionData directionData;
        private TileVisuals visuals;
        private TileMovement movement;
        private Vector2Int matrixPosition;
        private Vector2Int movementStartPosition;
        private bool isCollected;
        private bool isMoving;
        private Coroutine movementCoroutine;
        private TileMotionTask activeTask;
        private PlatformBehavior reservedPlatform;
        private HexaAwayDirection savedDirection;
        private bool releasedOrigin;
        private LevelRuntime movementLevel;

        public TileVisualData VisualData => visualData;
        public List<TileEffectBehavior> Effects => effects;
        public bool IsCollected => isCollected;
        public bool IsMoving => isMoving;
        public Vector2Int MatrixPosition => matrixPosition;
        public Vector2Int MovementStartPosition => movementStartPosition;
        public TileVisuals Visuals => visuals;
        public HexaAwayDirection Direction => direction;

        public event Action Collected;

        /// <summary>
        /// 初始�?Tile 的运行时上下文、矩阵坐标、方向和依赖组件�?        /// </summary>
        public void Init(LevelContext runtimeContext, Vector2Int position, HexaAwayDirection initialDirection)
        {
            context = runtimeContext;
            matrixPosition = position;
            movementStartPosition = position;
            direction = initialDirection;
            directionData = DirectionHelper.Get(direction);
            isCollected = false;
            isMoving = false;
            effects.Clear();

            visuals = GetComponent<TileVisuals>();
            movement = GetComponent<TileMovement>();

            if (visuals == null || movement == null)
            {
                Debug.LogError("HexaAway tile requires visuals and movement components.", this);
                return;
            }

            visuals.Init(this);
            movement.Init(this);
            visuals.ApplyDirection(directionData, true);
        }

        /// <summary>
        /// 设置 Tile 视觉配置并刷新视觉表现。
        /// </summary>
        public void SetVisualData(TileVisualData data)
        {
            visualData = data;
            visuals?.ApplyTileVisual(data);
        }

        /// <summary>
        /// 覆盖 Tile 朝向，并同步刷新箭头/模型旋转�?        /// </summary>
        public void OverrideDirection(HexaAwayDirection newDirection, bool instant)
        {
            direction = newDirection;
            directionData = DirectionHelper.Get(direction);
            visuals?.ApplyDirection(directionData, instant);
        }

        /// <summary>
        /// 覆盖 Tile 的矩阵坐标�?        /// </summary>
        public void OverridePosition(Vector2Int position)
        {
            matrixPosition = position;
        }

        /// <summary>
        /// 处理 Tile 点击：检测路径、消耗步数并执行移动/回退/收集流程�?        /// </summary>
        public void OnObjectClicked()
        {
            if (isMoving || isCollected || movement == null || !CanBeClicked()) return;
            LevelRuntime level = context?.Manager?.LevelRepresentation;
            if (level == null || level.IsTileInputBlocked) return;
            var (status, path, _) = level.IsMovementAllowed(this, matrixPosition, direction);
            if (status == PathStatus.Busy) return;

            movementStartPosition = matrixPosition;
            savedDirection = direction;
            GimmickBehavior terminal = status == PathStatus.Stopped && path.Count > 0
                ? GetInteractable(path[path.Count - 1].Position) : null;
            movementLevel = level;
            releasedOrigin = status == PathStatus.Allowed || (terminal != null && terminal.CollectsTileOnArrival);
            TileMotionEnding ending = status == PathStatus.Allowed ? TileMotionEnding.Fall
                : status == PathStatus.Blocked ? TileMotionEnding.Return
                : terminal != null && terminal.ConsumesPieces ? TileMotionEnding.Shatter : TileMotionEnding.Stop;

            activeTask = new TileMotionTask(transform.position, path, level.GetPosition, ending,
                movementDuration, movementReverseDuration, teleportAnimationDuration,
                index =>
                {
                    TileMoveStep step = path[index];
                    RunGroundAnimation(step.Position);
                    if (status == PathStatus.Stopped && index == path.Count - 1 && terminal != null && terminal.TriggerOnFirstPiece)
                        terminal.OnTileStepped(this);
                    else if (status != PathStatus.Stopped || index != path.Count - 1)
                        OnStep(step.Position, step.Type);
                },
                point => { if (terminal != null) terminal.OnPieceArrived(this, point); });
            movement.CapturePose();
            isMoving = true;
            if (status == PathStatus.Stopped && path.Count > 0 && (terminal == null || !terminal.ConsumesPieces))
            {
                Vector2Int target = path[path.Count - 1].Position;
                reservedPlatform = level.PlatformsGrid.Get(target.x, target.y);
                reservedPlatform?.MarkAsBusy();
            }
            // A guaranteed collection frees the starting cell immediately; returns still reserve it.
            if (releasedOrigin && level.TilesGrid.Get(matrixPosition.x, matrixPosition.y) == this)
                level.TilesGrid.Remove(matrixPosition.x, matrixPosition.y);
            foreach (TileBehavior tile in level.Tiles.ToArray())
                tile?.OnTileMovementStartedGlobal(status, this);
            context.Manager.ConsumeMove();
            if (activeTask != null)
                movementCoroutine = StartCoroutine(RunMovement(activeTask, terminal));
        }

        private IEnumerator RunMovement(TileMotionTask task, GimmickBehavior terminal)
        {
            yield return null;
            yield return movement.Execute(task);
            if (task.IsCancelled || activeTask != task) yield break;

            LevelRuntime level = context.Manager.LevelRepresentation;
            activeTask = null;
            movementCoroutine = null;
            reservedPlatform?.ResetBusyState();
            reservedPlatform = null;
            if (task.Ending == TileMotionEnding.Return)
            {
                OverrideDirection(savedDirection, true);
            }
            else
            {
                if (level.TilesGrid.Get(matrixPosition.x, matrixPosition.y) == this)
                    level.TilesGrid.Remove(matrixPosition.x, matrixPosition.y);
                if (task.Steps.Length > 0) matrixPosition = task.Steps[task.Steps.Length - 1].Position;
                if (task.Ending == TileMotionEnding.Stop)
                {
                    level.TilesGrid.Set(matrixPosition.x, matrixPosition.y, this);
                    if (terminal != null)
                    {
                        terminal.OnTileStepped(this);
                        if (!isCollected) OverrideDirection(terminal.GetOverridedDirection(direction), true);
                    }
                }
                else
                {
                    OnTileCollected(movementStartPosition);
                    level.OnTileDestructed(this);
                }
            }

            isMoving = false;
            if (isCollected)
            {
                DisableEffects();
                Destroy(gameObject);
            }
            context.Manager.CheckCompleteStatus();
        }

        public void StopMovement()
        {
            bool wasActive = activeTask != null;
            activeTask?.Cancel();
            activeTask = null;
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = null;
            reservedPlatform?.ResetBusyState();
            reservedPlatform = null;
            if (wasActive && !isCollected)
            {
                if (releasedOrigin && movementLevel != null && context?.Manager?.LevelRepresentation == movementLevel)
                {
                    TileBehavior occupant = movementLevel.TilesGrid.Get(matrixPosition.x, matrixPosition.y);
                    PlatformBehavior originPlatform = movementLevel.PlatformsGrid.Get(matrixPosition.x, matrixPosition.y);
                    if ((occupant != null && occupant != this) || (originPlatform != null && originPlatform.IsBusy))
                    {
                        // The exit was committed and another tile now owns its old cell.
                        OnTileCollected(movementStartPosition);
                        DisableEffects();
                        movementLevel.OnTileDestructed(this);
                        Destroy(gameObject);
                        context.Manager.CheckCompleteStatus();
                        return;
                    }
                    movementLevel.TilesGrid.Set(matrixPosition.x, matrixPosition.y, this);
                }
                movement?.RestorePose();
                OverrideDirection(savedDirection, true);
            }
            isMoving = false;
        }

        private void OnDisable() => StopMovement();

        private GimmickBehavior GetInteractable(Vector2Int position)
        {
            LevelRuntime level = context.Manager.LevelRepresentation;
            if (level.InteractablesGrid.TryGet(position.x, position.y, out GimmickBehavior interactable))
                return interactable;
            if (level.PlatformsGrid.TryGet(position.x, position.y, out PlatformBehavior platform)
                && platform.IsInteractivePlatform) return platform;
            return null;
        }

        private void OnStep(Vector2Int stepPosition, TileMoveStepType stepType)
        {
            if (stepType == TileMoveStepType.TeleportExit) return;
            GimmickBehavior interactable = GetInteractable(stepPosition);
            if (interactable == null) return;
            interactable.OnTileStepped(this);
            if (activeTask == null) return;
            HexaAwayDirection next = interactable.GetOverridedDirection(direction);
            if (next != direction) OverrideDirection(next, false);
        }

        private void RunGroundAnimation(Vector2Int groundPosition)
        {
            LevelRuntime level = context.Manager.LevelRepresentation;
            if (visualData != null && level.PlatformsGrid.TryGet(groundPosition.x, groundPosition.y, out PlatformBehavior platform))
                platform.DoColorAnimation(visualData.FeedbackColor);
        }

        public bool CanBeClicked()
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].IsActive && !effects[i].IsClickable())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 广播任意 Tile 开始移动的事件给本 Tile 的所有激活效果�?        /// </summary>
        public void OnTileMovementStartedGlobal(PathStatus pathStatus, TileBehavior behavior)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].IsActive)
                {
                    effects[i].OnTileMovementStarted(pathStatus, behavior);
                }
            }
        }

        /// <summary>
        /// 标记当前 Tile 已被收集，并通知效果和外部监听者�?        /// </summary>
        public void OnTileCollected()
        {
            OnTileCollected(matrixPosition);
        }

        /// <summary>
        /// 标记当前 Tile 已被收集，并使用指定位置广播全局收集事件。
        /// </summary>
        public void OnTileCollected(Vector2Int collectionPosition)
        {
            if (isCollected)
            {
                return;
            }

            isCollected = true;
            StopMovement();
            Collected?.Invoke();

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].IsActive)
                {
                    effects[i].OnTileCollected();
                }
            }

            context?.Manager?.LevelRepresentation?.NotifyTileCollectedGlobal(this, collectionPosition);
        }

        /// <summary>
        /// 绑定一个运行时效果实例�?        /// </summary>
        public void ApplyEffect(TileEffectBehavior effect)
        {
            effects.Add(effect);
            effect.OnCreated(this);
        }

        /// <summary>
        /// 移除并销毁当�?Tile 上的所有效果实例�?        /// </summary>
        public void DisableEffects()
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                TileEffectBehavior effect = effects[i];
                if (effect != null)
                {
                    effect.OnDisabled(this);
                    Destroy(effect.gameObject);
                }
            }

            effects.Clear();
        }

        /// <summary>
        /// 单个效果禁用后的回调入口�?        /// </summary>
        public void OnEffectDisabled(TileEffectBehavior effect)
        {
            effect?.OnDisabled(this);
        }

        /// <summary>
        /// 返回其他 Tile 是否可以穿过当前 Tile。基础 Tile 默认阻挡�?        /// </summary>
        public bool TileCanGoThrough(TileBehavior tile)
        {
            return false;
        }
    }
}
