using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Lokas.Activities.Collector.UI
{
    /// <summary>
    /// Collector 首页摘要卡片。
    ///
    /// 所有 UI 引用都由当前 Prefab 在 Inspector 中显式绑定；组件不通过名称、层级路径或
    /// 运行时添加组件来补齐引用。奖励切换使用两个预制好的 RewardSlotView：当前槽位向上
    /// 淡出后，下一槽位从零缩放到其预制体比例。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CollectorHomeWidget : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private Image m_Background;
        [SerializeField] private Button m_OpenButton;
        [SerializeField] private Button m_CollectButton;

        [Header("Progress")]
        [SerializeField] private Image m_CollectibleIcon;
        [SerializeField] private TMP_Text m_Progress;
        [SerializeField] private Image m_ProgressFill;
        [SerializeField, Min(0f)] private float m_ProgressAnimationDuration = 0.25f;
        [SerializeField] private Ease m_ProgressAnimationEase = Ease.OutCubic;

        [Header("Reward transition")]
        [FormerlySerializedAs("m_RewardSlot")]
        [SerializeField] private RewardSlotView m_RewardSlot1;
        [SerializeField] private RectTransform m_RewardSlot1Root;
        [SerializeField] private CanvasGroup m_RewardSlot1CanvasGroup;
        [SerializeField] private RewardSlotView m_RewardSlot2;
        [SerializeField] private RectTransform m_RewardSlot2Root;
        [SerializeField] private CanvasGroup m_RewardSlot2CanvasGroup;
        [SerializeField, Min(0f)] private float m_RewardExitDuration = 0.25f;
        [SerializeField, Min(0f)] private float m_RewardEnterDuration = 0.25f;
        [SerializeField, Min(0f)] private float m_RewardMoveUpDistance = 60f;
        [SerializeField] private Ease m_RewardExitEase = Ease.OutSine;
        [SerializeField] private Ease m_RewardEnterEase = Ease.OutBack;

        [Header("Timer")]
        [SerializeField] private Image m_ClockIcon;
        [Tooltip("绑定 TimerBox/TimeText。保留字段名以兼容现有 Prefab 序列化。")]
        [SerializeField] private TMP_Text m_Countdown;

        /// <summary>
        /// 首页宿主收到收集卡片点击后打开 Collector 主页面。
        /// Widget 本身只发出导航意图，具体 UIForm 打开由 MainUIPanel 绑定处理。
        /// </summary>
        public event Action MainPanelRequested;

        private RewardVisual m_VisibleReward;
        private RewardVisual m_HiddenReward;
        private Tween m_RewardTransition;
        private RewardDataSO m_CurrentRewardData;
        private RewardDataSO m_NextRewardData;
        private RewardDataSO m_PendingCurrentRewardData;
        private RewardDataSO m_PendingNextRewardData;
        private Tween m_ProgressTween;
        private bool m_RewardVisualsInitialized;
        private bool m_HasRewards;
        private bool m_HasPendingRewards;
        private bool m_ProgressInitialized;
        private bool m_HasProgressTarget;
        private float m_ProgressTarget;

        /// <summary>
        /// 一次刷新首页卡片。初次绑定不会播放切换动画；后续奖励数据变化默认播放双槽切换。
        /// 需要无动画刷新时，将 <paramref name="animateRewardTransition"/> 传为 false。
        /// </summary>
        public void Bind(Sprite collectibleIcon, int currentProgress, int requiredProgress,
            RewardDataSO currentReward, RewardDataSO nextReward, string countdown,
            bool animateRewardTransition = true, bool animateProgress = true)
        {
            SetCollectibleIcon(collectibleIcon);
            SetProgress(currentProgress, requiredProgress, animateProgress);
            SetCountdown(countdown);
            SetRewards(currentReward, nextReward, animateRewardTransition);
        }

        /// <summary>设置首页收集图标。</summary>
        public void SetCollectibleIcon(Sprite icon)
        {
            // 配置未指定图标时保留设计师在 Prefab 上制作的默认图，不把它清空。
            if (m_CollectibleIcon == null || icon == null) return;
            m_CollectibleIcon.sprite = icon;
            m_CollectibleIcon.enabled = icon != null;
        }

        /// <summary>设置进度文本与进度条。</summary>
        public void SetProgress(int currentProgress, int requiredProgress, bool animate = true)
        {
            if (m_Progress != null)
                m_Progress.SetText("{0}/{1}", currentProgress, requiredProgress);

            float fill = requiredProgress > 0
                ? Mathf.Clamp01((float)currentProgress / requiredProgress)
                : 0f;
            SetProgressFill(fill, animate);
        }

        /// <summary>设置 TimerBox/TimeText 的显示内容。</summary>
        public void SetCountdown(string countdown)
        {
            if (m_Countdown != null)
                m_Countdown.SetText(countdown ?? string.Empty);
        }

        /// <summary>
        /// 设置当前奖励和下一奖励。
        /// 当已有奖励且允许动画时：旧槽位上移淡出，新槽位随后从零缩放进入；动画结束后交换槽位角色。
        /// </summary>
        public void SetRewards(RewardDataSO currentReward, RewardDataSO nextReward,
            bool animateTransition = true)
        {
            EnsureRewardVisualsInitialized();

            bool isSameData = m_HasRewards
                && m_CurrentRewardData == currentReward
                && m_NextRewardData == nextReward;
            if (isSameData) return;

            // A change to only the preview/next reward is not a task transition. Keep the
            // visible current slot stable and refresh the hidden preview without replaying
            // the exit/enter animation.
            if (m_HasRewards && m_RewardTransition == null && m_CurrentRewardData == currentReward)
            {
                m_HiddenReward.Slot?.Bind(nextReward);
                m_NextRewardData = nextReward;
                return;
            }

            if (!m_HasRewards || !animateTransition || !CanAnimateRewards)
            {
                StopRewardTransition();
                ApplyRewardsImmediately(currentReward, nextReward);
                return;
            }

            // StateChanged can arrive while the previous visual transition is still running.
            // Queue the newest pair and start one follow-up transition after the current one;
            // killing/restarting the sequence on every fact was the source of visible stutter.
            if (m_RewardTransition != null)
            {
                m_PendingCurrentRewardData = currentReward;
                m_PendingNextRewardData = nextReward;
                m_HasPendingRewards = true;
                return;
            }

            m_HiddenReward.Slot?.Bind(currentReward);
            m_CurrentRewardData = currentReward;
            m_NextRewardData = nextReward;
            PlayRewardTransition(nextReward);
        }

        private void Awake()
        {
            m_OpenButton?.AddSafeClick(OnClickCollect);
            m_CollectButton?.AddSafeClick(OnClickCollect);
            EnsureRewardVisualsInitialized();
            ResetRewardVisuals();
        }

        private void OnClickCollect()
        {
            MainPanelRequested?.Invoke();
        }


        private void OnDisable()
        {
            // A panel can be hidden while the two-slot transition is running. The data
            // fields already contain the newest requested pair, so settle that pair before
            // killing the tween; otherwise the next Bind may see the same data and skip the
            // refresh while the old visible slot is still on screen.
            RewardDataSO settledCurrent = m_HasPendingRewards
                ? m_PendingCurrentRewardData
                : m_CurrentRewardData;
            RewardDataSO settledNext = m_HasPendingRewards
                ? m_PendingNextRewardData
                : m_NextRewardData;
            bool settleRewards = m_RewardVisualsInitialized && m_HasRewards;
            StopRewardTransition();
            StopProgressTween();
            m_ProgressInitialized = false;
            m_HasProgressTarget = false;
            if (settleRewards)
                ApplyRewardsImmediately(settledCurrent, settledNext);
            else if (m_RewardVisualsInitialized)
                ResetRewardVisuals();
        }

        private void OnDestroy()
        {
            StopRewardTransition();
            StopProgressTween();
        }

        private void EnsureRewardVisualsInitialized()
        {
            if (m_RewardVisualsInitialized) return;

            m_VisibleReward = CreateRewardVisual(
                m_RewardSlot1,
                m_RewardSlot1Root,
                m_RewardSlot1CanvasGroup);
            m_HiddenReward = CreateRewardVisual(
                m_RewardSlot2,
                m_RewardSlot2Root,
                m_RewardSlot2CanvasGroup);
            m_RewardVisualsInitialized = true;
        }

        private void ApplyRewardsImmediately(RewardDataSO currentReward, RewardDataSO nextReward)
        {
            m_VisibleReward.Slot?.Bind(currentReward);
            m_HiddenReward.Slot?.Bind(nextReward);
            m_CurrentRewardData = currentReward;
            m_NextRewardData = nextReward;
            m_HasRewards = true;
            m_HasPendingRewards = false;
            m_PendingCurrentRewardData = null;
            m_PendingNextRewardData = null;
            ResetRewardVisuals();
        }

        private void PlayRewardTransition(RewardDataSO nextReward)
        {
            RewardVisual exitingReward = m_VisibleReward;
            RewardVisual enteringReward = m_HiddenReward;

            PrepareVisibleReward(exitingReward);
            PrepareEnteringReward(enteringReward);

            Sequence sequence = DOTween.Sequence();
            m_RewardTransition = sequence;
            AppendExitAnimation(sequence, exitingReward);
            AppendEnterAnimation(sequence, enteringReward);
            sequence.OnComplete(() => CompleteRewardTransition(
                exitingReward,
                enteringReward,
                nextReward));
            sequence.OnKill(() =>
            {
                if (ReferenceEquals(m_RewardTransition, sequence))
                    m_RewardTransition = null;
            });
        }

        private void AppendExitAnimation(Sequence sequence, RewardVisual reward)
        {
            bool hasAnimation = false;
            float duration = Mathf.Max(0f, m_RewardExitDuration);

            if (reward.Root != null)
            {
                sequence.Append(reward.Root
                    .DOAnchorPos(reward.BasePosition + Vector2.up * m_RewardMoveUpDistance, duration)
                    .SetEase(m_RewardExitEase));
                hasAnimation = true;
            }

            if (reward.CanvasGroup != null)
            {
                Tween fade = reward.CanvasGroup
                    .DOFade(0f, duration)
                    .SetEase(Ease.InSine);
                if (hasAnimation)
                    sequence.Join(fade);
                else
                    sequence.Append(fade);
                hasAnimation = true;
            }

            if (!hasAnimation)
                sequence.AppendInterval(duration);
        }

        private void AppendEnterAnimation(Sequence sequence, RewardVisual reward)
        {
            float duration = Mathf.Max(0f, m_RewardEnterDuration);
            if (reward.Root != null)
            {
                sequence.Append(reward.Root
                    .DOScale(reward.BaseScale, duration)
                    .SetEase(m_RewardEnterEase));
            }
            else
            {
                sequence.AppendInterval(duration);
            }
        }

        private void CompleteRewardTransition(
            RewardVisual exitingReward,
            RewardVisual enteringReward,
            RewardDataSO nextReward)
        {
            m_VisibleReward = enteringReward;
            m_HiddenReward = exitingReward;
            m_HiddenReward.Slot?.Bind(nextReward);
            ResetRewardVisuals();
            m_RewardTransition = null;

            if (m_HasPendingRewards)
            {
                RewardDataSO pendingCurrent = m_PendingCurrentRewardData;
                RewardDataSO pendingNext = m_PendingNextRewardData;
                m_HasPendingRewards = false;
                m_PendingCurrentRewardData = null;
                m_PendingNextRewardData = null;
                SetRewards(pendingCurrent, pendingNext, animateTransition: true);
            }
        }

        private void ResetRewardVisuals()
        {
            ApplyVisualState(m_VisibleReward, true);
            ApplyVisualState(m_HiddenReward, false);
        }

        private static void ApplyVisualState(RewardVisual reward, bool visible)
        {
            if (reward.Root != null)
            {
                reward.Root.anchoredPosition = reward.BasePosition;
                reward.Root.localScale = visible ? reward.BaseScale : Vector3.zero;
            }

            if (reward.CanvasGroup != null)
            {
                reward.CanvasGroup.alpha = visible ? 1f : 0f;
                reward.CanvasGroup.interactable = visible;
                reward.CanvasGroup.blocksRaycasts = visible;
            }
        }

        private static void PrepareVisibleReward(RewardVisual reward)
        {
            ApplyVisualState(reward, true);
            if (reward.CanvasGroup != null)
            {
                reward.CanvasGroup.interactable = false;
                reward.CanvasGroup.blocksRaycasts = false;
            }
        }

        private static void PrepareEnteringReward(RewardVisual reward)
        {
            if (reward.Root != null)
            {
                reward.Root.anchoredPosition = reward.BasePosition;
                reward.Root.localScale = Vector3.zero;
            }

            if (reward.CanvasGroup != null)
            {
                reward.CanvasGroup.alpha = 1f;
                reward.CanvasGroup.interactable = false;
                reward.CanvasGroup.blocksRaycasts = false;
            }
        }

        private void StopRewardTransition()
        {
            m_RewardTransition?.Kill();
            m_RewardTransition = null;
            m_HasPendingRewards = false;
            m_PendingCurrentRewardData = null;
            m_PendingNextRewardData = null;
        }

        private bool CanAnimateRewards =>
            m_VisibleReward.Slot != null &&
            m_VisibleReward.Root != null &&
            m_VisibleReward.CanvasGroup != null &&
            m_HiddenReward.Slot != null &&
            m_HiddenReward.Root != null &&
            m_HiddenReward.CanvasGroup != null;

        private void SetProgressFill(float fill, bool animate)
        {
            if (m_ProgressFill == null) return;

            fill = Mathf.Clamp01(fill);
            // EntriesChanged can mirror StateChanged in the same frame. If the target did
            // not change, leave the existing tween alone instead of restarting it.
            if (animate && m_HasProgressTarget && Mathf.Abs(m_ProgressTarget - fill) <= 0.0001f)
                return;
            m_ProgressTarget = fill;
            m_HasProgressTarget = true;
            if (!animate || !m_ProgressInitialized || !isActiveAndEnabled ||
                m_ProgressAnimationDuration <= 0f)
            {
                StopProgressTween();
                ApplyProgressFill(fill);
                m_ProgressInitialized = true;
                return;
            }

            float start = GetProgressFill();
            if (Mathf.Abs(start - fill) <= 0.0001f)
            {
                m_ProgressInitialized = true;
                return;
            }

            StopProgressTween();
            m_ProgressTween = DOTween.To(
                    () => start,
                    value =>
                    {
                        start = value;
                        ApplyProgressFill(value);
                    },
                    fill,
                    m_ProgressAnimationDuration)
                .SetEase(m_ProgressAnimationEase)
                .SetTarget(this)
                .OnComplete(() => m_ProgressTween = null)
                .OnKill(() => m_ProgressTween = null);
            m_ProgressInitialized = true;
        }

        private float GetProgressFill()
        {
            if (m_ProgressFill == null) return 0f;
            if (m_ProgressFill.type == Image.Type.Filled)
                return Mathf.Clamp01(m_ProgressFill.fillAmount);
            return Mathf.Clamp01(m_ProgressFill.rectTransform.anchorMax.x);
        }

        private void ApplyProgressFill(float fill)
        {
            if (m_ProgressFill == null) return;
            fill = Mathf.Clamp01(fill);
            if (m_ProgressFill.type == Image.Type.Filled)
            {
                m_ProgressFill.fillAmount = fill;
                return;
            }

            RectTransform rect = m_ProgressFill.rectTransform;
            Vector2 anchorMax = rect.anchorMax;
            anchorMax.x = fill;
            rect.anchorMax = anchorMax;
        }

        private void StopProgressTween()
        {
            m_ProgressTween?.Kill();
            m_ProgressTween = null;
        }

        private static RewardVisual CreateRewardVisual(
            RewardSlotView slot,
            RectTransform root,
            CanvasGroup canvasGroup)
        {
            return new RewardVisual
            {
                Slot = slot,
                Root = root,
                CanvasGroup = canvasGroup,
                BasePosition = root != null ? root.anchoredPosition : Vector2.zero,
                BaseScale = root != null ? root.localScale : Vector3.one
            };
        }

        private struct RewardVisual
        {
            public RewardSlotView Slot;
            public RectTransform Root;
            public CanvasGroup CanvasGroup;
            public Vector2 BasePosition;
            public Vector3 BaseScale;
        }
    }
}
