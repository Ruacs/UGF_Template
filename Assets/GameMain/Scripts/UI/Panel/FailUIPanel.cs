using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class FailUIPanel : UGuiForm
    {
        [SerializeField] private Button m_BtnClose;
        [SerializeField] private Button m_BtnGetHeart;
        [SerializeField] private Button m_BtnRestart;

        private ProcedureGame m_ProcedureGame;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData); 
            m_BtnRestart.AddSafeClick(OnClickRestart);
        }

        protected override void OnOpen(object userData)
        {
            PlayUISound(SoundId.UI_Fail);
            base.OnOpen(userData);
            m_ProcedureGame = (ProcedureGame)userData;

        }
 
        private void OnClickRestart()
        {
            PlayUISound(SoundId.UI_Click);
            m_ProcedureGame.Restart();
            Close();
        }

        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }
    }
}
