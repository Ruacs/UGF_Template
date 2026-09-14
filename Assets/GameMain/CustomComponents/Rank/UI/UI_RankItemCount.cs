using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public class UI_RankItemCount : MonoBehaviour
    {
        [SerializeField] private RectTransform m_selfRT;
        [SerializeField] private CanvasGroup m_canvasGroup;
        [SerializeField] private Image m_iconImg;
        [SerializeField] private TMP_Text m_numTMP;

        [SerializeField] private float m_scaleDuration = 0.25f;
        [SerializeField] private float m_moveDuration = 0.5f;
        [SerializeField] private float m_fadeDuration = 0.25f;
        [SerializeField] private float m_moveUpDistance = 200f;

        private Sequence m_seq;

        public async UniTask Play(Sprite icon, int count, Vector2 startAnchoredPos)
        {
            m_seq?.Kill();

            m_iconImg.sprite = icon;
            m_numTMP.text =  "+" + count.ToString();

            m_selfRT.anchoredPosition = startAnchoredPos;
            m_selfRT.localScale = Vector3.zero;
            m_canvasGroup.alpha = 1f;

            m_seq = DOTween.Sequence()
                .Append(m_selfRT.DOScale(Vector3.one, m_scaleDuration).SetEase(Ease.OutBack))
                .Append(m_selfRT.DOAnchorPosY(startAnchoredPos.y + m_moveUpDistance, m_moveDuration).SetEase(Ease.OutSine))
                .Append(m_canvasGroup.DOFade(0f, m_fadeDuration).SetDelay(0.2f).SetEase(Ease.InSine));

            await m_seq.ToUniTask();
        }

        private void OnDestroy()
        {
            m_seq?.Kill();
        }
    }
}
