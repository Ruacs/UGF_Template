using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    public sealed class GalaxyChallengeEndUIPanel : UGuiForm
    { 
        [SerializeField] private TMP_Text m_BestProgress;
        [SerializeField] private TMP_Text m_PrizePool;
        [SerializeField] private Button m_CloseButton;
        private GalaxyChallengeActivityModule m_Module;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
        }

        protected override void OnOpen(object userData)
        {
            m_Module = GalaxyChallengeUIUtility.ResolveModule(userData);
            base.OnOpen(userData);
            if (m_Module == null) return;
            GalaxyChallengeSnapshot snapshot = m_Module.GetSnapshot();
            if (m_BestProgress != null) m_BestProgress.SetText("{0}/{1}", snapshot.BestProgress, snapshot.StepCount);
            if (m_PrizePool != null) m_PrizePool.SetText("{0}", snapshot.RewardCoinPool);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_Module = null;
            base.OnClose(isShutdown, userData);
        }
    }
}
