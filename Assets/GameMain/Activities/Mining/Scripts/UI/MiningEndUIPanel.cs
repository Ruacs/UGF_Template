using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Mining.UI
{
    /// <summary>Mining 活动结束页；Next 返回被覆盖页面。</summary>
    public sealed class MiningEndUIPanel : UGuiForm
    {
        [SerializeField] private Button m_NextButton;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_NextButton != null) m_NextButton.AddSafeClick(() => Close());
        }

#if UNITY_EDITOR
        public void BindSerializedReferences(Button nextButton)
        {
            m_NextButton = nextButton;
        }
#endif
    }
}
