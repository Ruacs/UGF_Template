using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    /// <summary>
    /// 单期 Mining 关卡配置。Gate 数据来自运行时导出；GridOrigin 是在缺少原始格子映射时，
    /// 按 GatePos 和宝石占格尺寸生成的确定性、无重叠布局。
    /// </summary>
    [CreateAssetMenu(fileName = "MiningEventConfig", menuName = "GF/Activity/Mining/Event Config")]
    public sealed class MiningEventConfig : ScriptableObject
    {
        [SerializeField, Range(1, 2)] private int m_EventId = 1;
        [SerializeField] private MiningStageDefinition[] m_Stages = Array.Empty<MiningStageDefinition>();

        public int EventId => m_EventId;
        public IReadOnlyList<MiningStageDefinition> Stages => m_Stages;

        private void OnEnable()
        {
            if (m_Stages == null || m_Stages.Length == 0)
                m_Stages = CreateDefaultStages(m_EventId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if ((m_EventId == 1 || m_EventId == 2) && !StagesBelongToEvent())
            {
                m_Stages = CreateDefaultStages(m_EventId);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        public bool TryGetStage(int stepId, out MiningStageDefinition stage)
        {
            if (m_Stages != null)
            {
                for (int index = 0; index < m_Stages.Length; index++)
                {
                    MiningStageDefinition candidate = m_Stages[index];
                    if (candidate == null || candidate.StepId != stepId) continue;
                    stage = candidate;
                    return true;
                }
            }

            stage = null;
            return false;
        }

        public MiningStageDefinition GetStage(int stepId)
        {
            if (TryGetStage(stepId, out MiningStageDefinition stage)) return stage;
            throw new InvalidOperationException($"Mining stage Event={m_EventId}, Step={stepId} is not configured.");
        }

        public void ValidateConfiguration(MiningGemVisualConfig gemVisualConfig)
        {
            if (m_EventId != 1 && m_EventId != 2)
                throw new InvalidOperationException($"Mining Event={m_EventId} is unsupported.");
            if (m_Stages == null || m_Stages.Length != 5)
                throw new InvalidOperationException($"Mining Event={m_EventId} needs exactly 5 stages.");
            if (gemVisualConfig == null)
                throw new ArgumentNullException(nameof(gemVisualConfig));

            var stepIds = new HashSet<int>();
            for (int stageIndex = 0; stageIndex < m_Stages.Length; stageIndex++)
            {
                MiningStageDefinition stage = m_Stages[stageIndex];
                if (stage == null)
                    throw new InvalidOperationException($"Mining Event={m_EventId} stage {stageIndex} is null.");
                if (stage.EventId != m_EventId)
                    throw new InvalidOperationException(
                        $"Mining Event={m_EventId} contains a stage owned by Event={stage.EventId}.");
                if (!stepIds.Add(stage.StepId))
                    throw new InvalidOperationException($"Mining Event={m_EventId}, Step={stage.StepId} is duplicated.");

                ValidateStage(stage, gemVisualConfig);
            }

            for (int stepId = 1; stepId <= 5; stepId++)
                if (!stepIds.Contains(stepId))
                    throw new InvalidOperationException($"Mining Event={m_EventId}, Step={stepId} is missing.");
        }

        [ContextMenu("Regenerate Default Stages")]
        private void RegenerateDefaultStages()
        {
            m_Stages = CreateDefaultStages(m_EventId);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Regenerate Derived Grid Placements")]
        private void RegenerateDerivedGridPlacements()
        {
            if (m_Stages == null) return;
            for (int index = 0; index < m_Stages.Length; index++)
            {
                MiningStageDefinition stage = m_Stages[index];
                if (stage == null) continue;
                AssignDerivedGridOrigins(stage.CellCount, stage.Gems);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private bool StagesBelongToEvent()
        {
            if (m_Stages == null || m_Stages.Length == 0) return false;
            for (int index = 0; index < m_Stages.Length; index++)
                if (m_Stages[index] == null || m_Stages[index].EventId != m_EventId) return false;
            return true;
        }

        private static void ValidateStage(MiningStageDefinition stage, MiningGemVisualConfig gemVisualConfig)
        {
            if (stage.EventId <= 0 || stage.StepId <= 0 || stage.CellCount <= 0)
                throw new InvalidOperationException("Mining stage identifiers and CellCount must be positive.");
            if (stage.Gems == null || stage.Gems.Count == 0)
                throw new InvalidOperationException($"Mining stage Event={stage.EventId}, Step={stage.StepId} has no gems.");

            bool[] occupied = new bool[stage.CellCount * stage.CellCount];
            var gemIds = new HashSet<int>();
            for (int index = 0; index < stage.Gems.Count; index++)
            {
                MiningGemPlacement gem = stage.Gems[index];
                if (gem == null) throw new InvalidOperationException("Mining stage contains a null gem placement.");
                if (!gemIds.Add(gem.GemId))
                    throw new InvalidOperationException($"Mining stage contains duplicate GemId {gem.GemId}.");
                MiningGemVisualDefinition visual = gemVisualConfig.GetGemVisual(gem.GemType);
                if (visual == null)
                    throw new InvalidOperationException($"Mining GemId {gem.GemId} uses unknown type {gem.GemType}.");
                if (gem.GateScale <= 0f)
                    throw new InvalidOperationException($"Mining GemId {gem.GemId} has an invalid GateScale.");

                Vector2Int size = visual.GetRotatedGridSize(gem.GridQuarterTurns);
                if (gem.GridOrigin.x < 0 || gem.GridOrigin.y < 0 ||
                    gem.GridOrigin.x + size.x > stage.CellCount || gem.GridOrigin.y + size.y > stage.CellCount)
                    throw new InvalidOperationException(
                        $"Mining GemId {gem.GemId} is outside the {stage.CellCount}x{stage.CellCount} grid.");

                for (int row = gem.GridOrigin.y; row < gem.GridOrigin.y + size.y; row++)
                {
                    for (int column = gem.GridOrigin.x; column < gem.GridOrigin.x + size.x; column++)
                    {
                        int cellIndex = row * stage.CellCount + column;
                        if (occupied[cellIndex])
                            throw new InvalidOperationException(
                                $"Mining stage Event={stage.EventId}, Step={stage.StepId} overlaps at cell {cellIndex}.");
                        occupied[cellIndex] = true;
                    }
                }
            }
        }

        private static MiningStageDefinition[] CreateDefaultStages(int eventId)
        {
            return eventId switch
            {
                1 => CreateEvent01Stages(),
                2 => CreateEvent02Stages(),
                _ => Array.Empty<MiningStageDefinition>()
            };
        }

        private static MiningStageDefinition[] CreateEvent01Stages()
        {
            return new[]
            {
                BuildStage(1, 1, 4, 9001, MiningTreasureBoxType.Pink,
                    Gem(1, 1, 55, -100, 0, 0.4f), Gem(2, 1, -55, -100, 0, 0.4f),
                    Gem(3, 3, 0, 100, 0, 0.4f), Gem(4, 4, 0, 0, 0, 0.5f)),
                BuildStage(1, 2, 6, 9002, MiningTreasureBoxType.Blue,
                    Gem(1, 1, 0, 110, 0, 0.5f), Gem(2, 3, -100, -100, 0, 0.5f),
                    Gem(3, 3, 100, -100, 0, 0.5f), Gem(4, 6, -120, 50, 0, 0.5f),
                    Gem(5, 6, 120, 50, 0, 0.5f), Gem(6, 9, 0, 0, 90, 0.4f)),
                BuildStage(1, 3, 7, 9003, MiningTreasureBoxType.Yellow,
                    Gem(1, 5, -100, -100, 0, 0.4f), Gem(2, 5, 100, -100, 0, 0.4f),
                    Gem(3, 7, -150, 20, 90, 0.4f), Gem(4, 7, 150, 20, 90, 0.4f),
                    Gem(5, 9, 0, 135, 90, 0.4f), Gem(6, 13, 0, -10, 0, 0.5f)),
                BuildStage(1, 4, 8, 9004, MiningTreasureBoxType.Orange,
                    Gem(1, 3, 0, -150, 0, 0.4f), Gem(2, 7, -100, -100, 0, 0.4f),
                    Gem(3, 7, 100, -100, 0, 0.4f), Gem(4, 11, -125, 70, 0, 0.35f),
                    Gem(5, 11, 125, 70, 0, 0.35f), Gem(6, 12, 0, 140, 0, 0.3f),
                    Gem(7, 13, 0, -10, 0, 0.4f)),
                BuildStage(1, 5, 9, 9005, MiningTreasureBoxType.Purple,
                    Gem(1, 1, -80, 140, -45, 0.4f), Gem(2, 1, 80, 140, 45, 0.4f),
                    Gem(3, 2, -75, -150, 0, 0.4f), Gem(4, 2, 75, -150, 0, 0.4f),
                    Gem(5, 8, -130, 0, 90, 0.4f), Gem(6, 8, 130, 0, 90, 0.4f),
                    Gem(7, 12, 0, 140, 180, 0.25f), Gem(8, 14, 0, -25, 0, 0.4f))
            };
        }

        private static MiningStageDefinition[] CreateEvent02Stages()
        {
            return new[]
            {
                BuildStage(2, 1, 4, 9001, MiningTreasureBoxType.Pink,
                    Gem(1, 1, 0, 100, 0, 0.4f), Gem(2, 1, 0, -100, 0, 0.4f),
                    Gem(3, 2, 100, 0, 90, 0.4f), Gem(4, 2, -100, 0, 90, 0.4f),
                    Gem(5, 4, 0, 0, 0, 0.5f)),
                BuildStage(2, 2, 6, 9002, MiningTreasureBoxType.Blue,
                    Gem(1, 1, 0, 110, 0, 0.5f), Gem(2, 3, -100, -100, 0, 0.5f),
                    Gem(3, 3, 100, -100, 0, 0.5f), Gem(4, 6, -120, 50, 0, 0.5f),
                    Gem(5, 6, 120, 50, 0, 0.5f), Gem(6, 9, 0, 0, 90, 0.4f)),
                BuildStage(2, 3, 7, 9003, MiningTreasureBoxType.Yellow,
                    Gem(1, 5, -100, -100, 0, 0.4f), Gem(2, 5, 100, -100, 0, 0.4f),
                    Gem(3, 7, -140, 20, 90, 0.4f), Gem(4, 7, 140, 20, 90, 0.4f),
                    Gem(5, 9, 0, 140, 90, 0.4f), Gem(6, 13, 0, -10, 0, 0.5f)),
                BuildStage(2, 4, 8, 9004, MiningTreasureBoxType.Orange,
                    Gem(1, 3, 0, -130, 0, 0.4f), Gem(2, 7, 100, -65, 90, 0.4f),
                    Gem(3, 7, -100, -65, 90, 0.4f), Gem(4, 11, 120, 80, 90, 0.35f),
                    Gem(5, 11, -120, 80, 90, 0.35f), Gem(6, 12, 0, 140, 0, 0.3f),
                    Gem(7, 13, 0, -10, 0, 0.4f)),
                BuildStage(2, 5, 9, 9005, MiningTreasureBoxType.Purple,
                    Gem(1, 1, -75, 135, -45, 0.35f), Gem(2, 1, 75, 135, 45, 0.35f),
                    Gem(3, 2, -70, -150, 0, 0.35f), Gem(4, 2, 70, -150, 0, 0.35f),
                    Gem(5, 8, -130, 0, 90, 0.35f), Gem(6, 8, 130, 0, 90, 0.35f),
                    Gem(7, 12, 0, 145, 180, 0.3f), Gem(8, 14, 0, -30, 0, 0.4f))
            };
        }

        private static MiningGemPlacement Gem(int gemId, int gemType, float gateX, float gateY,
            float gateRotation, float gateScale)
        {
            return new MiningGemPlacement(gemId, gemType, gateX, gateY, gateRotation, gateScale);
        }

        private static MiningStageDefinition BuildStage(int eventId, int stepId, int cellCount, int rewardId,
            MiningTreasureBoxType boxType, params MiningGemPlacement[] gems)
        {
            AssignDerivedGridOrigins(cellCount, gems);
            return new MiningStageDefinition(eventId, stepId, cellCount, rewardId, boxType, gems);
        }

        private static void AssignDerivedGridOrigins(int cellCount, IReadOnlyList<MiningGemPlacement> gems)
        {
            bool[] occupied = new bool[cellCount * cellCount];
            var placementOrder = new List<MiningGemPlacement>(gems.Count);
            for (int index = 0; index < gems.Count; index++)
                if (gems[index] != null) placementOrder.Add(gems[index]);

            placementOrder.Sort((left, right) =>
            {
                Vector2Int leftSize = GetDefaultFootprint(left.GemType, left.GridQuarterTurns);
                Vector2Int rightSize = GetDefaultFootprint(right.GemType, right.GridQuarterTurns);
                int areaCompare = (rightSize.x * rightSize.y).CompareTo(leftSize.x * leftSize.y);
                return areaCompare != 0 ? areaCompare : left.GemId.CompareTo(right.GemId);
            });

            for (int gemIndex = 0; gemIndex < placementOrder.Count; gemIndex++)
            {
                MiningGemPlacement gem = placementOrder[gemIndex];
                Vector2Int size = GetDefaultFootprint(gem.GemType, gem.GridQuarterTurns);
                float targetColumn = (gem.GatePosition.x / 300f + 0.5f) * (cellCount - 1);
                float targetRow = (0.5f - gem.GatePosition.y / 300f) * (cellCount - 1);
                Vector2Int bestOrigin = new Vector2Int(-1, -1);
                float bestDistance = float.MaxValue;

                for (int row = 0; row <= cellCount - size.y; row++)
                {
                    for (int column = 0; column <= cellCount - size.x; column++)
                    {
                        if (!CanPlace(occupied, cellCount, column, row, size)) continue;
                        float centerX = column + (size.x - 1) * 0.5f;
                        float centerY = row + (size.y - 1) * 0.5f;
                        float distance = (centerX - targetColumn) * (centerX - targetColumn) +
                            (centerY - targetRow) * (centerY - targetRow);
                        if (distance >= bestDistance) continue;
                        bestDistance = distance;
                        bestOrigin = new Vector2Int(column, row);
                    }
                }

                if (bestOrigin.x < 0)
                    throw new InvalidOperationException(
                        $"Cannot derive a non-overlapping {cellCount}x{cellCount} Mining layout for GemId {gem.GemId}.");

                gem.SetDerivedGridOrigin(bestOrigin);
                MarkOccupied(occupied, cellCount, bestOrigin, size);
            }
        }

        private static bool CanPlace(bool[] occupied, int cellCount, int column, int row, Vector2Int size)
        {
            for (int y = row; y < row + size.y; y++)
                for (int x = column; x < column + size.x; x++)
                    if (occupied[y * cellCount + x]) return false;
            return true;
        }

        private static void MarkOccupied(bool[] occupied, int cellCount, Vector2Int origin, Vector2Int size)
        {
            for (int row = origin.y; row < origin.y + size.y; row++)
                for (int column = origin.x; column < origin.x + size.x; column++)
                    occupied[row * cellCount + column] = true;
        }

        private static Vector2Int GetDefaultFootprint(int gemType, int quarterTurns)
        {
            Vector2Int size = MiningGemVisualDefinition.GetDefaultGridSize(gemType);
            return (MiningGemVisualDefinition.NormalizeQuarterTurns(quarterTurns) & 1) == 0
                ? size
                : new Vector2Int(size.y, size.x);
        }
    }
}
