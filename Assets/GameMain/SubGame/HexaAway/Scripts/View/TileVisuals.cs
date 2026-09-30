using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// Tile 视觉表现基类，隔离逻辑 Tile 与具体渲染实现。
    /// </summary>
    [RequireComponent(typeof(TileBehavior))]
    public abstract class TileVisuals : MonoBehaviour
    {
        protected TileBehavior behavior;
        protected Material storedMaterial;
        protected bool isMaterialOverrided;

        /// <summary>
        /// 绑定所属 Tile，并调用子类初始化扩展点。
        /// </summary>
        public void Init(TileBehavior tileBehavior)
        {
            behavior = tileBehavior;
            OnInit();
        }

        /// <summary>
        /// 初始化扩展点。
        /// </summary>
        public virtual void OnInit() { }
        /// <summary>
        /// 设置方向箭头或等价提示物的显隐。
        /// </summary>
        public abstract void SetArrowState(bool state);

        /// <summary>
        /// 应用方向视觉配置。
        /// </summary>
        public abstract void ApplyTileVisual(TileVisualData visualData);
        /// <summary>
        /// 应用方向表现。
        /// </summary>
        public abstract void ApplyDirection(DirectionData directionData, bool instant);
        /// <summary>
        /// 获取当前默认材质。
        /// </summary>
        public abstract Material GetDefaultMaterial();
        /// <summary>
        /// 子类实现材质替换。
        /// </summary>
        protected abstract void OnOverrideMaterialApplied(Material material);

        /// <summary>
        /// 临时覆盖 Tile 材质，并缓存覆盖前材质。
        /// </summary>
        public void ApplyOverrideMaterial(Material material)
        {
            isMaterialOverrided = true;
            storedMaterial = GetDefaultMaterial();
            OnOverrideMaterialApplied(material);
        }

        /// <summary>
        /// 还原被覆盖前的材质。
        /// </summary>
        public void ResetOverridedMaterial()
        {
            if (!isMaterialOverrided)
            {
                return;
            }

            isMaterialOverrided = false;
            OnOverrideMaterialApplied(storedMaterial);
        }
    }
}
