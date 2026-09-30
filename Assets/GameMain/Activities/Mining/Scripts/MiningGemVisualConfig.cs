using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    /// <summary>
    /// Mining 共用的格子与宝石图片、占格尺寸配置。
    /// </summary>
    [CreateAssetMenu(fileName = "MiningGemVisualConfig", menuName = "GF/Activity/Mining/Gem Visual Config")]
    public sealed class MiningGemVisualConfig : ScriptableObject
    {
        [Header("Cell sprites")]
        [SerializeField] private Sprite m_ClosedCellSprite;
        [SerializeField] private Sprite m_CrackCellSprite;
        [SerializeField] private Sprite m_ClashCellSprite;

        [Header("Gem visual catalog")]
        [SerializeField] private MiningGemVisualDefinition[] m_GemVisuals = CreateDefaultGemVisuals();

        public Sprite ClosedCellSprite => m_ClosedCellSprite;
        public Sprite CrackCellSprite => m_CrackCellSprite;
        public Sprite ClashCellSprite => m_ClashCellSprite;
        public IReadOnlyList<MiningGemVisualDefinition> GemVisuals => m_GemVisuals;

        private void OnEnable()
        {
            if (m_GemVisuals == null || m_GemVisuals.Length == 0)
                m_GemVisuals = CreateDefaultGemVisuals();
        }

        public MiningGemVisualDefinition GetGemVisual(int gemType)
        {
            if (m_GemVisuals == null) return null;
            for (int index = 0; index < m_GemVisuals.Length; index++)
            {
                MiningGemVisualDefinition visual = m_GemVisuals[index];
                if (visual != null && visual.GemType == gemType) return visual;
            }

            return null;
        }

        public void ValidateConfiguration(bool requireSprites = true)
        {
            if (m_GemVisuals == null || m_GemVisuals.Length != 14)
                throw new InvalidOperationException("Mining needs exactly 14 gem visual definitions.");
            if (requireSprites && (m_ClosedCellSprite == null || m_CrackCellSprite == null || m_ClashCellSprite == null))
                throw new InvalidOperationException("Mining cell sprites are not fully configured.");

            var gemTypes = new HashSet<int>();
            for (int index = 0; index < m_GemVisuals.Length; index++)
            {
                MiningGemVisualDefinition visual = m_GemVisuals[index];
                if (visual == null) throw new InvalidOperationException($"Mining gem visual {index} is null.");
                if (!gemTypes.Add(visual.GemType))
                    throw new InvalidOperationException($"Mining gem type {visual.GemType} is duplicated.");
                if (visual.GridSize.x <= 0 || visual.GridSize.y <= 0)
                    throw new InvalidOperationException($"Mining gem type {visual.GemType} has an invalid grid size.");
                if (requireSprites &&
                    (visual.GemSprite == null || visual.HoleSprite == null || visual.FrameSprite == null))
                    throw new InvalidOperationException($"Mining gem type {visual.GemType} is missing a sprite.");
            }
        }

        private static MiningGemVisualDefinition[] CreateDefaultGemVisuals()
        {
            // Occupied cell sizes are derived from the exported sprite dimensions at a 128 px cell baseline.
            var visuals = new MiningGemVisualDefinition[14];
            for (int gemType = 1; gemType <= visuals.Length; gemType++)
            {
                Vector2Int size = MiningGemVisualDefinition.GetDefaultGridSize(gemType);
                visuals[gemType - 1] = new MiningGemVisualDefinition(gemType, size.x, size.y);
            }

            return visuals;
        }
    }
}
