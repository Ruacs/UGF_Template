using DG.Tweening;
using System;
using UnityEngine;

public class GuideMaskController : MonoBehaviour
{
    public static GuideMaskController Instance;
    [SerializeField] private GuideMask _graphic;
    [SerializeField] private GuideHintView _hintView;
    [SerializeField] private Camera _targetCamera;
    [SerializeField, Min(0f)] private float _defaultDuration = 0.3f;
    [SerializeField] private Ease _defaultEase = Ease.OutCubic;

    private Tween _tween;
    private GuideMaskTarget _currentTarget;
    private bool _hasTarget;

    public GuideMask Graphic => _graphic;
    public Camera TargetCamera => _targetCamera;
    public event Action Closed;
    public bool IsPlaying => _tween != null && _tween.IsActive();

    private bool _activeCloseOnAnyClick;
    private Tween _autoCloseTween;

    private void Awake()
    {
        // Prefer the controller that owns the GuideMask Canvas when legacy
        // prefabs still contain a second controller on the graphic object.
        if (Instance == null || GetComponent<Canvas>() != null)
        {
            Instance = this;
        }
        EnsureReferences();
        if (_graphic != null)
        {
            _graphic.PointerClicked += HandlePointerClicked;
        }
    }

    private void OnDestroy()
    {
        if (_graphic != null)
        {
            _graphic.PointerClicked -= HandlePointerClicked;
        }

        StopAnimation();
        _autoCloseTween?.Kill();
        _autoCloseTween = null;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!_activeCloseOnAnyClick || _graphic == null || !_graphic.gameObject.activeInHierarchy)
        {
            return;
        }

        bool mouseClicked = Input.GetMouseButtonDown(0);
        bool touchStarted = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        if (mouseClicked || touchStarted)
        {
            Close(0.15f);
        }
    }

    private void HandlePointerClicked()
    {
        if (_activeCloseOnAnyClick)
        {
            Close(0.15f);
        }
    }

    public bool Show(RectTransform target, bool closeOnClick = false, string hintText = null)
    {
        GuideMaskOptions options = GuideMaskOptions.Default;
        options.closeOnClick = closeOnClick;
        options.hintText = hintText;
        return Show(target, options);
    }

    public bool Show(RectTransform target, GuideMaskOptions options)
    {
        bool result = MoveTo(
            target,
            options.targetCamera,
            options.sizeMultiplier <= 0f ? 1f : options.sizeMultiplier,
            options.duration,
            true,
            options.closeOnClick,
            options.autoCloseAfter);
        if (result)
        {
            ShowHint(options.hintText, target, options.hintPosition, options.hintOffset);
        }

        return result;
    }

    public bool MoveTo(
        RectTransform target,
        Camera targetCamera = null,
        float sizeMultiplier = 1f,
        float duration = -1f,
        bool closeOnClick = false,
        float autoCloseAfter = -1f)
    {
        return MoveTo(target, targetCamera, sizeMultiplier, duration, false, closeOnClick, autoCloseAfter);
    }

    public void Close(float duration = 0f)
    {
        StopAnimation();
        _autoCloseTween?.Kill();
        _autoCloseTween = null;
        _activeCloseOnAnyClick = false;
        _hintView?.Hide(duration > 0f ? duration : 0f);
        if (duration <= 0f || !_hasTarget)
        {
            _hasTarget = false;
            if (_graphic != null)
            {
                _graphic.gameObject.SetActive(false);
            }
            Closed?.Invoke();
            return;
        }

        _tween = DOTween.To(
                () => _currentTarget.size,
                size => ApplyTarget(new GuideMaskTarget(_currentTarget.center, size, _currentTarget.cornerRadius)),
                Vector2.zero,
                duration)
            .SetEase(_defaultEase)
            .OnComplete(() =>
            {
                _hasTarget = false;
                if (_graphic != null)
                {
                    _graphic.gameObject.SetActive(false);
                }
                _tween = null;
                Closed?.Invoke();
            });
    }

    public void StopAnimation()
    {
        _tween?.Kill();
        _tween = null;
    }

    private bool MoveTo(
        RectTransform target,
        Camera targetCamera,
        float sizeMultiplier,
        float duration,
        bool isShow,
        bool closeOnClick,
        float autoCloseAfter)
    {
        EnsureReferences();
        if (!GuideMaskTargetResolver.TryResolve(
                target,
                _graphic.RectTransform,
                targetCamera != null ? targetCamera : _targetCamera,
                sizeMultiplier,
                _graphic.CornerRadius,
                out GuideMaskTarget nextTarget))
        {
            return false;
        }

        _graphic.gameObject.SetActive(true);
        StopAnimation();
        _autoCloseTween?.Kill();
        _autoCloseTween = null;
        _activeCloseOnAnyClick = closeOnClick;

        float actualDuration = duration >= 0f ? duration : _defaultDuration;
        if (actualDuration <= 0f)
        {
            ApplyTarget(nextTarget);
            ScheduleAutoClose(autoCloseAfter);
            return true;
        }

        if (isShow || !_hasTarget)
        {
            GuideMaskTarget showFrom = new GuideMaskTarget(
                nextTarget.center,
                Vector2.zero,
                nextTarget.cornerRadius);

            ApplyTarget(showFrom);
            _tween = DOTween.To(
                    () => 0f,
                    t => ApplyTarget(GuideMaskTarget.Lerp(showFrom, nextTarget, t)),
                    1f,
                    actualDuration)
                .SetEase(_defaultEase)
                .OnComplete(() =>
                {
                    _tween = null;
                    ScheduleAutoClose(autoCloseAfter);
                });

            return true;
        }

        GuideMaskTarget from = _currentTarget;
        _tween = DOTween.To(
                () => 0f,
                t => ApplyTarget(GuideMaskTarget.Lerp(from, nextTarget, t)),
                1f,
                actualDuration)
            .SetEase(_defaultEase)
            .OnComplete(() =>
            {
                _tween = null;
                ScheduleAutoClose(autoCloseAfter);
            });

        return true;
    }

    private void ApplyTarget(GuideMaskTarget target)
    {
        _currentTarget = target;
        _hasTarget = true;
        _graphic.SetTarget(target);
    }

    private void EnsureReferences()
    {
        if (_graphic == null)
        {
            _graphic = GetComponentInChildren<GuideMask>(true);
        }

        if (_graphic == null)
        {
            _graphic = GetComponentInParent<GuideMask>();
        }

        if (_hintView == null)
        {
            _hintView = GetComponentInChildren<GuideHintView>(true);
        }

        if (_hintView == null)
        {
            _hintView = GetComponentInParent<GuideHintView>();
        }

        if (_hintView == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _hintView = canvas.GetComponentInChildren<GuideHintView>(true);
            }
        }

        if (_hintView == null)
        {
            _hintView = FindObjectOfType<GuideHintView>(true);
        }
    }

    private void ShowHint(
        string hintText,
        RectTransform target,
        GuideHintPosition hintPosition,
        float hintOffset)
    {
        if (_hintView == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(hintText))
        {
            _hintView.Hide();
            return;
        }

        _hintView.Show(hintText);
    }

    private void ScheduleAutoClose(float autoCloseAfter)
    {
        if (autoCloseAfter <= 0f)
        {
            return;
        }

        _autoCloseTween = DOVirtual.DelayedCall(autoCloseAfter, () => Close(0.15f));
    }
}
