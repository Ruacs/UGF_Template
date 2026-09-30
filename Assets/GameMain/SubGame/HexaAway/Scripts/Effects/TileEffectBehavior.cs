using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// Tile 附加效果行为基类。具体效果通过继承重写生命周期回调。
    /// </summary>
    public abstract class TileEffectBehavior : MonoBehaviour
    {
        // linkedTile/effectData 在 ApplyEffect 克隆实例后写入，Prefab 模板本身不应保存运行时状态。
        protected TileBehavior linkedTile;
        protected TileEffectData effectData;
        protected TileEffectType type;
        protected bool isActive;

        public TileEffectType Type => type;
        public bool IsActive => isActive;

        /// <summary>
        /// 效果实例创建并绑定到 Tile 后触发。
        /// </summary>
        public virtual void OnCreated(TileBehavior behavior) { }
        /// <summary>
        /// 效果被移除或 Tile 被销毁前触发。
        /// </summary>
        public virtual void OnDisabled(TileBehavior behavior) { }
        /// <summary>
        /// 当前 Tile 被收集时触发。
        /// </summary>
        public virtual void OnTileCollected() { }
        /// <summary>
        /// 任意 Tile 被收集时的全局回调预留。
        /// </summary>
        public virtual void OnTileCollectedGlobal(TileBehavior behavior) { }
        /// <summary>
        /// 任意 Tile 被收集时的全局回调，并携带本次收集位置。
        /// 默认保持旧回调行为，避免影响已有效果。
        /// </summary>
        public virtual void OnTileCollectedGlobal(TileBehavior behavior, Vector2Int collectionPosition)
        {
            OnTileCollectedGlobal(behavior);
        }
        /// <summary>
        /// 任意 Tile 开始移动时触发，用于链条、冰冻等效果更新状态。
        /// </summary>
        public virtual void OnTileMovementStarted(PathStatus pathStatus, TileBehavior behavior) { }
        /// <summary>
        /// 返回当前效果是否允许所属 Tile 被点击。
        /// </summary>
        public virtual bool IsClickable() { return true; }

        /// <summary>
        /// 从效果模板克隆运行时实例，并绑定到目标 Tile。
        /// </summary>
        public TileEffectBehavior ApplyEffect(TileBehavior behavior, TileEffectData data)
        {
            TileEffectBehavior effect = Instantiate(this, behavior.transform);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;
            effect.transform.localScale = Vector3.one;
            effect.linkedTile = behavior;
            effect.effectData = data;
            effect.type = data.Type;
            effect.isActive = true;
            behavior.ApplyEffect(effect);
            return effect;
        }

        /// <summary>
        /// 禁用效果实例，并通知所属 Tile 做后续清理。
        /// </summary>
        public void DisableEffect()
        {
            isActive = false;
            linkedTile?.OnEffectDisabled(this);
            gameObject.SetActive(false);
        }
    }
}
