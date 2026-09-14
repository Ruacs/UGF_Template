using GameFramework.Event;
using GameFramework.Fsm;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public class ProcedureGameDefault : ProcedureGame
    {
        protected override void OnEnter(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            GameEntry.UI.OpenUIForm(UIFormId.GameUIPanel, this); 
 
        }

        protected override void OnUpdate(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        }

        protected override void OnLeave(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner, bool isShutdown)
        {
            GameEntry.DefaultGameManager?.ResetGame();
            base.OnLeave(procedureOwner, isShutdown);
        }

        public override void NextLevel()
        {
            m_ProcedureOwner.SetData<VarInt32>("NextSceneId", GameEntry.Config.GetInt("Scene.Game"));
            m_ProcedureOwner.SetData<VarBoolean>(ProcedureChangeScene.ShowLoadingProgressKey, false);
            ChangeState<ProcedureChangeScene>(m_ProcedureOwner);
        }

        public override void Restart()
        {
            m_ProcedureOwner.SetData<VarInt32>("NextSceneId", GameEntry.Config.GetInt("Scene.Game"));
            m_ProcedureOwner.SetData<VarBoolean>(ProcedureChangeScene.ShowLoadingProgressKey, false);
            ChangeState<ProcedureChangeScene>(m_ProcedureOwner);
        }


        override protected void OnOpenUIFormSuccess(object sender, GameEventArgs e)
        {
            base.OnOpenUIFormSuccess(sender, e);
            OpenUIFormSuccessEventArgs en = (OpenUIFormSuccessEventArgs)e;
            if (en.UserData != this)
            {
                return;
            }

            if (en.UIForm.Logic is GameUIPanel gameUIPanel)
            {
                 
            }
        }
    }
}
