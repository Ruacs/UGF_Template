using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    public enum MiningDigStatus
    {
        Opened,
        InvalidCell,
        AlreadyOpened,
        NoPickaxes,
        StageCompleted
    }

    public sealed class MiningDigResult
    {
        private static readonly int[] NoGems = Array.Empty<int>();

        public MiningDigStatus Status { get; }
        public int CellId { get; }
        public int RemainingPickaxes { get; }
        public IReadOnlyList<int> CollectedGemIds { get; }
        public bool IsStageComplete { get; }
        public bool OpenedCell => Status == MiningDigStatus.Opened;

        internal MiningDigResult(MiningDigStatus status, int cellId, int remainingPickaxes,
            IReadOnlyList<int> collectedGemIds = null, bool isStageComplete = false)
        {
            Status = status;
            CellId = cellId;
            RemainingPickaxes = remainingPickaxes;
            CollectedGemIds = collectedGemIds ?? NoGems;
            IsStageComplete = isStageComplete;
        }
    }

    public sealed class MiningBoardSnapshot
    {
        public int EventId { get; }
        public int StepId { get; }
        public int PickaxeCount { get; }
        public IReadOnlyList<int> OpenCellIds { get; }
        public IReadOnlyList<int> CollectedGemIds { get; }
        public bool IsStageComplete { get; }

        internal MiningBoardSnapshot(int eventId, int stepId, int pickaxeCount, IReadOnlyList<int> openCellIds,
            IReadOnlyList<int> collectedGemIds, bool isStageComplete)
        {
            EventId = eventId;
            StepId = stepId;
            PickaxeCount = pickaxeCount;
            OpenCellIds = openCellIds;
            CollectedGemIds = collectedGemIds;
            IsStageComplete = isStageComplete;
        }
    }

    /// <summary>不依赖 UI 的挖掘状态机；存档层保存 Snapshot 中的关卡、镐子、格子和宝石进度。</summary>
    public sealed class MiningBoardModel
    {
        private readonly MiningActivityConfig m_Config;
        private readonly MiningStageDefinition m_Stage;
        private readonly HashSet<int> m_OpenCells = new HashSet<int>();
        private readonly HashSet<int> m_CollectedGems = new HashSet<int>();
        private readonly Dictionary<int, int[]> m_GemCells = new Dictionary<int, int[]>();

        public MiningStageDefinition Stage => m_Stage;
        public int PickaxeCount { get; private set; }
        public bool IsStageComplete => m_CollectedGems.Count == m_Stage.Gems.Count;

        public MiningBoardModel(MiningActivityConfig config, MiningStageDefinition stage, int pickaxeCount,
            IEnumerable<int> openCellIds = null, IEnumerable<int> collectedGemIds = null)
        {
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            PickaxeCount = Math.Max(0, pickaxeCount);
            BuildGemCellMap();
            Restore(openCellIds, collectedGemIds);
        }

        public bool IsCellOpen(int cellId) => m_OpenCells.Contains(cellId);
        public bool IsGemCollected(int gemId) => m_CollectedGems.Contains(gemId);

        internal void SetPickaxeCount(int pickaxeCount)
        {
            PickaxeCount = Math.Max(0, pickaxeCount);
        }

        public IReadOnlyList<int> GetCellsForGem(int gemId)
        {
            return m_GemCells.TryGetValue(gemId, out int[] cells) ? cells : Array.Empty<int>();
        }

        public MiningDigResult Dig(int cellId)
        {
            int cellTotal = m_Stage.CellCount * m_Stage.CellCount;
            if (cellId < 0 || cellId >= cellTotal)
                return new MiningDigResult(MiningDigStatus.InvalidCell, cellId, PickaxeCount);
            if (IsStageComplete)
                return new MiningDigResult(MiningDigStatus.StageCompleted, cellId, PickaxeCount,
                    isStageComplete: true);
            if (m_OpenCells.Contains(cellId))
                return new MiningDigResult(MiningDigStatus.AlreadyOpened, cellId, PickaxeCount);
            if (PickaxeCount <= 0)
                return new MiningDigResult(MiningDigStatus.NoPickaxes, cellId, PickaxeCount);

            PickaxeCount--;
            m_OpenCells.Add(cellId);
            var collected = new List<int>();
            for (int index = 0; index < m_Stage.Gems.Count; index++)
            {
                MiningGemPlacement gem = m_Stage.Gems[index];
                if (gem == null || m_CollectedGems.Contains(gem.GemId)) continue;
                if (!AreAllCellsOpen(m_GemCells[gem.GemId])) continue;
                m_CollectedGems.Add(gem.GemId);
                collected.Add(gem.GemId);
            }

            return new MiningDigResult(MiningDigStatus.Opened, cellId, PickaxeCount, collected,
                IsStageComplete);
        }

        public MiningBoardSnapshot CreateSnapshot()
        {
            var openCells = new List<int>(m_OpenCells);
            var collectedGems = new List<int>(m_CollectedGems);
            openCells.Sort();
            collectedGems.Sort();
            return new MiningBoardSnapshot(m_Stage.EventId, m_Stage.StepId, PickaxeCount, openCells,
                collectedGems, IsStageComplete);
        }

        private void BuildGemCellMap()
        {
            for (int index = 0; index < m_Stage.Gems.Count; index++)
            {
                MiningGemPlacement gem = m_Stage.Gems[index];
                MiningGemVisualDefinition visual = m_Config.GetGemVisual(gem.GemType);
                if (visual == null)
                    throw new InvalidOperationException($"Mining GemId {gem.GemId} uses unknown type {gem.GemType}.");
                Vector2Int size = visual.GetRotatedGridSize(gem.GridQuarterTurns);
                var cells = new int[size.x * size.y];
                int writeIndex = 0;
                for (int row = gem.GridOrigin.y; row < gem.GridOrigin.y + size.y; row++)
                {
                    for (int column = gem.GridOrigin.x; column < gem.GridOrigin.x + size.x; column++)
                        cells[writeIndex++] = row * m_Stage.CellCount + column;
                }

                m_GemCells.Add(gem.GemId, cells);
            }
        }

        private void Restore(IEnumerable<int> openCellIds, IEnumerable<int> collectedGemIds)
        {
            int cellTotal = m_Stage.CellCount * m_Stage.CellCount;
            if (openCellIds != null)
            {
                foreach (int cellId in openCellIds)
                    if (cellId >= 0 && cellId < cellTotal) m_OpenCells.Add(cellId);
            }

            if (collectedGemIds != null)
            {
                foreach (int gemId in collectedGemIds)
                {
                    if (!m_GemCells.TryGetValue(gemId, out int[] cells)) continue;
                    m_CollectedGems.Add(gemId);
                    for (int index = 0; index < cells.Length; index++) m_OpenCells.Add(cells[index]);
                }
            }

            // Repair a save that wrote OpenCellIds immediately before HasGemIds.
            for (int index = 0; index < m_Stage.Gems.Count; index++)
            {
                MiningGemPlacement gem = m_Stage.Gems[index];
                if (!m_CollectedGems.Contains(gem.GemId) && AreAllCellsOpen(m_GemCells[gem.GemId]))
                    m_CollectedGems.Add(gem.GemId);
            }
        }

        private bool AreAllCellsOpen(IReadOnlyList<int> cells)
        {
            for (int index = 0; index < cells.Count; index++)
                if (!m_OpenCells.Contains(cells[index])) return false;
            return true;
        }
    }
}
