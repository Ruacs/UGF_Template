using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class ReviveUIPanel : UGuiForm
    {
        [SerializeField] private Button m_BtnClose;

        [SerializeField] private Button m_BtnRevive;
        [SerializeField] private Button m_BtnRestart;

        [SerializeField] private RectTransform m_freeCorner;
        [SerializeField] private RectTransform m_adsCorner;

        private ProcedureGame m_ProcedureGame;

        private bool m_FristFree;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);



            if (m_BtnClose != null)
            {
                m_BtnClose.AddSafeClick(OnClickRestart);
            }

            if (m_BtnRevive != null)
            {
                m_BtnRevive.AddSafeClick(OnClickRevive);
            }

            if (m_BtnRestart != null)
            {
                m_BtnRestart.AddSafeClick(OnClickRestart);
            }
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_ProcedureGame = (ProcedureGame)userData;
            m_FristFree = !GameEntry.SaveData.GetEventData("Revive_Frist");
            m_freeCorner.gameObject.SetActive(m_FristFree);
            m_adsCorner.gameObject.SetActive(!m_FristFree);
        }
        private void OnClickRestart()
        {
            PlayUISound(SoundId.UI_Click);
            Ads.AdsAnalytics.EventWithName("复活页面_点击重新开始");
            GameEntry.UI.OpenUIForm(UIFormId.FailUIPanel, m_ProcedureGame);
            Close();


        }


        private void OnClickRevive()
        {
            PlayUISound(SoundId.UI_Click);

            Ads.AdsAnalytics.EventWithName("复活页面_点击复活");


            Ads.AdsAnalytics.EventWithName("ClickRevive", ("lv", GetCurrentLevel()), ("scene", "Revive"));

            if (m_FristFree)
            {
                if (m_ProcedureGame != null && m_ProcedureGame.Revive())
                {
                    Ads.AdsAnalytics.EventWithName("ReviveSuccess", ("lv", GetCurrentLevel()), ("type", "free"), ("scene", "Revive"));
                    GameEntry.SaveData.SetEventData("Revive_Frist");
                    Close();
                }
                return;
            }

            Action action = () =>
            {
                if (m_ProcedureGame == null || !m_ProcedureGame.Revive())
                {
                    return;
                }

                Ads.AdsAnalytics.EventWithName("ReviveSuccess", ("lv", GetCurrentLevel()), ("type", "ad"), ("scene", "Revive"));
                Close();
            };

            Ads.AdsManager.ShowRewardedAd(action);

        }

        private int GetCurrentLevel() => GameEntry.GameManager.GetCurrentLevel();


        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }
    }
}
