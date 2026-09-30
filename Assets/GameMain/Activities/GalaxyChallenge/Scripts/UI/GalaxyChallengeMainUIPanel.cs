using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    /// <summary>
    /// 主页面只驱动 Prefab 中已经排好的 5/7 步平台和头像池。运行时不创建 UI，也不做屏幕适配；
    /// RectTransform 的坐标变化仅用于玩家要求的跳跃、落下和淘汰演出。
    /// </summary>
    public sealed class GalaxyChallengeMainUIPanel : UGuiForm
    {
        [Header("Header")]
        [SerializeField] private TMP_Text m_Countdown;
        [SerializeField] private TMP_Text m_Description;
        [SerializeField] private TMP_Text m_LevelCount;
        [SerializeField] private TMP_Text m_PlayerCount;
        [SerializeField] private TMP_Text m_PrizePool;
        [SerializeField] private Button m_DetailButton;
        [SerializeField] private Button m_CloseButton;

        [Header("Authored Board Modes")]
        [SerializeField] private GalaxyChallengeStepContainerView m_Level5Container;
        [SerializeField] private GalaxyChallengeStepContainerView m_Level7Container;

        [Header("Authored Avatar Pool")]
        [SerializeField] private RectTransform m_AvatarLayer;
        [SerializeField] private RectTransform m_AvatarLayoutOrigin;
        [SerializeField] private GalaxyChallengeAvatarCellView[] m_Avatars =
            Array.Empty<GalaxyChallengeAvatarCellView>();

        [Header("Board States")]
        [SerializeField] private GameObject m_GoalReadyState;
        [SerializeField] private GameObject m_FailedState;
        [SerializeField] private GameObject m_ReadyOverlay;
        [SerializeField] private Button m_TapToContinueButton;

        [Header("Victory Overlay")]
        [SerializeField] private GameObject m_VictoryOverlay;
        [SerializeField] private TMP_Text m_VictoryReward;
        [SerializeField] private Button m_VictoryClaimButton;
        [SerializeField] private GalaxyChallengeAvatarCellView m_VictoryPlayerAvatar;
        [SerializeField] private GalaxyChallengeAvatarCellView[] m_VictoryWinnerAvatars =
            Array.Empty<GalaxyChallengeAvatarCellView>();

        [Header("Avatar Animation")]
        [SerializeField, Min(0f)] private float m_CrouchDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float m_JumpDuration = 0.48f;
        [SerializeField, Min(0f)] private float m_LandDuration = 0.12f;
        [SerializeField, Min(0f)] private float m_AvatarStagger = 0.025f;
        [SerializeField, Min(0f)] private float m_JumpPower = 72f;
        [SerializeField, Min(0.01f)] private float m_DropDuration = 0.42f;
        [SerializeField, Min(0f)] private float m_DropDistance = 260f;

        private GalaxyChallengeActivityModule m_Module;
        private GalaxyChallengeStepContainerView m_ActiveContainer;
        private Vector2[] m_AuthoredAvatarOffsets = Array.Empty<Vector2>();
        private Vector3[] m_AuthoredAvatarScales = Array.Empty<Vector3>();
        private Sequence m_TransitionSequence;
        private float m_NextRefreshAt;
        private bool m_IsAnimating;
        private bool m_IsReadyOverlayVisible;
        private int m_BoundEventId = int.MinValue;
        private int m_BoundCrowdSeed = int.MinValue;
        private int m_TransitionEventId = int.MinValue;
        private int m_TransitionCrowdSeed = int.MinValue;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_DetailButton != null) m_DetailButton.AddSafeClick(OnDetailClicked);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => CloseRoundAsync().Forget(Debug.LogException));
            if (m_TapToContinueButton != null) m_TapToContinueButton.AddSafeClick(HideReadyOverlay);
            if (m_VictoryClaimButton != null)
                m_VictoryClaimButton.AddSafeClick(() => ClaimVictoryAsync().Forget(Debug.LogException));
            CacheAuthoredAvatarLayout();
        }

        protected override void OnOpen(object userData)
        {
            m_Module = GalaxyChallengeUIUtility.ResolveModule(userData);
            if (m_Module != null) m_Module.StateChanged += Refresh;
            GalaxyChallengeSnapshot openingSnapshot = m_Module?.GetSnapshot();
            m_IsReadyOverlayVisible = m_ReadyOverlay != null && openingSnapshot != null &&
                !openingSnapshot.HasPendingProgress && openingSnapshot.State == GalaxyChallengeState.Running;
            if (m_ReadyOverlay != null) m_ReadyOverlay.SetActive(m_IsReadyOverlayVisible);
            base.OnOpen(userData);
            Refresh();
            if (!m_IsReadyOverlayVisible) TryPlayPendingProgress();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            KillTransition();
            if (m_Module != null) m_Module.StateChanged -= Refresh;
            m_Module = null;
            base.OnClose(isShutdown, userData);
        }

        private void Update()
        {
            if (m_Module == null || Time.unscaledTime < m_NextRefreshAt) return;
            m_NextRefreshAt = Time.unscaledTime + 1f;
            RefreshCountdown(m_Module.GetSnapshot());
        }

        private void Refresh()
        {
            if (m_Module == null) return;
            GalaxyChallengeSnapshot snapshot = m_Module.GetSnapshot();
            RefreshCountdown(snapshot);
            ResolveActiveContainer(snapshot.StepCount);
            if (m_IsAnimating && (snapshot.EventId != m_TransitionEventId ||
                                  snapshot.CrowdSeed != m_TransitionCrowdSeed))
                KillTransition();
            BindAvatarSprites(snapshot);
            if (m_IsAnimating) return;

            int displayedProgress = snapshot.HasPendingProgress
                ? snapshot.LastPresentedProgress
                : snapshot.CurrentProgress;
            ApplyStableState(snapshot, displayedProgress);
            if (!m_IsReadyOverlayVisible) TryPlayPendingProgress(snapshot);
        }

        private void TryPlayPendingProgress(GalaxyChallengeSnapshot snapshot = null)
        {
            if (m_Module == null || m_IsAnimating || m_IsReadyOverlayVisible) return;
            snapshot ??= m_Module.GetSnapshot();
            if (!snapshot.HasPendingProgress)
            {
                ApplyStableState(snapshot, snapshot.CurrentProgress);
                return;
            }
            if (m_ActiveContainer == null || m_Avatars == null || m_Avatars.Length == 0)
            {
                m_Module.MarkProgressPresented(snapshot.CurrentProgress);
                ApplyStableState(snapshot, snapshot.CurrentProgress);
                return;
            }

            var sequence = DOTween.Sequence().Pause();
            for (int progress = snapshot.LastPresentedProgress + 1;
                 progress <= snapshot.CurrentProgress;
                 progress++)
            {
                int fromProgress = progress - 1;
                int toProgress = progress;
                sequence.Append(BuildStepTransition(snapshot, fromProgress, toProgress));
            }

            m_IsAnimating = true;
            m_TransitionEventId = snapshot.EventId;
            m_TransitionCrowdSeed = snapshot.CrowdSeed;
            m_TransitionSequence = sequence;
            sequence
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    m_IsAnimating = false;
                    m_TransitionSequence = null;
                    m_TransitionEventId = int.MinValue;
                    m_TransitionCrowdSeed = int.MinValue;
                    if (m_Module == null) return;
                    GalaxyChallengeSnapshot current = m_Module.GetSnapshot();
                    ApplyStableState(current, current.CurrentProgress);
                })
                .OnKill(() =>
                {
                    if (m_TransitionSequence != sequence) return;
                    m_TransitionSequence = null;
                    m_IsAnimating = false;
                    m_TransitionEventId = int.MinValue;
                    m_TransitionCrowdSeed = int.MinValue;
                });
            m_TransitionSequence.Play();
        }

        private Sequence BuildStepTransition(GalaxyChallengeSnapshot snapshot, int fromProgress, int toProgress)
        {
            int sourceVisible = GetVisualAvatarCount(snapshot, fromProgress);
            int targetVisible = GetVisualAvatarCount(snapshot, toProgress);
            bool logicalElimination = snapshot.UserCountsInStep[fromProgress] >
                snapshot.UserCountsInStep[toProgress];
            int dropCount = Mathf.Max(sourceVisible - targetVisible, logicalElimination ? 1 : 0);
            int movingCount = Mathf.Min(m_Avatars.Length, targetVisible + dropCount);
            movingCount = Mathf.Max(movingCount, sourceVisible);

            GalaxyChallengeStepView sourceStep = m_ActiveContainer.GetStep(fromProgress);
            GalaxyChallengeStepView targetStep = m_ActiveContainer.GetStep(toProgress);
            Vector2 sourceCenter = GetLayerPosition(sourceStep?.PeopleRoot);
            Vector2 targetCenter = GetLayerPosition(targetStep?.PeopleRoot);
            const float preparationDelay = 0.01f;
            var stepSequence = DOTween.Sequence().Pause();
            stepSequence.AppendCallback(() => PrepareTransition(snapshot, fromProgress, movingCount, sourceCenter));
            stepSequence.AppendInterval(preparationDelay);

            for (int index = 0; index < movingCount; index++)
            {
                GalaxyChallengeAvatarCellView cell = m_Avatars[index];
                if (cell == null || cell.RectTransform == null) continue;
                RectTransform avatar = cell.RectTransform;
                Vector3 baseScale = GetAuthoredScale(index);
                Vector3 crouchScale = new Vector3(baseScale.x * 1.08f, baseScale.y * 0.82f, baseScale.z);
                Vector2 destination = targetCenter + GetAuthoredOffset(index);
                var avatarSequence = DOTween.Sequence().Pause()
                    .Append(avatar.DOScale(crouchScale, m_CrouchDuration).SetEase(Ease.OutQuad))
                    .Append(avatar.DOJumpAnchorPos(destination, m_JumpPower, 1, m_JumpDuration)
                        .SetEase(Ease.OutQuad))
                    .Append(avatar.DOScale(baseScale, m_LandDuration).SetEase(Ease.OutBack));
                stepSequence.Insert(preparationDelay + index * m_AvatarStagger, avatarSequence);
            }

            float jumpEnd = preparationDelay + Mathf.Max(0, movingCount - 1) * m_AvatarStagger +
                m_CrouchDuration + m_JumpDuration + m_LandDuration;
            bool eliminatePlayer = snapshot.IsFailed && toProgress == snapshot.CurrentProgress;
            int npcDropStart = eliminatePlayer ? targetVisible + 1 : targetVisible;
            for (int index = npcDropStart; index < movingCount; index++)
                AppendDrop(stepSequence, m_Avatars[index], targetCenter, index, toProgress, jumpEnd);
            if (eliminatePlayer) AppendDrop(stepSequence, m_Avatars[0], targetCenter, 0, toProgress, jumpEnd);

            float completionTime = jumpEnd + (movingCount > targetVisible || eliminatePlayer ? m_DropDuration : 0f);
            stepSequence.InsertCallback(completionTime,
                () => CompleteTransitionStep(snapshot, toProgress));
            return stepSequence;
        }

        private void PrepareTransition(GalaxyChallengeSnapshot snapshot, int fromProgress, int movingCount,
            Vector2 sourceCenter)
        {
            RefreshText(snapshot, fromProgress);
            m_ActiveContainer?.Bind(snapshot, fromProgress);
            for (int index = 0; index < m_Avatars.Length; index++)
            {
                GalaxyChallengeAvatarCellView cell = m_Avatars[index];
                if (cell == null || cell.RectTransform == null) continue;
                bool visible = index < movingCount;
                cell.SetVisible(visible);
                if (!visible) continue;
                RectTransform avatar = cell.RectTransform;
                avatar.anchoredPosition = sourceCenter + GetAuthoredOffset(index);
                avatar.localScale = GetAuthoredScale(index);
                avatar.localEulerAngles = Vector3.zero;
                if (cell.CanvasGroup != null) cell.CanvasGroup.alpha = 1f;
            }
        }

        private void CompleteTransitionStep(GalaxyChallengeSnapshot snapshot, int progress)
        {
            if (m_Module == null) return;
            m_Module.MarkProgressPresented(progress);
            ApplyStableState(snapshot, progress);
        }

        private void ApplyStableState(GalaxyChallengeSnapshot snapshot, int displayedProgress)
        {
            if (snapshot == null) return;
            displayedProgress = Mathf.Clamp(displayedProgress, 0, snapshot.StepCount);
            ResolveActiveContainer(snapshot.StepCount);
            BindAvatarSprites(snapshot);
            RefreshText(snapshot, displayedProgress);
            m_ActiveContainer?.Bind(snapshot, displayedProgress);

            GalaxyChallengeStepView step = m_ActiveContainer?.GetStep(displayedProgress);
            Vector2 center = GetLayerPosition(step?.PeopleRoot);
            int visibleCount = GetVisualAvatarCount(snapshot, displayedProgress);
            for (int index = 0; index < m_Avatars.Length; index++)
            {
                GalaxyChallengeAvatarCellView cell = m_Avatars[index];
                if (cell == null || cell.RectTransform == null) continue;
                bool visible = index < visibleCount &&
                    !(snapshot.IsFailed && displayedProgress >= snapshot.CurrentProgress && index == 0);
                cell.SetVisible(visible);
                RectTransform avatar = cell.RectTransform;
                avatar.anchoredPosition = center + GetAuthoredOffset(index);
                avatar.localScale = GetAuthoredScale(index);
                avatar.localEulerAngles = Vector3.zero;
                if (cell.CanvasGroup != null) cell.CanvasGroup.alpha = visible ? 1f : 0f;
            }
        }

        private void RefreshText(GalaxyChallengeSnapshot snapshot, int displayedProgress)
        {
            int people = displayedProgress < snapshot.UserCountsInStep.Count
                ? snapshot.UserCountsInStep[displayedProgress]
                : snapshot.CurrentUserCount;
            if (m_Description != null)
                m_Description.SetText("Beat {0} more levels to finish!", Mathf.Max(0,
                    snapshot.StepCount - displayedProgress));
            if (m_LevelCount != null) m_LevelCount.SetText("{0}/{1}", displayedProgress, snapshot.StepCount);
            if (m_PlayerCount != null) m_PlayerCount.SetText("{0}/{1}", people, snapshot.JoinedUserCount);
            if (m_PrizePool != null) m_PrizePool.SetText("{0}", snapshot.RewardCoinPool);
            if (m_GoalReadyState != null)
                m_GoalReadyState.SetActive(displayedProgress >= snapshot.StepCount);
            if (m_FailedState != null) m_FailedState.SetActive(snapshot.IsFailed);
            RefreshVictoryOverlay(snapshot, displayedProgress);
        }

        private void ResolveActiveContainer(int stepCount)
        {
            GalaxyChallengeStepContainerView selected = m_Level5Container != null &&
                m_Level5Container.Supports(stepCount)
                ? m_Level5Container
                : m_Level7Container != null && m_Level7Container.Supports(stepCount)
                    ? m_Level7Container
                    : null;
            if (m_Level5Container != null) m_Level5Container.gameObject.SetActive(selected == m_Level5Container);
            if (m_Level7Container != null) m_Level7Container.gameObject.SetActive(selected == m_Level7Container);
            m_ActiveContainer = selected;
        }

        private void CacheAuthoredAvatarLayout()
        {
            int count = m_Avatars?.Length ?? 0;
            m_AuthoredAvatarOffsets = new Vector2[count];
            m_AuthoredAvatarScales = new Vector3[count];
            if (m_AvatarLayer == null) return;
            Vector2 origin = GetLayerPosition(m_AvatarLayoutOrigin);
            for (int index = 0; index < count; index++)
            {
                GalaxyChallengeAvatarCellView cell = m_Avatars[index];
                if (cell == null || cell.RectTransform == null) continue;
                m_AuthoredAvatarOffsets[index] = GetLayerPosition(cell.RectTransform) - origin;
                m_AuthoredAvatarScales[index] = cell.RectTransform.localScale;
            }
        }

        private void BindAvatarSprites(GalaxyChallengeSnapshot snapshot)
        {
            if (snapshot.EventId == m_BoundEventId && snapshot.CrowdSeed == m_BoundCrowdSeed) return;
            AvatarDatabaseSO database = GameEntry.CustomConfig == null ? null : GameEntry.CustomConfig.AvatarConfig;
            if (database == null) return;
            IReadOnlyList<AvatarEntrySO> avatars = database.GetAllAvatars();
            for (int index = 0; index < m_Avatars.Length; index++)
            {
                GalaxyChallengeAvatarCellView cell = m_Avatars[index];
                if (cell == null) continue;
                Sprite avatar = null;
                Sprite frame = null;
                if (index == 0)
                {
                    if (database.TryGetAvatar(GameEntry.SaveData.AvatarId, out AvatarEntrySO avatarEntry))
                        avatar = avatarEntry.sprite;
                    if (database.TryGetFrame(GameEntry.SaveData.AvatarFrameId, out AvatarFrameEntrySO frameEntry))
                        frame = frameEntry.sprite;
                }
                else if (avatars.Count > 0)
                {
                    uint value = unchecked((uint)snapshot.CrowdSeed + (uint)index * 2654435761u);
                    AvatarEntrySO entry = avatars[(int)(value % (uint)avatars.Count)];
                    if (entry != null) avatar = entry.sprite;
                    IReadOnlyList<AvatarFrameEntrySO> frames = database.GetAllFrames();
                    if (frames.Count > 0)
                    {
                        AvatarFrameEntrySO frameEntry = frames[(int)((value >> 8) % (uint)frames.Count)];
                        if (frameEntry != null) frame = frameEntry.sprite;
                    }
                }
                cell.Bind(avatar, frame);
            }
            BindVictoryAvatars(database, avatars, snapshot);
            m_BoundEventId = snapshot.EventId;
            m_BoundCrowdSeed = snapshot.CrowdSeed;
        }

        private int GetVisualAvatarCount(GalaxyChallengeSnapshot snapshot, int progress)
        {
            int poolCount = m_Avatars?.Length ?? 0;
            if (poolCount == 0) return 0;
            int finalCount = snapshot.UserCountsInStep.Count > snapshot.StepCount
                ? snapshot.UserCountsInStep[snapshot.StepCount]
                : 1;
            int finalVisible = Mathf.Clamp(finalCount, 1, poolCount);
            if (progress <= 0) return poolCount;
            if (progress >= snapshot.StepCount) return finalVisible;
            float progress01 = progress / (float)snapshot.StepCount;
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(poolCount, finalVisible, progress01)),
                finalVisible, poolCount);
        }

        private Vector2 GetLayerPosition(RectTransform target)
        {
            if (m_AvatarLayer == null || target == null) return Vector2.zero;
            return m_AvatarLayer.InverseTransformPoint(target.position);
        }

        private Vector2 GetAuthoredOffset(int index) => index >= 0 && index < m_AuthoredAvatarOffsets.Length
            ? m_AuthoredAvatarOffsets[index]
            : Vector2.zero;

        private Vector3 GetAuthoredScale(int index) => index >= 0 && index < m_AuthoredAvatarScales.Length &&
            m_AuthoredAvatarScales[index] != Vector3.zero
            ? m_AuthoredAvatarScales[index]
            : Vector3.one;

        private void RefreshCountdown(GalaxyChallengeSnapshot snapshot)
        {
            if (m_Countdown != null)
                m_Countdown.SetText(GalaxyChallengeUIUtility.FormatCountdown(snapshot.EndUtc - DateTimeOffset.UtcNow));
        }

        private void OnDetailClicked()
        {
            if (m_Module != null) m_Module.OpenRulesAsync().Forget(Debug.LogException);
        }

        private void HideReadyOverlay()
        {
            m_IsReadyOverlayVisible = false;
            if (m_ReadyOverlay != null) m_ReadyOverlay.SetActive(false);
            TryPlayPendingProgress();
        }

        private void RefreshVictoryOverlay(GalaxyChallengeSnapshot snapshot, int displayedProgress)
        {
            bool visible = snapshot != null && !snapshot.IsFailed && displayedProgress >= snapshot.StepCount &&
                snapshot.CurrentProgress >= snapshot.StepCount;
            if (m_VictoryOverlay != null) m_VictoryOverlay.SetActive(visible);
            if (!visible) return;
            if (m_VictoryReward != null) m_VictoryReward.SetText("{0}", snapshot.RewardCoinShare);
            if (m_VictoryClaimButton != null) m_VictoryClaimButton.interactable = snapshot.CanReceiveReward;
        }

        private async UniTask CloseRoundAsync()
        {
            if (m_Module != null) await m_Module.EndRoundAsync();
            Close();
        }

        private async UniTask ClaimVictoryAsync()
        {
            if (m_Module == null || m_VictoryClaimButton == null || !m_VictoryClaimButton.interactable) return;
            m_VictoryClaimButton.interactable = false;
            ActivityRewardReceipt receipt = await m_Module.ClaimRewardAsync();
            if (receipt.Status == ActivityRewardStatus.Granted || receipt.Status == ActivityRewardStatus.AlreadyGranted)
                Close();
            else
                Refresh();
        }

        private void BindVictoryAvatars(AvatarDatabaseSO database, IReadOnlyList<AvatarEntrySO> avatars,
            GalaxyChallengeSnapshot snapshot)
        {
            if (database == null || snapshot == null) return;
            BindAvatarCell(m_VictoryPlayerAvatar, database, avatars, snapshot, 0, true);
            if (m_VictoryPlayerAvatar != null) m_VictoryPlayerAvatar.SetVisible(true);
            if (m_VictoryWinnerAvatars == null) return;
            int finalWinnerCount = snapshot.UserCountsInStep.Count > snapshot.StepCount
                ? snapshot.UserCountsInStep[snapshot.StepCount]
                : 1;
            int npcWinnerCount = Mathf.Clamp(finalWinnerCount - 1, 0, m_VictoryWinnerAvatars.Length);
            for (int index = 0; index < m_VictoryWinnerAvatars.Length; index++)
            {
                BindAvatarCell(m_VictoryWinnerAvatars[index], database, avatars, snapshot, index + 1, false);
                if (m_VictoryWinnerAvatars[index] != null)
                    m_VictoryWinnerAvatars[index].SetVisible(index < npcWinnerCount);
            }
        }

        private static void BindAvatarCell(GalaxyChallengeAvatarCellView cell, AvatarDatabaseSO database,
            IReadOnlyList<AvatarEntrySO> avatars, GalaxyChallengeSnapshot snapshot, int index, bool player)
        {
            if (cell == null) return;
            Sprite avatar = null;
            Sprite frame = null;
            uint value = unchecked((uint)snapshot.CrowdSeed + (uint)Mathf.Max(1, index) * 2654435761u);
            if (player)
            {
                if (database.TryGetAvatar(GameEntry.SaveData.AvatarId, out AvatarEntrySO avatarEntry)) avatar = avatarEntry.sprite;
                if (database.TryGetFrame(GameEntry.SaveData.AvatarFrameId, out AvatarFrameEntrySO frameEntry)) frame = frameEntry.sprite;
            }
            else if (avatars.Count > 0)
            {
                AvatarEntrySO entry = avatars[(int)(value % (uint)avatars.Count)];
                if (entry != null) avatar = entry.sprite;
                IReadOnlyList<AvatarFrameEntrySO> frames = database.GetAllFrames();
                if (frames.Count > 0)
                {
                    AvatarFrameEntrySO frameEntry = frames[(int)((value >> 8) % (uint)frames.Count)];
                    if (frameEntry != null) frame = frameEntry.sprite;
                }
            }
            cell.Bind(avatar, frame);
        }

        private void AppendDrop(Sequence sequence, GalaxyChallengeAvatarCellView cell, Vector2 targetCenter,
            int index, int progress, float startTime)
        {
            if (cell == null || cell.RectTransform == null) return;
            RectTransform avatar = cell.RectTransform;
            Vector2 destination = targetCenter + GetAuthoredOffset(index);
            float angle = ((index + progress) & 1) == 0 ? 32f : -32f;
            sequence.Insert(startTime, avatar.DOAnchorPosY(destination.y - m_DropDistance, m_DropDuration).SetEase(Ease.InQuad));
            sequence.Insert(startTime, avatar.DOLocalRotate(new Vector3(0f, 0f, angle), m_DropDuration).SetEase(Ease.InQuad));
            if (cell.CanvasGroup != null)
                sequence.Insert(startTime, cell.CanvasGroup.DOFade(0f, m_DropDuration).SetEase(Ease.InQuad));
        }

        private void KillTransition()
        {
            Sequence sequence = m_TransitionSequence;
            m_TransitionSequence = null;
            m_IsAnimating = false;
            m_TransitionEventId = int.MinValue;
            m_TransitionCrowdSeed = int.MinValue;
            sequence?.Kill(false);
        }
    }
}
