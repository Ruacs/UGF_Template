using System;
using System.Collections;
using System.Collections.Generic;
using GF_Mahjong;
using UnityEngine;
using UnityEngine.UI;

public class UI_AvatarFrameItem : MonoBehaviour
{
    [SerializeField] private RectTransform m_SelectedRT;
    [SerializeField] private Image m_AvatarFrame;
    [SerializeField] private RectTransform m_MarkRT;
    [SerializeField] private Button m_SelfBtn;

    private Action m_OnClick;

    private void Awake()
    {
        m_SelfBtn.AddSafeClick(OnClick);
    }

    private void OnDestroy()
    {
        m_SelfBtn.onClick.RemoveAllListeners();
    }

    public void Init(Sprite frame, bool isSelected, bool hasMark, Action onClick)
    {
        m_AvatarFrame.sprite = frame;
        m_OnClick = onClick;
        SetSelected(isSelected);
        // m_MarkRT.gameObject.SetActive(hasMark);
    }

    public void SetSelected(bool selected)
    {
        
        m_SelectedRT.gameObject.SetActive(selected);
    }

    private void OnClick()
    {
        m_OnClick?.Invoke();
    }
}
