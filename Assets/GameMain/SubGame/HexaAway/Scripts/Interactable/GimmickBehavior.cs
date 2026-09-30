using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 棋盘交互物行为基类，例如阻挡、旋转、停止或消除类格子
    /// </summary>
    public class GimmickBehavior : MonoBehaviour
    {
        // data 保存关卡中该交互物的参数；position 为当前矩阵坐标
        protected Vector2Int position;
        protected LevelObjectData data;
        protected LevelContext context;

        public Vector2Int Position => position;
        public LevelObjectData Data => data;
        public LevelObjectType Type => data != null ? data.Type : LevelObjectType.None;

        /// <summary>
        /// 初始化交互物运行时数据并触发创建回调
        /// </summary>
        public void Init(LevelContext runtimeContext, LevelObjectData objectData, Vector2Int matrixPosition)
        {
            context = runtimeContext;
            data = objectData;
            position = matrixPosition;
            OnCreated();
        }

        /// <summary>
        /// 覆盖矩阵坐标，用于交互物发生位移或重排的扩展场景
        /// </summary>
        public void OverridePosition(Vector2Int matrixPosition)
        {
            position = matrixPosition;
        }

        /// <summary>
        /// 创建完成回调
        /// </summary>
        public virtual void OnCreated() { }
        /// <summary>
        /// 关卡内全部交互物生成完成后触发，用于解析依赖其他交互物的数据。
        /// </summary>
        public virtual void OnLevelSpawned() { }
        /// <summary>
        /// 交互物被点击时触发
        /// </summary>
        public virtual void OnClicked() { }
        /// <summary>
        /// Tile 移动到该格时触发
        /// </summary>
        public virtual void OnTileStepped(TileBehavior tile) { }
        public virtual bool ConsumesPieces => false;
        public virtual bool CollectsTileOnArrival => ConsumesPieces;
        public virtual bool TriggerOnFirstPiece => false;
        public virtual bool BlocksTileInput => false;
        public virtual void OnPieceArrived(TileBehavior tile, Vector3 worldPosition) { }
        /// <summary>
        /// 返回 Tile 是否可以穿过该交互物
        /// </summary>
        public virtual bool TileCanGoThrough(TileBehavior tile) { return false; }
        /// <summary>
        /// 返回 Tile 是否应该停留在该交互物所在格
        /// </summary>
        public virtual bool ShouldStopMovementHere(TileBehavior tile) { return false; }
        /// <summary>
        /// 返回交互物修正后的移动方向，默认不改变方向
        /// </summary>
        public virtual HexaAwayDirection GetOverridedDirection(HexaAwayDirection direction) { return direction; }
        /// <summary>
        /// 返回 Tile 进入该交互物后是否会被传送到其他格子。
        /// </summary>
        public virtual bool TryGetTeleportExit(TileBehavior tile, out Vector2Int exitPosition)
        {
            exitPosition = default;
            return false;
        }
    }
}
