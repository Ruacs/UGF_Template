using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using UnityGameFramework.Runtime;

/// <summary>
/// 道具数据结构
/// </summary>
[Serializable]
public class ItemData
{
    public int itemId;           // 道具ID
    public string itemName;      // 道具名称
    public Sprite icon;          // 道具图标
    public int count;            // 数量
    public bool hasAd;           // 是否有广告标识
    public bool isLocked;        // 是否锁定
    public object extraData;     // 扩展数据（可按需自定义）


    public ItemData(int itemId, int count)
    {
        this.itemId = itemId;
        this.count = count;
        this.hasAd = true;
    }


    public ItemData(int itemId, string itemName, Sprite icon, int count = 0, bool hasAd = false, bool isLocked = false, object extraData = null)
    {
        this.itemId = itemId;
        this.itemName = itemName;
        this.icon = icon;
        this.count = count;
        this.hasAd = hasAd;
        this.isLocked = isLocked;
        this.extraData = extraData;
    }
}

/// <summary>
/// 通用道具按钮
/// 层级结构：
/// BTN_Base (RectTransform)
/// ├── BTN (RectTransform, CanvasRenderer, Image, Button)
/// ├── Icon (RectTransform, CanvasRenderer, Image)
/// ├── Num_bg (RectTransform, CanvasRenderer, Image)
/// │   └── tmp_Num (RectTransform, CanvasRenderer, TextMeshProUGUI)
/// └── Img_Ad (RectTransform, CanvasRenderer, Image)
/// </summary>
public class ItemButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    /// <summary>
    /// 数量显示模式：
    /// Normal - 按照 count 显示正常数量
    /// InfiniteForever - 永久无限（显示∞）
    /// InfiniteWithTimer - 时间窗口内无限（显示外部注入的倒计时）
    /// </summary>
    public enum CountDisplayMode
    {
        Normal,
        InfiniteForever,
        InfiniteWithTimer
    }

    // ───────── Inspector 引用 ─────────
    [Header("UI 引用")]
    [SerializeField] private Button btn;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject numBg;
    [SerializeField] private TextMeshProUGUI tmpNum;
    [SerializeField] private GameObject imgInfinite;
    [SerializeField] private GameObject imgAd;

    [SerializeField] private GameObject timeBg;
    [SerializeField] private TextMeshProUGUI tmpTime;

    [Header("外观配置")]
    [SerializeField] private Sprite defaultIcon;          // 默认/空道具图标
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("数量显示")]
    [SerializeField] private int numHideThreshold = 1;   // 数量 ≤ 该值时隐藏数量背景
    [SerializeField] private bool showZeroCount = false;

    [Header("动画配置")]
    [Tooltip("是否启用点击缩放动画。")]
    [SerializeField] private bool enableClickAnim = true;
    [Tooltip("点击时缩放到的比例，数值越小按压感越明显。")]
    [SerializeField] private float clickScaleAmount = 0.88f;
    [Tooltip("点击缩放动画总时长，包含缩小和回弹。")]
    [SerializeField] private float clickAnimDuration = 0.08f;
    [Tooltip("是否启用状态相关动画，包括选中颜色过渡、选中缩放、角标显隐和数量增长动画。SetInteractable 不触发动画。")]
    [SerializeField] private bool enableStateAnim = true;
    [Tooltip("选中/取消选中时图标颜色过渡时长。")]
    [SerializeField] private float stateColorDuration = 0.15f;
    [Tooltip("选中状态切换时的缩放峰值，取消选中时会使用对称的缩小比例。")]
    [SerializeField] private float stateScaleAmount = 1.08f;
    [Tooltip("选中/取消选中缩放动画总时长。")]
    [SerializeField] private float stateScaleDuration = 0.18f;
    [Tooltip("数量角标和广告角标显隐缩放动画时长。")]
    [SerializeField] private float badgeAnimDuration = 0.3f;
    [Tooltip("数量增加时，数字从旧值滚动到新值的时长。数量减少不播放该动画。")]
    [SerializeField] private float countAnimDuration = 0.35f;

    public DOTweenSequenceAnimator m_DOTweenAnimator;

    // ───────── 事件 ─────────
    /// <summary>点击回调，参数为当前道具数据</summary>
    public event Action<ItemData> OnItemClicked;
    /// <summary>长按回调</summary>
    public event Action<ItemData> OnItemLongPressed;
    /// <summary>选中状态变化回调</summary>
    public event Action<ItemButton, bool> OnSelectChanged;

    // ───────── 运行时状态 ─────────
    [SerializeField] private ItemData _itemData;
    [SerializeField] private bool _isSelected;
    [SerializeField] private bool _isInteractable = true;
    [SerializeField] private CountDisplayMode _countDisplayMode = CountDisplayMode.Normal;
    [SerializeField] private string _timedInfiniteText = "\u221E";

    // 长按检测
    private bool _isPointerDown;
    private float _pointerDownTime;
    private const float LongPressThreshold = 0.5f;

    // 点击动画
    private Vector3 _originalScale;
    private Coroutine _animCoroutine;
    private Tween _colorTween;
    private Tween _stateScaleTween;
    private Tween _numBgTween;
    private Tween _adTween;
    private Tween _countTween;
    private Vector3 _numBgOriginalScale = Vector3.one;
    private Vector3 _adOriginalScale = Vector3.one;

    private RectTransform _selfRT;

    public RectTransform rectTransform => _selfRT;

    // ═══════════════════════════════════════════
    #region Unity 生命周期
    // ═══════════════════════════════════════════

    private void Awake()
    {
        _originalScale = transform.localScale;
        _selfRT = GetComponent<RectTransform>();
        AutoBindReferences();
        CacheBadgeScales();
        RegisterButtonEvent();
    }

    private void Update()
    {
        // 长按检测
        if (_isPointerDown && _isInteractable)
        {
            if (Time.time - _pointerDownTime >= LongPressThreshold)
            {
                _isPointerDown = false;
                OnItemLongPressed?.Invoke(_itemData);
            }
        }
    }

    private void OnDestroy()
    {
        if (btn != null) btn.onClick.RemoveAllListeners();
        KillTweens();
        m_DOTweenAnimator?.KillAll();
    }

    #endregion

    // ═══════════════════════════════════════════
    #region 公共接口
    // ═══════════════════════════════════════════

    /// <summary>
    /// 设置道具数据并刷新显示（主入口）
    /// </summary>
    public void Setup(ItemData data)
    {
        _itemData = data;
        _countDisplayMode = CountDisplayMode.Normal;
        Refresh();
    }


    /// <summary>
    /// 仅更新数量显示（高频调用场景使用，避免全量刷新）
    /// </summary>
    public void UpdateCount(int count)
    {
        if (_itemData == null) return;
        int oldCount = _itemData.count;
        _itemData.count = count;
        RefreshCount(true, oldCount);
        RefreshAd(true);
    }

    /// <summary>
    /// 外部切换显示模式：ItemButton 不做时间运算，只负责更新 UI。
    /// </summary>
    public void SetCountDisplayMode(CountDisplayMode mode)
    {
        if (_countDisplayMode == mode) return;
        _countDisplayMode = mode;
        Refresh();
    }

    /// <summary>
    /// 外部每秒更新倒计时显示文本，ItemButton 只负责渲染字符串。
    /// 典型值："14:59"、"00:30"。
    /// </summary>
    public void SetInfiniteTimerText(string timerText)
    {
        if (_countDisplayMode != CountDisplayMode.InfiniteWithTimer) return;
        _timedInfiniteText = string.IsNullOrEmpty(timerText) ? "\u221E" : timerText;
        if (tmpTime != null)
        {
            tmpTime.text = _timedInfiniteText;
        }
    }

    /// <summary>
    /// 清除时间无限显示，切回普通 count 展示。
    /// </summary>
    public void ClearInfiniteMode()
    {
        _countDisplayMode = CountDisplayMode.Normal;
        _timedInfiniteText = "\u221E";
        Refresh();
    }

    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected, bool notify = true)
    {
        if (_isSelected == selected) return;
        _isSelected = selected;
        RefreshColor(true);
        PlayStateSwitchAnim(selected);
        if (notify) OnSelectChanged?.Invoke(this, _isSelected);
    }

    /// <summary>
    /// 设置可交互状态
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        if (btn != null) btn.interactable = interactable;
        RefreshColor();
    }

    /// <summary>
    /// 设置广告标识显示
    /// </summary>
    public void SetAdVisible(bool visible)
    {
        if (_itemData != null) _itemData.hasAd = visible;
        if (imgAd != null) _adTween = SetActiveAnimated(imgAd, visible, true, _adTween);
    }

    /// <summary>
    /// 清空按钮（显示为空格子）
    /// </summary>
    public void Clear()
    {
        _itemData = null;
        _isSelected = false;
        _countDisplayMode = CountDisplayMode.Normal;
        if (iconImage != null) iconImage.sprite = defaultIcon;
        if (tmpTime != null)
        {
            tmpTime.gameObject.SetActive(false);
            tmpTime.text = string.Empty;
        }
        if (imgInfinite != null)
        {
            imgInfinite.SetActive(false);
        }
        _numBgTween?.Kill();
        _adTween?.Kill();
        _countTween?.Kill();
        _timedInfiniteText = "\u221E";
        if (numBg != null)
        {
            numBg.SetActive(false);
            numBg.transform.localScale = _numBgOriginalScale;
        }
        if (imgAd != null)
        {
            imgAd.SetActive(false);
            imgAd.transform.localScale = _adOriginalScale;
        }
        RefreshColor();
    }

    /// <summary>获取当前道具数据（只读）</summary>
    public ItemData GetItemData() => _itemData;

    /// <summary>是否选中</summary>
    public bool IsSelected => _isSelected;

    #endregion

    // ═══════════════════════════════════════════
    #region 内部刷新
    // ═══════════════════════════════════════════

    /// <summary>全量刷新所有子节点显示</summary>
    private void Refresh()
    {
        if (_itemData == null) { Clear(); return; }

        RefreshIcon();
        RefreshCount();
        RefreshAd();
        RefreshColor();
    }

    private void RefreshIcon()
    {
        if (iconImage == null) return;
        iconImage.sprite = (_itemData?.icon != null) ? _itemData.icon : defaultIcon;
        iconImage.enabled = iconImage.sprite != null;
    }

    private void RefreshCount(bool animate = false, int previousCount = -1)
    {
        if (_itemData == null) { if (numBg) numBg.SetActive(false); return; }

        bool isInfiniteMode = _countDisplayMode == CountDisplayMode.InfiniteForever || _countDisplayMode == CountDisplayMode.InfiniteWithTimer;
        if (isInfiniteMode)
        {
            if (numBg != null)
            {
                _numBgTween = SetActiveAnimated(numBg, true, animate, _numBgTween);
            }

            if (_countDisplayMode == CountDisplayMode.InfiniteForever)
            {
                RefreshInfiniteVisual(false);
                if (tmpTime != null) tmpTime.gameObject.SetActive(false);
            }
            else
            {
                RefreshInfiniteVisual(true);
                if (tmpTime != null)
                {
                    tmpTime.text = _timedInfiniteText;
                    tmpTime.gameObject.SetActive(true);
                }
            }
            if (timeBg != null)
            {
                timeBg.SetActive(_countDisplayMode == CountDisplayMode.InfiniteWithTimer);
            }
            return;
        }

        if (timeBg != null) timeBg.SetActive(false);
        if (imgInfinite != null) imgInfinite.SetActive(false);
        if (tmpTime != null) tmpTime.gameObject.SetActive(false);

        bool shouldShow = _itemData.count > numHideThreshold
                          || (showZeroCount && _itemData.count == 0);

        if (numBg != null)
        {
            _numBgTween = SetActiveAnimated(numBg, shouldShow, animate, _numBgTween);
        }

        if (tmpNum != null)
        {
            SetTmpNumVisible(true);
            PlayCountTextAnim(previousCount, _itemData.count, animate && shouldShow);
        }

    }

    private void SetTmpNumVisible(bool visible)
    {
        if (tmpNum == null)
        {
            return;
        }

        tmpNum.enabled = visible;
        tmpNum.gameObject.SetActive(visible);
    }

    private void RefreshInfiniteVisual(bool hasTimer)
    {
        bool useImage = imgInfinite != null;
        if (imgInfinite != null)
        {
            imgInfinite.SetActive(true);
        }

        if (tmpNum == null)
        {
            return;
        }

        // 无限图标优先；没有图标节点时回退到文本显示。
        SetTmpNumVisible(!useImage);
        tmpNum.text = useImage ? string.Empty : "∞";
    }

    private void PlayCountTextAnim(int fromCount, int toCount, bool animate)
    {
        _countTween?.Kill();

        if (!animate || fromCount < 0 || toCount <= fromCount || !gameObject.activeInHierarchy)
        {
            tmpNum.text = FormatCount(toCount, 999, "+");
            return;
        }

        int currentCount = fromCount;
        tmpNum.text = FormatCount(currentCount, 999, "+");
        _countTween = DOTween
            .To(() => currentCount, value =>
            {
                currentCount = value;
                tmpNum.text = FormatCount(currentCount, 999, "+");
            }, toCount, countAnimDuration)
            .SetUpdate(true)
            .SetEase(Ease.OutCubic)
            .OnKill(() => _countTween = null);
    }

    private void RefreshAd(bool animate = false)
    {
        bool shouldShow = false;
        if (_countDisplayMode == CountDisplayMode.Normal)
        {
            // 无限模式只展示数量/倒计时，不展示补充广告角标
            shouldShow = _itemData != null &&
                         (_itemData.count <= numHideThreshold || (showZeroCount && _itemData.count == 0));
        }

        bool isShow = shouldShow && _itemData.hasAd;
        if (imgAd != null)
        {
            _adTween = SetActiveAnimated(imgAd, isShow, animate, _adTween);
        }
    }

    private void RefreshColor(bool animate = false)
    {
        if (iconImage == null) return;

        Color targetColor;
        if (!_isInteractable)
            targetColor = disabledColor;
        else if (_itemData != null && _itemData.isLocked)
            targetColor = lockedColor;
        else if (_isSelected)
            targetColor = selectedColor;
        else
            targetColor = normalColor;

        _colorTween?.Kill();
        if (enableStateAnim && animate && gameObject.activeInHierarchy)
        {
            _colorTween = iconImage
                .DOColor(targetColor, stateColorDuration)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad)
                .OnKill(() => _colorTween = null);
        }
        else
        {
            iconImage.color = targetColor;
        }
    }

    private void PlayStateSwitchAnim(bool emphasize)
    {
        if (!enableStateAnim || !gameObject.activeInHierarchy) return;

        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }

        _stateScaleTween?.Kill();
        transform.localScale = _originalScale;

        float targetScale = emphasize ? stateScaleAmount : Mathf.Max(0.01f, 2f - stateScaleAmount);
        _stateScaleTween = DOTween.Sequence()
            .Append(transform.DOScale(_originalScale * targetScale, stateScaleDuration * 0.5f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(_originalScale, stateScaleDuration * 0.5f).SetEase(Ease.OutBack))
            .SetUpdate(true)
            .OnKill(() => _stateScaleTween = null);
    }

    private Tween SetActiveAnimated(GameObject target, bool active, bool animate, Tween currentTween)
    {
        if (target == null) return null;

        bool wasActive = target.activeSelf;
        currentTween?.Kill();

        if (!enableStateAnim || !animate || wasActive == active || !gameObject.activeInHierarchy)
        {
            target.SetActive(active);
            target.transform.localScale = GetBadgeOriginalScale(target);
            return null;
        }

        Vector3 originalScale = GetBadgeOriginalScale(target);
        if (active)
        {
            target.SetActive(true);
            target.transform.localScale = Vector3.zero;
            return target.transform
                .DOScale(originalScale, badgeAnimDuration)
                .SetUpdate(true)
                .SetEase(Ease.OutBack);
        }

        target.transform.localScale = originalScale;
        return target.transform
            .DOScale(Vector3.zero, badgeAnimDuration)
            .SetUpdate(true)
            .SetEase(Ease.InBack)
            .OnComplete(() => target.SetActive(false));
    }

    private Vector3 GetBadgeOriginalScale(GameObject target)
    {
        if (target == numBg) return _numBgOriginalScale;
        if (target == imgAd) return _adOriginalScale;
        return Vector3.one;
    }

    private void KillTweens()
    {
        _colorTween?.Kill();
        _stateScaleTween?.Kill();
        _numBgTween?.Kill();
        _adTween?.Kill();
        _countTween?.Kill();
    }

    /// <summary>数量格式化：超过 9999 显示 9999+</summary>
    private static string FormatCount(int count, int maxDisplay = 9999, string overflowSuffix = "+")
    {
        if (count > maxDisplay) return $"{maxDisplay}{overflowSuffix}";
        return count.ToString();
    }

    #endregion

    // ═══════════════════════════════════════════
    #region 事件处理
    // ═══════════════════════════════════════════

    private void RegisterButtonEvent()
    {
        if (btn != null)
            btn.AddSafeClick(HandleClick);
    }

    private void HandleClick()
    {
        if (!_isInteractable) return;
        if (enableClickAnim) PlayClickAnim();
        OnItemClicked?.Invoke(_itemData);
    }

    public void OnPointerClick(PointerEventData eventData) { }   // Button.onClick 已处理

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isInteractable) return;
        _isPointerDown = true;
        _pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region 点击动画
    // ═══════════════════════════════════════════

    public void PlayClickAnim()
    {
        _stateScaleTween?.Kill();
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(ClickAnimCoroutine());
    }

    private System.Collections.IEnumerator ClickAnimCoroutine()
    {
        float half = clickAnimDuration * 0.5f;
        float elapsed = 0f;

        // 缩小
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / half;
            float scale = Mathf.Lerp(1f, clickScaleAmount, t);
            transform.localScale = _originalScale * scale;
            yield return null;
        }

        elapsed = 0f;
        // 还原
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / half;
            float scale = Mathf.Lerp(clickScaleAmount, 1f, t);
            transform.localScale = _originalScale * scale;
            yield return null;
        }

        transform.localScale = _originalScale;
        _animCoroutine = null;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region 自动绑定（Editor辅助）
    // ═══════════════════════════════════════════

    /// <summary>
    /// 按层级结构自动查找并绑定子节点引用
    /// 如果已在 Inspector 中手动赋值则跳过
    /// </summary>
    private void AutoBindReferences()
    {
        if (btn == null)
        {
            var btnTrans = transform.Find("BTN");
            if (btnTrans != null) btn = btnTrans.GetComponent<Button>();
        }

        if (iconImage == null)
        {
            var iconTrans = transform.Find("Icon");
            if (iconTrans != null) iconImage = iconTrans.GetComponent<Image>();
        }

        if (numBg == null)
        {
            var numBgTrans = transform.Find("Num_bg");
            if (numBgTrans != null) numBg = numBgTrans.gameObject;

            if (tmpNum == null && numBg != null)
            {
                var numTrans = numBg.transform.Find("tmp_Num");
                if (numTrans != null) tmpNum = numTrans.GetComponent<TextMeshProUGUI>();
            }
        }

        if (imgAd == null)
        {
            var adTrans = transform.Find("Img_Ad");
            if (adTrans != null) imgAd = adTrans.gameObject;
        }

        if (imgInfinite == null)
        {
            var infiniteTrans = transform.Find("Img_Infinite");
            if (infiniteTrans == null)
            {
                infiniteTrans = transform.Find("imgInfinite");
            }
            if (infiniteTrans == null && numBg != null)
            {
                infiniteTrans = numBg.transform.Find("Img_Infinite");
            }
            if (infiniteTrans == null && numBg != null)
            {
                infiniteTrans = numBg.transform.Find("imgInfinite");
            }
            if (infiniteTrans != null)
            {
                imgInfinite = infiniteTrans.gameObject;
            }
        }

        if (tmpTime == null)
        {
            var timerTrans = transform.Find("tmp_Time");
            if (timerTrans == null)
            {
                timerTrans = transform.Find("tmp_TimeText");
            }
            if (timerTrans != null)
            {
                tmpTime = timerTrans.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void CacheBadgeScales()
    {
        if (numBg != null) _numBgOriginalScale = numBg.transform.localScale;
        if (imgAd != null) _adOriginalScale = imgAd.transform.localScale;
    }

#if UNITY_EDITOR
    /// <summary>在 Editor 中一键绑定所有引用（右键菜单）</summary>
    [ContextMenu("Auto Bind References")]
    private void EditorAutoBindReferences()
    {
        AutoBindReferences();
        CacheBadgeScales();
        UnityEditor.EditorUtility.SetDirty(this);
        Log.Info("[ItemButton] 引用绑定完成");
    }
#endif

    #endregion
}
