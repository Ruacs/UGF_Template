using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.WinStreak.UI
{
    /// <summary>连胜规则说明弹窗；图文内容由 Prefab 素材维护。</summary>
    public sealed class WinStreakDetailUIPanel : UGuiForm
    {
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private Button m_ContinueButton;
        private WinStreakActivityModule m_Module;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
            if (m_ContinueButton != null) m_ContinueButton.AddSafeClick(OnContinue);
        }

        protected override void OnOpen(object userData)
        {
            m_Module = (userData as ActivityPageUserData)?.Request.Arguments as WinStreakActivityModule;
            if (m_Module == null) GameEntry.Activities?.TryGetModule(WinStreakActivityModule.Id, out m_Module);
            base.OnOpen(userData);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            WinStreakActivityModule module = m_Module;
            m_Module = null;
            base.OnClose(isShutdown, userData);
            module?.NotifyRulesPageClosed();
        }

        private void OnContinue()
        {
            if (m_Module != null) m_Module.OpenMainAsync().Forget(Debug.LogException);
            Close();
        }

#if UNITY_EDITOR
        public void BindSerializedReferences(Button closeButton, Button continueButton)
        {
            m_CloseButton = closeButton;
            m_ContinueButton = continueButton;
        }
#endif
    }
}
