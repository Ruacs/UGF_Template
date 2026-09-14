using System.Collections;
using System.Collections.Generic;
using Lokas;
using TMPro;
using UnityEngine;

public class UI_BlockCount : MonoBehaviour
{
    public const string SHOW = "Show";
    public const string HIDE = "Hide";


    [SerializeField] private DOTweenSequenceAnimator m_Animator;
    [SerializeField] private TMP_Text m_countTMP;

    [SerializeField] private RectTransform _selfRT;
    [SerializeField] private RectTransform m_parentRT;

    [SerializeField] private Vector2 m_offset;

    [SerializeField] private float speed = 10;

    [SerializeField] private bool m_IsShow = true;



    void Update()
    {
        if (_selfRT == null || m_parentRT == null)
        {
            return;
        }
        if (!m_IsShow) return;

        Camera uiCamera = GameEntry.UI != null ? GameEntry.UI.UICamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_parentRT, Input.mousePosition, uiCamera, out Vector2 localPos))
        {
            _selfRT.anchoredPosition = Vector2.Lerp(_selfRT.anchoredPosition, localPos + m_offset, Time.deltaTime * speed);
        }
    }


    public void PlayAnimate(bool isShow = true)
    {
        if (m_IsShow == isShow) return;
        m_IsShow = isShow;
        if (isShow)
        {
            Camera uiCamera = GameEntry.UI != null ? GameEntry.UI.UICamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_parentRT, Input.mousePosition, uiCamera, out Vector2 localPos))
            {
                _selfRT.anchoredPosition = localPos + m_offset;
            }
            m_Animator?.Play(SHOW);
        }
        else
        {
            m_Animator?.Play(HIDE);
        }
    }


    public void SetCount(int count)
    {
        m_countTMP.text = count.ToString();
    }


}
