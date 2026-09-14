using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ItemCollectBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform m_RootRT;
    [SerializeField] private RectTransform m_IconRT;
    [SerializeField] private Image m_Icon;

    [SerializeField] private TMP_Text m_CountText;

    public RectTransform IconRT { get => m_IconRT;}

    public void Init(Sprite icon, int count)
    {
        m_Icon.sprite = icon;
        m_CountText.text = count.ToString();
    }

    public void SetCount(int count)
    {
        m_CountText.text = count.ToString();
    }

    public void SetIcon(Sprite icon)
    {
        m_Icon.sprite = icon;
    }
}
