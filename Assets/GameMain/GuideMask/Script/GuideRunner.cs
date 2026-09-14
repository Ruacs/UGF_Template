using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideRunner : MonoBehaviour
{
    [SerializeField] private GuideMaskController _maskController;
    [SerializeField] private GuideHintView _hintView;
    [SerializeField] private float _searchInterval = 0.05f;

    private Coroutine _playRoutine;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    public void Play(IReadOnlyList<GuideStep> steps)
    {
        Stop();
        _playRoutine = StartCoroutine(PlayRoutine(steps));
    }

    public void Stop()
    {
        if (_maskController != null)
        {
            _maskController.Closed -= HandleMaskClosed;
        }

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _isPlaying = false;
        _maskController?.Close(0.15f);
    }

    private IEnumerator PlayRoutine(IReadOnlyList<GuideStep> steps)
    {
        _isPlaying = true;
        if (_maskController == null)
        {
            _maskController = FindObjectOfType<GuideMaskController>(true);
        }

        if (_hintView == null)
        {
            _hintView = FindObjectOfType<GuideHintView>(true);
        }

        if (_maskController == null || steps == null)
        {
            Finish();
            yield break;
        }

        _maskController.Closed += HandleMaskClosed;

        for (int i = 0; i < steps.Count; i++)
        {
            GuideStep step = steps[i];
            switch (step.type)
            {
                case GuideStepType.FocusTarget:
                    yield return FocusTarget(step);
                    break;
                case GuideStepType.WaitPage:
                    yield return WaitForPage(step.pageId);
                    break;
                case GuideStepType.Complete:
                    Finish();
                    yield break;
            }
        }

        Finish();
    }

    private IEnumerator FocusTarget(GuideStep step)
    {
        GuideTarget target = null;
        while (target == null)
        {
            target = FindTarget(step.pageId, step.targetId);
            if (target == null)
            {
                yield return new WaitForSeconds(_searchInterval);
            }
        }

        if (!_maskController.MoveTo(
                target.RectTransform,
                duration: step.focusDuration,
                closeOnClick: step.closeOnClick,
                autoCloseAfter: step.autoCloseAfter))
        {
            Finish();
            yield break;
        }

        if (_hintView != null && !string.IsNullOrEmpty(step.hintText))
        {
            _hintView.Show(step.hintText, target.RectTransform, step.hintPosition, step.hintOffset);
        }

        bool clicked = false;
        target.Clicked += OnClicked;
        while (!clicked && target.IsAvailable)
        {
            yield return null;
        }

        target.Clicked -= OnClicked;
        _hintView?.Hide();

        void OnClicked(GuideTarget clickedTarget)
        {
            clicked = clickedTarget == target;
        }
    }

    private IEnumerator WaitForPage(string pageId)
    {
        while (FindPage(pageId) == null)
        {
            yield return new WaitForSeconds(_searchInterval);
        }
    }

    private GuideTarget FindTarget(string pageId, string targetId)
    {
        GuideTarget[] targets = FindObjectsOfType<GuideTarget>(true);
        for (int i = 0; i < targets.Length; i++)
        {
            GuideTarget target = targets[i];
            if (target.IsAvailable && target.PageId == pageId && target.TargetId == targetId)
            {
                return target;
            }
        }

        return null;
    }

    private GuidePage FindPage(string pageId)
    {
        GuidePage[] pages = FindObjectsOfType<GuidePage>(true);
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i].IsReady && pages[i].PageId == pageId)
            {
                return pages[i];
            }
        }

        return null;
    }

    private void Finish()
    {
        if (_maskController != null)
        {
            _maskController.Closed -= HandleMaskClosed;
        }

        _isPlaying = false;
        _playRoutine = null;
        _hintView?.Hide();
        _maskController?.Close(0.2f);
    }

    private void HandleMaskClosed()
    {
        if (_isPlaying)
        {
            Finish();
        }
    }
}
