using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    /// <summary>三段玩法说明由 Prefab 图文完整持有。</summary>
    public sealed class GalaxyChallengeDetailUIPanel : UGuiForm
    {
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Button m_ContinueButton;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
            if (m_ContinueButton != null) m_ContinueButton.AddSafeClick(() => Close());
        }
    }
}
