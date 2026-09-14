using UnityEngine;

public struct GuideMaskTarget
{
    public Vector2 center;
    public Vector2 size;
    public float cornerRadius;

    public GuideMaskTarget(Vector2 center, Vector2 size, float cornerRadius)
    {
        this.center = center;
        this.size = size;
        this.cornerRadius = cornerRadius;
    }

    public static GuideMaskTarget Lerp(GuideMaskTarget from, GuideMaskTarget to, float t)
    {
        return new GuideMaskTarget(
            Vector2.LerpUnclamped(from.center, to.center, t),
            Vector2.LerpUnclamped(from.size, to.size, t),
            Mathf.LerpUnclamped(from.cornerRadius, to.cornerRadius, t));
    }
}

public static class GuideMaskTargetResolver
{
    public static bool TryResolve(
        RectTransform target,
        RectTransform maskRect,
        Camera camera,
        float sizeMultiplier,
        float cornerRadius,
        out GuideMaskTarget result)
    {
        result = default;
        if (target == null || maskRect == null)
        {
            return false;
        }

        camera = ResolveCamera(target, camera);

        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskRect, screenPoint, camera, out Vector2 localPoint))
            {
                return false;
            }

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        Vector2 size = (max - min) * Mathf.Max(0f, sizeMultiplier);
        result = new GuideMaskTarget((min + max) * 0.5f, size, cornerRadius);
        return true;
    }

    private static Camera ResolveCamera(RectTransform target, Camera camera)
    {
        if (camera != null)
        {
            return camera;
        }

        Canvas canvas = target.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }
}
