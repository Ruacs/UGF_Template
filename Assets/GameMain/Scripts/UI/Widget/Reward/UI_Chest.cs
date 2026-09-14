using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Ads;

namespace Lokas
{
    public class UI_Chest : MonoBehaviour
    {
        private int m_ChestProgress;
        [SerializeField] private RectTransform m_rootRT;
        [SerializeField] private Button m_ChestBtn;
        [SerializeField] private TMP_Text m_ProgressText;

        [Header("PlayCount Progress (5)")]
        [SerializeField] private RectTransform m_ProgressRoot;
        [SerializeField] private CanvasGroup[] m_ProgressIcons;

        [SerializeField] private UI_RewardTipsBox m_RewardTipsBox;

        private const int PlayCountTarget = 5;
        private const float IconFadeDuration = 0.25f;
        private const float IconStagger = 0.1f;
        private const int InitialAnimateDelayMs = 500;
        private const int ClaimEventDelayAfterFullMs = 500;

        private int m_LastShownProgress5;
        private void Awake()
        {
            if (m_ChestBtn != null)
                m_ChestBtn.AddSafeClick(OnChestClick);
        }

        public void ResetPos()
        {
            m_rootRT.anchoredPosition = new(-27, -300);
        }

        public void Reset()
        {
            ResetChest();
        }

        public void ResetChest()
        {
            RefreshProgress();
        }

        public void RefreshProgress()
        {
            int progress = Mathf.Max(0, GetChestProgress());

            m_LastShownProgress5 = Mathf.Clamp(progress, 0, PlayCountTarget);

            SetIconsImmediate(m_ProgressIcons, m_LastShownProgress5);
        }

        private void SetIconsImmediate(CanvasGroup[] icons, int filledCount)
        {
            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null) continue;
                icons[i].DOKill();
                icons[i].alpha = i < filledCount ? 1f : 0f;
            }
        }

        public async UniTask Show()
        { 
            await UniTask.Delay(1200);
            await m_rootRT.DOAnchorPosY(0, 0.5f).SetEase(Ease.OutQuad);
        }

        public async UniTask Refresh()
        {
            AddCurrentGameProgress();
            await RefreshUI();
        }

        private void AddCurrentGameProgress()
        {
            int target = PlayCountTarget;

            if (GetChestProgress() >= target) return;

            int oldProgress = GetChestProgress();

            SetChestProgress(Mathf.Min(GetChestProgress() + 1, PlayCountTarget));

            if (GetChestProgress() > oldProgress) GameEntry.Sound.PlaySound(SoundId.SFX_chest_rise);
        }
        int target;


        public void OnChestClick()
        {
            Log.Info("[UI_Chest] OnChestClick");
            GameEntry.Sound.PlayUISound(SoundId.UI_Click);
            var chestRewards = GameEntry.CustomConfig.RewardConfig?.ChestRewardList;
            if (chestRewards == null || chestRewards.Count == 0 || chestRewards[0] == null) return;
            m_RewardTipsBox?.Show(chestRewards[0].rewardDatas);
        }

        private async UniTask RefreshUI()
        {
            int progress = GetChestProgress();

            await AnimateProgressIcons(m_ProgressIcons, progress, PlayCountTarget);

        }

        private async UniTask AnimateProgressIcons(CanvasGroup[] icons, int progress, int target)
        {
            if (icons == null) return;

            int lastShownProgress = m_LastShownProgress5;
            progress = Mathf.Clamp(progress, 0, icons.Length);
            int previousProgress = Mathf.Clamp(lastShownProgress, 0, icons.Length);
            int startAnimateIndex = Mathf.Clamp(previousProgress, 0, progress);
            int newIconCount = progress - startAnimateIndex;
            bool justReachedTarget = previousProgress < target && progress >= target;


            m_LastShownProgress5 = progress;

            await UniTask.Delay(InitialAnimateDelayMs);

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null) continue;

                if (i < progress)
                {
                    icons[i].DOKill();

                    if (i < startAnimateIndex)
                    {
                        icons[i].alpha = 1f;
                    }
                    else
                    {
                        float delay = (i - startAnimateIndex) * IconStagger;
                        icons[i].alpha = 0f;
                        icons[i].DOFade(1f, IconFadeDuration).SetDelay(delay).SetEase(Ease.OutQuad);
                    }
                }
                else
                {
                    icons[i].DOKill();
                    icons[i].alpha = 0f;
                }
            }

            if (!justReachedTarget) return;

            float fullAnimDuration = newIconCount > 0
                ? (newIconCount - 1) * IconStagger + IconFadeDuration
                : 0f;
            int waitMs = Mathf.CeilToInt(fullAnimDuration * 1000f) + ClaimEventDelayAfterFullMs;
            await UniTask.Delay(waitMs);

            OnClaimRewardEvent();
        }

        private void OnClaimRewardEvent()
        {
            var chestRewards = GameEntry.CustomConfig.RewardConfig?.ChestRewardList;
            if (chestRewards == null || chestRewards.Count == 0 || chestRewards[0] == null)
                return;

            Log.Info("[UI_Chest] OnClaimRewardEvent");
            GameEntry.Event.Fire(this, OnClaimRewardsEventArgs.Create(0, chestRewards[0].rewardDatas));

            SetChestProgress(0);

        }



        private int GetChestProgress()
        {
            return m_ChestProgress;
        }

        private void SetChestProgress(int progress)
        {
            m_ChestProgress = progress;
        }
    }
}
