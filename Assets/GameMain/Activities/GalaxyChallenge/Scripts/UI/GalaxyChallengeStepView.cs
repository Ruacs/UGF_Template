using TMPro;
using UnityEngine;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    /// <summary>预制体中的固定一步；脚本只切换状态，不创建、移动或缩放 UI。</summary>
    public sealed class GalaxyChallengeStepView : MonoBehaviour
    {
        [SerializeField] private RectTransform m_PeopleRoot;
        [SerializeField] private GameObject m_CompletedState;
        [SerializeField] private GameObject m_CurrentState;
        [SerializeField] private GameObject m_PlayerMarker;
        [SerializeField] private TMP_Text m_StepText;
        [SerializeField] private TMP_Text m_PeopleText;

        public RectTransform PeopleRoot => m_PeopleRoot;

        public void Bind(int step, bool completed, bool current, int people)
        {
            if (m_CompletedState != null) m_CompletedState.SetActive(completed);
            if (m_CurrentState != null) m_CurrentState.SetActive(current);
            if (m_PlayerMarker != null) m_PlayerMarker.SetActive(current);
            if (m_StepText != null)
            {
                if (step <= 0) m_StepText.SetText("START");
                else m_StepText.SetText("LEVEL {0}", step);
            }
            if (m_PeopleText != null) m_PeopleText.SetText("{0}", people);
        }
    }
}
