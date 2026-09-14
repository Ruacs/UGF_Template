using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class GuideMask : MaskableGraphic, ICanvasRaycastFilter, IPointerClickHandler
{
    [SerializeField, Min(0f)] private float _cornerRadius = 5f;
    [SerializeField, Range(1, 16)] private int _cornerSegments = 6;
    [Header("Preview")]
    [SerializeField] private bool _testMode = false;
    [SerializeField] private RectTransform _testTarget;
    [SerializeField] private Camera _testCamera;

    private Vector2 _targetMin;
    private Vector2 _targetMax;

    public RectTransform RectTransform => rectTransform;
    public float CornerRadius => _cornerRadius;
    public event Action PointerClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        PointerClicked?.Invoke();
    }

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out Vector2 local))
        {
            return true;
        }

        Rect targetRect = Rect.MinMaxRect(_targetMin.x, _targetMin.y, _targetMax.x, _targetMax.y);
        bool inHole = IsPointInsideRoundedRect(targetRect, local, GetEffectiveCornerRadius());
        return !inHole;
    }

    public void Close()
    {
        GuideMaskController controller = GetComponent<GuideMaskController>();
        if (controller != null)
        {
            controller.Close();
            return;
        }

        gameObject.SetActive(false);
    }

    public void SetTarget(GuideMaskTarget target)
    {
        Vector2 halfSize = Vector2.Max(Vector2.zero, target.size) * 0.5f;
        _targetMin = target.center - halfSize;
        _targetMax = target.center + halfSize;
        SetAllDirty();
    }

    /// <summary>
    /// 打开指引
    /// 注意 摄像机为主摄像机时调用
    /// </summary>
    public void Play(RectTransform target, float size = 1f)
    {
        GuideMaskController controller = GetComponent<GuideMaskController>();
        if (controller != null)
        {
            controller.Show(target, new GuideMaskOptions
            {
                sizeMultiplier = size,
                duration = 0f
            });
            return;
        }

        if (GuideMaskTargetResolver.TryResolve(target, rectTransform, null, size, _cornerRadius, out GuideMaskTarget resolved))
        {
            gameObject.SetActive(true);
            SetTarget(resolved);
        }
    }

    /// <summary>
    /// 打开指引
    /// 需指定摄像机
    /// </summary>
    public void Play(RectTransform target, Camera ui_Camera, float size = 1f)
    {
        GuideMaskController controller = GetComponent<GuideMaskController>();
        if (controller != null)
        {
            controller.Show(target, new GuideMaskOptions
            {
                targetCamera = ui_Camera,
                sizeMultiplier = size,
                duration = 0f
            });
            return;
        }

        if (GuideMaskTargetResolver.TryResolve(target, rectTransform, ui_Camera, size, _cornerRadius, out GuideMaskTarget resolved))
        {
            gameObject.SetActive(true);
            SetTarget(resolved);
        }
    }

    public void Init()
    {
        Close();
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear();

        Rect maskRect = rectTransform.rect;
        float xmin = _targetMin.x;
        float xmax = _targetMax.x;
        float ymin = _targetMin.y;
        float ymax = _targetMax.y;

        // Outside of target bounding box.
        AddRect(toFill, new Rect(maskRect.xMin, maskRect.yMin, Mathf.Max(0f, xmin - maskRect.xMin), maskRect.height));
        AddRect(toFill, new Rect(xmax, maskRect.yMin, Mathf.Max(0f, maskRect.xMax - xmax), maskRect.height));
        AddRect(toFill, new Rect(xmin, ymax, Mathf.Max(0f, xmax - xmin), Mathf.Max(0f, maskRect.yMax - ymax)));
        AddRect(toFill, new Rect(xmin, maskRect.yMin, Mathf.Max(0f, xmax - xmin), Mathf.Max(0f, ymin - maskRect.yMin)));

        float radius = GetEffectiveCornerRadius();
        if (radius <= 0f)
        {
            return;
        }

        int seg = Mathf.Max(1, _cornerSegments);
        AddCornerFan(toFill, new Vector2(xmin, ymax), new Vector2(xmin + radius, ymax - radius), radius, 90f, 180f, seg);
        AddCornerFan(toFill, new Vector2(xmax, ymax), new Vector2(xmax - radius, ymax - radius), radius, 90f, 0f, seg);
        AddCornerFan(toFill, new Vector2(xmax, ymin), new Vector2(xmax - radius, ymin + radius), radius, 0f, -90f, seg);
        AddCornerFan(toFill, new Vector2(xmin, ymin), new Vector2(xmin + radius, ymin + radius), radius, -90f, -180f, seg);
    }

    private float GetEffectiveCornerRadius()
    {
        float width = Mathf.Max(0f, _targetMax.x - _targetMin.x);
        float height = Mathf.Max(0f, _targetMax.y - _targetMin.y);
        return Mathf.Min(_cornerRadius, width * 0.5f, height * 0.5f);
    }

    private static bool IsPointInsideRoundedRect(Rect rect, Vector2 point, float radius)
    {
        if (!rect.Contains(point))
        {
            return false;
        }

        if (radius <= 0f)
        {
            return true;
        }

        float left = rect.xMin + radius;
        float right = rect.xMax - radius;
        float bottom = rect.yMin + radius;
        float top = rect.yMax - radius;

        if (point.x >= left && point.x <= right)
        {
            return true;
        }

        if (point.y >= bottom && point.y <= top)
        {
            return true;
        }

        float cx = point.x < left ? left : right;
        float cy = point.y < bottom ? bottom : top;
        return (point - new Vector2(cx, cy)).sqrMagnitude <= radius * radius;
    }

    private void AddRect(VertexHelper vh, Rect rect)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        int start = vh.currentVertCount;
        vh.AddVert(new Vector2(rect.xMin, rect.yMin), color, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMin, rect.yMax), color, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }

    private void AddCornerFan(VertexHelper vh, Vector2 corner, Vector2 center, float radius, float startDeg, float endDeg, int segments)
    {
        if (radius <= 0f)
        {
            return;
        }

        List<Vector2> arc = new List<Vector2>(segments + 1);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
            arc.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        for (int i = 0; i < arc.Count - 1; i++)
        {
            int start = vh.currentVertCount;
            vh.AddVert(corner, color, Vector2.zero);
            vh.AddVert(arc[i], color, Vector2.zero);
            vh.AddVert(arc[i + 1], color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }

    // protected override void OnValidate()
    // {
    //     if (_cornerSegments < 1)
    //     {
    //         _cornerSegments = 1;
    //     }

    //     if (_cornerRadius < 0f)
    //     {
    //         _cornerRadius = 0f;
    //     }

    //     if (!Application.isPlaying && _testMode)
    //     {
    //         PreviewInEditor();
    //     }

    //     SetAllDirty();
    // }

    private void PreviewInEditor()
    {
        RectTransform target = _testTarget;
        if (target == null)
        {
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        Camera cam = _testCamera != null ? _testCamera : null;
        if (!GuideMaskTargetResolver.TryResolve(target, rectTransform, cam, 1f, _cornerRadius, out GuideMaskTarget resolved))
        {
            return;
        }

        SetTarget(resolved);
        SetAllDirty();
    }
}
