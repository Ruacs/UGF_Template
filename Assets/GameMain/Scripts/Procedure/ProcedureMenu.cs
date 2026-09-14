using GameFramework.Event;
using GameFramework.Fsm;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class ProcedureMenu : ProcedureBase
    {
        private bool m_startGame = false;
        private int m_NextGameSceneId;

        private MainUIPanel m_mainUIPanel;


        protected override void OnEnter(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner)
        {


            base.OnEnter(procedureOwner);


            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEnitiySuccess);
            GameEntry.UI.OpenUIForm(UIFormId.MainUIPanel, this);

            m_startGame = false;
            m_NextGameSceneId = 0;

        }



        protected override void OnUpdate(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_startGame)
            {
                procedureOwner.SetData<VarInt32>("NextSceneId", m_NextGameSceneId);
                // procedureOwner.SetData<VarBoolean>(ProcedureChangeScene.ReuseLoadingProgressKey, false);
                procedureOwner.SetData<VarBoolean>(ProcedureChangeScene.ShowLoadingProgressKey, true);
                ChangeState<ProcedureChangeScene>(procedureOwner);
            }
        }


        protected override void OnLeave(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner, bool isShutdown)
        {


            m_mainUIPanel?.Close();
            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEnitiySuccess);
            base.OnLeave(procedureOwner, isShutdown);


        }

        private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
        {
            OpenUIFormSuccessEventArgs en = (OpenUIFormSuccessEventArgs)e;
            if (en.UserData != this)
            {
                return;
            }

            if (en.UIForm.Logic is MainUIPanel)
            {
                m_mainUIPanel = (MainUIPanel)en.UIForm.Logic;
            }

        }
        private void OnShowEnitiySuccess(object sender, GameEventArgs e)
        {
            ShowEntitySuccessEventArgs en = (ShowEntitySuccessEventArgs)e;


            if (en.Entity.Logic is Entity entity)
            {

            }
        }


        public void StartGame()
        {
            StartGame(GameEntry.GameManager.PrimaryGameMode);
        }

        /// <summary>
        /// Starts a registered child game without making the menu depend on a specific game UI.
        /// </summary>
        public void StartGame(GameMode gameMode)
        {
            var manager = GameEntry.SubGames?.Get(gameMode);
            if (manager == null)
            {
                Log.Warning("Can not start uninstalled child game mode '{0}'.", gameMode);
                return;
            }
            m_NextGameSceneId = GameEntry.Config.GetInt(manager.SceneConfigKey);
            if (m_NextGameSceneId <= 0) { Log.Error("Missing subgame scene: {0}", manager.SceneConfigKey); return; }
            m_startGame = true;
        }

    }
}
