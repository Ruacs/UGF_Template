using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Coffee.UIExtensions;
using Ads;

namespace Lokas
{
    public class SettlementRankUIPanel : UGuiForm
    {
        [SerializeField] private RectTransform m_TipsBoxRT;
        [SerializeField] private TMP_Text m_tipTmp;
        [SerializeField] private ScrollRect m_RankListSR;
        [SerializeField] private UI_RankingItem m_RankItemPrefab;
        [SerializeField] private RectTransform m_RankItemRoot;
        [SerializeField] private TMP_Text m_titleTMP;
        [SerializeField] private TMP_Text m_CountdownTMP;

        [SerializeField] private WinStreakMultiplierUI m_WinStreakMultiplierUI;
        [SerializeField] private UI_RankItemCount m_rankItem;
        [SerializeField] private TMP_Text m_WinStreakTMP;


        [SerializeField] private RectTransform m_NextRT;
        private Button m_NextBtn;
        private readonly UI_RankingVirtualList m_RankVirtualList = new();
        private IReadOnlyList<RankData> m_CurrentRanks;
        private Sprite m_CurrentItemIconSprite;
        [SerializeField] private LayoutGroup m_rankItemLayoutGroup;
        [SerializeField] private List<UIParticle> m_CaidaiList;

        [SerializeField] private TMP_Text m_topTMP;
        [SerializeField] private DOTweenSequence m_topsOpen;
        [SerializeField] private DOTweenSequence m_topsClose;
        [SerializeField] private List<GameObject> m_rankTopLabelList;
        private UI_RankingItem m_AnimatingSelfItem;
        private readonly HashSet<int> m_AnimatingHiddenRankIndices = new();
        private int m_WinStreakTextAnimationVersion;


        private ProcedureGame m_ProcedureGame;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_WinStreakMultiplierUI == null)
                m_WinStreakMultiplierUI = GetComponentInChildren<WinStreakMultiplierUI>(true);

            m_NextBtn = m_NextRT.GetComponentInChildren<Button>();
            m_NextBtn.AddSafeClick(OnClickNext);
            m_rankItemLayoutGroup = m_RankItemRoot.GetComponent<LayoutGroup>();
            m_RankVirtualList.Initialize(m_RankListSR, m_RankItemPrefab, m_RankItemRoot, BindVirtualRankItem);
        }

        protected override void OnOpen(object userData)
        {
            m_NextBtn.interactable = true;
            RefreshWinStreakText();
            AdsAnalytics.EventWithName("结算排行榜页面_打开");

            PlayUISound(SoundId.SFX_LeaderBoard_Card_Flip);
            m_NextRT.localScale = Vector3.zero;
            m_TipsBoxRT.anchoredPosition = new Vector2(0, 400);
            ResetRankListScrollState();
            base.OnOpen(userData);
            m_ProcedureGame = (ProcedureGame)userData;
            if (GameEntry.Rank != null)
            {
                GameEntry.Rank.EnsureFullRankList();
                // 快照伪进度生效前的玩家真实名次。PreApply 会把?NPC 的分数提上来?
                // 让玩家在显示数据里临时下掉若干名（用于后面的"逆袭"动画），但首屏滚动定?
                // 应该锚到玩家最终落点（真实名次），否则打开会先滚到 7-8 名再升回去?
                int preFakeSelfRank = GameEntry.Rank.SelfRank != null ? GameEntry.Rank.SelfRank.Rank : -1;
                PreApplyFakePlayerScoreProgress();
                GameEntry.Rank.OnCycleCountdownChanged += OnRankCycleCountdownChanged;
                GameEntry.Rank.OnCycleChanged += OnRankCycleChanged;
                GameEntry.Rank.OnRanksChanged += OnRanksChanged;

                m_titleTMP.text = RankingUIPanel.GetCurrentRankTitle();
                RefreshRankCycleUI();
                RefreshRankList(GameEntry.Rank.Ranks);
                SetRankListScrollEnabled(false);
                ScrollRankListToSelfOnOpen().Forget();
            }

            PlayOpenSequence().Forget();
        }

        private void RefreshWinStreakText()
        {
            if (m_WinStreakTMP == null)
                return;

            m_WinStreakTMP.text = GameEntry.Localization.GetString(LocalizationKeys.Rank_WinStreak, 0, WinStreakHightlightColor);

            int winStreak = 0;
            m_WinStreakTMP.text = GameEntry.Localization.GetString(LocalizationKeys.Rank_WinStreak, winStreak, WinStreakHightlightColor);
        }

        private async UniTask AnimateWinStreakText(int targetWinStreak)
        {
            int version = ++m_WinStreakTextAnimationVersion;
            targetWinStreak = Mathf.Max(0, targetWinStreak);

            for (int value = 0; value <= targetWinStreak; value++)
            {
                if (version != m_WinStreakTextAnimationVersion || m_WinStreakTMP == null)
                    return;

                m_WinStreakTMP.text = GameEntry.Localization.GetString(LocalizationKeys.Rank_WinStreak, value, WinStreakHightlightColor);

                if (value < targetWinStreak)
                    await UniTask.Delay(80);
            }
        }

        private void PreApplyFakePlayerScoreProgress()
        {
            var rank = GameEntry.Rank;
            if (rank == null) return;
        }




        private void PlayEffect(int rank)
        {
            if (rank <= 10 && m_topsOpen != null)
            {
                PlayUISound(SoundId.SFX_Rank_TOP);
                for (int i = 0; i < m_rankTopLabelList.Count; i++)
                {
                    m_rankTopLabelList[i]?.SetActive(i == rank - 1);
                }
                m_topsOpen?.Play();
            }

            foreach (var effect in m_CaidaiList)
            {
                effect.Play();
            }
        }
        private async UniTask ApplyRankCollectScore()
        {
            await UniTask.Delay(1000);
            SetRankListScrollEnabled(true);
        }

        private async UniTaskVoid PlayOpenSequence()
        {


            await UniTask.Delay(1800);
            if (m_WinStreakMultiplierUI != null)
            {
                m_WinStreakMultiplierUI.Refresh(0);
            }
            await UniTask.Delay(500);
            await ApplyRankCollectScore();
        }

        private async UniTaskVoid ScrollRankListToSelfOnOpen()
        {
            await UniTask.Yield();
            await UniTask.Yield();
            ScrollRankListToSelfImmediate();
        }

        private void ScrollRankListToSelfImmediate()
        {
            if (m_RankListSR == null || m_RankListSR.content == null) return;

            RectTransform viewport = m_RankListSR.viewport != null ? m_RankListSR.viewport : m_RankListSR.GetComponent<RectTransform>();
            RectTransform content = m_RankListSR.content;
            if (viewport == null || GameEntry.Rank.SelfRank == null) return;

            // 关键：杀掉上一次打开?AnimateRankChange Step 3 ?content ?join ?DOAnchorPosY 补间残留?
            // 否则面板重开后这个补间会继续?content 推到旧目标位置，看起来像"自动向下滚动"?
            content.DOKill();
            m_RankListSR.StopMovement();
            m_RankListSR.velocity = Vector2.zero;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float viewH = viewport.rect.height;
            float maxY = Mathf.Max(0f, content.rect.height - viewH);

            // 内容不超过视口，无需滚动
            if (maxY <= 0f)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
                m_RankListSR.StopMovement();
                m_RankListSR.velocity = Vector2.zero;
                return;
            }

            // 玩家在第一名时保持在顶部不?
            int selfIndex = GameEntry.Rank.SelfRank != null ? GameEntry.Rank.SelfRank.Rank - 1 : -1;
            if (selfIndex <= 0)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
                m_RankListSR.StopMovement();
                m_RankListSR.velocity = Vector2.zero;
                return;
            }

            float clampedTargetY = Mathf.Clamp(m_RankVirtualList.GetContentYForIndex(selfIndex), 0f, maxY);

            content.anchoredPosition = new Vector2(content.anchoredPosition.x, clampedTargetY);
            m_RankListSR.StopMovement();
            m_RankListSR.velocity = Vector2.zero;
            m_RankVirtualList.Refresh();
        }

        private void SetRankListScrollEnabled(bool enabled)
        {
            if (m_RankListSR == null) return;

            if (!enabled)
            {
                m_RankListSR.StopMovement();
                m_RankListSR.velocity = Vector2.zero;
            }

            m_RankListSR.enabled = enabled;
        }



        private Vector2 GetLocalPositionForItemTween(RectTransform rt)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            var localPoint = rt.WorldToLocalPointInRect(rootCanvas.GetComponent<RectTransform>(), GameEntry.UI.UICamera);
            return localPoint;
        }


        private async UniTask ScrollRankListToSelfAsync()
        {
            if (m_RankListSR == null || m_RankListSR.content == null) return;

            UI_RankingItem selfItem = m_RankVirtualList.FindVisibleItem(item => item.IsSelf);
            if (selfItem == null) return;

            RectTransform viewport = m_RankListSR.viewport != null ? m_RankListSR.viewport : m_RankListSR.GetComponent<RectTransform>();
            RectTransform content = m_RankListSR.content;
            if (viewport == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Bounds itemBounds = TransformBoundsTo(selfItem.transform as RectTransform, viewport);
            float targetY = content.anchoredPosition.y + (viewport.rect.center.y - itemBounds.center.y);
            float maxY = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            float clampedTargetY = Mathf.Clamp(targetY, 0f, maxY);

            if (Mathf.Abs(content.anchoredPosition.y - clampedTargetY) < 1f)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, clampedTargetY);
                return;
            }
            m_RankListSR.velocity = Vector2.zero;
            m_RankListSR.StopMovement();
            await content
                .DOAnchorPosY(clampedTargetY, 0)
                .SetEase(Ease.Linear)
                .ToUniTask();

            m_RankListSR.velocity = Vector2.zero;
        }


        private static Bounds TransformBoundsTo(RectTransform source, Transform target)
        {
            Vector3[] corners = new Vector3[4];
            source.GetWorldCorners(corners);

            Matrix4x4 matrix = target.worldToLocalMatrix;
            Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 corner = matrix.MultiplyPoint3x4(corners[i]);
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }

            Bounds bounds = new(min, Vector3.zero);
            bounds.Encapsulate(max);
            return bounds;
        }

        private async UniTask AnimateRankChange(UI_RankingItem selfItem, int oldRank)
        {
            if (selfItem == null || GameEntry.Rank == null) return;

            var selfRankData = GameEntry.Rank.SelfRank;
            if (selfRankData == null) return;

            int selfNewIndex = selfRankData.Rank - 1; // 目标索引?-based?
            int selfOldIndex = oldRank - 1;

            if (selfOldIndex < 0) return;

            // 是否上升名次：仅决定中间的位移动画是否执行；抬起与落下无论是否上升都会播?
            bool rankUp = selfNewIndex >= 0 && selfNewIndex < selfOldIndex;

            m_AnimatingSelfItem = selfItem;
            m_AnimatingHiddenRankIndices.Clear();
            m_AnimatingHiddenRankIndices.Add(selfOldIndex);
            m_RankVirtualList.LockItem(selfItem);

            RectTransform selfRT = (RectTransform)selfItem.transform;
            Vector2 oldPos = selfRT.anchoredPosition;
            Vector2 newPos = rankUp ? m_RankVirtualList.GetItemAnchoredPosition(selfNewIndex) : oldPos;

            // 置顶渲染，避免被其他 item 遮挡
            BringAnimatingSelfItemToFront();

            const float liftY = 40f;

            // =========================
            // Step 2：抬?+ 放大
            // =========================
            await DOTween.Sequence()
                .Join(selfRT.DOAnchorPosY(oldPos.y + liftY, 0.2f).SetEase(Ease.OutQuad))
                .Join(selfRT.DOScale(1.08f, 0.2f).SetEase(Ease.OutQuad))
                .ToUniTask();

            // =========================
            // Step 3：移动（核心，仅在升名次时执行）
            // =========================
            if (rankUp)
            {
                selfItem.PlayRankUp();
                await PlayRankUpMoveStepByStep(selfRT, oldPos, newPos, selfOldIndex, selfNewIndex, liftY);
                PlayEffect(selfRankData.Rank);
            }
            selfItem.PlayRankAnimEnd();

            PlayUISound(SoundId.SFX_Rank_Down);
            // =========================
            // Step 4：落?+ 回弹
            // =========================
            await DOTween.Sequence()
                .Join(selfRT.DOAnchorPosY(newPos.y, 0.2f).SetEase(Ease.OutBounce))
                .Join(selfRT.DOScale(1f, 0.15f).SetEase(Ease.OutQuad))
                .ToUniTask();

            m_RankVirtualList.UnlockItem(selfItem);
            m_AnimatingSelfItem = null;
            m_AnimatingHiddenRankIndices.Clear();
            m_RankVirtualList.Refresh();
        }

        // 旧动画：玩家直接平滑移动到目标位置，被挤下去的元素整体下移一格（齐发?
        private async UniTask PlayRankUpMoveTogether(RectTransform selfRT, Vector2 oldPos, Vector2 newPos, int selfOldIndex, int selfNewIndex, float liftY)
        {
            Sequence moveSeq = DOTween.Sequence();

            // 自己移动
            _ = moveSeq.Join(
                selfRT.DOAnchorPos(new Vector2(oldPos.x, newPos.y + liftY), 0.5f)
                .SetEase(Ease.InOutCubic)
            );

            // 被挤下去的元?
            // 同步滚动 ScrollRect，确?selfRT 始终在屏幕内可见
            if (m_RankListSR != null && m_RankListSR.content != null)
            {
                RectTransform scrollContent = m_RankListSR.content;
                RectTransform vp = m_RankListSR.viewport != null
                    ? m_RankListSR.viewport
                    : m_RankListSR.GetComponent<RectTransform>();
                float viewH = vp.rect.height;
                float maxScrollY = Mathf.Max(0f, scrollContent.rect.height - viewH);
                // newPos.y ?content 局部坐标（向下为负），?selfRT 滚动?viewport 中央
                float targetScrollY = Mathf.Clamp(-newPos.y - viewH * 0.5f, 0f, maxScrollY);
                _ = moveSeq.Join(scrollContent.DOAnchorPosY(targetScrollY, 0.5f).SetEase(Ease.InOutCubic));
            }

            await moveSeq.ToUniTask();
        }

        // 新动画：一格一格地交换。玩家每次向上挪一格，紧邻上方的元素同步下移一格，循环到目标名次?
        private async UniTask PlayRankUpMoveStepByStep(RectTransform selfRT, Vector2 oldPos, Vector2 newPos, int selfOldIndex, int selfNewIndex, float liftY)
        {
            int steps = selfOldIndex - selfNewIndex;
            if (steps <= 0) return;

            const float defaultStepDuration = 0.2f;
            const float maxTotalDuration = defaultStepDuration * 10;
            // 步数过多时按总时长上限动态压缩单步时长，避免长时间等待影响体?
            float stepDuration = steps * defaultStepDuration > maxTotalDuration
                ? maxTotalDuration / steps
                : defaultStepDuration;
            // 滚动条与逐格动画并行平滑滚动到玩家最终位?
            if (steps > 10)
            {
                PlayUISound(SoundId.SFX_Rank_UP_2);
            }

            int currentSelfIndex = selfOldIndex;
            HashSet<int> displacedVisualIndices = new();
            List<UI_RankingItem> displacedItems = new();
            RebuildAnimatingHiddenRankIndices(currentSelfIndex, displacedVisualIndices);

            for (int targetIdx = selfOldIndex - 1; targetIdx >= selfNewIndex; targetIdx--)
            {
                RefreshRankVirtualListDuringAnimation();
                UI_RankingItem aboveItem = m_RankVirtualList.GetVisibleItem(targetIdx);
                RectTransform aboveRT = aboveItem != null ? aboveItem.transform as RectTransform : null;
                bool animateAbove = aboveRT != null && aboveRT != selfRT && aboveItem.gameObject.activeSelf;
                if (animateAbove)
                {
                    m_RankVirtualList.LockItem(aboveItem);
                    if (!displacedItems.Contains(aboveItem))
                        displacedItems.Add(aboveItem);
                }

                if (steps <= 10)
                {
                    PlayUISound(SoundId.SFX_Rank_UP);
                }

                RebuildAnimatingHiddenRankIndices(animateAbove ? currentSelfIndex : -1, displacedVisualIndices, targetIdx);

                Vector2 selfTargetPos = m_RankVirtualList.GetItemAnchoredPosition(targetIdx);
                Vector2 aboveTargetPos = m_RankVirtualList.GetItemAnchoredPosition(currentSelfIndex);

                Sequence stepSeq = DOTween.Sequence();
                _ = stepSeq.Join(selfRT.DOAnchorPos(new Vector2(selfTargetPos.x, selfTargetPos.y + liftY), stepDuration).SetEase(Ease.Linear));

                if (animateAbove)
                    _ = stepSeq.Join(aboveRT.DOAnchorPosY(aboveTargetPos.y, stepDuration).SetEase(Ease.OutQuad));

                Tween scrollTween = CreateRankStepScrollTween(targetIdx, stepDuration);
                if (scrollTween != null)
                    _ = stepSeq.Join(scrollTween);

                await stepSeq.ToUniTask();

                if (animateAbove)
                    displacedVisualIndices.Add(currentSelfIndex);

                currentSelfIndex = targetIdx;
                RebuildAnimatingHiddenRankIndices(currentSelfIndex, displacedVisualIndices);

            }

            for (int i = 0; i < displacedItems.Count; i++)
                m_RankVirtualList.UnlockItem(displacedItems[i]);

        }

        private void RebuildAnimatingHiddenRankIndices(int selfIndex, HashSet<int> displacedVisualIndices, int extraIndex = -1)
        {
            m_AnimatingHiddenRankIndices.Clear();
            if (selfIndex >= 0)
                m_AnimatingHiddenRankIndices.Add(selfIndex);

            if (extraIndex >= 0)
                m_AnimatingHiddenRankIndices.Add(extraIndex);

            if (displacedVisualIndices == null)
                return;

            foreach (int index in displacedVisualIndices)
            {
                if (index >= 0)
                    m_AnimatingHiddenRankIndices.Add(index);
            }
        }

        private Tween CreateRankStepScrollTween(int focusIndex, float duration)
        {
            if (m_RankListSR == null || m_RankListSR.content == null)
                return null;

            RectTransform content = m_RankListSR.content;
            float targetY = m_RankVirtualList.GetContentYForIndex(focusIndex);
            if (Mathf.Abs(content.anchoredPosition.y - targetY) < 0.5f)
                return null;

            return content
                .DOAnchorPosY(targetY, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(RefreshRankVirtualListDuringAnimation);
        }

        private void RefreshRankVirtualListDuringAnimation()
        {
            m_RankVirtualList.Refresh();
            BringAnimatingSelfItemToFront();
        }

        private void BringAnimatingSelfItemToFront()
        {
            if (m_AnimatingSelfItem == null) return;

            RectTransform selfRT = m_AnimatingSelfItem.transform as RectTransform;
            if (selfRT == null) return;

            selfRT.SetAsLastSibling();
        }

        private const string RankHighlightColor = "#13cb00";
        private const string WinStreakHightlightColor = "#FFD700";
        private async void ShowTipBox(int rank)
        {
            if (rank <= 0) return;
            m_tipTmp.text = GameEntry.Localization.GetString(LocalizationKeys.Rank_TipLabel, rank, RankHighlightColor);
            await m_TipsBoxRT.DOAnchorPosY(0, 0.5f).SetEase(Ease.OutQuad);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_WinStreakTextAnimationVersion++;
            AdsAnalytics.EventWithName("结算排行榜页面_关闭");
            if (GameEntry.Rank != null)
            {
                GameEntry.Rank.OnCycleCountdownChanged -= OnRankCycleCountdownChanged;
                GameEntry.Rank.OnCycleChanged -= OnRankCycleChanged;
                GameEntry.Rank.OnRanksChanged -= OnRanksChanged;
            }
            base.OnClose(isShutdown, userData);
        }

        private void ResetRankListScrollState()
        {
            if (m_RankListSR == null) return;

            m_RankListSR.DOKill();
            m_RankListSR.StopMovement();
            m_RankListSR.velocity = Vector2.zero;
            m_RankListSR.enabled = true;

            if (m_RankListSR.content == null) return;

            m_RankListSR.content.DOKill();
            m_RankListSR.content.anchoredPosition = new Vector2(m_RankListSR.content.anchoredPosition.x, 0f);
            m_RankVirtualList.Refresh();
        }

        private void OnClickNext()
        {
            if (m_NextBtn != null) m_NextBtn.interactable = false;
            // ResetRankListScrollState();
            PlayUISound(SoundId.UI_Close);
            Close();
            GameEntry.UI.OpenUIForm(UIFormId.GameOverUIPanel, m_ProcedureGame);
        }

        private void OnRankCycleCountdownChanged(int remainingSeconds)
        {
            if (m_CountdownTMP != null)
            {
                string content =  NumberFormatUtil.FormatToHM(GameEntry.Rank.CycleRemainingSeconds, GameEntry.SaveData.IsCN);
                m_CountdownTMP.text = content;
            }

        }

        private void OnRanksChanged(IReadOnlyList<RankData> ranks)
        {
            if (GameEntry.Rank != null && ranks != null && ranks.Count > 0 && ranks.Count < RankComponent.FullFakeCount + 1)
            {
                GameEntry.Rank.EnsureFullRankList();
                return;
            }

            RefreshRankList(ranks);
        }

        private void OnRankCycleChanged(RankCycleData cycle)
        {
            if (GameEntry.Rank == null) return;

            GameEntry.Rank.EnsureFullRankList();
            m_titleTMP.text = RankingUIPanel.GetCurrentRankTitle();
            RefreshRankCycleUI();
            RefreshRankList(GameEntry.Rank.Ranks);

            PromptUIPanel.ShowToastUntilClick(GameEntry.Localization.GetString(LocalizationKeys.Rank_ResetTips));
        }

        private void RefreshRankCycleUI()
        {
            if (GameEntry.Rank == null) return;
            if (m_CountdownTMP != null)
            {
                string content = NumberFormatUtil.FormatToHM(GameEntry.Rank.CycleRemainingSeconds, GameEntry.SaveData.IsCN);
                m_CountdownTMP.text = content;
            }
        }

        private void RefreshRankList(IReadOnlyList<RankData> ranks)
        {
            if (ranks == null) return;
            Sprite itemIconSprite = GameEntry.Rank != null ? GameEntry.Rank.CurrentTargetItemSprite : null;

            m_CurrentRanks = ranks;
            m_CurrentItemIconSprite = itemIconSprite;
            m_RankVirtualList.SetDataCount(ranks.Count, false);
        }

        private void BindVirtualRankItem(int index, UI_RankingItem item)
        {
            if (m_CurrentRanks == null || index < 0 || index >= m_CurrentRanks.Count)
                return;

            RankData data = m_CurrentRanks[index];
            if (m_AnimatingSelfItem != null && item != m_AnimatingSelfItem && (m_AnimatingHiddenRankIndices.Contains(index) || (data != null && data.IsSelf)))
            {
                item.gameObject.SetActive(false);
                return;
            }

            ApplyItem(item, data, m_CurrentItemIconSprite);
        }

        private static void ApplyItem(UI_RankingItem item, RankData data, Sprite itemIcon)
        {
            if (item == null || data == null) return;
            var db = GameEntry.CustomConfig.AvatarConfig;
            Sprite avatar = null;
            Sprite frame = null;
            string playerName = data.PlayerName;

            if (data.IsSelf)
            {
                var gd = GameEntry.SaveData;
                if (gd != null)
                {
                    playerName = gd.PlayerName;
                    if (db != null)
                    {
                        if (db.TryGetAvatar(gd.AvatarId, out var a)) avatar = a.sprite;
                        if (db.TryGetFrame(gd.AvatarFrameId, out var f)) frame = f.sprite;
                    }
                }
            }
            else if (db != null)
            {
                if (int.TryParse(data.AvatarId, out int avatarId) && db.TryGetAvatar(avatarId, out var avatarEntry))
                    avatar = avatarEntry.sprite;
                if (int.TryParse(data.AvatarFrameId, out int frameId) && db.TryGetFrame(frameId, out var frameEntry))
                    frame = frameEntry.sprite;
            }

            item.SetData(data.Rank, playerName, avatar, frame, data.Score, data.IsSelf, isHomeRank: false);
            item.SetItemSlot(itemIcon, data.Score);
        }
    }

}
