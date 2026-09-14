using Ads;
using DG.Tweening;
using GameFramework;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

using Cysharp.Threading.Tasks;
namespace Lokas
{
    public class MainUIPanel : UGuiForm
    {
        [SerializeField] private RectTransform m_SettingRect;
        [SerializeField] private Button m_ProfileBtn;
        [SerializeField] private Button m_RankingBtn;
        [SerializeField] private UI_AvatarBox m_avatarBox;
        [SerializeField] private TMP_Text m_playerName;

        [SerializeField] private UIGraphicColorGroup m_taskBtnGraphicGroup;
        [SerializeField] private UIGraphicColorGroup m_rankingBtnGraphicGroup;
        [SerializeField] private UIGraphicColorGroup m_dailyBtnGraphicGroup;
        [SerializeField] private UIGraphicColorGroup m_shopBtnGraphicGroup;


        [SerializeField] private TMP_Text m_taskLockTips;
        [SerializeField] private TMP_Text m_rankingLockTips;
        [SerializeField] private TMP_Text m_dailyLockTips;
        [SerializeField] private TMP_Text m_shopLockTips;

        [SerializeField] private RectTransform m_LevelRect;
        [SerializeField] private ButtonStateController m_StartStateBtn;
        [SerializeField] private TMP_Text m_LevelTMP;


        [SerializeField] private RectTransform m_RectMatchLevelRT;
        [SerializeField] private ButtonStateController m_RectMatchStateBtn;
        [SerializeField] private TMP_Text m_RectMatchLevelTMP;


        private ProcedureMenu m_ProcedureMenu;
        [SerializeField] private GameMode m_SecondaryGameMode = GameMode.None;
        private SubGameManagerComponent m_BoundPrimary;
        private SubGameManagerComponent m_BoundSecondary;

        protected override void OnInit(object userData)
        {
            m_ProcedureMenu = (ProcedureMenu)userData;
            m_SettingRect.GetComponent<Button>().AddSafeClick(OnClickSetting);
            m_StartStateBtn.OnClick.AddListener(OnClickStart);
            m_RectMatchStateBtn.OnClick.AddListener(OnClickStartRectMatch);
            m_ProfileBtn.AddSafeClick(OnClickProfile);
            m_RankingBtn.AddSafeClick(OnClickRanking);
            base.OnInit(userData);
        }

        public override void InitLocalization()
        {
            base.InitLocalization();
            SetStartBtnState();

        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            SetStartBtnState();
            UpdatePlayerInfo();
            SetStartBtnState();
            SetUIInteractable();
            TryOpenRankReward();
            AdsAnalytics.EventWithName("首页_打开");
            AdsManager.ShowBanner(AdsServerConfig.PrimaryGameMode);
        }

        protected override void OnReveal()
        {

            UpdatePlayerInfo();
            SetStartBtnState();
            SetUIInteractable();
            base.OnReveal();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);

            AdsAnalytics.EventWithName("首页_关闭");

        }

        public async UniTaskVoid TryOpenRankReward()
        {
            SetBlocksRaycasts(false);
            try
            {
                if (GameEntry.Rank == null || GameEntry.UI == null)
                {
                    return;
                }

                if (!GameEntry.Rank.TryPopPendingRankReward(out int rank))
                {
                    return;
                }

                await UniTask.Delay(1000);
                var rankRewardList = GameEntry.CustomConfig?.RewardConfig?.RankRewardList;
                if (rankRewardList == null || rank < 1 || rank > RankComponent.MaxRankRewardRank || rank > rankRewardList.Count)
                {
                    return;
                }

                var rewardDataSO = rankRewardList[rank - 1];
                if (rewardDataSO?.rewardDatas == null || rewardDataSO.rewardDatas.Count == 0)
                {
                    return;
                }

                ChestSkinType chestSkinType = ChestSkinType.Chest_4;
                switch (rank)
                {
                    case 1:
                        chestSkinType = ChestSkinType.Chest_1;
                        break;
                    case 2:
                        chestSkinType = ChestSkinType.Chest_2;
                        break;
                    case 3:
                        chestSkinType = ChestSkinType.Chest_3;
                        break;
                }

                var chestRewardData = new ChestRewardData(true, chestSkinType, rewardDataSO.rewardDatas, sourcePage: "Main");

                chestRewardData.OnClaim = () =>
                {
                    if (m_LevelRect != null)
                    {
                        m_LevelRect.PlayScale(1, 0.8f, 0.3f).Forget();
                    }
                };

                if (m_LevelRect != null && GameEntry.UI.UICamera != null)
                {
                    chestRewardData.userData = GameEntry.UI.UICamera.WorldToScreenPoint(m_LevelRect.position);
                }

                GameEntry.UI.OpenUIForm(UIFormId.ClaimRewardsUIPanel, chestRewardData);
            }
            finally
            {
                SetBlocksRaycasts(true);
            }
        }

        public static ButtonState GetLevelBtnState()
        {
            return ButtonState.Normal;
        }


        private void SetStartBtnState()
        {
            m_StartStateBtn.SetState(GetLevelBtnState());
            m_RectMatchStateBtn.SetState(GetLevelBtnState());
            var primary = GameEntry.SubGames?.Get(GameEntry.GameManager.PrimaryGameMode);
            var secondary = GameEntry.SubGames?.Get(m_SecondaryGameMode);
            m_StartStateBtn.gameObject.SetActive(primary != null);
            m_RectMatchStateBtn.gameObject.SetActive(secondary != null && secondary != primary);
            m_LevelTMP.text = primary != null ? GameEntry.Localization.GetString(LocalizationKeys.LEVEL, primary.DisplayLevel) : string.Empty;
            m_RectMatchLevelTMP.text = secondary != null ? GameEntry.Localization.GetString(LocalizationKeys.LEVEL, secondary.DisplayLevel) : string.Empty;

        }

        private void UpdatePlayerInfo()
        {
            m_playerName.text = GameEntry.SaveData.PlayerName;

            var db = GameEntry.CustomConfig.AvatarConfig;
            Sprite avatarSpr = db.TryGetAvatar(GameEntry.SaveData.AvatarId, out var avatarSO) ? avatarSO.sprite : null;
            Sprite frameSpr = db.TryGetFrame(GameEntry.SaveData.AvatarFrameId, out var frameSO) ? frameSO.sprite : null;
            m_avatarBox.SetAvatar(avatarSpr);
            m_avatarBox.SetFrame(frameSpr);
        }


        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            m_BoundPrimary = GameEntry.SubGames?.Get(GameEntry.GameManager.PrimaryGameMode);
            m_BoundSecondary = GameEntry.SubGames?.Get(m_SecondaryGameMode);
            if (m_BoundPrimary != null) m_BoundPrimary.ProgressChanged += OnCurrentLevelChanged;
            if (m_BoundSecondary != null && m_BoundSecondary != m_BoundPrimary) m_BoundSecondary.ProgressChanged += OnRectMatchCurrentLevelChanged;
        }


        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();
            if (m_BoundPrimary != null) m_BoundPrimary.ProgressChanged -= OnCurrentLevelChanged;
            if (m_BoundSecondary != null && m_BoundSecondary != m_BoundPrimary) m_BoundSecondary.ProgressChanged -= OnRectMatchCurrentLevelChanged;
            m_BoundPrimary = null;
            m_BoundSecondary = null;

        }

        private void OnCurrentLevelChanged(int obj)
        {
            SetUIInteractable();
            SetStartBtnState();
        }

        private void OnRectMatchCurrentLevelChanged(int obj)
        {
            SetStartBtnState();
        }


        private void SetUIInteractable()
        {
            var config = AdsServerConfig.Common;
            bool hasProgress = AdsServerConfig.TryGetPrimaryLevel(out int level);
            // Task 当前没有关卡门槛，此次只迁移配置提示，不激活旧的未接入规则。
            m_taskBtnGraphicGroup?.SetInteractable(GameEntry.Task.IsUnlocked);
            m_rankingBtnGraphicGroup?.SetInteractable(GameEntry.Rank.IsUnlocked);
            m_dailyBtnGraphicGroup?.SetInteractable(hasProgress && level >= config.FirstWinUnlockLevel);

            m_taskLockTips?.SetText(Utility.Text.Format("Lv.{0}", config.TaskUnlockLevel));
            m_rankingLockTips?.SetText(Utility.Text.Format("Lv.{0}", config.RankUnlockLevel));
            m_dailyLockTips?.SetText(Utility.Text.Format("Lv.{0}", config.FirstWinUnlockLevel));
        }

        #region 按钮点击事件绑定

        /// <summary>
        /// 点击设置按钮
        /// </summary>
        private void OnClickSetting()
        {
            PlayUISound(SoundId.UI_Click);
            GameEntry.UI.OpenUIForm(UIFormId.SettingUIPanel, new SettingUIData(SettingType.Main));
            Log.Info("设置按钮被点击了");
        }

        /// <summary>
        /// 点击开始按钮
        /// </summary>
        private void OnClickStart()
        {
            PlayUISound(SoundId.UI_Click);


            if (!GameEntry.SaveData.GetEventData("FirstStartGame"))
            {
                GameEntry.SaveData.SetEventData("FirstStartGame");
                AdsAnalytics.EventWithName("用户首次点击开始游戏");
            }


            Log.Info("开始按钮被点击了");
            m_ProcedureMenu.StartGame();

        }

        /// <summary>
        /// 从首页进入 RectMatch 子游戏。
        /// </summary>
        private void OnClickStartRectMatch()
        {
            PlayUISound(SoundId.UI_Click);

            if (!GameEntry.SaveData.GetEventData("FirstStartGame"))
            {
                GameEntry.SaveData.SetEventData("FirstStartGame");
                AdsAnalytics.EventWithName("用户首次点击开始游戏");
            }

            Log.Info("开始已配置副玩法: {0}", m_SecondaryGameMode);
            m_ProcedureMenu?.StartGame(m_SecondaryGameMode);
        }


        private void OnClickProfile()
        {
            PlayUISound(SoundId.UI_Click);
            GameEntry.UI.OpenUIForm(UIFormId.ProfileUIPanel);
            Log.Info("个人信息按钮被点击了");
        }

        private void OnClickRanking()
        {
            PlayUISound(SoundId.UI_Click);
           
            if (GameEntry.Rank.IsUnlocked)
            {
                GameEntry.UI.OpenUIForm(UIFormId.RankingUIPanel);
            }
            else
            {
                PromptUIPanel.ShowToast(GameEntry.Localization.GetString(LocalizationKeys.Rank_LockTips, AdsServerConfig.Common.RankUnlockLevel));
            }

            Log.Info("排行榜按钮被点击了");
        }

        #endregion
    }
}
