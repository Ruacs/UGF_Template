using GameFramework.Event;
using GameFramework.Localization;
using Lokas;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Lokas
{
    public class ProcedureLaunch : ProcedureBase
    {
        // Start is called before the first frame update
        /// <summary>
        /// 社会化流程调用
        /// </summary>
        /// <param name="procedureOwner"></param>
        protected override void OnInit(ProcedureOwner procedureOwner)
        {
            base.OnInit(procedureOwner);
        }

        /// <summary>
        /// 进入流程调用
        /// </summary>
        /// <param name="procedureOwner"></param>
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            InitLanguageSettings();
        }
        /// <summary>
        /// 初始化语言
        /// </summary>
        private void InitLanguageSettings()
        {
            GameEntry.Localization.Language = GameEntry.Base.EditorLanguage;//将语言强制设置为GF框架设置的语言
            if (GameEntry.Base.EditorResourceMode && GameEntry.Base.EditorLanguage != Language.Unspecified)
            {
                // 编辑器资源模式直接使用 Inspector 上设置的语言
                return;
            }
        }

        /// <summary>
        /// 每帧调用
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            ChangeState<ProcedureSplash>(procedureOwner);

        }

        /// <summary>
        /// 离开调用
        /// </summary>
        /// <param name="procedureOwner"></param>
        /// <param name="isShutdown"></param>
        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// 销毁时调用
        /// </summary>
        /// <param name="procedureOwner"></param>
        protected override void OnDestroy(ProcedureOwner procedureOwner)
        {
            base.OnDestroy(procedureOwner);
        }
    }
}
