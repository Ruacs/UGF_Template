using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 棋盘平台格表现，负责坐标记录、点击转发和颜色反馈。
    /// </summary>
    public class PlatformBehavior : GimmickBehavior
    {
        [SerializeField] private MeshRenderer rendererRef;
        [SerializeField] private Transform m_PlatformCrosshair;
        [SerializeField] private Transform m_TileCrosshair;

        private bool isBusy;
        private Color defaultColor = Color.white;
        private GroundType groundType = GroundType.Normal;

        public bool IsBusy => isBusy;
        public GroundType GroundType => groundType;
        public virtual bool IsInteractivePlatform => false;

        /// <summary>
        /// 记录平台默认颜色，供颜色反馈动画还原。
        /// </summary>
        private void Awake()
        {
            ResolveMissingReferences();

            if (rendererRef != null)
            {
                defaultColor = rendererRef.material.color;
            }

            SetPlatformCrosshairState(false);
            SetTileCrosshairState(false);
        }

        /// <summary>
        /// Drill/TNT 选择目标时显示在普通 Platform 上的准星提示。
        /// </summary>
        public void SetPlatformCrosshairState(bool state)
        {
            if (m_PlatformCrosshair != null)
            {
                m_PlatformCrosshair.gameObject.SetActive(state);
            }
        }

        /// <summary>
        /// Hammer 选择目标时显示在 Tile 所在 Platform 上的准星提示。
        /// </summary>
        public void SetTileCrosshairState(bool state)
        {
            if (m_TileCrosshair != null)
            {
                m_TileCrosshair.gameObject.SetActive(state);
            }
        }

        private void ResolveMissingReferences()
        {
            if (m_PlatformCrosshair != null && m_TileCrosshair != null)
            {
                return;
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == transform)
                {
                    continue;
                }

                string childName = children[i].name.ToLowerInvariant();
                if (m_PlatformCrosshair == null && childName.Contains("platform") && childName.Contains("crosshair"))
                {
                    m_PlatformCrosshair = children[i];
                }

                if (m_TileCrosshair == null && childName.Contains("tile") && childName.Contains("crosshair"))
                {
                    m_TileCrosshair = children[i];
                }
            }
        }

        /// <summary>
        /// 初始化平台所在关卡上下文和矩阵坐标。
        /// </summary>
        public void Init(LevelContext runtimeContext, Vector2Int matrixPosition)
        {
            Init(runtimeContext, null, matrixPosition, GroundType.Normal);
        }

        public void Init(LevelContext runtimeContext, LevelObjectData platformData, Vector2Int matrixPosition, GroundType groundType = GroundType.Normal)
        {
            this.groundType = groundType;
            base.Init(runtimeContext, platformData, matrixPosition);
        }

        /// <summary>
        /// 普通平台默认允许 Tile 通过；具体平台类型可覆盖规则。
        /// </summary>
        public override bool TileCanGoThrough(TileBehavior tile) => true;

        /// <summary>
        /// 播放平台颜色反馈动画。
        /// </summary>
        public void DoColorAnimation(Color color)
        {
            if (rendererRef == null)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(ColorRoutine(color));
        }

        /// <summary>
        /// 标记平台被移动中的 Tile 占用，路径检测会返回 Busy。
        /// </summary>
        public void MarkAsBusy()
        {
            isBusy = true;
        }

        /// <summary>
        /// 清理平台占用状态。
        /// </summary>
        public void ResetBusyState()
        {
            isBusy = false;
        }

        /// <summary>
        /// 将平台颜色从指定颜色渐变回默认颜色。
        /// </summary>
        private System.Collections.IEnumerator ColorRoutine(Color color)
        {
            rendererRef.material.color = color;
            const float duration = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rendererRef.material.color = Color.Lerp(color, defaultColor, t);
                yield return null;
            }

            rendererRef.material.color = defaultColor;
        }
    }
}
