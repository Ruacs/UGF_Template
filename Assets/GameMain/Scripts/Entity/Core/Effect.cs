using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{

    public class Effect : Entity
    {
        ParticleSystem particle;
        [SerializeField]
        protected EffectData m_EffectData;

        protected float m_KeepTimer = 0f;
        protected bool m_canHide = false;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_EffectData = (EffectData)userData;
            m_KeepTimer = 0;
            m_canHide = false;

            transform.localScale = m_EffectData.Scale;

            if (m_EffectData.Parent != null)
            {
                transform.parent = m_EffectData.Parent;
            }

            particle = GetComponent<ParticleSystem>();
            particle.Play();

        }
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            m_KeepTimer += elapseSeconds;
            if (m_KeepTimer > m_EffectData.KeepTime && !m_canHide)
            {
                m_canHide = true;
                GameEntry.Entity.HideEntity(this);
            }

        }
    }

}