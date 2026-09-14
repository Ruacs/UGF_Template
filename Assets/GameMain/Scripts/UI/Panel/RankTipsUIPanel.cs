using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class RankTipsUIPanel : UGuiForm
    {
        public enum OpenSource
        {
            Unknown = 0,
            RankingPage = 1,
            MainPage = 2,
            GamePage = 3,
        }

        public class OpenData
        {
            public OpenSource source;
            public GameUIPanel gameUIPanel;
            public Action onGo;
        }

        [SerializeField] private Button m_CloseBtn;
        [SerializeField] private TMP_Text m_TitleTmp;
        [SerializeField] private TMP_Text m_DescriptionTmp;
        [SerializeField] private Image[] m_TileImages;
        [SerializeField] private Button m_GoBtn;
        [SerializeField] private TMP_Text m_TimerTmp;

        private GameUIPanel m_gameUIPanel;
        private OpenSource m_OpenSource = OpenSource.Unknown;
        private Action m_OnGoAction;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_CloseBtn.AddSafeClick(OnClickClose);
            m_GoBtn.AddSafeClick(OnClickGo);
        }

        protected override void OnOpen(object userData)
        {
            Ads.AdsAnalytics.EventWithName("排名提示页面_打开");
            base.OnOpen(userData);
            ApplyOpenData(userData);

            RefreshContent();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            Ads.AdsAnalytics.EventWithName("排名提示页面_关闭");
            // if (m_gameUIPanel != null)
            // {
            //     m_gameUIPanel?.OnSequentialPanelClosed(); 
            // }

            base.OnClose(isShutdown, userData);
        }

        private void RefreshContent()
        {
            if (GameEntry.Rank == null) return;

            int targetItemId = GameEntry.Rank.CurrentTargetItemId;
            m_TitleTmp.text = RankingUIPanel.GetCurrentRankTitle(true);
            m_DescriptionTmp.text = RankingUIPanel.GetCurrentRankDescription();

            if (m_TileImages != null)
            {
                for (int i = 0; i < m_TileImages.Length; i++)
                {
                    if (m_TileImages[i] != null)
                    {
                        m_TileImages[i].gameObject.SetActive(i == targetItemId - 201);
                    }
                }
            }

            if (m_TimerTmp != null)
                m_TimerTmp.text = NumberFormatUtil.FormatToHM(GameEntry.Rank.CycleRemainingSeconds, GameEntry.SaveData.IsCN);

            // PlayUISound(SoundId.SFX_PopTips);
        }

        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }

        private void OnClickGo()
        {
            PlayUISound(SoundId.UI_Click);
            HandleGoButtonEvent();
            Close();
        }

        private void ApplyOpenData(object userData)
        {
            m_OpenSource = OpenSource.Unknown;
            m_OnGoAction = null;
            m_gameUIPanel = null;

            if (userData == null)
                return;

           

            if (userData is OpenData openData)
            {
                 m_CloseBtn.gameObject.SetActive(openData.source != OpenSource.MainPage);    

                m_OpenSource = openData.source;
                m_gameUIPanel = openData.gameUIPanel;
                m_OnGoAction = openData.onGo;
                return;
            }

            if (userData is GameUIPanel gameUIPanel)
            {
                m_OpenSource = OpenSource.GamePage;
                m_gameUIPanel = gameUIPanel;
            }
        }

        private void HandleGoButtonEvent()
        {
            if (m_OnGoAction != null)
            {
                m_OnGoAction();
                return;
            }
 
        }
    }
}
