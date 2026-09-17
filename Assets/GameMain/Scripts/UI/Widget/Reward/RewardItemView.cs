using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting.APIUpdating;

namespace Lokas
{
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: null, sourceClassName: "UI_ItemProp")]
    public class RewardItemView : MonoBehaviour
    {

        [SerializeField] private RectTransform bg;
        [SerializeField] private Image m_icon;
        [SerializeField] private TMP_Text m_content;

        private Tween m_ScaleTween;
        private Tween m_MoveTween;
        private Tween m_BgRotateTween;

        public void Init()
        {
            transform.localScale = Vector3.zero;
        }

        public void RefreshUI(Sprite icon, string content)
        {
            Bind(new RewardItemViewData(icon, content));
        }

        public void Bind(RewardItemViewData data)
        {
            if (m_icon != null)
            {
                m_icon.sprite = data?.Icon;
                m_icon.enabled = data?.Icon != null;
            }
            if (m_content != null) m_content.text = data?.Text ?? string.Empty;
        }

        public void SetIcon(Sprite icon, bool IsSetNativeSize = false)
        {
            m_icon.sprite = icon;
            m_icon.enabled = icon != null;
            if (IsSetNativeSize)
            {
                m_icon.SetNativeSize();
            }

        }


        public void SetContent(string count)
        {
            m_content.text = count;
        }

        public void DoPlay(float delay = 0)
        {
            StopTweens();

            transform.localScale = Vector3.zero;
            if (bg != null) bg.localEulerAngles = Vector3.zero;

            m_ScaleTween = transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);
            if (bg != null)
                m_BgRotateTween = bg
                    .DORotate(new Vector3(0f, 0f, -360f), 2f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
        }

        public void DoPlayMove(Vector2 targetPos, float delay = 0)
        {
            StopTweens();

            var rt = GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            transform.localScale = Vector3.zero;
            if (bg != null) bg.localEulerAngles = Vector3.zero;

            m_MoveTween = rt.DOAnchorPos(targetPos, 0.4f).SetEase(Ease.OutCubic).SetDelay(delay);
            m_ScaleTween = transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);
            if (bg != null)
                m_BgRotateTween = bg
                    .DORotate(new Vector3(0f, 0f, -360f), 2f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
        }

        public void Hide()
        {
            m_ScaleTween?.Kill();

            m_ScaleTween = transform.DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(StopTweens);
        }

        private void OnDisable() => StopTweens();

        private void StopTweens()
        {
            m_ScaleTween?.Kill();
            m_MoveTween?.Kill();
            m_BgRotateTween?.Kill();
            m_ScaleTween = null;
            m_MoveTween = null;
            m_BgRotateTween = null;
        }

    }
}
