using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class ProcedureGameHexaAway : ProcedureGame
    {

        private HexaAwayUIPanel m_HexaAwayUIPanel;

        private bool m_NextLevel = false;
        private HexaAwayTestModeModule m_TestModeModule;
        private TestModeComponent m_TestModeOwner;
        private int m_EntryVersion;
        private bool m_IsActive;

        protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            int entryVersion = ++m_EntryVersion;
            m_IsActive = true;
            await GameEntry.HexaAway.InitializeResourcesAsync();
            if (!m_IsActive || entryVersion != m_EntryVersion)
            {
                return;
            }

            GameEntry.UI.OpenUIForm(UIFormId.HexaAwayUIPanel, this);
            GameEntry.HexaAway.GameStart();
            RegisterTestModeModule();
            m_NextLevel = false;
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            // if (m_NextLevel)
            // {
            //     procedureOwner.SetData<VarInt32>("NextSceneId", GameEntry.Config.GetInt("Scene.HexaAway"));
            //     procedureOwner.SetData<VarBoolean>(ProcedureChangeScene.ShowLoadingProgressKey, true);
            //     ChangeState<ProcedureChangeScene>(procedureOwner);
            // }
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {

            m_IsActive = false;
            ++m_EntryVersion;
            UnregisterTestModeModule();
            m_HexaAwayUIPanel?.Close();
            m_HexaAwayUIPanel = null;
            GameEntry.HexaAway?.ResetGame();
            base.OnLeave(procedureOwner, isShutdown);
        }

        public override void Restart()
        {
            base.Restart();
            GameEntry.HexaAway?.Restart();
        }

        public override bool CanRevive()
        {
            return GameEntry.HexaAway != null
                && GameEntry.HexaAway.CurrentResult == GameResult.Fail
                && GameEntry.HexaAway.IsGameOver
                && GameEntry.HexaAway.MovesCount <= 0;
        }

        public override bool Revive()
        {
            if (!CanRevive())
            {
                return false;
            }

            if (!GameEntry.HexaAway.AddMoves(5))
            {
                return false;
            }

            GameEntry.HexaAway.ResumeAfterRevive();
            return true;
        }

        public override void NextLevel()
        {
            base.NextLevel();

            GameEntry.HexaAway.ClearLevel();
            GameEntry.BuiltinView.ShowTransition();

            GameEntry.HexaAway.GameStart();

            GameEntry.BuiltinView.HideTransition(1);


        }

        public override void UpdateLevelData()
        {
            base.UpdateLevelData();
            if (GameEntry.SaveData == null)
            {
                return;
            }

            int nextLevelIndex = GameEntry.SaveData.Get<HexaAwayGameData>().CurrentLevel + 1;
            int levelCount = GameEntry.HexaAway != null ? GameEntry.HexaAway.AmountOfLevels : 0;
            if (levelCount > 0)
            {
                nextLevelIndex %= levelCount;
            }

            GameEntry.SaveData.Get<HexaAwayGameData>().CurrentLevel = nextLevelIndex;
        }


        public override void ReturnMenu()
        {
            base.ReturnMenu();
            GameEntry.HexaAway?.ReturnMenu();
        }

        protected override void OnOpenUIFormSuccess(object sender, GameEventArgs e)
        {
            OpenUIFormSuccessEventArgs en = (OpenUIFormSuccessEventArgs)e;
            if (en.UserData != this)
            {
                return;
            }

            if (en.UIForm.Logic is HexaAwayUIPanel)
            {
                m_HexaAwayUIPanel = (HexaAwayUIPanel)en.UIForm.Logic;
            }
        }

        private void RegisterTestModeModule()
        {
            UnregisterTestModeModule();
            m_TestModeOwner = GameEntry.TestMode;
            if (m_TestModeOwner == null) return;
            m_TestModeModule = new HexaAwayTestModeModule(GameEntry.HexaAway, GameEntry.SaveData?.Get<HexaAwayGameData>());
            m_TestModeOwner.RegisterModule(m_TestModeModule);
        }

        private void UnregisterTestModeModule()
        {
            if (m_TestModeOwner != null) m_TestModeOwner.UnregisterModule(m_TestModeModule);
            m_TestModeModule = null;
            m_TestModeOwner = null;
        }

    }

}
