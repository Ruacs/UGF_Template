using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.WinStreak.UI
{
    /// <summary>活动结束/完成弹窗。</summary>
    public sealed class WinStreakEndUIPanel : UGuiForm
    {
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_Summary;
        [SerializeField] private Button m_CloseButton;
        private WinStreakActivityModule m_Module;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            if (m_CloseButton != null) m_CloseButton.AddSafeClick(() => Close());
        }

        protected override void OnOpen(object userData)
        {
            m_Module = (userData as ActivityPageUserData)?.Request.Arguments as WinStreakActivityModule;
            if (m_Module == null) GameEntry.Activities?.TryGetModule(WinStreakActivityModule.Id, out m_Module);
            base.OnOpen(userData);
            Refresh();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_Module = null;
            base.OnClose(isShutdown, userData);
        }

        private void Refresh()
        {
            if (m_Module == null) return;
            WinStreakSnapshot snapshot = m_Module.GetSnapshot();
            if (m_Title != null) m_Title.SetText(snapshot.DisplayName);
            if (m_Summary != null)
                m_Summary.SetText(string.Format("BEST STREAK {0}\nREWARDS CLAIMED THROUGH {1}", snapshot.BestProgress,
                    snapshot.RewardReceiveProgress));
        }

#if UNITY_EDITOR
        public void BindSerializedReferences(TMP_Text title, TMP_Text summary, Button closeButton)
        {
            m_Title = title;
            m_Summary = summary;
            m_CloseButton = closeButton;
        }
#endif
    }
}
