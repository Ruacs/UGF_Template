using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;

public class UI_GuideTipsBox : MonoBehaviour
{
    public static UI_GuideTipsBox Instance;
    [SerializeField] private RectTransform m_contentRootRT;
    [SerializeField] private TMP_Text m_contentTMP;
    [SerializeField] private CanvasGroup m_canvasGroup;

    private bool isShow = false;


    void Awake()
    {
        m_contentRootRT.localScale = new Vector3(0, 1, 1);
        Instance = this;
        isShow = false;
    }

    
    public async UniTask Show(string content)
    {
        if (Instance == null) return;
        if (isShow) return;
        isShow = true;
        m_contentTMP.text = content;
        RefreshContentLayout();
        m_canvasGroup.alpha = 1;
        await m_contentRootRT.DOScaleX(1, 0.3f).SetEase(Ease.OutBack).ToUniTask();
    }

    public async UniTask Hide()
    {
        if (Instance == null) return;
        if (!isShow) return;
        isShow = false;
        await m_contentRootRT.DOScaleX(0, 0.2f).ToUniTask();
        m_contentTMP.text = "";
        m_canvasGroup.alpha = 0;
    }

    private void RefreshContentLayout()
    {
        m_contentTMP.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_contentTMP.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_contentRootRT);
        Canvas.ForceUpdateCanvases();
    }
}

