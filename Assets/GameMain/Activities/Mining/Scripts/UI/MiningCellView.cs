using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lokas.Activities.Mining.UI
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class MiningCellView : MonoBehaviour, IPointerClickHandler
    {
        private const float OpenDuration = 0.18f;

        private RectTransform m_RectTransform;
        private Image m_Image;
        private Sprite m_ClosedSprite;
        private Sprite m_CrackSprite;
        private Action<int> m_Clicked;
        private Sequence m_OpenTween;
        private bool m_IsOpen;
        private bool m_IsInteractable;

        public int CellId { get; private set; }

        public void Bind(int cellId, Sprite closedSprite, Sprite crackSprite, Action<int> clicked)
        {
            ResolveComponents();
            CellId = cellId;
            m_ClosedSprite = closedSprite != null ? closedSprite : m_Image.sprite;
            m_CrackSprite = crackSprite;
            m_Clicked = clicked;
        }

        public void SetClosed(bool interactable)
        {
            KillTween();
            ResolveComponents();
            m_IsOpen = false;
            m_IsInteractable = interactable;
            m_RectTransform.localScale = Vector3.one;
            m_Image.enabled = true;
            m_Image.color = Color.white;
            if (m_ClosedSprite != null) m_Image.sprite = m_ClosedSprite;
            m_Image.raycastTarget = interactable;
        }

        public void SetOpenedImmediate()
        {
            KillTween();
            ResolveComponents();
            m_IsOpen = true;
            m_IsInteractable = false;
            m_RectTransform.localScale = Vector3.one;
            m_Image.raycastTarget = false;
            m_Image.enabled = false;
        }

        public void SetInteractable(bool interactable)
        {
            ResolveComponents();
            m_IsInteractable = interactable && !m_IsOpen;
            m_Image.raycastTarget = m_IsInteractable;
        }

        public void PlayDig(Action opened)
        {
            KillTween();
            ResolveComponents();
            m_IsOpen = true;
            m_IsInteractable = false;
            m_Image.raycastTarget = false;
            if (m_CrackSprite != null) m_Image.sprite = m_CrackSprite;

            m_OpenTween = DOTween.Sequence()
                .Append(m_Image.DOFade(0f, OpenDuration).SetEase(Ease.InQuad))
                .Join(m_RectTransform.DOPunchScale(Vector3.one * 0.08f, OpenDuration, 5, 0.35f))
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    m_Image.enabled = false;
                    m_RectTransform.localScale = Vector3.one;
                    opened?.Invoke();
                })
                .OnKill(() => m_OpenTween = null);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_IsInteractable && !m_IsOpen) m_Clicked?.Invoke(CellId);
        }

        private void OnDisable()
        {
            KillTween();
        }

        private void ResolveComponents()
        {
            if (m_RectTransform == null) m_RectTransform = (RectTransform)transform;
            if (m_Image == null) m_Image = GetComponent<Image>();
        }

        private void KillTween()
        {
            if (m_OpenTween != null && m_OpenTween.IsActive()) m_OpenTween.Kill();
            m_OpenTween = null;
        }
    }
}
