using System;

namespace Lokas
{
    /// <summary>
    /// 道具按钮状态模型，聚合静态配置和运行时状态，并提供 UI 所需的判断结果。
    /// </summary>
    public class PropButtonModel
    {
        private PropButtonConfig m_Config;
        private PropButtonState m_State;
        private bool m_IsDirty = true;

        public PropButtonConfig Config => m_Config;
        public PropButtonState State => m_State;
        public bool IsDirty => m_IsDirty;

        public int ItemId => m_Config != null ? m_Config.ItemId : 0;
        public int Count => m_State != null ? m_State.Count : 0;
        public bool IsSelectable => m_Config != null && m_Config.IsSelectable;
        public bool IsInfinite => DisplayMode == PropButtonDisplayMode.InfiniteForever || DisplayMode == PropButtonDisplayMode.InfiniteWithTimer;
        public bool IsSelected => m_State != null && m_State.IsSelected;
        public bool IsBusy => m_State != null && m_State.IsBusy;
        public bool IsInteractable => m_State == null || m_State.IsInteractable;
        public PropButtonDisplayMode DisplayMode => m_State != null ? m_State.DisplayMode : PropButtonDisplayMode.Normal;

        /// <summary>
        /// 状态发生变化时触发，View 可监听后自动 Redraw。
        /// </summary>
        public event Action<PropButtonModel> Changed;

        public PropButtonModel(PropButtonConfig config, PropButtonState state = null)
        {
            Bind(config, state);
        }

        /// <summary>
        /// 绑定配置和状态。未传入状态时会按配置的默认数量创建状态。
        /// </summary>
        public void Bind(PropButtonConfig config, PropButtonState state = null)
        {
            m_Config = config;
            m_State = state ?? new PropButtonState();

            if (state == null && config != null)
            {
                m_State.SetCount(config.DefaultCount);
            }

            MarkDirty();
        }

        /// <summary>
        /// 判断当前是否解锁。显式解锁状态优先，否则按 RequiredLevel 判断。
        /// </summary>
        public bool IsUnlocked()
        {
            if (m_Config == null)
            {
                return true;
            }

            if (m_State != null && m_State.UseExplicitUnlockState)
            {
                return m_State.IsUnlocked;
            }

            return m_Config.RequiredLevel <= 0 || (m_State != null && m_State.CurrentLevel >= m_Config.RequiredLevel);
        }

        /// <summary>
        /// 判断当前是否可以直接使用道具，不包含广告、购买等补充流程。
        /// </summary>
        public bool CanUse()
        {
            return IsInteractable && IsUnlocked() && !IsBusy && (IsInfinite || Count > 0);
        }

        /// <summary>
        /// 判断数量不足时是否展示补充入口。
        /// </summary>
        public bool ShouldShowPurchase()
        {
            return m_Config != null
                   && m_Config.ShowPurchaseWhenEmpty
                   && m_Config.Purchase != null
                   && m_Config.Purchase.HasPurchaseOption
                   && DisplayMode == PropButtonDisplayMode.Normal
                   && Count <= 0;
        }

        /// <summary>
        /// 判断是否展示数量区域。
        /// </summary>
        public bool ShouldShowCount(int hideThreshold, bool showZeroCount)
        {
            if (DisplayMode != PropButtonDisplayMode.Normal)
            {
                return IsInfinite;
            }

            return Count > hideThreshold || (showZeroCount && Count == 0);
        }

        /// <summary>
        /// 判断是否展示广告提示角标。
        /// </summary>
        public bool ShouldShowAdHint(int hideThreshold, bool showZeroCount)
        {
            return m_State != null
                   && m_State.ShowAdHint
                   && DisplayMode == PropButtonDisplayMode.Normal
                   && Count <= hideThreshold
                   && (!showZeroCount || Count == 0)
                   && ShouldShowPurchase();
        }

        public void SetCount(int count)
        {
            if (m_State == null || m_State.Count == count)
            {
                return;
            }

            m_State.SetCount(count);
            MarkDirty();
        }

        public void SetCurrentLevel(int currentLevel)
        {
            if (m_State == null || m_State.CurrentLevel == currentLevel)
            {
                return;
            }

            m_State.SetCurrentLevel(currentLevel);
            MarkDirty();
        }

        public void SetUnlocked(bool isUnlocked)
        {
            if (m_State == null || (m_State.UseExplicitUnlockState && m_State.IsUnlocked == isUnlocked))
            {
                return;
            }

            m_State.SetUnlocked(isUnlocked);
            MarkDirty();
        }

        public void ClearExplicitUnlockState()
        {
            if (m_State == null || !m_State.UseExplicitUnlockState)
            {
                return;
            }

            m_State.ClearExplicitUnlockState();
            MarkDirty();
        }

        public void SetSelected(bool isSelected)
        {
            if (m_State == null || m_State.IsSelected == isSelected)
            {
                return;
            }

            m_State.SetSelected(isSelected);
            MarkDirty();
        }

        public void SetBusy(bool isBusy)
        {
            if (m_State == null || m_State.IsBusy == isBusy)
            {
                return;
            }

            m_State.SetBusy(isBusy);
            MarkDirty();
        }

        public void SetInteractable(bool isInteractable)
        {
            if (m_State == null || m_State.IsInteractable == isInteractable)
            {
                return;
            }

            m_State.SetInteractable(isInteractable);
            MarkDirty();
        }

        public void SetShowAdHint(bool showAdHint)
        {
            if (m_State == null || m_State.ShowAdHint == showAdHint)
            {
                return;
            }

            m_State.SetShowAdHint(showAdHint);
            MarkDirty();
        }

        public void SetDisplayMode(PropButtonDisplayMode displayMode)
        {
            if (m_State == null || m_State.DisplayMode == displayMode)
            {
                return;
            }

            m_State.SetDisplayMode(displayMode);
            MarkDirty();
        }

        public void SetTimerText(string timerText)
        {
            if (m_State == null || m_State.TimerText == timerText)
            {
                return;
            }

            m_State.SetTimerText(timerText);
            MarkDirty();
        }

        /// <summary>
        /// 标记状态变更，并通知 View 刷新。
        /// </summary>
        public void MarkDirty()
        {
            m_IsDirty = true;
            Changed?.Invoke(this);
        }

        /// <summary>
        /// View 完成刷新后清除脏标记。
        /// </summary>
        public void ClearDirty()
        {
            m_IsDirty = false;
        }
    }
}
