using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening;
using System;

public class PromptLogic : MonoBehaviour
{
    private TMP_Text m_Content;
    private CanvasGroup m_CanvasGroup;
    private RectTransform _rect;
    private Tween m_MoveTween;
    private Tween m_FadeTween;
    private bool m_IsShowing;
    private bool m_IsHiding;
    private bool m_HideOnClick;
    private int m_ShowFrame = -1;
    private const float ClickHideDuration = 0.2f;
    private const float ClickHideMoveOffsetY = 80f;

    public Action onHide;

    private void Init()
    {
        m_Content = transform.Find("Text_Content").GetComponent<TMP_Text>();
        m_CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        _rect = transform.GetComponent<RectTransform>();
    }



    public void SetContent(string content)
    {
        m_Content.text = content;
    }



    private void Update()
    {
        if (!m_IsShowing || !m_HideOnClick)
        {
            return;
        }

        if (Time.frameCount <= m_ShowFrame)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Hide(true);
            return;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Hide(true);
        }
    }



    public void Show(float duration = 0.5f, float delay = 1.5f, bool hideOnClick = false)
    {
        if (m_Content == null || _rect == null || m_CanvasGroup == null)
        {
            Init();
        }

        KillTweens();

        m_IsShowing = true;
        m_IsHiding = false;
        m_HideOnClick = hideOnClick;
        m_ShowFrame = Time.frameCount;

        gameObject.SetActive(true);
        Log.Info("Show Prompt");
        _rect.anchoredPosition = new Vector2(0, 200);
        _rect.localScale = Vector3.one;
        m_CanvasGroup.alpha = 1;

        m_MoveTween = _rect.DOAnchorPosY(-200, duration).SetEase(Ease.OutBack).SetUpdate(true);
        if (m_HideOnClick)
        {
            return;
        }

        m_FadeTween = m_CanvasGroup.DOFade(0, duration).SetEase(Ease.OutBack).SetDelay(delay + 1).SetUpdate(true).OnComplete(CompleteHide);

    }


    public void Hide(bool playAnimation = false)
    {
        if (!m_IsShowing || m_IsHiding)
        {
            return;
        }

        m_HideOnClick = false;

        if (!playAnimation)
        {
            CompleteHide();
            return;
        }

        m_IsHiding = true;
        KillTweens();
        m_MoveTween = _rect.DOAnchorPosY(_rect.anchoredPosition.y + ClickHideMoveOffsetY, ClickHideDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true);
        m_FadeTween = m_CanvasGroup.DOFade(0, ClickHideDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(CompleteHide);
    }


    private void CompleteHide()
    {
        if (!m_IsShowing)
        {
            return;
        }

        m_IsShowing = false;
        m_IsHiding = false;
        m_HideOnClick = false;
        KillTweens();
        onHide?.Invoke();
        gameObject.SetActive(false);
    }


    private void KillTweens()
    {
        m_MoveTween?.Kill();
        m_MoveTween = null;
        m_FadeTween?.Kill();
        m_FadeTween = null;
    }


    private void OnDisable()
    {
        m_IsShowing = false;
        m_IsHiding = false;
        m_HideOnClick = false;
        KillTweens();
    }




}
