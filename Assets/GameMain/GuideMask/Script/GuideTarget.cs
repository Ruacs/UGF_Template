using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GuideTarget : MonoBehaviour
{
    [SerializeField] private string _pageId;
    [SerializeField] private string _targetId;
    [SerializeField] private Button _button;

    public string PageId => _pageId;
    public string TargetId => _targetId;
    public RectTransform RectTransform => transform as RectTransform;
    public bool IsAvailable => isActiveAndEnabled && gameObject.activeInHierarchy;
    public event Action<GuideTarget> Clicked;

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.onClick.AddListener(NotifyClicked);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(NotifyClicked);
        }
    }

    public void Configure(string pageId, string targetId, Button button = null)
    {
        _pageId = pageId;
        _targetId = targetId;
        if (button != null && button != _button)
        {
            _button?.onClick.RemoveListener(NotifyClicked);
            _button = button;
            _button.onClick.AddListener(NotifyClicked);
        }
    }

    private void NotifyClicked()
    {
        Clicked?.Invoke(this);
    }
}
