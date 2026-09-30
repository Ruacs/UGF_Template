using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 基础 MeshRenderer Tile 视觉实现，负责材质和方向旋转。
    /// </summary>
    public sealed class BasicTileVisuals : TileVisuals
    {
        [SerializeField] private MeshRenderer m_TileMR;
        [SerializeField] private MeshRenderer m_ArrowMR;
        [SerializeField] private Transform m_ArrowTransform;

        protected Material m_ArrowStoredMat;

        public override void OnInit()
        {
            ResolveMissingReferences();
        }

        /// <summary>
        /// 设置箭头显示状态。
        /// </summary>
        public override void SetArrowState(bool state)
        {
            if (m_ArrowTransform != null)
            {
                m_ArrowTransform.gameObject.SetActive(state);
            }
        }



        /// <summary>
        /// 应用 Tile 方向材质。
        /// </summary>
        public override void ApplyTileVisual(TileVisualData visualData)
        {
            if (m_TileMR == null || visualData == null)
            {
                return;
            }

            storedMaterial = visualData.Mat_Tile;
            m_TileMR.material = storedMaterial;


            if (m_ArrowMR == null)
            {
                return;
            }

            m_ArrowStoredMat = visualData.Mat_Arrow;
            m_ArrowMR.material = m_ArrowStoredMat;
        }

        /// <summary>
        /// 应用方向旋转。
        /// </summary>
        public override void ApplyDirection(DirectionData directionData, bool instant)
        {
            if (directionData != null)
            {
                transform.localRotation = Quaternion.Euler(0, directionData.Angle, 0);
            }
        }

        /// <summary>
        /// 获取当前默认材质。编辑器下使用 sharedMaterial，运行时使用 material。
        /// </summary>
        public override Material GetDefaultMaterial()
        {
            if (m_TileMR == null)
            {
                return null;
            }

            return Application.isPlaying ? m_TileMR.material : m_TileMR.sharedMaterial;
        }

        /// <summary>
        /// 应用临时覆盖材质。
        /// </summary>
        protected override void OnOverrideMaterialApplied(Material material)
        {
            if (m_TileMR != null && material != null)
            {
                m_TileMR.material = material;
            }
        }

        private void ResolveMissingReferences()
        {
            if (m_ArrowTransform == null)
            {
                Transform[] children = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != transform && children[i].name.ToLowerInvariant().Contains("arrow"))
                    {
                        m_ArrowTransform = children[i];
                        break;
                    }
                }
            }

            if (m_TileMR != null)
            {
                return;
            }

            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (m_ArrowTransform != null && renderers[i].transform == m_ArrowTransform)
                {
                    continue;
                }

                m_TileMR = renderers[i];
                return;
            }

            if (renderers.Length > 0)
            {
                m_TileMR = renderers[0];
            }
        }
    }
}
