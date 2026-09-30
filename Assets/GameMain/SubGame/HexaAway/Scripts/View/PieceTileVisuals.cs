using UnityEngine;

namespace Lokas
{
    public sealed class PieceTileVisuals : TileVisuals
    {
        [SerializeField] private Transform m_ArrowTransform;
        [SerializeField] private MeshRenderer m_ArrowRenderer;
        [SerializeField] private MeshRenderer[] bodies;
        private bool arrowVisible = true;
        private bool moving;

        public override void OnInit()
        {
            if (m_ArrowTransform == null) m_ArrowTransform = transform.Find("Piece/arrow");
            if (m_ArrowTransform != null)
            {
                m_ArrowRenderer = m_ArrowTransform.GetComponent<MeshRenderer>();
                // The arrow stays upright even when the old top piece becomes the bottom.
                m_ArrowTransform.SetParent(transform, true);
            }

            if (bodies == null || bodies.Length == 0)
            {
                var renderers = new System.Collections.Generic.List<MeshRenderer>();
                foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer != m_ArrowRenderer) renderers.Add(renderer);
                bodies = renderers.ToArray();
            }

        }

        public override void SetArrowState(bool state)
        {
            arrowVisible = state;
            RefreshArrow();
        }

        public void SetMoving(bool state)
        {
            moving = state;
            RefreshArrow();
        }

        private void RefreshArrow()
        {
            if (m_ArrowTransform != null) m_ArrowTransform.gameObject.SetActive(arrowVisible && !moving);
        }

        public override void ApplyTileVisual(TileVisualData data)
        {
            if (data == null) return;
            storedMaterial = data.Mat_Tile;
            if (!isMaterialOverrided) OnOverrideMaterialApplied(storedMaterial);
            if (m_ArrowRenderer != null) m_ArrowRenderer.sharedMaterial = data.Mat_Arrow;
        }

        public override void ApplyDirection(DirectionData data, bool instant)
        {
            if (data != null && m_ArrowTransform != null)
                m_ArrowTransform.localRotation = Quaternion.Euler(0f, data.Angle, 0f);
        }

        public override Material GetDefaultMaterial()
        {
            return bodies != null && bodies.Length > 0 ? bodies[0].sharedMaterial : null;
        }

        protected override void OnOverrideMaterialApplied(Material material)
        {
            if (bodies == null) return;
            foreach (MeshRenderer renderer in bodies)
                if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
