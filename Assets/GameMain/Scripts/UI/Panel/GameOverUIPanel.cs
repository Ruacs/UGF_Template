using Ads;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameFramework;
using GameFramework.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lokas
{
    public class GameOverUIPanel : UGuiForm
    {
        [SerializeField] private RectTransform m_titleRect;
        [SerializeField] private TMP_Text m_TitleTMP;

        [SerializeField] private RectTransform m_closeRect;
        [SerializeField] private RectTransform m_LevelBtnRT;
        [SerializeField] private ButtonStateController m_LevelStateBtn;
        [SerializeField] private TMP_Text m_LevelTMP;

        [SerializeField] private UI_Chest m_Chest;
        private bool m_chestProgressFull = false;
        private ProcedureGame m_ProcedureGame;
        private GameMode m_GameMode;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_LevelStateBtn.OnClick.AddListener(OnClickLevel);
        }

        public override void InitLocalization()
        {
            base.InitLocalization();

        }

        private void OnClickLevel()
        {
            PlayUISound(SoundId.UI_Click);
            //下一关
            m_ProcedureGame?.NextLevel();
            Close();
        }

        protected override void OnOpen(object userData)
        {
            m_GameMode = GameEntry.GameManager != null ? GameEntry.GameManager.CurrentGameMode : GameMode.None;
            PlayUISound(SoundId.UI_GameOver);
            int index = UnityEngine.Random.Range(1, 5);
            m_TitleTMP.SetText(GameEntry.Localization.GetString(Utility.Text.Format(LocalizationKeys.GameOver_Title, index)));
            base.OnOpen(userData);

            AdsAnalytics.EventWithName($"结算页_打开");

            m_ProcedureGame = (ProcedureGame)userData;



            m_LevelBtnRT.localScale = Vector3.zero;
            UpdateLevelData();
            SetStartBtnState(); 
            ShowChest();
        }


        private async void ShowChest()
        {
            if (m_Chest == null)
            {
                return;
            }

            m_Chest.ResetPos();
            m_Chest.RefreshProgress();
            if (ShouldShowChest())
            {
                await m_Chest.Show();
                await m_Chest.Refresh();
            }

            m_LevelBtnRT.DOScale(1, 0.3f).SetDelay(0.3f).SetEase(Ease.OutBack);
        }

        public bool ShouldShowChest()
        {
            return AdsServerConfig.TryGetPrimaryLevel(out int currentLevel)
                && currentLevel >= AdsServerConfig.Common.FirstWinUnlockLevel;
        }


        /// <summary>
        /// 更新关卡号 
        /// </summary>
        private void UpdateLevelData()
        {
            m_ProcedureGame?.UpdateLevelData();
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            if (Input.GetKeyDown(KeyCode.K))
            {
                PlayChsetAnimation(GameEntry.CustomConfig.RewardConfig.ChestRewardList[0].rewardDatas);
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
            AdsAnalytics.EventWithName($"结算页_关闭");

            if (AdsManager.ShouldShowAds(m_GameMode) && !m_chestProgressFull)
            {
                AdsManager.ShowInterstitialAd(YzAdComponent.YzAdLocation.Over);
            }
        }


        private void SetStartBtnState()
        {
           // m_LevelStateBtn.SetState(GetLevelBtnState());

        }

        public static ButtonState GetLevelBtnState()
        {
            return ButtonState.Normal;
        }

        public void OnCloseClick()
        {
            PlayUISound(SoundId.UI_Click);
            m_ProcedureGame?.ReturnMenu();
            Close();


        }


        protected override void SubscribeEvents()
        {
            GameEntry.Event.Subscribe(OnClaimRewardsEventArgs.EventId, OnChestProgressFull);
        }

        protected override void UnsubscribeEvents()
        {
            GameEntry.Event.Unsubscribe(OnClaimRewardsEventArgs.EventId, OnChestProgressFull);
        }


        private void OnChestProgressFull(object sender, GameEventArgs e)
        {
            var ne = (OnClaimRewardsEventArgs)e;
            m_chestProgressFull = true;
            PlayChsetAnimation(ne.rewardDatas);

        }



        public async void PlayChsetAnimation(List<RewardData> rewardDatas)
        {

            // Action action = () => { CheckWin(m_currentLevel, true); };

            var levelRect = m_LevelStateBtn.GetComponent<RectTransform>();

            string sourcePage = "GameOver";
            var chsetRewardData = new ChestRewardData(true, ChestSkinType.WinStreak, rewardDatas, null, sourcePage);


            chsetRewardData.OnClaim = () => { levelRect.PlayScale(1, 0.8f, 0.3f).Forget(); };
            chsetRewardData.userData = GameEntry.UI.UICamera.WorldToScreenPoint(levelRect.position);

            GameEntry.UI.OpenUIForm(UIFormId.ClaimRewardsUIPanel, chsetRewardData);
        }



    }

}
