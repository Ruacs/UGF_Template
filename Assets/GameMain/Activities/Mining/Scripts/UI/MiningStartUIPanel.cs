using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Mining.UI
{
    /// <summary>Mining 活动开始说明页；视觉内容由 Prefab 素材维护。</summary>
    public sealed class MiningStartUIPanel : UGuiForm
    {
        [SerializeField] private Button m_StartButton;
        [SerializeField] private Button m_CloseButton;

        private MiningActivityModule m_Module;
        private bool m_IsOpeningMain;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_StartButton != null) m_StartButton.AddSafeClick(OnStartClicked);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
        }

        protected override void OnOpen(object userData)
        {
            m_Module = ExtractModule(userData);
            if (m_Module == null)
                GameEntry.Activities?.TryGetModule(MiningActivityModule.Id, out m_Module);
            m_IsOpeningMain = false;
            base.OnOpen(userData);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_Module = null;
            m_IsOpeningMain = false;
            base.OnClose(isShutdown, userData);
        }

        private void OnStartClicked()
        {
            OpenMainAsync().Forget(Debug.LogException);
        }

        private async UniTask OpenMainAsync()
        {
            if (m_Module == null || m_IsOpeningMain) return;
            m_IsOpeningMain = true;
            if (m_StartButton != null) m_StartButton.interactable = false;
            try
            {
                await m_Module.OpenMainAsync();
                Close();
            }
            finally
            {
                m_IsOpeningMain = false;
                if (m_StartButton != null) m_StartButton.interactable = true;
            }
        }

#if UNITY_EDITOR
        public void BindSerializedReferences(Button startButton, Button closeButton)
        {
            m_StartButton = startButton;
            m_CloseButton = closeButton;
        }
#endif

        private static MiningActivityModule ExtractModule(object userData) =>
            (userData as ActivityPageUserData)?.Request.Arguments as MiningActivityModule;
    }
}
