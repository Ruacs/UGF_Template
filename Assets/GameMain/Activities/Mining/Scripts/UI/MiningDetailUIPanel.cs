using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Mining.UI
{
    /// <summary>Mining 玩法说明页；点击说明区域即可返回。</summary>
    public sealed class MiningDetailUIPanel : UGuiForm
    {
        [SerializeField] private Button m_ContinueButton;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_ContinueButton != null) m_ContinueButton.AddSafeClick(() => Close());
        }

#if UNITY_EDITOR
        public void BindSerializedReferences(Button continueButton)
        {
            m_ContinueButton = continueButton;
        }
#endif
    }
}
