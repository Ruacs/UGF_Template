using Ads;
using Lokas;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUIPanel : UGuiForm
{

    private ProcedureChangeScene m_ProcedureChangeScene;

    [SerializeField] private Image m_ProgressBar;
    [SerializeField] private TMP_Text m_TextProgress;

    [SerializeField] private Slider m_progressSlider;

    [SerializeField] private bool m_useSlider = false;

    private Image m_Mask;


    private float m_Progress;

    private float m_ProgressSpeed = 0.5f;

    private static bool m_HideSplash = false;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        m_ProgressBar = transform.Find("Root_Progress/ProgressBar").GetComponent<Image>();
        m_TextProgress = transform.Find("Root_Progress/tmp_Progress").GetComponent<TMP_Text>();

    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        HideSplash();

        m_ProcedureChangeScene = (ProcedureChangeScene)userData;
        m_Progress = 0.0f;
        if (m_useSlider)
            m_progressSlider.value = 0;
        else
            m_ProgressBar.fillAmount = 0;
        m_TextProgress.text = "0%";

        AdsAnalytics.EventWithName($"加载页_打开");
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        m_Progress += Time.deltaTime * m_ProgressSpeed;
        if (m_Progress <= 0.95f)
        {
            if (m_useSlider)
                m_progressSlider.value = m_Progress;
            else
                m_ProgressBar.fillAmount = m_Progress;

            m_TextProgress.text = NumberFormatUtil.FormatToPercent(m_Progress);

            // TransitionUI.Instance.SetProgress(m_Progress);
        }
        else
        {
            LoadComplete();
        }

    }


    private void LoadComplete()
    {
        // m_ProcedureChangeScene.LoadComplete();
        // TransitionUI.Instance.PlayIn();
    }


    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        AdsAnalytics.EventWithName($"加载页_关闭");
    }


    private void HideSplash()
    {
        if (m_HideSplash) return;
        m_HideSplash = true;
        AdsManager.HideSplash();

    }



}
