using TMPro;
using UnityEngine;

namespace Lokas.Activities.Collector.UI
{
    /// <summary>
    /// Collector 主页面任务行。
    ///
    /// Row 的业务数据由活动模块提供；这个组件只负责把数据映射到已制作好的
    /// UI 层级。列表位置使用 0-based index：第一行没有上方连接线，最后一行
    /// 没有下方连接线。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CollectorTaskRowView : MonoBehaviour
    {
        public enum CollectorTaskState
        {
            Locked,
            Active,
            Completed
        }

        [Header("Row visuals")]
        [SerializeField] private GameObject m_Bg;
        [Tooltip("Prefab 中名为 Select 的当前行高亮对象。保留旧字段名以兼容已有 Prefab 序列化。")]
        [SerializeField] private GameObject m_ActiveState;

        [Header("Tier visuals")]
        [SerializeField] private GameObject m_Line1;
        [SerializeField] private GameObject m_Line2;
        [SerializeField] private GameObject m_TierNormal;
        [SerializeField] private GameObject m_TierCompleted;
        [SerializeField] private TMP_Text m_TaskNumber;

        [Header("Reward visuals")]
        [SerializeField] private RewardSlotView m_RewardSlot;
        [SerializeField] private GameObject m_LockedState;
        [SerializeField] private GameObject m_CompletedState;

        private CollectorTaskState m_State;
        private bool m_IsSelected;
        private RewardDataSO m_BoundReward;
        private bool m_HasBoundReward;

        private void Awake()
        {
            // 行模板在真正绑定前先保持安全的默认状态，避免三套状态图层同时可见。
            SetState(CollectorTaskState.Locked);
            SetListPosition(0, 0);
        }

        private void OnDisable()
        {
            // RewardSlotView may be reset while its parent page is hidden; force a fresh
            // presentation the next time this row is shown.
            m_HasBoundReward = false;
        }

        /// <summary>
        /// 一次绑定任务号、奖励、状态和列表位置。
        /// <paramref name="rowIndex"/> 为从 0 开始的行号，<paramref name="rowCount"/>
        /// 为列表总行数。
        /// </summary>
        public void Bind(int tier, RewardDataSO reward, CollectorTaskState state, int rowIndex, int rowCount)
        {
            SetTier(tier);
            SetReward(reward);
            SetState(state);
            SetListPosition(rowIndex, rowCount);
        }

        /// <summary>只更新左侧档位数字。</summary>
        public void SetTier(int tier)
        {
            if (m_TaskNumber != null)
                m_TaskNumber.SetText(tier.ToString());
        }

        /// <summary>将奖励数据交给已有的 RewardSlotView 负责展示。</summary>
        public void SetReward(RewardDataSO reward)
        {
            if (m_HasBoundReward && ReferenceEquals(m_BoundReward, reward)) return;
            m_BoundReward = reward;
            m_HasBoundReward = true;
            if (m_RewardSlot != null)
                m_RewardSlot.Bind(reward);
        }

        /// <summary>
        /// 设置任务状态。状态对象互斥：Locked、Select/Active、Complete 三者只显示一个；
        /// Tier 下的 Normal/Completed 也会同步切换。
        /// </summary>
        public void SetState(CollectorTaskState state)
        {
            m_State = state;
            m_IsSelected = state == CollectorTaskState.Active;

            SetActive(m_LockedState, state == CollectorTaskState.Locked);
            SetActive(m_ActiveState, m_IsSelected);
            SetActive(m_CompletedState, state == CollectorTaskState.Completed);
            SetActive(m_TierNormal, state != CollectorTaskState.Completed);
            SetActive(m_TierCompleted, state == CollectorTaskState.Completed);
        }

        /// <summary>
        /// 设置当前行是否被选中。通常由主页面在点击/聚焦任务时调用；不会改变锁定或完成状态。
        /// </summary>
        public void SetSelected(bool selected)
        {
            m_IsSelected = selected;
            SetActive(m_ActiveState, selected);
        }

        /// <summary>
        /// 配置行与相邻行的连接线。
        /// N 行列表中：第一行隐藏 Line_1，最后一行隐藏 Line_2；单行时两条都隐藏。
        /// 非法位置会安全地隐藏两条线。
        /// </summary>
        public void SetListPosition(int rowIndex, int rowCount)
        {
            bool valid = rowCount > 0 && rowIndex >= 0 && rowIndex < rowCount;
            bool hasPrevious = valid && rowIndex > 0;
            bool hasNext = valid && rowIndex < rowCount - 1;
            SetActive(m_Line1, hasPrevious);
            SetActive(m_Line2, hasNext);
        }

        public CollectorTaskState State => m_State;
        public bool IsSelected => m_IsSelected;

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
