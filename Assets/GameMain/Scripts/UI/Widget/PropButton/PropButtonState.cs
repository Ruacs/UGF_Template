using System;

namespace Lokas
{
    /// <summary>
    /// 道具按钮运行时状态。业务层负责从存档、关卡和活动状态同步这些值。
    /// </summary>
    [Serializable]
    public class PropButtonState
    {
        private int m_Count;
        private int m_CurrentLevel;
        private bool m_IsUnlocked = true;
        private bool m_UseExplicitUnlockState;
        private bool m_IsSelected;
        private bool m_IsBusy;
        private bool m_IsInteractable = true;
        private bool m_ShowAdHint = true;
        private PropButtonDisplayMode m_DisplayMode = PropButtonDisplayMode.Normal;
        private string m_TimerText = string.Empty;

        public int Count => m_Count;
        public int CurrentLevel => m_CurrentLevel;
        public bool IsUnlocked => m_IsUnlocked;
        public bool UseExplicitUnlockState => m_UseExplicitUnlockState;
        public bool IsSelected => m_IsSelected;
        public bool IsBusy => m_IsBusy;
        public bool IsInteractable => m_IsInteractable;
        public bool ShowAdHint => m_ShowAdHint;
        public PropButtonDisplayMode DisplayMode => m_DisplayMode;
        public string TimerText => m_TimerText;

        public void SetCount(int count)
        {
            m_Count = count;
        }

        public void SetCurrentLevel(int currentLevel)
        {
            m_CurrentLevel = currentLevel;
        }

        /// <summary>
        /// 显式设置解锁状态。设置后会优先于 RequiredLevel 判断。
        /// </summary>
        public void SetUnlocked(bool isUnlocked)
        {
            m_IsUnlocked = isUnlocked;
            m_UseExplicitUnlockState = true;
        }

        /// <summary>
        /// 清除显式解锁覆盖，恢复为按配置 RequiredLevel 和 CurrentLevel 判断。
        /// </summary>
        public void ClearExplicitUnlockState()
        {
            m_UseExplicitUnlockState = false;
        }

        public void SetSelected(bool isSelected)
        {
            m_IsSelected = isSelected;
        }

        public void SetBusy(bool isBusy)
        {
            m_IsBusy = isBusy;
        }

        public void SetInteractable(bool isInteractable)
        {
            m_IsInteractable = isInteractable;
        }

        public void SetShowAdHint(bool showAdHint)
        {
            m_ShowAdHint = showAdHint;
        }

        public void SetDisplayMode(PropButtonDisplayMode displayMode)
        {
            m_DisplayMode = displayMode;
        }

        public void SetTimerText(string timerText)
        {
            m_TimerText = timerText ?? string.Empty;
        }
    }
}
