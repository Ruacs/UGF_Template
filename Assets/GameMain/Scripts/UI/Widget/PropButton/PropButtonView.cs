using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// 通用道具按钮 View。只负责 UI 显示和点击上抛，不处理购买、广告、存档和具体道具效果。
    /// </summary>
    public class PropButtonView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button m_Button;
        [SerializeField] private Image m_BackgroundImage;
        [SerializeField] private Image m_IconImage;
        [SerializeField] private GameObject m_DefaultRoot;
        [SerializeField] private GameObject m_CountRoot;
        [SerializeField] private TextMeshProUGUI m_CountText;
        [SerializeField] private GameObject m_PurchaseRoot;
        [SerializeField] private GameObject m_AdHintRoot;
        [SerializeField] private GameObject m_BusyRoot;
        [SerializeField] private GameObject m_SelectedRoot;
        [SerializeField] private GameObject m_InfiniteRoot;
        [SerializeField] private GameObject m_TimerRoot;
        [SerializeField] private TextMeshProUGUI m_TimerText;
        [SerializeField] private Image m_TimerFillImage;
        [SerializeField] private GameObject m_LockRoot;
        [SerializeField] private TextMeshProUGUI m_LockText;

        [Header("Display")]
        [SerializeField] private Sprite m_DefaultIcon;
        [SerializeField] private Color m_NormalColor = Color.white;
        [SerializeField] private Color m_LockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color m_DisabledColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private int m_CountHideThreshold = 1;
        [SerializeField] private bool m_ShowZeroCount;
        [SerializeField] private string m_LockTextFormat = "LEVEL {0}";
        [SerializeField] private string m_InfiniteText = "\u221E";

        [Header("Animation")]
        [SerializeField] private bool m_EnableClickAnimation = true;
        [SerializeField] private float m_ClickScaleAmount = 0.88f;
        [SerializeField] private float m_ClickAnimationDuration = 0.08f;
        [SerializeField] private float m_ClickInterval = 0.5f;

        private PropButtonModel m_Model;
        private Vector3 m_OriginalScale = Vector3.one;
        private Tween m_ClickTween;
        private float m_LastClickTime = -999f;

        public PropButtonModel Model => m_Model;
        public RectTransform RectTransform { get; private set; }

        /// <summary>
        /// 按钮点击事件。外部根据 Model 决定使用道具、打开购买页或播放广告。
        /// </summary>
        public event Action<PropButtonView, PropButtonModel> Clicked;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            m_OriginalScale = transform.localScale;
            AutoBindReferences();

            if (m_Button != null)
            {
                m_Button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (m_Button != null)
            {
                m_Button.onClick.RemoveListener(HandleClick);
            }

            if (m_Model != null)
            {
                m_Model.Changed -= OnModelChanged;
            }

            m_ClickTween?.Kill();
        }

        /// <summary>
        /// 绑定状态模型，并可选择立即刷新 UI。
        /// </summary>
        public void Bind(PropButtonModel model, bool redrawImmediately = true)
        {
            if (m_Model == model)
            {
                if (redrawImmediately)
                {
                    Redraw();
                }

                return;
            }

            if (m_Model != null)
            {
                m_Model.Changed -= OnModelChanged;
            }

            m_Model = model;

            if (m_Model != null)
            {
                m_Model.Changed += OnModelChanged;
            }

            if (redrawImmediately)
            {
                Redraw();
            }
        }

        /// <summary>
        /// 按当前 Model 完整刷新一次按钮显示。
        /// </summary>
        public void Redraw()
        {
            if (m_Model == null)
            {
                Clear();
                return;
            }

            PropButtonConfig config = m_Model.Config;
            PropButtonState state = m_Model.State;
            bool isUnlocked = m_Model.IsUnlocked();
            bool isInteractable = m_Model.IsInteractable && isUnlocked && !m_Model.IsBusy;

            SetActive(m_LockRoot, !isUnlocked);
            SetActive(m_DefaultRoot, isUnlocked);
            SetActive(m_BusyRoot, isUnlocked && m_Model.IsBusy);
            SetActive(m_SelectedRoot, isUnlocked && m_Model.IsSelectable && m_Model.IsSelected);

            if (m_Button != null)
            {
                m_Button.interactable = isInteractable;
            }

            if (m_BackgroundImage != null && config != null)
            {
                m_BackgroundImage.color = config.BackgroundColor;
            }

            if (m_IconImage != null)
            {
                m_IconImage.sprite = config != null && config.Icon != null ? config.Icon : m_DefaultIcon;
                m_IconImage.enabled = m_IconImage.sprite != null;
                m_IconImage.color = GetIconColor(isUnlocked, state);
                m_IconImage.SetNativeSize();
            }

            if (m_LockText != null && config != null)
            {
                m_LockText.text = string.Format(m_LockTextFormat, config.RequiredLevel);
            }

            RedrawCount();
            m_Model.ClearDirty();
        }

        /// <summary>
        /// 清空显示，通常用于未绑定 Model 或复用对象回收前。
        /// </summary>
        public void Clear()
        {
            if (m_Button != null)
            {
                m_Button.interactable = false;
            }

            if (m_IconImage != null)
            {
                m_IconImage.sprite = m_DefaultIcon;
                m_IconImage.enabled = m_DefaultIcon != null;
                m_IconImage.color = m_NormalColor;
            }

            SetActive(m_DefaultRoot, true);
            SetActive(m_CountRoot, false);
            SetActive(m_PurchaseRoot, false);
            SetActive(m_AdHintRoot, false);
            SetActive(m_BusyRoot, false);
            SetActive(m_SelectedRoot, false);
            SetActive(m_InfiniteRoot, false);
            SetActive(m_TimerRoot, false);
            SetActive(m_LockRoot, false);
        }

        /// <summary>
        /// 播放点击反馈动画，不改变业务状态。
        /// </summary>
        public void PlayClickAnimation()
        {
            if (!m_EnableClickAnimation || !gameObject.activeInHierarchy)
            {
                return;
            }

            m_ClickTween?.Kill();
            transform.localScale = m_OriginalScale;

            m_ClickTween = DOTween.Sequence()
                .Append(transform.DOScale(m_OriginalScale * m_ClickScaleAmount, m_ClickAnimationDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(m_OriginalScale, m_ClickAnimationDuration * 0.5f).SetEase(Ease.OutBack))
                .SetUpdate(true)
                .OnKill(() => m_ClickTween = null);
        }

        private void RedrawCount()
        {
            PropButtonState state = m_Model.State;
            PropButtonDisplayMode displayMode = m_Model.DisplayMode;
            bool showCount = m_Model.ShouldShowCount(m_CountHideThreshold, m_ShowZeroCount);

            SetActive(m_CountRoot, showCount);
            SetActive(m_PurchaseRoot, m_Model.ShouldShowPurchase());
            SetActive(m_AdHintRoot, m_Model.ShouldShowAdHint(m_CountHideThreshold, m_ShowZeroCount));
            SetActive(m_InfiniteRoot, displayMode == PropButtonDisplayMode.InfiniteForever || displayMode == PropButtonDisplayMode.InfiniteWithTimer);
            SetActive(m_TimerRoot, displayMode == PropButtonDisplayMode.InfiniteWithTimer);

            if (m_CountText != null)
            {
                m_CountText.text = displayMode == PropButtonDisplayMode.Normal ? FormatCount(m_Model.Count) : m_InfiniteText;
            }

            if (m_TimerText != null)
            {
                m_TimerText.text = string.IsNullOrEmpty(state.TimerText) ? m_InfiniteText : state.TimerText;
            }

            if (m_TimerFillImage != null)
            {
                m_TimerFillImage.fillAmount = displayMode == PropButtonDisplayMode.InfiniteWithTimer ? 1f : 0f;
            }
        }

        private Color GetIconColor(bool isUnlocked, PropButtonState state)
        {
            if (!isUnlocked)
            {
                return m_LockedColor;
            }

            if (state != null && !state.IsInteractable)
            {
                return m_DisabledColor;
            }

            return m_NormalColor;
        }

        private void HandleClick()
        {
            if (Time.unscaledTime - m_LastClickTime < m_ClickInterval)
            {
                return;
            }

            if (m_Model == null || !m_Model.IsInteractable || !m_Model.IsUnlocked() || m_Model.IsBusy)
            {
                return;
            }

            m_LastClickTime = Time.unscaledTime;
            PlayClickAnimation();
            Clicked?.Invoke(this, m_Model);
        }

        private void OnModelChanged(PropButtonModel model)
        {
            if (model == m_Model)
            {
                Redraw();
            }
        }

        /// <summary>
        /// 当前只自动绑定根节点 Button，其它复杂 UI 引用建议在 Prefab 上显式配置。
        /// </summary>
        private void AutoBindReferences()
        {
            if (m_Button == null)
            {
                m_Button = GetComponent<Button>();
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static string FormatCount(int count, int maxDisplay = 999, string overflowSuffix = "+")
        {
            return count > maxDisplay ? $"{maxDisplay}{overflowSuffix}" : count.ToString();
        }
    }
}
