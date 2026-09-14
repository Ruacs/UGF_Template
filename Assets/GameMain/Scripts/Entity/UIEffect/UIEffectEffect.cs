
using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIExtensions;
using UnityEngine;

namespace Lokas
{

    public class UIEffect : Entity
    {
        private RectTransform _selfRect;
        private Transform _originalParent;
        private UIParticle particle;
        [SerializeField]
        protected UIEffectData m_Data;

        protected float m_KeepTime = 0f;
        protected bool m_canHide = false;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            _selfRect = GetComponent<RectTransform>();
            m_Data = (UIEffectData)userData;
            m_KeepTime = 0;
            m_canHide = false;
            _originalParent = transform.parent;
            transform.localScale = m_Data.Scale;

            if (m_Data.Parent != null)
            {
                transform.SetParent(m_Data.Parent);
            }
            _selfRect.anchoredPosition = ScreenToCanvasLocal(m_Data.SpawnScreenPos);

            particle = GetComponent<UIParticle>();
            particle.Play();

        }
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            m_KeepTime += elapseSeconds;
            if (m_KeepTime > m_Data.KeepTime && !m_canHide)
            {
                m_canHide = true;
                GameEntry.Entity.HideEntity(this);
            }

        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            transform.SetParent(_originalParent);
            particle.Stop();
        }



        private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_Data.CanvasRect, screenPos, GameEntry.UI.UICamera, out Vector2 local);

            return local;
        }
    }

}