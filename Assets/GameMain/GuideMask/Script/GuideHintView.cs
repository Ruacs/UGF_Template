using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GuideHintPosition
{
    Auto,
    Top,
    Bottom,
    Left,
    Right
}

public class GuideHintView : MonoBehaviour
{
    [SerializeField] private RectTransform _root;
    [SerializeField] private TMP_Text _text;

    [SerializeField] private RectTransform _pointer;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Camera _targetCamera;
    [SerializeField, Min(0f)] private float _gap = 16f;
    [SerializeField, Min(0f)] private float _edgePadding = 20f;
    [SerializeField, Min(0f)] private float _animationDuration = 0.2f;

    private static TMP_FontAsset s_FontAsset;
    private Tween _animation;

    public bool IsVisible => _root != null && _root.gameObject.activeSelf;

    private void Awake()
    {
        if (_root == null)
        {
            _root = transform as RectTransform;
        }

        if (_canvasGroup == null && _root != null)
        {
            _canvasGroup = _root.GetComponent<CanvasGroup>();
        }

        // HintRoot and Pointer use local coordinates relative to their parent.
        // Center anchors make the calculation independent from the prefab's
        // original anchor setup.
        if (_root != null)
        {
            return;
            Vector2 rootSize = _root.rect.size;
            _root.anchorMin = Vector2.one * 0.5f;
            _root.anchorMax = Vector2.one * 0.5f;
            _root.pivot = Vector2.one * 0.5f;
            _root.sizeDelta = rootSize;
        }

        if (_pointer != null)
        {
            
            Vector2 pointerSize = _pointer.rect.size;
            _pointer.anchorMin = Vector2.one * 0.5f;
            _pointer.anchorMax = Vector2.one * 0.5f;
            _pointer.pivot = Vector2.one * 0.5f;
            _pointer.sizeDelta = pointerSize;
        }
    }

    public static void SetFont(TMP_FontAsset fontAsset)
    {
        s_FontAsset = fontAsset;
    }

    public void Show(
        string text,
        RectTransform target,
        GuideHintPosition position = GuideHintPosition.Auto,
        float offset = -1f)
    {
        if (_root == null || _text == null || target == null)
        {
            return;
        }

        RectTransform parent = _root.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        _text.text = text ?? string.Empty;
        _text.font = s_FontAsset;
        _root.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);

        if (!TryGetTargetBounds(target, parent, out Rect targetBounds))
        {
            Hide(0f);
            return;
        }

        GuideHintPosition resolvedPosition = ResolvePosition(position, targetBounds, parent.rect);
        float gap = offset >= 0f ? offset : _gap;
        PositionHint(resolvedPosition, targetBounds, parent.rect, gap);
        PlayShowAnimation();
    }

    public void Show(string text)
    {
        _text.text = text ?? string.Empty;
        _text.font = s_FontAsset;
        _root.gameObject.SetActive(true);
        PlayShowAnimation();
    }

    public void Hide(float duration = -1f)
    {
        if (_root == null)
        {
            return;
        }

        _animation?.Kill();
        float actualDuration = duration >= 0f ? duration : _animationDuration;
        if (_canvasGroup == null || actualDuration <= 0f)
        {
            _root.gameObject.SetActive(false);
            return;
        }

        _animation = _canvasGroup.DOFade(0f, actualDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => _root.gameObject.SetActive(false));
    }

    private void PositionHint(
        GuideHintPosition position,
        Rect targetBounds,
        Rect parentRect,
        float gap)
    {
        Vector2 targetCenter = targetBounds.center;
        Vector2 hintSize = _root.rect.size;
        Vector2 hintPosition = targetCenter;
        Vector2 pointerPosition = Vector2.zero;
        float pointerRotation = 0f;

        switch (position)
        {
            case GuideHintPosition.Top:
                hintPosition.y = targetBounds.yMax + gap + hintSize.y * 0.5f;
                pointerPosition = new Vector2(
                    ClampPointer(targetCenter.x - hintPosition.x, hintSize.x),
                    -hintSize.y * 0.5f);
                pointerRotation = 180f;
                break;
            case GuideHintPosition.Bottom:
                hintPosition.y = targetBounds.yMin - gap - hintSize.y * 0.5f;
                pointerPosition = new Vector2(
                    ClampPointer(targetCenter.x - hintPosition.x, hintSize.x),
                    hintSize.y * 0.5f);
                pointerRotation = 0f;
                break;
            case GuideHintPosition.Left:
                hintPosition.x = targetBounds.xMin - gap - hintSize.x * 0.5f;
                pointerPosition = new Vector2(
                    hintSize.x * 0.5f,
                    ClampPointer(targetCenter.y - hintPosition.y, hintSize.y));
                pointerRotation = -90f;
                break;
            case GuideHintPosition.Right:
                hintPosition.x = targetBounds.xMax + gap + hintSize.x * 0.5f;
                pointerPosition = new Vector2(
                    -hintSize.x * 0.5f,
                    ClampPointer(targetCenter.y - hintPosition.y, hintSize.y));
                pointerRotation = 90f;
                break;
        }

        hintPosition.x = Mathf.Clamp(
            hintPosition.x,
            parentRect.xMin + hintSize.x * 0.5f,
            parentRect.xMax - hintSize.x * 0.5f);
        hintPosition.y = Mathf.Clamp(
            hintPosition.y,
            parentRect.yMin + hintSize.y * 0.5f,
            parentRect.yMax - hintSize.y * 0.5f);

        // Recalculate the pointer after clamping the bubble. Otherwise the
        // bubble can move at the screen edge while the pointer stays at its
        // old position.
        switch (position)
        {
            case GuideHintPosition.Top:
                pointerPosition = new Vector2(
                    ClampPointer(targetCenter.x - hintPosition.x, hintSize.x),
                    -hintSize.y * 0.5f);
                break;
            case GuideHintPosition.Bottom:
                pointerPosition = new Vector2(
                    ClampPointer(targetCenter.x - hintPosition.x, hintSize.x),
                    hintSize.y * 0.5f);
                break;
            case GuideHintPosition.Left:
                pointerPosition = new Vector2(
                    hintSize.x * 0.5f,
                    ClampPointer(targetCenter.y - hintPosition.y, hintSize.y));
                break;
            case GuideHintPosition.Right:
                pointerPosition = new Vector2(
                    -hintSize.x * 0.5f,
                    ClampPointer(targetCenter.y - hintPosition.y, hintSize.y));
                break;
        }

        _root.anchoredPosition = hintPosition;
        if (_pointer != null)
        {
            _pointer.anchoredPosition = pointerPosition;
            _pointer.localEulerAngles = new Vector3(0f, 0f, pointerRotation);
        }
    }

    private void PlayShowAnimation()
    {
        _animation?.Kill();
        if (_canvasGroup == null || _animationDuration <= 0f)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            return;
        }

        _canvasGroup.alpha = 0f;
        _animation = _canvasGroup.DOFade(1f, _animationDuration).SetEase(Ease.OutQuad);
    }

    private bool TryGetTargetBounds(RectTransform target, RectTransform parent, out Rect bounds)
    {
        bounds = default;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(ResolveCamera(target), corners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, ResolveCamera(target), out Vector2 localPoint))
            {
                return false;
            }

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private float ClampPointer(float localOffset, float hintLength)
    {
        return Mathf.Clamp(
            localOffset,
            -hintLength * 0.5f + _edgePadding,
            hintLength * 0.5f - _edgePadding);
    }

    private GuideHintPosition ResolvePosition(GuideHintPosition position, Rect targetBounds, Rect parentRect)
    {
        if (position != GuideHintPosition.Auto)
        {
            return position;
        }

        float spaceAbove = parentRect.yMax - targetBounds.yMax;
        float spaceBelow = targetBounds.yMin - parentRect.yMin;
        return spaceBelow >= spaceAbove ? GuideHintPosition.Bottom : GuideHintPosition.Top;
    }

    private Camera ResolveCamera(RectTransform target)
    {
        if (_targetCamera != null)
        {
            return _targetCamera;
        }

        Canvas canvas = target.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }
}
