using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas.Activities.Mining
{
    public enum MiningTreasureBoxType
    {
        Pink,
        Blue,
        Yellow,
        Orange,
        Purple
    }

    [Serializable]
    public sealed class MiningGemVisualDefinition
    {
        [SerializeField, Min(1)] private int m_GemType;
        [SerializeField] private Vector2Int m_GridSize = Vector2Int.one;
        [SerializeField] private Sprite m_GemSprite;
        [SerializeField] private Sprite m_HoleSprite;
        [SerializeField] private Sprite m_FrameSprite;

        public int GemType => m_GemType;
        public Vector2Int GridSize => m_GridSize;
        public Sprite GemSprite => m_GemSprite;
        public Sprite HoleSprite => m_HoleSprite;
        public Sprite FrameSprite => m_FrameSprite;

        internal MiningGemVisualDefinition(int gemType, int width, int height)
        {
            m_GemType = gemType;
            m_GridSize = new Vector2Int(width, height);
        }

        public Vector2Int GetRotatedGridSize(int quarterTurns)
        {
            int normalizedTurns = NormalizeQuarterTurns(quarterTurns);
            return (normalizedTurns & 1) == 0
                ? m_GridSize
                : new Vector2Int(m_GridSize.y, m_GridSize.x);
        }

        internal static int NormalizeQuarterTurns(int quarterTurns)
        {
            int normalized = quarterTurns % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }

        internal static Vector2Int GetDefaultGridSize(int gemType)
        {
            return gemType switch
            {
                1 => new Vector2Int(2, 1),
                2 => new Vector2Int(3, 1),
                3 => new Vector2Int(3, 1),
                4 => new Vector2Int(2, 2),
                5 => new Vector2Int(2, 2),
                6 => new Vector2Int(2, 2),
                7 => new Vector2Int(4, 1),
                8 => new Vector2Int(5, 1),
                9 => new Vector2Int(2, 3),
                10 => new Vector2Int(2, 3),
                11 => new Vector2Int(2, 3),
                12 => new Vector2Int(2, 4),
                13 => new Vector2Int(3, 3),
                14 => new Vector2Int(4, 4),
                _ => throw new InvalidOperationException($"Unknown Mining gem type {gemType}.")
            };
        }
    }

    [Serializable]
    public sealed class MiningGemPlacement
    {
        [SerializeField, Min(1)] private int m_GemId;
        [SerializeField, Min(1)] private int m_GemType;
        [Tooltip("本工程派生的埋藏区域左上格；原 APK 导出未包含 GemId 到格子的映射。")]
        [SerializeField] private Vector2Int m_GridOrigin;
        [SerializeField, Range(0, 3)] private int m_GridQuarterTurns;
        [SerializeField] private Vector2 m_GatePosition;
        [SerializeField] private float m_GateRotation;
        [SerializeField, Min(0.01f)] private float m_GateScale = 0.4f;

        public int GemId => m_GemId;
        public int GemType => m_GemType;
        public Vector2Int GridOrigin => m_GridOrigin;
        public int GridQuarterTurns => m_GridQuarterTurns;
        public Vector2 GatePosition => m_GatePosition;
        public float GateRotation => m_GateRotation;
        public float GateScale => m_GateScale;

        internal MiningGemPlacement(int gemId, int gemType, float gateX, float gateY, float gateRotation,
            float gateScale)
        {
            m_GemId = gemId;
            m_GemType = gemType;
            m_GatePosition = new Vector2(gateX, gateY);
            m_GateRotation = gateRotation;
            m_GateScale = gateScale;
            m_GridOrigin = new Vector2Int(-1, -1);

            int roundedRotation = Mathf.Abs(Mathf.RoundToInt(gateRotation)) % 180;
            m_GridQuarterTurns = roundedRotation == 90 ? 1 : 0;
        }

        internal void SetDerivedGridOrigin(Vector2Int origin)
        {
            m_GridOrigin = origin;
        }
    }

    [Serializable]
    public sealed class MiningStageDefinition
    {
        [SerializeField, Min(1)] private int m_EventId = 1;
        [SerializeField, Range(1, 5)] private int m_StepId = 1;
        [SerializeField, Range(1, 16)] private int m_CellCount = 4;
        [SerializeField] private int m_RewardId;
        [SerializeField] private MiningTreasureBoxType m_TreasureBoxType;
        [SerializeField] private MiningGemPlacement[] m_Gems = Array.Empty<MiningGemPlacement>();

        public int EventId => m_EventId;
        public int StepId => m_StepId;
        public int CellCount => m_CellCount;
        public int RewardId => m_RewardId;
        public MiningTreasureBoxType TreasureBoxType => m_TreasureBoxType;
        public IReadOnlyList<MiningGemPlacement> Gems => m_Gems;

        internal MiningStageDefinition(int eventId, int stepId, int cellCount, int rewardId,
            MiningTreasureBoxType treasureBoxType, MiningGemPlacement[] gems)
        {
            m_EventId = eventId;
            m_StepId = stepId;
            m_CellCount = cellCount;
            m_RewardId = rewardId;
            m_TreasureBoxType = treasureBoxType;
            m_Gems = gems ?? Array.Empty<MiningGemPlacement>();
        }

        public MiningGemPlacement GetGem(int gemId)
        {
            for (int index = 0; index < m_Gems.Length; index++)
            {
                MiningGemPlacement gem = m_Gems[index];
                if (gem != null && gem.GemId == gemId) return gem;
            }

            return null;
        }
    }

    /// <summary>
    /// Mining 配置入口，只负责关联活动时间、宝石图片表、Gate 主题表、奖励表和各期关卡表，不保存具体业务数据。
    /// </summary>
    [CreateAssetMenu(fileName = "MiningActivityConfig", menuName = "GF/Activity/Mining/Activity Config")]
    public sealed class MiningActivityConfig : ScriptableObject
    {
        [SerializeField] private MiningScheduleConfig m_ScheduleConfig;
        [SerializeField] private MiningGemVisualConfig m_GemVisualConfig;
        [SerializeField] private MiningGateThemeConfig m_GateThemeConfig;
        [SerializeField] private MiningRewardConfig m_RewardConfig;
        [SerializeField] private MiningEventConfig[] m_EventConfigs = Array.Empty<MiningEventConfig>();

        public MiningScheduleConfig ScheduleConfig => m_ScheduleConfig;
        public MiningGemVisualConfig GemVisualConfig => m_GemVisualConfig;
        public MiningGateThemeConfig GateThemeConfig => m_GateThemeConfig;
        public MiningRewardConfig RewardConfig => m_RewardConfig;
        public IReadOnlyList<MiningEventConfig> EventConfigs => m_EventConfigs;
        public Sprite ClosedCellSprite => m_GemVisualConfig != null ? m_GemVisualConfig.ClosedCellSprite : null;
        public Sprite CrackCellSprite => m_GemVisualConfig != null ? m_GemVisualConfig.CrackCellSprite : null;
        public Sprite ClashCellSprite => m_GemVisualConfig != null ? m_GemVisualConfig.ClashCellSprite : null;

        public MiningGemVisualDefinition GetGemVisual(int gemType)
        {
            return m_GemVisualConfig != null ? m_GemVisualConfig.GetGemVisual(gemType) : null;
        }

        public MiningGateThemeDefinition GetGateTheme(int stepId)
        {
            return m_GateThemeConfig != null ? m_GateThemeConfig.GetTheme(stepId) : null;
        }

        public MiningEventConfig GetEventConfig(int eventId)
        {
            if (m_EventConfigs != null)
            {
                for (int index = 0; index < m_EventConfigs.Length; index++)
                {
                    MiningEventConfig eventConfig = m_EventConfigs[index];
                    if (eventConfig != null && eventConfig.EventId == eventId) return eventConfig;
                }
            }

            return null;
        }

        public bool TryGetStage(int eventId, int stepId, out MiningStageDefinition stage)
        {
            MiningEventConfig eventConfig = GetEventConfig(eventId);
            if (eventConfig != null) return eventConfig.TryGetStage(stepId, out stage);
            stage = null;
            return false;
        }

        public MiningStageDefinition GetStage(int eventId, int stepId)
        {
            if (TryGetStage(eventId, stepId, out MiningStageDefinition stage)) return stage;
            throw new InvalidOperationException($"Mining stage Event={eventId}, Step={stepId} is not configured.");
        }

        public MiningRewardDefinition GetReward(int rewardId)
        {
            if (m_RewardConfig != null) return m_RewardConfig.GetReward(rewardId);
            throw new InvalidOperationException("Mining has no reward config.");
        }

        public MiningRewardDefinition GetStageReward(int eventId, int stepId)
        {
            return GetReward(GetStage(eventId, stepId).RewardId);
        }

        public void ValidateConfiguration(bool requireSprites = true)
        {
            if (m_ScheduleConfig == null)
                throw new InvalidOperationException("Mining has no schedule config.");
            m_ScheduleConfig.ValidateConfiguration();

            if (m_GemVisualConfig == null)
                throw new InvalidOperationException("Mining has no gem visual config.");
            m_GemVisualConfig.ValidateConfiguration(requireSprites);

            if (m_GateThemeConfig == null)
                throw new InvalidOperationException("Mining has no Gate theme config.");
            m_GateThemeConfig.ValidateConfiguration(requireSprites);

            if (m_RewardConfig == null)
                throw new InvalidOperationException("Mining has no reward config.");
            m_RewardConfig.ValidateConfiguration();

            if (m_EventConfigs == null || m_EventConfigs.Length != 2)
                throw new InvalidOperationException("Mining needs separate Event01 and Event02 configs.");

            var eventIds = new HashSet<int>();
            int totalGemCount = 0;
            for (int index = 0; index < m_EventConfigs.Length; index++)
            {
                MiningEventConfig eventConfig = m_EventConfigs[index];
                if (eventConfig == null)
                    throw new InvalidOperationException($"Mining event config {index} is null.");
                if (!eventIds.Add(eventConfig.EventId))
                    throw new InvalidOperationException($"Mining Event={eventConfig.EventId} is duplicated.");

                eventConfig.ValidateConfiguration(m_GemVisualConfig);
                for (int stageIndex = 0; stageIndex < eventConfig.Stages.Count; stageIndex++)
                {
                    MiningStageDefinition stage = eventConfig.Stages[stageIndex];
                    totalGemCount += stage.Gems.Count;
                    if (!m_RewardConfig.TryGetReward(stage.RewardId, out _))
                        throw new InvalidOperationException(
                            $"Mining Event={stage.EventId}, Step={stage.StepId} references missing RewardId={stage.RewardId}.");
                }
            }

            if (!eventIds.Contains(1) || !eventIds.Contains(2))
                throw new InvalidOperationException("Mining needs Event01 and Event02 configs.");
            if (!eventIds.Contains(m_ScheduleConfig.EventId))
                throw new InvalidOperationException(
                    $"Mining schedule references missing Event={m_ScheduleConfig.EventId}.");
            if (totalGemCount != 63)
                throw new InvalidOperationException(
                    $"Mining runtime export contains 63 gems, but configs contain {totalGemCount}.");
        }
    }
}
