using DG.Tweening;
using TMPro;
using UnityEngine;
using GameFramework.ObjectPool;
using Coffee.UIExtensions;
using UnityEngine.UI;

namespace UnityGameFramework.Runtime
{
    public class FloatingText : MonoBehaviour, IPoolable
    {
        [SerializeField] private RectTransform m_bgRT;
        [SerializeField] private TMP_Text m_floatingTMP;
        [SerializeField] private FloatingTextData m_data;
        [SerializeField] private RectTransform m_selfRect;
        [SerializeField] private UIParticle m_particle;

        private Sequence m_sequence;

        public void Init()
        {
            m_selfRect ??= GetComponent<RectTransform>();

        }

        public void OnRecycle()
        {
            m_sequence?.Kill();
            m_sequence = null;

            m_floatingTMP.SetText("");
            m_floatingTMP.alpha = 1f;
            m_selfRect.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        public void OnSpawn()
        {
            m_floatingTMP.enableVertexGradient = false;
        }

        public void Play(FloatingTextData floatingTextData, Vector2 canvasStartLocalPos, Vector2 endLocalPos, System.Action callback = null)
        {
            m_sequence?.Kill();

            m_particle?.Play();
            m_bgRT.gameObject.SetActive(floatingTextData.bgActive);
            m_data = floatingTextData;
            gameObject.SetActive(true);
            m_floatingTMP.SetText(m_data.content);
            RefreshLayout();
            m_floatingTMP.alpha = 1f;
            m_selfRect.anchoredPosition = canvasStartLocalPos;
            m_selfRect.localScale = Vector3.one * m_data.startScale;

            Vector2 endPos;
            if (endLocalPos == Vector2.zero)
            {
                endPos = m_selfRect.anchoredPosition + m_data.offset;
            }
            else
            {
                endPos = endLocalPos;
            }

            m_sequence = DOTween.Sequence();
            switch (m_data.animType)
            {
                case FloatingTextAnimType.Pop:
                    m_selfRect.localScale = Vector3.zero;
                    m_sequence.Append(m_selfRect.DOScale(m_data.endScale * 1.25f, 0.15f).SetEase(Ease.OutBack));
                    m_sequence.Append(m_selfRect.DOScale(m_data.endScale, 0.1f));
                    m_sequence.Join(m_selfRect.DOAnchorPos(endPos, m_data.duration).SetEase(m_data.ease));
                    m_sequence.Join(m_selfRect.DOScale(m_data.endScale * 0.5f, m_data.duration / 2).SetDelay(m_data.duration / 2));
                    break;
                case FloatingTextAnimType.Shake:
                    m_sequence.Append(m_selfRect.DOScale(m_data.endScale * 1.15f, 0.1f).SetEase(Ease.OutQuad));
                    m_sequence.Append(m_selfRect.DOShakeAnchorPos(0.25f, 20f, 12));
                    m_sequence.Append(m_selfRect.DOAnchorPos(endPos, m_data.duration).SetEase(m_data.ease));
                    m_sequence.Join(m_selfRect.DOScale(m_data.endScale, m_data.duration));
                    break;
                case FloatingTextAnimType.FlyToTarget:
                    m_sequence.Append(m_selfRect.DOScale(m_data.endScale * 1.2f, 0.12f).SetEase(Ease.OutBack));
                    m_sequence.Append(m_selfRect.DOAnchorPos(endPos, m_data.duration).SetEase(Ease.InCubic));
                    m_sequence.Join(m_selfRect.DOScale(0.5f, m_data.duration));
                    m_sequence.Join(m_floatingTMP.DOFade(0f, m_data.duration));
                    break;
                case FloatingTextAnimType.FadeMove:
                    m_sequence.Append(m_selfRect.DOAnchorPos(endPos, m_data.duration).SetEase(m_data.ease));
                    m_sequence.Join(m_selfRect.DOScale(m_data.endScale, m_data.duration));
                    m_sequence.Join(m_floatingTMP.DOFade(0f, m_data.duration));
                    break;
                default:
                    m_sequence.Append(m_selfRect.DOAnchorPos(endPos, m_data.duration).SetEase(m_data.ease));
                    m_sequence.Join(m_selfRect.DOScale(m_data.endScale, m_data.duration));
                    break;
            }

            m_sequence.OnComplete(() => callback?.Invoke());

        }

        private void RefreshLayout()
        {
            m_selfRect ??= GetComponent<RectTransform>();

            m_floatingTMP.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_floatingTMP.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_selfRect);
        }




    }
}

