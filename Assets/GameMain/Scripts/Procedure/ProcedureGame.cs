using GameFramework.Event;
using GameFramework.Fsm;
using UnityGameFramework.Runtime;

namespace Lokas
{
    public abstract class ProcedureGame : ProcedureBase
    {
        private bool m_ReturnMenu;
        protected IFsm<GameFramework.Procedure.IProcedureManager> m_ProcedureOwner;

        protected override void OnEnter(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_ProcedureOwner = procedureOwner;
            m_ReturnMenu = false;

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEnitiySuccess);

        }

        protected override void OnUpdate(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_ReturnMenu)
            {
                procedureOwner.SetData<VarInt32>("NextSceneId", GameEntry.Config.GetInt("Scene.Menu"));
                procedureOwner.SetData<VarBoolean>(ProcedureChangeScene.ShowLoadingProgressKey, true);
                ChangeState<ProcedureChangeScene>(procedureOwner);
            }
        }

        protected override void OnLeave(IFsm<GameFramework.Procedure.IProcedureManager> procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEnitiySuccess);
            base.OnLeave(procedureOwner, isShutdown);
        }

        public virtual void ReturnMenu()
        {
            m_ReturnMenu = true;
        }

        public virtual void NextLevel()
        {
        }

        public virtual void UpdateLevelData()
        {
        }

        public virtual void Restart()
        {
        }

        /// <summary>
        /// 当前流程是否允许复活。
        /// </summary>
        public virtual bool CanRevive()
        {
            return false;
        }

        /// <summary>
        /// 尝试复活当前关卡。
        /// </summary>
        /// <returns>复活成功返回 true，否则返回 false。</returns>
        public virtual bool Revive()
        {
            return false;
        }

        protected virtual void OnOpenUIFormSuccess(object sender, GameEventArgs e)
        {
            OpenUIFormSuccessEventArgs en = (OpenUIFormSuccessEventArgs)e;
            if (en.UserData != this)
            {
                return;
            }

       

        }
        protected virtual void OnShowEnitiySuccess(object sender, GameEventArgs e)
        {
            ShowEntitySuccessEventArgs en = (ShowEntitySuccessEventArgs)e;

 
        }

    }
}
