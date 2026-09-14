using System;

public enum GuideStepType
{
    FocusTarget,
    WaitPage,
    Complete
}

[Serializable]
public class GuideStep
{
    public GuideStepType type;
    public string pageId;
    public string targetId;
    public float focusDuration = 0.3f;
    public bool closeOnClick;
    public float autoCloseAfter = -1f;
    public string hintText;
    public GuideHintPosition hintPosition = GuideHintPosition.Auto;
    public float hintOffset = -1f;

    public static GuideStep FocusTarget(
        string pageId,
        string targetId,
        float duration = 0.3f,
        bool closeOnClick = false,
        float autoCloseAfter = -1f,
        string hintText = null,
        GuideHintPosition hintPosition = GuideHintPosition.Auto,
        float hintOffset = -1f)
    {
        return new GuideStep
        {
            type = GuideStepType.FocusTarget,
            pageId = pageId,
            targetId = targetId,
            focusDuration = duration,
            closeOnClick = closeOnClick,
            autoCloseAfter = autoCloseAfter,
            hintText = hintText,
            hintPosition = hintPosition,
            hintOffset = hintOffset
        };
    }

    public static GuideStep WaitPage(string pageId)
    {
        return new GuideStep
        {
            type = GuideStepType.WaitPage,
            pageId = pageId
        };
    }

    public static GuideStep CompleteStep()
    {
        return new GuideStep { type = GuideStepType.Complete };
    }
}
