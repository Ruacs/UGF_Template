using Cysharp.Threading.Tasks;
using UnityEngine;

public class BuiltinViewComponent : UnityGameFramework.Runtime.GameFrameworkComponent
{
    [Header("LoadingViews")]
    [SerializeField] private LoadingViews m_LoadingViews;

    [Header("Loading Progress")]
    [SerializeField] private float m_MinimumLoadingDuration = 2.0f;
    [SerializeField] private float m_ProgressSmoothSpeed = 2.0f;
    [SerializeField] private float m_CompleteSmoothSpeed = 1.0f;
    [SerializeField] private float m_CompletionDelay = 0.2f;

    private float m_SmoothProgress;
    private float m_ElapsedLoadingTime;
    private float m_CompletionTimer;
    private bool m_IsTransitionVisible;
    private bool m_IsTransitionProgressVisible;

    public void ShowTransition(bool showProgress = false, float progress = 0, bool reset = true)
    {
        if (reset)
        {
            ResetTransitionProgress(progress);
        }
        else
        {
            m_SmoothProgress = Mathf.Max(m_SmoothProgress, Mathf.Clamp01(progress));
            m_CompletionTimer = 0f;
        }

        m_IsTransitionVisible = true;
        m_IsTransitionProgressVisible = showProgress;

        if (m_LoadingViews != null)
        {
            m_LoadingViews.SetActive(true).Forget();
            m_LoadingViews.SetProgressVisible(showProgress);
            m_LoadingViews.SetProgress(m_SmoothProgress);
        }
    }

    public async void HideTransition(float delay = 0)
    {
        m_IsTransitionVisible = false;

        if (m_LoadingViews != null)
        {
            if (delay > 0)
            {
                await UniTask.Delay((int)(delay * 1000));
            }

            if (m_IsTransitionVisible)
            {
                return;
            }

            m_LoadingViews.SetActive(false).Forget();
            m_LoadingViews.SetProgress(0);
        }

        ResetTransitionProgress();
    }

    public void SetTransitionProgress(float progress)
    {
        m_SmoothProgress = Mathf.Clamp01(progress);

        if (m_LoadingViews != null)
        {
            m_LoadingViews.SetProgress(m_SmoothProgress);
        }
    }

    public bool UpdateTransitionProgress(float targetProgress, bool isComplete, float elapseSeconds)
    {
        if (!m_IsTransitionProgressVisible)
        {
            return isComplete;
        }

        targetProgress = Mathf.Clamp01(targetProgress);
        m_ElapsedLoadingTime += elapseSeconds;

        bool isMinimumTimeReached = m_MinimumLoadingDuration <= 0f || m_ElapsedLoadingTime >= m_MinimumLoadingDuration;
        if (isComplete)
        {
            float timedProgress = m_MinimumLoadingDuration > 0f
                ? Mathf.Clamp01(m_ElapsedLoadingTime / m_MinimumLoadingDuration)
                : 1f;
            targetProgress = Mathf.Max(m_SmoothProgress, timedProgress);

            if (isMinimumTimeReached)
            {
                targetProgress = 1f;
                m_CompletionTimer += elapseSeconds;
            }
            else
            {
                m_CompletionTimer = 0f;
            }
        }
        else
        {
            m_CompletionTimer = 0f;
        }

        float smoothSpeed = isComplete ? m_CompleteSmoothSpeed : m_ProgressSmoothSpeed;
        m_SmoothProgress = Mathf.MoveTowards(m_SmoothProgress, targetProgress, smoothSpeed * elapseSeconds);

        if (m_LoadingViews != null)
        {
            m_LoadingViews.SetProgress(m_SmoothProgress);
        }

        return isComplete && isMinimumTimeReached && m_CompletionTimer > m_CompletionDelay && m_SmoothProgress >= 1f;
    }

    public bool IsTransitionProgressReached(float progress)
    {
        return m_SmoothProgress >= Mathf.Clamp01(progress);
    }

    public void BeginLoadingProgress(float progress = 0, bool reset = true, bool showProgressBar = true)
    {
        ShowTransition(showProgressBar, progress, reset);
    }

    public void ShowLoadingProgress(float progress = 0, bool showProgressBar = true)
    {
        BeginLoadingProgress(progress, true, showProgressBar);
    }

    public void SetLoadingProgress(float progress)
    {
        SetTransitionProgress(progress);
    }

    public bool UpdateLoadingProgress(float targetProgress, bool isComplete, float elapseSeconds)
    {
        return UpdateTransitionProgress(targetProgress, isComplete, elapseSeconds);
    }

    public bool IsLoadingProgressReached(float progress)
    {
        return IsTransitionProgressReached(progress);
    }


    public void HideLoadingProgress(float delay = 0)
    {
        HideTransition(delay);
    }

    private void ResetTransitionProgress(float progress = 0)
    {
        m_SmoothProgress = Mathf.Clamp01(progress);
        m_ElapsedLoadingTime = 0f;
        m_CompletionTimer = 0f;
        m_IsTransitionProgressVisible = false;
    }
}
