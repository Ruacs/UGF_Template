
using Ads;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameFramework.Event;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
namespace Lokas
{
    public class GameUIPanel : UGuiForm
    {
 
        [SerializeField] private TMP_Text m_levelTMP; 


        ProcedureGame m_ProcedureGame;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData); 

        }

        protected override void OnInitUIAnimation()
        {
            base.OnInitUIAnimation();
        }


        protected override void OnOpen(object userData)
        {
            m_ProcedureGame = (ProcedureGame)userData;

            SubscribeEvents(); 
            m_levelTMP.text = "LEVEL " + (GameEntry.SubGames?.Get(GameEntry.GameManager.PrimaryGameMode)?.DisplayLevel ?? 0);
 
            base.OnOpen(userData);

            AdsAnalytics.EventWithName("游戏页面_打开");

       
       

        }



        private void GameManager_OnGameOver()
        {
            GameEntry.UI.OpenUIForm(UIFormId.GameOverUIPanel, m_ProcedureGame);
        }


        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
 
        }


        protected override void OnClose(bool isShutdown, object userData)
        {
            AdsAnalytics.EventWithName("游戏页面_关闭");
            UnsubscribeEvents();
            base.OnClose(isShutdown, userData);
        }




        #region Event Handlers

    
        #endregion

    }

}
