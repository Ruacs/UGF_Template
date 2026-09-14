using System.Collections.Generic;
using GameFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{


    public class NewClueUIData
    {
        public ClueType ClueType;
    }
    public class NewClueUIPanel : UGuiForm
    {
        [SerializeField] private Button m_BtnClose;

        [SerializeField] private TMP_Text m_TitleTMP;
        [SerializeField] private TMP_Text m_DescTMP;

        [SerializeField] private List<GameObject> m_ClueGOList;

        private NewClueUIData m_ClueData;


        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            if (m_BtnClose != null)
            {
                m_BtnClose.AddSafeClick(OnClickClose);
            }
        }

        override protected void OnOpen(object userData)
        {
            base.OnOpen(userData);

            m_ClueData = userData as NewClueUIData;

            if (m_ClueData != null)
            {
                m_TitleTMP.text = GameEntry.Localization.GetString(Utility.Text.Format(LocalizationKeys.NewClue_Title, (int)m_ClueData.ClueType));
                m_DescTMP.text = GameEntry.Localization.GetString(Utility.Text.Format(LocalizationKeys.NewClue_Desc, (int)m_ClueData.ClueType));

                for (int i = 0; i < m_ClueGOList.Count; i++)
                {
                    m_ClueGOList[i].SetActive(i == ((int)m_ClueData.ClueType) - 1);
                }
            }
        }

        private void OnClickClose()
        {
            PlayUISound(SoundId.UI_Close);
            Close();
        }
    }
}
