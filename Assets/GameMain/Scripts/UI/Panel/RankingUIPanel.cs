using System;
using System.Collections.Generic;
using Ads;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Log = UnityGameFramework.Runtime.Log;
namespace Lokas
{
    public class RankingUIPanel : UGuiForm
    {
        [SerializeField] private TMP_Text m_tltleTmp;
        [SerializeField] private Button m_CloseBtn;
        [SerializeField] private Button m_TipsBtn;
        [SerializeField] private ScrollRect m_RankListSR;
        [SerializeField] private UI_RankingItem m_RankItemPrefab;
        [SerializeField] private RectTransform m_RankItemRoot;
        [SerializeField] private Button m_LevelBtn;
        [SerializeField] private ButtonStateController m_LevelBtnState;
        [SerializeField] private TMP_Text m_CountdownTMP;

        /// <summary>
        /// 默认已在面板上赋值前三的Item
        /// </summary>
        [SerializeField] private List<UI_RankingItem> m_RankingItems;

        [SerializeField] private List<Sprite> m_ChestIconList;

        [SerializeField] private WinStreakMultiplierUI m_WinStreakMultiplierUI;

        private readonly UI_RankingVirtualList m_RankVirtualList = new();
        private IReadOnlyList<RankData> m_CurrentRanks;
        private Sprite m_CurrentItemIconSprite;

        private ProcedureMenu m_procedureMenu;
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_CloseBtn.AddSafeClick(OnClickClose);
            m_TipsBtn.AddSafeClick(OnClickTips);
            m_LevelBtn.AddSafeClick(OnClickLevel);
            m_RankVirtualList.Initialize(m_RankListSR, m_RankItemPrefab, m_RankItemRoot, BindVirtualRankItem);
        }

        protected override void OnOpen(object userData)
        {
            // PlayUISound(SoundId.SFX_LeaderBoard_Card_Flip);
            m_procedureMenu = (ProcedureMenu)userData;
            AdsAnalytics.EventWithName("排行榜页面_打开");
            SubscribeLevelChangedEvent();
            SetStartBtnState();
            base.OnOpen(userData);
            ResetRankListPosition();
            if (GameEntry.Rank != null)
            {
                ApplyOfflineFakeProgress();
                GameEntry.Rank.OnCycleCountdownChanged += OnRankCycleCountdownChanged;
                GameEntry.Rank.OnCycleChanged += OnRankCycleChanged;
                GameEntry.Rank.OnRanksChanged += OnRanksChanged;
                RefreshRankCycleUI();
                RefreshRankList(GameEntry.Rank.Ranks);
            }

            m_tltleTmp.text = GetCurrentRankTitle();

            m_WinStreakMultiplierUI.Refresh(0);
        }

        private void ResetRankListPosition()
        {
            if (m_RankItemRoot != null)
            {
                Vector2 anchoredPosition = m_RankItemRoot.anchoredPosition;
                anchoredPosition.y = 0f;
                m_RankItemRoot.anchoredPosition = anchoredPosition;
            }

            if (m_RankListSR != null)
            {
                m_RankListSR.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// 获取当前排行榜收集物品标题
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentRankTitle(bool enableLineBreak = false)
        {
            string titleKey = GameEntry.Rank != null ? GameEntry.Rank.CurrentTargetTitleKey : null;
            string title = GameEntry.Localization.GetString(string.IsNullOrEmpty(titleKey) ? "Rank" : titleKey);
            if (!enableLineBreak)
            {
                title = title.Replace("<br>", " ");
            }
            return title;
        }
        /// <summary>
        /// 获取当前排行榜收集物品描述
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentRankDescription()
        {
            string descriptionKey = GameEntry.Rank != null ? GameEntry.Rank.CurrentTargetDescriptionKey : null;
            return GameEntry.Localization.GetString(string.IsNullOrEmpty(descriptionKey) ? "描述" : descriptionKey);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            AdsAnalytics.EventWithName("排行榜页面_关闭");
            if (GameEntry.Rank != null)
            {
                GameEntry.Rank.OnCycleCountdownChanged -= OnRankCycleCountdownChanged;
                GameEntry.Rank.OnCycleChanged -= OnRankCycleChanged;
                GameEntry.Rank.OnRanksChanged -= OnRanksChanged;
            }
            UnsubscribeLevelChangedEvent();
            if (GameEntry.SaveData != null)
                GameEntry.SaveData.LastExitTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            base.OnClose(isShutdown, userData);
        }

        /// <summary>
        /// 距上次退出排行榜面板的间隔秒数大于 0 时，根据时间让部分假玩家分数自然增长。
        /// </summary>
        private void ApplyOfflineFakeProgress()
        {
            var saveData = GameEntry.SaveData;
            if (saveData == null) return;
            long lastExit = saveData.LastExitTime;
            if (lastExit <= 0) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - lastExit;
            if (elapsed <= 0) return;
            GameEntry.Rank.ApplyFakePlayersOfflineProgress(elapsed, pointsPerHour: 4);
        }
        private void SetStartBtnState()
        {
            if (m_LevelBtnState != null)
                m_LevelBtnState.SetState(ButtonState.Normal);
        }

        private SubGameManagerComponent m_ProgressSource;
        private void SubscribeLevelChangedEvent()
        {
            UnsubscribeLevelChangedEvent();
            m_ProgressSource = GameEntry.SubGames?.Get(GameEntry.GameManager.PrimaryGameMode);
            if (m_ProgressSource != null) m_ProgressSource.ProgressChanged += OnCurrentLevelChanged;
        }

        private void UnsubscribeLevelChangedEvent()
        {
            if (m_ProgressSource != null) m_ProgressSource.ProgressChanged -= OnCurrentLevelChanged;
            m_ProgressSource = null;
        }

        private void OnCurrentLevelChanged(int level)
        {
            SetStartBtnState();
        }

        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }
        private void OnClickLevel()
        {
            PlayUISound(SoundId.UI_Click);

            Log.Info("[RankUI] 开始按钮被点击了");
            Close();
            m_procedureMenu.StartGame();
        }

        private void OnClickTips()
        {
            PlayUISound(SoundId.UI_Click);
            Log.Info("[RankUI] 提示按钮被点击了");
            GameEntry.UI.OpenUIForm(UIFormId.RankTipsUIPanel, new RankTipsUIPanel.OpenData
            {
                source = RankTipsUIPanel.OpenSource.RankingPage
            });
        }

        private void OnRankCycleChanged(RankCycleData cycle)
        {
            RefreshRankCycleUI();
            if (GameEntry.Rank != null)
                RefreshRankList(GameEntry.Rank.Ranks);
        }

        private void OnRankCycleCountdownChanged(int remainingSeconds)
        {
            if (m_CountdownTMP != null)
                m_CountdownTMP.text = NumberFormatUtil.FormatToHM(GameEntry.Rank.CycleRemainingSeconds, GameEntry.SaveData.IsCN);
        }

        private void OnRanksChanged(IReadOnlyList<RankData> ranks)
        {
            RefreshRankList(ranks);
        }

        private void RefreshRankCycleUI()
        {
            if (GameEntry.Rank == null) return;

            if (m_CountdownTMP != null)
                m_CountdownTMP.text = NumberFormatUtil.FormatToHM(GameEntry.Rank.CycleRemainingSeconds, GameEntry.SaveData.IsCN);

        }

        private void RefreshRankList(IReadOnlyList<RankData> ranks)
        {
            if (ranks == null) return;
            Sprite itemIconSprite = GameEntry.Rank != null ? GameEntry.Rank.CurrentTargetItemSprite : null;

            int preplacedCount = m_RankingItems != null ? m_RankingItems.Count : 0;
            for (int i = 0; i < preplacedCount; i++)
            {
                UI_RankingItem item = m_RankingItems[i];
                if (item == null) continue;

                if (i < ranks.Count)
                {
                    item.gameObject.SetActive(true);
                    ApplyItem(item, ranks[i], itemIconSprite);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }

            int scrollCount = Mathf.Max(0, ranks.Count - preplacedCount);
            m_CurrentRanks = ranks;
            m_CurrentItemIconSprite = itemIconSprite;
            m_RankVirtualList.SetDataCount(scrollCount, false);
        }

        private void BindVirtualRankItem(int virtualIndex, UI_RankingItem item)
        {
            int preplacedCount = m_RankingItems != null ? m_RankingItems.Count : 0;
            int rankIndex = preplacedCount + virtualIndex;
            if (m_CurrentRanks == null || rankIndex < 0 || rankIndex >= m_CurrentRanks.Count)
                return;

            ApplyItem(item, m_CurrentRanks[rankIndex], m_CurrentItemIconSprite);
        }

        private void ApplyItem(UI_RankingItem item, RankData data, Sprite itemIcon)
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

            item.SetData(data.Rank, playerName, avatar, frame, data.Score, data.IsSelf);

            item.SetRewardIcon(GetChestIconByRank(data.Rank));
            item.SetItemSlot(itemIcon, data.Score);
        }


        private Sprite GetChestIconByRank(int rank)
        {
            if (rank < 0 || rank > GameEntry.CustomConfig.RewardConfig.RankRewardList.Count) return null;
            if (rank < 4)
            {
                return m_ChestIconList[rank - 1];
            }
            return m_ChestIconList[3];
        }
    }
}
