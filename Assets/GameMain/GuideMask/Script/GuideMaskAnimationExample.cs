using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GuideMask 的最小运行案例：显示第一个按钮，移动到第二个按钮，再关闭。
/// 仅用于示例，不属于正式引导流程。
/// </summary>
public class GuideMaskAnimationExample : MonoBehaviour
{
    [SerializeField] private GuideMaskController _controller;
    [SerializeField] private RectTransform _firstTarget;
    [SerializeField] private RectTransform _secondTarget;
    [SerializeField, Min(0f)] private float _stayDuration = 1f;
    [SerializeField, Min(0f)] private float _moveDuration = 0.5f;
    [SerializeField, Min(0f)] private float _closeDuration = 0.25f;

    private IEnumerator Start()
    {
        ResolveReferences();
        yield return null;

        if (_controller == null || _firstTarget == null || _secondTarget == null)
        {
            Debug.LogWarning("GuideMaskAnimationExample: 请设置 GuideMaskController 和两个目标。", this);
            yield break;
        }

        _controller.Show(_firstTarget, new GuideMaskOptions { duration = 0f });
        yield return new WaitForSeconds(_stayDuration);

        _controller.MoveTo(_secondTarget, duration: _moveDuration);
        yield return new WaitForSeconds(_moveDuration + _stayDuration);

        _controller.Close(_closeDuration);
    }

    private void ResolveReferences()
    {
        if (_controller == null)
        {
            _controller = GetComponentInChildren<GuideMaskController>(true);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (_firstTarget == null && buttons.Length > 0)
        {
            _firstTarget = buttons[0].GetComponent<RectTransform>();
        }

        if (_secondTarget == null && buttons.Length > 1)
        {
            _secondTarget = buttons[1].GetComponent<RectTransform>();
        }
    }
}
