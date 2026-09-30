using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.WinStreak.UI
{
    /// <summary>同一份塔段预制体通过类型切换为顶部、中段或底部。</summary>
    public enum TowerType
    {
        Top = 0,
        Main = 1,
        Bottom = 2
    }

    [Serializable]
    public struct TowerTypeData
    {
        public Vector2 StagePosition;
        public Vector2 CheckpointPosition;
        public int ProgressTrackHeight;
    }

    /// <summary>
    /// 连胜塔中的一个节点。奖励节点绑定检查点快照，底部节点使用 BindBottom 单独绑定。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WinStreakCheckpointRowView : MonoBehaviour
    {
        [SerializeField] private TowerType m_TowerType = TowerType.Main;

        [SerializeField] private RectTransform m_StageBoxRT;
        [SerializeField] private RectTransform m_CheckpointBadgeRT;
        [SerializeField] private RectTransform m_ProgressTrackRT;

        [Header("Labels")]
        [SerializeField] private TMP_Text m_Checkpoint;
        [SerializeField] private TMP_Text m_Reward;

        [Header("Visuals")]
        [SerializeField] private Image m_Tower;
        [SerializeField] private Image m_Chest;
        [SerializeField] private Image m_Stage;
        [Tooltip("当前节点负责的局部进度条；不是整个活动的总进度。")]
        [SerializeField] private Image m_ProgressFill;
        [SerializeField] private RectTransform m_RewardTooltip;
        [SerializeField] private GameObject m_CheckpointBadge;
        [SerializeField] private GameObject m_LockedState;
        [SerializeField] private GameObject m_ReachedState;
        [SerializeField] private GameObject m_ClaimedState;
        [SerializeField] private Sprite[] m_ChestSprites;
        [SerializeField] private Sprite[] m_StageSprites;
        [SerializeField] private Sprite[] m_TowerSprites;

        [Header("Interaction")]
        [SerializeField] private Button m_ClaimButton;

        [SerializeField] private TowerTypeData[] m_TowerTypeDatas;

        private Action m_ClaimAction;
        private RewardTooltipView m_RewardTooltipView;
        private int m_SegmentStart;
        private int m_SegmentEnd;
        private int m_ClaimCheckpoint = -1;
        private bool m_CanClaim;

        public TowerType Type => m_TowerType;
        public int SegmentStart => m_SegmentStart;
        public int SegmentEnd => m_SegmentEnd;

        private void Awake()
        {
            ResolveOptionalReferences();
            if (m_ClaimButton != null) m_ClaimButton.AddSafeClick(OnClickClaim);
            // The authored row is also used as a runtime template. Keep the action hidden until
            // Bind supplies a reached, claimable checkpoint.
            SetClaimButtonVisible(-1f);
            SetTower(m_TowerType);
        }

        private void OnDisable()
        {
            m_ClaimAction = null;
            m_ClaimCheckpoint = -1;
            m_CanClaim = false;
        }

        /// <summary>运行时复用同一份 TowerView 预制体时切换节点类型。</summary>
        public void SetTowerType(TowerType towerType)
        {
            m_TowerType = towerType;
            SetTower(towerType);
        }

        /// <summary>
        /// 绑定奖励检查点及其局部塔段。进度仍由活动累计值提供，但 fill 只计算本段范围。
        /// </summary>
        public void Bind(WinStreakCheckpointSnapshot checkpoint, float currentProgress, int segmentStart,
            int segmentEnd, Action claimAction)
        {
            if (checkpoint == null)
            {
                m_ClaimAction = null;
                gameObject.SetActive(false);
                return;
            }

            ResolveOptionalReferences();
            gameObject.SetActive(true);
            m_ClaimAction = claimAction;
            m_SegmentStart = segmentStart;
            m_SegmentEnd = segmentEnd;
            m_ClaimCheckpoint = checkpoint.Checkpoint;
            m_CanClaim = checkpoint.CanClaim;
            SetTower(m_TowerType);

            if (m_Checkpoint != null) m_Checkpoint.SetText(checkpoint.Checkpoint.ToString());
            SetSprite(m_Chest, m_ChestSprites, (int)checkpoint.ChestTier);
            SetSprite(m_Stage, m_StageSprites, (int)checkpoint.ChestTier);
            SetRewardText(checkpoint);
            SetCheckpointState(checkpoint.IsReached, checkpoint.IsClaimed);
            SetSegmentProgress(currentProgress, segmentStart, segmentEnd);

            SetRewardVisible(true);
            ShowRewardTooltip(checkpoint);
            SetClaimButtonVisible(currentProgress);
        }

        /// <summary>
        /// 绑定无奖励的塔底。它默认显示为已领取，只有累计值达到 first checkpoint 时填满进度。
        /// </summary>
        public void BindBottom(int firstCheckpoint, float currentProgress)
        {
            ResolveOptionalReferences();
            gameObject.SetActive(true);
            m_ClaimAction = null;
            m_ClaimCheckpoint = -1;
            m_CanClaim = false;
            SetTowerType(TowerType.Bottom);

            // The base marker is the already-complete level immediately before the first reward.
            // Its only outgoing segment is the one that completes at the first reward checkpoint.
            int baseCheckpoint = Mathf.Max(0, firstCheckpoint - 1);
            m_SegmentStart = baseCheckpoint;
            m_SegmentEnd = firstCheckpoint;
            if (m_Checkpoint != null) m_Checkpoint.SetText(baseCheckpoint.ToString());
            SetRewardVisible(false);
            SetStageVisible(false);
            SetCheckpointState(reached: true, claimed: true);
            SetSegmentProgress(currentProgress);
            SetClaimButtonVisible(currentProgress);
        }

        /// <summary>兼容旧调用方的单检查点绑定入口。</summary>
        public void Bind(WinStreakCheckpointSnapshot checkpoint, Action claimAction)
        {
            int checkpointValue = checkpoint != null ? checkpoint.Checkpoint : 0;
            Bind(checkpoint, checkpointValue, checkpointValue, checkpointValue, claimAction);
        }

        public static float CalculateSegmentProgress(int currentProgress, int segmentStart, int segmentEnd)
        {
            return CalculateSegmentProgress((float)currentProgress, segmentStart, segmentEnd);
        }

        public static float CalculateSegmentProgress(float currentProgress, int segmentStart, int segmentEnd)
        {
            if (segmentEnd <= segmentStart)
                return currentProgress >= segmentEnd ? 1f : 0f;
            return Mathf.Clamp01((currentProgress - segmentStart) / (float)(segmentEnd - segmentStart));
        }

        /// <summary>按活动累计值立即更新当前塔段的局部填充。</summary>
        public void SetProgress(float currentProgress)
        {
            SetSegmentProgress(currentProgress);
            SetClaimButtonVisible(currentProgress);
        }

        public bool ContainsProgress(float currentProgress)
        {
            if (m_TowerType == TowerType.Top)
                return currentProgress >= m_SegmentEnd;
            return currentProgress >= m_SegmentStart && currentProgress <= m_SegmentEnd;
        }

        /// <summary>取得累计进度在本塔段轨道上的世界坐标，供 Character 与 Fill 共用。</summary>
        public bool TryGetProgressWorldPosition(float currentProgress, out Vector3 worldPosition)
        {
            if (m_TowerType == TowerType.Top || m_ProgressTrackRT == null)
            {
                if (m_CheckpointBadgeRT == null)
                {
                    worldPosition = default;
                    return false;
                }

                worldPosition = m_CheckpointBadgeRT.TransformPoint(m_CheckpointBadgeRT.rect.center);
                return true;
            }

            float progress = CalculateSegmentProgress(currentProgress, m_SegmentStart, m_SegmentEnd);
            Rect trackRect = m_ProgressTrackRT.rect;
            Vector3 localPoint = new Vector3(trackRect.center.x,
                Mathf.Lerp(trackRect.yMin, trackRect.yMax, progress), 0f);
            worldPosition = m_ProgressTrackRT.TransformPoint(localPoint);
            return true;
        }

        private void OnClickClaim()
        {
            m_ClaimAction?.Invoke();
        }

        private void ResolveOptionalReferences()
        {
            if (m_RewardTooltip != null && m_RewardTooltipView == null)
                m_RewardTooltipView = m_RewardTooltip.GetComponent<RewardTooltipView>();

            if (m_Reward == null && m_RewardTooltip != null && m_RewardTooltipView == null)
                m_Reward = m_RewardTooltip.GetComponentInChildren<TMP_Text>(true);
        }

        private void ShowRewardTooltip(WinStreakCheckpointSnapshot checkpoint)
        {
            if (m_RewardTooltipView != null)
            {
                m_RewardTooltipView.ShowPersistent(checkpoint.Rewards);
                return;
            }

            // Compatibility fallback for older authored rows that only contain a text tooltip.
            if (m_RewardTooltip != null) m_RewardTooltip.localScale = Vector3.one;
        }

        private void SetRewardText(WinStreakCheckpointSnapshot checkpoint)
        {
            if (m_Reward != null) m_Reward.SetText(DescribeReward(checkpoint));
        }

        private static string DescribeReward(WinStreakCheckpointSnapshot checkpoint)
        {
            if (checkpoint == null || checkpoint.Rewards == null || checkpoint.Rewards.Count == 0)
                return "REWARD";

            var values = new List<string>();
            foreach (RewardEntry reward in checkpoint.Rewards)
            {
                if (reward == null) continue;
                string amount = reward.GrantMode == RewardGrantMode.UnlimitedUse
                    ? string.Concat(reward.RequestAmount / 60, "m")
                    : string.Concat("x", reward.RequestAmount);
                values.Add(string.Concat(GetEnglishRewardName(reward.ResourceKey), " ", amount));
            }
            return values.Count == 0 ? "REWARD" : string.Join("  ", values);
        }

        private static string GetEnglishRewardName(string resourceKey)
        {
            return resourceKey switch
            {
                "currency.money" => "COINS",
                "effect.unlimited_life" => "LIFE",
                "game.hammer" => "HAMMER",
                "game.drill" => "DRILL",
                "game.bomb" => "BOMB",
                _ => "REWARD"
            };
        }

        private static void SetSprite(Image image, Sprite[] sprites, int index)
        {
            if (image == null) return;
            if (sprites == null || index < 0 || index >= sprites.Length || sprites[index] == null)
            {
                image.enabled = false;
                return;
            }

            image.sprite = sprites[index];
            image.enabled = true;
        }

        private void SetTower(TowerType towerType)
        {
            TowerTypeData data;
            bool hasData = TryGetTowerTypeData(towerType, out data);

            if (m_Tower != null && m_TowerSprites != null)
            {
                int index = (int)towerType;
                if (index >= 0 && index < m_TowerSprites.Length && m_TowerSprites[index] != null)
                {
                    m_Tower.sprite = m_TowerSprites[index];
                    m_Tower.enabled = true;
                }
            }

            if (hasData)
            {
                if (m_StageBoxRT != null) m_StageBoxRT.anchoredPosition = data.StagePosition;
                if (m_CheckpointBadgeRT != null)
                    m_CheckpointBadgeRT.anchoredPosition = data.CheckpointPosition;
                if (m_ProgressTrackRT != null && towerType != TowerType.Top && data.ProgressTrackHeight > 0)
                {
                    Vector2 size = m_ProgressTrackRT.sizeDelta;
                    size.y = data.ProgressTrackHeight;
                    m_ProgressTrackRT.sizeDelta = size;
                }
            }

            if (m_ProgressTrackRT != null) m_ProgressTrackRT.gameObject.SetActive(towerType != TowerType.Top);
            SetStageVisible(towerType != TowerType.Bottom);
            SetRewardVisible(towerType != TowerType.Bottom);
        }

        private bool TryGetTowerTypeData(TowerType towerType, out TowerTypeData data)
        {
            int index = (int)towerType;
            if (m_TowerTypeDatas != null && index >= 0 && index < m_TowerTypeDatas.Length)
            {
                data = m_TowerTypeDatas[index];
                return true;
            }

            data = default;
            return false;
        }

        private void SetCheckpointState(bool reached, bool claimed)
        {
            SetActive(m_LockedState, !reached && !claimed);
            SetActive(m_ReachedState, reached && !claimed);
            SetActive(m_ClaimedState, claimed);

            if (m_CheckpointBadge == null) return;

            // Some authored versions put ClaimedState inside CheckpointBadge, while others use
            // sibling objects. Keep the container alive for the nested version.
            bool claimedNested = m_ClaimedState != null &&
                                 m_ClaimedState.transform.IsChildOf(m_CheckpointBadge.transform);
            m_CheckpointBadge.SetActive(!claimed || claimedNested);
        }

        private void SetStageVisible(bool visible)
        {
            if (m_StageBoxRT != null && m_StageBoxRT != transform)
                m_StageBoxRT.gameObject.SetActive(visible);
            else if (m_Stage != null)
                m_Stage.gameObject.SetActive(visible);
        }

        private void SetRewardVisible(bool visible)
        {
            GameObject rewardRoot = FindRewardRoot();
            if (rewardRoot != null && rewardRoot.transform != transform)
                rewardRoot.SetActive(visible);

            if (rewardRoot == null)
            {
                if (m_Chest != null) m_Chest.gameObject.SetActive(visible);
                if (m_RewardTooltip != null) m_RewardTooltip.gameObject.SetActive(visible);
            }

            if (!visible) SetClaimButtonVisible(-1f);
        }

        private void SetClaimButtonVisible(float currentProgress)
        {
            if (m_ClaimButton == null || m_ClaimButton.transform == transform) return;

            // A claim action is only presented after the animated/displayed progress reaches
            // this row's checkpoint. This also prevents a newly reached reward from appearing
            // before the progress animation has visually arrived at its marker.
            bool visible = m_CanClaim && m_ClaimCheckpoint >= 0 &&
                           currentProgress >= m_ClaimCheckpoint;
            m_ClaimButton.gameObject.SetActive(visible);
            m_ClaimButton.interactable = visible;
        }

        private GameObject FindRewardRoot()
        {
            Transform chestParent = m_Chest != null ? m_Chest.transform.parent : null;
            if (chestParent != null && chestParent != transform) return chestParent.gameObject;

            Transform tooltipParent = m_RewardTooltip != null ? m_RewardTooltip.parent : null;
            if (tooltipParent != null && tooltipParent != transform) return tooltipParent.gameObject;
            return null;
        }

        private void SetSegmentProgress(float currentProgress, int segmentStart, int segmentEnd)
        {
            ApplySegmentProgress(CalculateSegmentProgress(currentProgress, segmentStart, segmentEnd));
        }

        private void SetSegmentProgress(float currentProgress)
        {
            SetSegmentProgress(currentProgress, m_SegmentStart, m_SegmentEnd);
        }

        private void ApplySegmentProgress(float progress)
        {
            if (m_ProgressFill == null) return;
            progress = Mathf.Clamp01(progress);
            if (m_ProgressFill.type == Image.Type.Filled)
            {
                m_ProgressFill.fillAmount = progress;
                return;
            }

            RectTransform rect = m_ProgressFill.rectTransform;
            Vector2 max = rect.anchorMax;
            max.y = progress;
            rect.anchorMax = max;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }
    }
}
