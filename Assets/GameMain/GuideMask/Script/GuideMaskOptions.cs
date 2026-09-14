using UnityEngine;

public struct GuideMaskOptions
{
    public Camera targetCamera;
    public float sizeMultiplier;
    public float duration;
    public bool closeOnClick;
    public float autoCloseAfter;
    public string hintText;
    public GuideHintPosition hintPosition;
    public float hintOffset;

    public static GuideMaskOptions Default => new GuideMaskOptions
    {
        sizeMultiplier = 1f,
        duration = -1f,
        autoCloseAfter = -1f,
        hintPosition = GuideHintPosition.Auto,
        hintOffset = -1f
    };
}
