using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.WinStreak.UI
{
    /// <summary>HEXA STREAK 主页面：累计进度映射到统一塔段预制体。</summary>
    public sealed class WinStreakMainUIPanel : UGuiForm
    {
        private const float ProgressSecondsPerLevel = 0.18f;
        private const float MinProgressAnimationDuration = 0.45f;
        private const float MaxProgressAnimationDuration = 2.5f;
        private const float ProgressViewportFocus = 0.45f;

        [Header("Header")]
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_Countdown;
        [SerializeField] private TMP_Text m_Progress;
        [SerializeField] private TMP_Text m_NextCheckpoint;
        [SerializeField] private Button m_DetailsButton;
        [SerializeField] private Button m_CloseButton;

        [Header("Tower")]
        [SerializeField] private ScrollRect m_CheckpointScroll;
        [SerializeField] private Transform m_CheckpointContent;
        [SerializeField] private WinStreakCheckpointRowView m_CheckpointRowTemplate;
        [SerializeField] private RectTransform m_BackgroundLayer;
        [SerializeField, Range(0f, 1f)] private float m_BackgroundParallax = 0.35f;
        [SerializeField] private Vector2 m_CharacterPositionOffset;

        [Header("Animation")]
        [SerializeField, Min(1.05f)] private float m_ProgressScrollDurationMultiplier = 1.25f;
        [SerializeField, Min(0.1f)] private float m_TutorialScrollDuration = 2.5f;

        private readonly List<WinStreakCheckpointRowView> m_Rows =
            new List<WinStreakCheckpointRowView>();
        private WinStreakActivityModule m_Module;
        private RectTransform m_Character;
        private CanvasGroup m_PageCanvasGroup;
        private Tween m_ProgressTween;
        private Tween m_TutorialTween;
        private bool m_IsClaiming;
        private bool m_IsAnimatingProgress;
        private bool m_IsWaitingForTutorialRules;
        private bool m_IsPlayingTutorialScroll;
        private bool m_HasDisplayedProgress;
        private bool m_ContentPrepared;
        private bool m_BackgroundBaseCached;
        private bool m_CharacterInitialPositionCached;
        private bool m_CharacterProgressOffsetCached;
        private Vector2 m_BackgroundBasePosition;
        private Vector2 m_CharacterInitialPosition;
        private float m_CharacterProgressOffsetY;
        private float m_DisplayedProgress;
        private float m_AnimatedProgress;
        private float m_NextRefreshAt;
        private int m_ProgressTweenVersion;
        private int m_TutorialTweenVersion;
        private int m_AnimationTargetProgress;
        private bool m_ReportedMissingBindings;

        protected override void OnInit(object userData)
        {
            ResolveBindings();
            PrepareContent();
            m_Module = ResolveModule(userData);
            int checkpointCount = m_Module?.Config?.Checkpoints?.Count ?? 0;
            if (checkpointCount > 0) EnsureRows(checkpointCount + 1);

            // TowerView instances must exist before UGuiForm initializes localization/fonts.
            base.OnInit(userData);
            m_PageCanvasGroup = GetComponent<CanvasGroup>();

            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
            if (m_DetailsButton != null) m_DetailsButton.AddSafeClick(OnClickDetails);
            if (m_CheckpointScroll != null) m_CheckpointScroll.onValueChanged.AddListener(OnScrollValueChanged);
            CacheBackgroundPosition();
            ReportMissingBindingsOnce();
        }

        protected override void OnOpen(object userData)
        {
            CancelFirstEntryTutorial(restoreInteraction: false);
            CancelProgressAnimation(restoreInteraction: true);
            m_Module = ResolveModule(userData);
            if (m_Module != null) m_Module.StateChanged += Refresh;
            m_IsClaiming = false;
            m_NextRefreshAt = 0f;
            m_HasDisplayedProgress = false;
            m_CharacterProgressOffsetCached = false;
            m_CharacterProgressOffsetY = 0f;
            if (m_Character != null && m_CharacterInitialPositionCached)
                m_Character.anchoredPosition = m_CharacterInitialPosition;
            CacheBackgroundPosition();
            base.OnOpen(userData);
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            if (m_Module != null) m_Module.StateChanged -= Refresh;
            CancelFirstEntryTutorial(restoreInteraction: false);
            CancelProgressAnimation(restoreInteraction: true);
            m_HasDisplayedProgress = false;
            m_Module = null;
            m_IsClaiming = false;
            RestoreBackgroundPosition();
            base.OnClose(isShutdown, userData);
        }

        private void Update()
        {
            if (m_Module == null || Time.unscaledTime < m_NextRefreshAt) return;
            m_NextRefreshAt = Time.unscaledTime + 1f;
            RefreshHeader(m_Module.GetSnapshot());
        }

        private void Refresh()
        {
            if (m_Module == null) return;

            WinStreakSnapshot snapshot = m_Module.GetSnapshot();
            RefreshHeader(snapshot);
            RefreshProgress(snapshot);
        }

        private void RefreshHeader(WinStreakSnapshot snapshot)
        {
            if (snapshot == null) return;
            SetText(m_Title, snapshot.DisplayName);
            SetText(m_Countdown, FormatCountdown(snapshot.EndUtc - DateTimeOffset.UtcNow));
            SetText(m_Progress, "{0}/{1}", snapshot.CurrentProgress, snapshot.MaxProgress);
            if (m_NextCheckpoint != null)
            {
                m_NextCheckpoint.SetText(snapshot.NextCheckpoint == null
                    ? string.Empty
                    : string.Concat("NEXT ", snapshot.NextCheckpoint.Checkpoint));
            }
        }

        private void RefreshProgress(WinStreakSnapshot snapshot)
        {
            int targetProgress = snapshot.CurrentProgress;
            if (!m_HasDisplayedProgress)
            {
                bool animateIncrease = Application.isPlaying &&
                    targetProgress > snapshot.LastCheckedProgress;
                bool showFirstEntryTutorial = Application.isPlaying && !snapshot.IsTutorialComplete;
                float initialProgress = animateIncrease
                    ? Mathf.Clamp(snapshot.LastCheckedProgress, 0, targetProgress)
                    : targetProgress;

                BindTowerSegments(snapshot, initialProgress);
                RebuildTowerLayout();
                CacheCharacterProgressOffset(snapshot);
                ApplyAnimatedProgress(initialProgress);
                ScrollToProgressImmediate(initialProgress);
                m_HasDisplayedProgress = true;
                m_DisplayedProgress = initialProgress;

                if (showFirstEntryTutorial)
                    BeginFirstEntryTutorial();
                else if (animateIncrease)
                    StartProgressAnimation(initialProgress, targetProgress);
                else
                    CompleteProgressImmediately(targetProgress);
                return;
            }

            if (m_IsWaitingForTutorialRules || m_IsPlayingTutorialScroll)
            {
                BindTowerSegments(snapshot, m_DisplayedProgress);
                RebuildTowerLayout();
                CacheCharacterProgressOffset(snapshot);
                ApplyAnimatedProgress(m_DisplayedProgress);
                return;
            }

            if (m_IsAnimatingProgress && targetProgress == m_AnimationTargetProgress)
                return;

            float displayedProgress = m_IsAnimatingProgress ? m_AnimatedProgress : m_DisplayedProgress;
            bool progressChanged = !Mathf.Approximately(displayedProgress, targetProgress);
            bool animate = Application.isPlaying && targetProgress > displayedProgress;
            CancelProgressAnimation(restoreInteraction: !animate);

            BindTowerSegments(snapshot, animate ? displayedProgress : targetProgress);
            RebuildTowerLayout();
            CacheCharacterProgressOffset(snapshot);

            if (animate)
                StartProgressAnimation(displayedProgress, targetProgress);
            else
            {
                if (progressChanged) ScrollToProgressImmediate(targetProgress);
                CompleteProgressImmediately(targetProgress);
            }
        }

        /// <summary>
        /// 按“顶部奖励 -> 中间奖励 -> 底部无奖励”顺序构造塔。每个节点都来自同一个
        /// WinStreakCheckpointRowView 模板，只通过 TowerType 区分视觉结构。
        /// </summary>
        private void BindTowerSegments(WinStreakSnapshot snapshot, float displayedProgress)
        {
            IReadOnlyList<WinStreakCheckpointSnapshot> checkpoints = snapshot.Checkpoints;
            int checkpointCount = checkpoints?.Count ?? 0;
            int requiredRows = checkpointCount > 0 ? checkpointCount + 1 : 0;
            EnsureRows(requiredRows);

            for (int index = 0; index < m_Rows.Count; index++)
            {
                WinStreakCheckpointRowView row = m_Rows[index];
                bool visible = index < requiredRows;
                row.gameObject.SetActive(visible);
                if (!visible) continue;
                row.transform.SetSiblingIndex(index);

                if (index == 0)
                {
                    WinStreakCheckpointSnapshot top = checkpoints[checkpointCount - 1];
                    int claimCheckpoint = top.Checkpoint;
                    row.name = "TowerTop";
                    row.SetTowerType(TowerType.Top);
                    row.Bind(top, displayedProgress, top.Checkpoint, top.Checkpoint,
                        () => ClaimAsync(claimCheckpoint).Forget(Debug.LogException));
                    continue;
                }

                if (index < checkpointCount)
                {
                    int checkpointIndex = checkpointCount - 1 - index;
                    WinStreakCheckpointSnapshot checkpoint = checkpoints[checkpointIndex];
                    WinStreakCheckpointSnapshot nextCheckpoint = checkpoints[checkpointIndex + 1];
                    int claimCheckpoint = checkpoint.Checkpoint;
                    row.name = string.Concat("TowerMain_", checkpoint.Checkpoint);
                    row.SetTowerType(TowerType.Main);
                    row.Bind(checkpoint, displayedProgress, checkpoint.Checkpoint,
                        nextCheckpoint.Checkpoint,
                        () => ClaimAsync(claimCheckpoint).Forget(Debug.LogException));
                    continue;
                }

                // TowerBottom intentionally has no reward. Its marker is considered claimed by
                // default, and its local 1->2 track only changes when the cumulative value reaches 2.
                row.name = "TowerBottom";
                row.SetTowerType(TowerType.Bottom);
                row.BindBottom(checkpoints[0].Checkpoint, displayedProgress);
            }
        }

        private void EnsureRows(int count)
        {
            if (m_CheckpointContent == null || m_CheckpointRowTemplate == null) return;

            while (m_Rows.Count < count)
            {
                WinStreakCheckpointRowView row = Instantiate(m_CheckpointRowTemplate, m_CheckpointContent);
                row.name = string.Concat("WinStreakTowerRow_", m_Rows.Count + 1);
                row.gameObject.SetActive(true);
                m_Rows.Add(row);
            }
        }

        private void StartProgressAnimation(float fromProgress, int targetProgress)
        {
            if (targetProgress <= fromProgress)
            {
                CompleteProgressImmediately(targetProgress);
                return;
            }

            int version = ++m_ProgressTweenVersion;
            m_IsAnimatingProgress = true;
            m_AnimationTargetProgress = targetProgress;
            m_AnimatedProgress = fromProgress;
            SetPageInteraction(false);
            ApplyAnimatedProgress(fromProgress);

            float duration = Mathf.Clamp((targetProgress - fromProgress) * ProgressSecondsPerLevel,
                MinProgressAnimationDuration, MaxProgressAnimationDuration);
            Tween progressTween = DOTween.To(
                    () => m_AnimatedProgress,
                    ApplyAnimatedProgress,
                    targetProgress,
                    duration)
                .SetEase(Ease.OutCubic);

            Sequence sequence = DOTween.Sequence();
            sequence.Join(progressTween);
            if (TryGetProgressScrollPosition(targetProgress, out float targetScrollPosition))
            {
                m_CheckpointScroll.StopMovement();
                float scrollDuration = duration * Mathf.Max(1.05f, m_ProgressScrollDurationMultiplier);
                sequence.Join(DOTween.To(
                        () => m_CheckpointScroll.verticalNormalizedPosition,
                        SetScrollNormalizedPosition,
                        targetScrollPosition,
                        scrollDuration)
                    .SetEase(Ease.OutCubic));
            }

            m_ProgressTween = sequence
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() => OnProgressAnimationComplete(version, targetProgress))
                .OnKill(() => OnProgressAnimationKilled(version));
        }

        private void OnProgressAnimationComplete(int version, int targetProgress)
        {
            if (version != m_ProgressTweenVersion) return;
            m_IsAnimatingProgress = false;
            m_ProgressTween = null;
            m_DisplayedProgress = targetProgress;
            ApplyAnimatedProgress(targetProgress);
            SetPageInteraction(true);
            m_Module?.MarkProgressChecked(targetProgress);
        }

        private void OnProgressAnimationKilled(int version)
        {
            if (version != m_ProgressTweenVersion || !m_IsAnimatingProgress) return;
            m_IsAnimatingProgress = false;
            m_ProgressTween = null;
            SetPageInteraction(true);
        }

        private void CancelProgressAnimation(bool restoreInteraction)
        {
            ++m_ProgressTweenVersion;
            Tween tween = m_ProgressTween;
            m_ProgressTween = null;
            m_IsAnimatingProgress = false;
            tween?.Kill();
            if (restoreInteraction) SetPageInteraction(true);
        }

        private void CompleteProgressImmediately(int targetProgress)
        {
            ApplyAnimatedProgress(targetProgress);
            m_DisplayedProgress = targetProgress;
            m_IsAnimatingProgress = false;
            SetPageInteraction(true);
            m_Module?.MarkProgressChecked(targetProgress);
        }

        private void ApplyAnimatedProgress(float progress)
        {
            m_AnimatedProgress = progress;
            foreach (WinStreakCheckpointRowView row in m_Rows)
            {
                if (row != null && row.gameObject.activeSelf) row.SetProgress(progress);
            }

            SetCharacterProgress(progress);
        }

        private void SetPageInteraction(bool enabled)
        {
            if (m_PageCanvasGroup == null) m_PageCanvasGroup = GetComponent<CanvasGroup>();
            if (m_PageCanvasGroup == null) return;
            m_PageCanvasGroup.interactable = enabled;
            // Keep this canvas as the raycast barrier while disabled so input cannot reach the page below it.
            m_PageCanvasGroup.blocksRaycasts = true;
        }

        private void CacheCharacterProgressOffset(WinStreakSnapshot snapshot)
        {
            if (m_CharacterProgressOffsetCached || m_Character == null || snapshot?.Checkpoints == null ||
                snapshot.Checkpoints.Count == 0) return;

            float baseProgress = Mathf.Max(0, snapshot.Checkpoints[0].Checkpoint - 1);
            if (!TryGetProgressAnchoredY(baseProgress, out float basePositionY)) return;
            m_CharacterProgressOffsetY = m_CharacterInitialPosition.y - basePositionY;
            m_CharacterProgressOffsetCached = true;
        }

        private void SetCharacterProgress(float progress)
        {
            if (m_Character == null || !TryGetProgressAnchoredY(progress, out float positionY)) return;
            Vector2 position = m_Character.anchoredPosition;
            position.x = m_CharacterInitialPosition.x + m_CharacterPositionOffset.x;
            position.y = positionY + m_CharacterProgressOffsetY + m_CharacterPositionOffset.y;
            m_Character.anchoredPosition = position;
        }

        private bool TryGetProgressAnchoredY(float progress, out float anchoredY)
        {
            anchoredY = 0f;
            if (!(m_CheckpointContent is RectTransform contentRect) ||
                !TryGetProgressContentLocalY(progress, out float localY)) return false;
            anchoredY = localY - contentRect.rect.yMin;
            return true;
        }

        private bool TryGetProgressContentLocalY(float progress, out float localY)
        {
            localY = 0f;
            if (!(m_CheckpointContent is RectTransform contentRect)) return false;
            if (!TryGetCharacterProgressWorldPosition(progress, out Vector3 worldPosition)) return false;
            localY = contentRect.InverseTransformPoint(worldPosition).y;
            return true;
        }

        /// <summary>
        /// Fill 使用每段固定长度的 ProgressTrack，但相邻 TowerView 的 Track 端点之间有美术留白。
        /// Character 改为在相邻 Checkpoint Marker 间插值，避免到达整数检查点时切换 Track 而跳过留白。
        /// </summary>
        private bool TryGetCharacterProgressWorldPosition(float progress, out Vector3 worldPosition)
        {
            worldPosition = default;
            WinStreakCheckpointRowView row = FindProgressRow(progress);
            if (row == null) return false;

            if (row.Type == TowerType.Top)
                return row.TryGetProgressWorldPosition(row.SegmentEnd, out worldPosition);

            if (!row.TryGetProgressWorldPosition(row.SegmentStart, out Vector3 segmentStart)) return false;
            if (!TryGetCheckpointWorldPosition(row.SegmentEnd, out Vector3 segmentEnd) &&
                !row.TryGetProgressWorldPosition(row.SegmentEnd, out segmentEnd))
                return false;

            float segmentProgress = WinStreakCheckpointRowView.CalculateSegmentProgress(progress,
                row.SegmentStart, row.SegmentEnd);
            worldPosition = Vector3.Lerp(segmentStart, segmentEnd, segmentProgress);
            return true;
        }

        private bool TryGetCheckpointWorldPosition(int checkpoint, out Vector3 worldPosition)
        {
            foreach (WinStreakCheckpointRowView row in m_Rows)
            {
                if (row == null || !row.gameObject.activeSelf) continue;
                bool ownsCheckpoint = row.Type == TowerType.Top
                    ? row.SegmentEnd == checkpoint
                    : row.SegmentStart == checkpoint;
                if (ownsCheckpoint && row.TryGetProgressWorldPosition(checkpoint, out worldPosition)) return true;
            }

            worldPosition = default;
            return false;
        }

        private WinStreakCheckpointRowView FindProgressRow(float progress)
        {
            WinStreakCheckpointRowView bottom = null;
            foreach (WinStreakCheckpointRowView row in m_Rows)
            {
                if (row == null || !row.gameObject.activeSelf) continue;
                if (row.Type == TowerType.Bottom) bottom = row;
                if (row.ContainsProgress(progress)) return row;
            }

            return bottom;
        }

        private void ScrollToProgressImmediate(float progress)
        {
            if (!TryGetProgressScrollPosition(progress, out float targetPosition)) return;
            m_CheckpointScroll.StopMovement();
            SetScrollNormalizedPosition(targetPosition);
        }

        private bool TryGetProgressScrollPosition(float progress, out float normalizedPosition)
        {
            normalizedPosition = 1f;
            if (m_CheckpointScroll == null || !(m_CheckpointContent is RectTransform contentRect) ||
                !TryGetProgressContentLocalY(progress, out float localY)) return false;

            RectTransform viewport = m_CheckpointScroll.viewport ?? m_CheckpointScroll.GetComponent<RectTransform>();
            if (viewport == null) return false;
            float scrollableHeight = Mathf.Max(0f, contentRect.rect.height - viewport.rect.height);
            if (scrollableHeight <= 0f) return true;

            float characterLocalY = localY + m_CharacterProgressOffsetY + m_CharacterPositionOffset.y;
            float distanceFromTop = contentRect.rect.yMax - characterLocalY;
            float targetOffset = Mathf.Clamp(
                distanceFromTop - viewport.rect.height * ProgressViewportFocus,
                0f,
                scrollableHeight);
            normalizedPosition = 1f - targetOffset / scrollableHeight;
            return true;
        }

        private void SetScrollNormalizedPosition(float normalizedPosition)
        {
            if (m_CheckpointScroll == null) return;
            m_CheckpointScroll.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
            ApplyBackgroundParallax();
        }

        private void BeginFirstEntryTutorial()
        {
            if (m_Module == null || m_IsWaitingForTutorialRules || m_IsPlayingTutorialScroll) return;
            m_IsWaitingForTutorialRules = true;
            SetPageInteraction(false);
            m_Module.RulesPageClosed += OnFirstEntryRulesClosed;
            OpenFirstEntryRulesAsync(m_Module).Forget();
        }

        private async UniTask OpenFirstEntryRulesAsync(WinStreakActivityModule module)
        {
            try
            {
                await module.OpenRulesAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (module != m_Module || !m_IsWaitingForTutorialRules) return;
                module.RulesPageClosed -= OnFirstEntryRulesClosed;
                m_IsWaitingForTutorialRules = false;
                ResumeAfterFirstEntryTutorial();
            }
        }

        private void OnFirstEntryRulesClosed()
        {
            if (!m_IsWaitingForTutorialRules) return;
            if (m_Module != null) m_Module.RulesPageClosed -= OnFirstEntryRulesClosed;
            m_IsWaitingForTutorialRules = false;
            StartTutorialRewardScroll();
        }

        private void StartTutorialRewardScroll()
        {
            int version = ++m_TutorialTweenVersion;
            m_IsPlayingTutorialScroll = true;
            SetPageInteraction(false);
            RebuildTowerLayout();

            if (m_CheckpointScroll == null)
            {
                OnTutorialRewardScrollComplete(version);
                return;
            }

            m_CheckpointScroll.StopMovement();
            SetScrollNormalizedPosition(1f);
            m_TutorialTween = DOTween.To(
                    () => m_CheckpointScroll.verticalNormalizedPosition,
                    SetScrollNormalizedPosition,
                    0f,
                    Mathf.Max(0.1f, m_TutorialScrollDuration))
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() => OnTutorialRewardScrollComplete(version));
        }

        private void OnTutorialRewardScrollComplete(int version)
        {
            if (version != m_TutorialTweenVersion || !m_IsPlayingTutorialScroll) return;
            m_TutorialTween = null;
            m_IsPlayingTutorialScroll = false;
            m_Module?.MarkTutorialComplete();
            ResumeAfterFirstEntryTutorial();
        }

        private void ResumeAfterFirstEntryTutorial()
        {
            if (m_Module == null)
            {
                SetPageInteraction(true);
                return;
            }

            WinStreakSnapshot snapshot = m_Module.GetSnapshot();
            RefreshHeader(snapshot);
            float displayedProgress = m_DisplayedProgress;
            int targetProgress = snapshot.CurrentProgress;
            BindTowerSegments(snapshot, displayedProgress);
            RebuildTowerLayout();
            CacheCharacterProgressOffset(snapshot);
            ApplyAnimatedProgress(displayedProgress);

            if (Application.isPlaying && targetProgress > displayedProgress)
                StartProgressAnimation(displayedProgress, targetProgress);
            else
            {
                ScrollToProgressImmediate(targetProgress);
                CompleteProgressImmediately(targetProgress);
            }
        }

        private void CancelFirstEntryTutorial(bool restoreInteraction)
        {
            ++m_TutorialTweenVersion;
            if (m_Module != null) m_Module.RulesPageClosed -= OnFirstEntryRulesClosed;
            m_IsWaitingForTutorialRules = false;
            m_IsPlayingTutorialScroll = false;
            Tween tween = m_TutorialTween;
            m_TutorialTween = null;
            tween?.Kill();
            if (restoreInteraction) SetPageInteraction(true);
        }

        private void RebuildTowerLayout()
        {
            if (m_CheckpointContent is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Canvas.ForceUpdateCanvases();
        }

        private void ResolveBindings()
        {
            if (m_Title == null) m_Title = FindComponent<TMP_Text>("Title");
            if (m_Countdown == null) m_Countdown = FindComponent<TMP_Text>("Countdown");
            if (m_Progress == null) m_Progress = FindComponent<TMP_Text>("Progress");
            if (m_NextCheckpoint == null) m_NextCheckpoint = FindComponent<TMP_Text>("NextCheckpoint");
            if (m_DetailsButton == null) m_DetailsButton = FindComponent<Button>("DetailBtn");
            if (m_CloseButton == null) m_CloseButton = FindComponent<Button>("Btn_Close");
            if (m_CheckpointScroll == null) m_CheckpointScroll = GetComponentInChildren<ScrollRect>(true);

            if (m_CheckpointContent == null && m_CheckpointScroll != null)
            {
                Transform content = m_CheckpointScroll.content;
                m_CheckpointContent = content != null ? content : FindTransform("CheckpointContent");
            }

            if (m_CheckpointRowTemplate == null && m_CheckpointContent != null)
            {
                Transform template = m_CheckpointContent.Find("CheckpointRowTemplate");
                if (template != null) m_CheckpointRowTemplate = template.GetComponent<WinStreakCheckpointRowView>();
            }

            if (m_BackgroundLayer == null)
            {
                Transform background = FindTransform("BackgroundLayer");
                if (background != null) m_BackgroundLayer = background as RectTransform;
            }

            if (m_Character == null)
            {
                Transform character = FindTransform("Character");
                if (character != null) m_Character = character as RectTransform;
            }

            if (m_Character != null && !m_CharacterInitialPositionCached)
            {
                m_CharacterInitialPosition = m_Character.anchoredPosition;
                m_CharacterInitialPositionCached = true;
            }
        }

        private void PrepareContent()
        {
            if (m_ContentPrepared || m_CheckpointContent == null) return;
            m_ContentPrepared = true;

            // The authored prefab stays untouched. Runtime layout owns row heights so every
            // cloned TowerView uses its LayoutElement instead of overlapping adjacent rows.
            VerticalLayoutGroup layout = m_CheckpointContent.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 0f;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }

            // Character/art overlays are authored inside Content but must not consume a vertical
            // layout slot. Runtime-only LayoutElement keeps the authored prefab untouched.
            for (int index = 0; index < m_CheckpointContent.childCount; index++)
            {
                Transform child = m_CheckpointContent.GetChild(index);
                WinStreakCheckpointRowView row = child.GetComponent<WinStreakCheckpointRowView>();
                if (row != null)
                {
                    if (child.name == "CheckpointRowTemplate") child.gameObject.SetActive(false);
                    else if (!m_Rows.Contains(row)) m_Rows.Add(row);
                    continue;
                }

                if (child.name != "Character") continue;
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element == null) element = child.gameObject.AddComponent<LayoutElement>();
                element.ignoreLayout = true;
            }

            CacheBackgroundPosition();
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            ApplyBackgroundParallax();
        }

        private void CacheBackgroundPosition()
        {
            if (m_BackgroundLayer == null || m_BackgroundBaseCached) return;
            m_BackgroundBasePosition = m_BackgroundLayer.anchoredPosition;
            m_BackgroundBaseCached = true;
        }

        private void RestoreBackgroundPosition()
        {
            if (m_BackgroundLayer == null || !m_BackgroundBaseCached) return;
            m_BackgroundLayer.anchoredPosition = m_BackgroundBasePosition;
        }

        private void ApplyBackgroundParallax()
        {
            if (m_BackgroundLayer == null || !(m_CheckpointContent is RectTransform contentRect) ||
                !m_BackgroundBaseCached) return;

            float contentOffset = contentRect.anchoredPosition.y;
            if (m_BackgroundLayer.IsChildOf(contentRect))
            {
                // The parent already moves by contentOffset. Apply only the delta needed to make
                // the total movement a fraction of the tower movement.
                m_BackgroundLayer.anchoredPosition = m_BackgroundBasePosition +
                    Vector2.up * (contentOffset * (m_BackgroundParallax - 1f));
            }
            else
            {
                m_BackgroundLayer.anchoredPosition = m_BackgroundBasePosition +
                    Vector2.up * (contentOffset * m_BackgroundParallax);
            }
        }

        private void OnClickDetails()
        {
            if (m_Module != null) m_Module.OpenRulesAsync().Forget(Debug.LogException);
        }

        private async UniTask ClaimAsync(int checkpoint)
        {
            if (m_Module == null || m_IsClaiming) return;
            m_IsClaiming = true;
            Refresh();
            try
            {
                ActivityRewardReceipt receipt = await m_Module.ClaimAsync(checkpoint);
                if (receipt.Status != ActivityRewardStatus.Granted &&
                    receipt.Status != ActivityRewardStatus.AlreadyGranted)
                    Debug.LogWarning($"[WinStreakMainUIPanel] Claim failed: {receipt.Reason}");
            }
            finally
            {
                m_IsClaiming = false;
                if (m_Module != null) Refresh();
            }
        }

        private void ReportMissingBindingsOnce()
        {
            if (m_ReportedMissingBindings) return;
            var missing = new List<string>();
            if (m_CheckpointScroll == null) missing.Add(nameof(m_CheckpointScroll));
            if (m_CheckpointContent == null) missing.Add(nameof(m_CheckpointContent));
            if (m_CheckpointRowTemplate == null) missing.Add(nameof(m_CheckpointRowTemplate));
            if (m_Character == null) missing.Add(nameof(m_Character));
            if (missing.Count == 0) return;
            m_ReportedMissingBindings = true;
            Debug.LogWarning($"[WinStreakMainUIPanel] Missing bindings: {string.Join(", ", missing)}.");
        }

#if UNITY_EDITOR
        // Kept for the existing editor layout builder. Runtime tower construction no longer uses
        // separately typed top/bottom components; both arguments are intentionally ignored.
        public void BindSerializedReferences(TMP_Text title, TMP_Text countdown, TMP_Text progress,
            TMP_Text nextCheckpoint, Button detailsButton, Button closeButton, ScrollRect checkpointScroll,
            Transform checkpointContent, WinStreakCheckpointRowView towerTop,
            WinStreakCheckpointRowView rowTemplate, Component towerBottom)
        {
            BindSerializedReferences(title, countdown, progress, nextCheckpoint, detailsButton, closeButton,
                checkpointScroll, checkpointContent, rowTemplate, null);
        }

        public void BindSerializedReferences(TMP_Text title, TMP_Text countdown, TMP_Text progress,
            TMP_Text nextCheckpoint, Button detailsButton, Button closeButton, ScrollRect checkpointScroll,
            Transform checkpointContent, WinStreakCheckpointRowView rowTemplate,
            RectTransform backgroundLayer)
        {
            m_Title = title;
            m_Countdown = countdown;
            m_Progress = progress;
            m_NextCheckpoint = nextCheckpoint;
            m_DetailsButton = detailsButton;
            m_CloseButton = closeButton;
            m_CheckpointScroll = checkpointScroll;
            m_CheckpointContent = checkpointContent;
            m_CheckpointRowTemplate = rowTemplate;
            m_BackgroundLayer = backgroundLayer;
        }
#endif

        private T FindComponent<T>(string objectName) where T : Component
        {
            Transform target = FindTransform(objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private Transform FindTransform(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return null;
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
                if (candidate.name == objectName) return candidate;
            return null;
        }

        private static WinStreakActivityModule ExtractModule(object userData) =>
            (userData as ActivityPageUserData)?.Request.Arguments as WinStreakActivityModule;

        private static WinStreakActivityModule ResolveModule(object userData)
        {
            WinStreakActivityModule module = ExtractModule(userData);
            if (module == null) GameEntry.Activities?.TryGetModule(WinStreakActivityModule.Id, out module);
            return module;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.SetText(value ?? string.Empty);
        }

        private static void SetText(TMP_Text text, string format, int arg0, int arg1)
        {
            if (text != null) text.SetText(format, arg0, arg1);
        }

        private static string FormatCountdown(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "00:00:00";
            int days = Mathf.Max(0, remaining.Days);
            return days > 0
                ? string.Format("{0}d {1:00}:{2:00}:{3:00}", days, remaining.Hours, remaining.Minutes,
                    remaining.Seconds)
                : string.Format("{0:00}:{1:00}:{2:00}", remaining.Hours, remaining.Minutes, remaining.Seconds);
        }
    }
}
