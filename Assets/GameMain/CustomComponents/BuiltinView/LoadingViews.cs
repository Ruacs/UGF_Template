using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class LoadingViews : MonoBehaviour
{

    [SerializeField] private DOTweenSequence m_OpenSequence;
    [SerializeField] private DOTweenSequence m_CloseSequence;


    [SerializeField] private Image m_ProgressBar;
    [SerializeField] private TMP_Text m_TextProgress;

    [SerializeField] private GameObject m_ProgressRoot;
    [SerializeField] private Slider m_progressSlider;

    [SerializeField] private bool m_useSlider = false;
    private float m_Progress;

    private bool m_IsActive = false;

    private void Awake()
    {
        if (m_ProgressRoot == null)
        {
            Transform progressRoot = transform.Find("Root_Progress");
            if (progressRoot != null)
            {
                m_ProgressRoot = progressRoot.gameObject;
            }
        }
    }

    private Tween OpenAnimation()
    {
        if (m_OpenSequence != null)
        {
            return m_OpenSequence.DOPlay();
        }

        return null;
    }

    private Tween CloseAnimation()
    {
        if (m_CloseSequence != null)
        {
            return m_CloseSequence.DOPlay();
        }
        return null;
    }

    public async UniTaskVoid SetActive(bool active)
    {
        if (m_IsActive == active)
        {
            return;
        }

        if (active)
        {
            gameObject.SetActive(true);
            m_IsActive = true;
            _ = OpenAnimation();
            return;
        }

        m_IsActive = false;
        Tween closeTween = CloseAnimation();
        if (closeTween != null)
        {
            await UniTask.Delay((int)(closeTween.Duration(true) * 1000));
        }

        if (!m_IsActive)
        {
            gameObject.SetActive(false);
        }
    }

    public void SetProgress(float progress)
    {
        m_Progress = progress;
        if (m_useSlider)
        {
            m_progressSlider.value = m_Progress;
        }
        else if (m_ProgressBar != null)
        {
            m_ProgressBar.fillAmount = m_Progress;
        }

        if (m_TextProgress != null)
        {
            m_TextProgress.text = NumberFormatUtil.FormatToPercent(m_Progress);
        }
    }

    public void SetProgressVisible(bool visible)
    {
        if (m_ProgressRoot != null)
        {
            m_ProgressRoot.SetActive(visible);
            return;
        }

        if (m_ProgressBar != null)
        {
            m_ProgressBar.gameObject.SetActive(visible);
        }

        if (m_progressSlider != null)
        {
            m_progressSlider.gameObject.SetActive(visible);
        }

        if (m_TextProgress != null)
        {
            m_TextProgress.gameObject.SetActive(visible);
        }
    }
}
