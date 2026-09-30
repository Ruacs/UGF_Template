using System;
using System.Collections;
using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// HexaAway 道具表现播放器。兼容动画事件，也能在没有命中事件时用结束事件兜底。
    /// </summary>
    public sealed class HexaAwayPropAnimationPlayer : MonoBehaviour
    {
        private static readonly int HitStateHash = Animator.StringToHash("Hit");
        private static readonly int DefaultStateHash = Animator.StringToHash("Default");

        [SerializeField] private Animator m_Animator;
        [SerializeField] private ParticleSystem[] m_Particles;
        [SerializeField] private float m_FallbackDuration = 1.2f;
        [SerializeField] private float m_HideDelayAfterHit = 0.6f;

        private Action m_OnHit;
        private Action m_OnComplete;
        private Coroutine m_PlayRoutine;
        private Coroutine m_HideRoutine;
        private bool m_HitInvoked;

        private void Awake()
        {
            AutoBindReferences();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 在指定世界坐标播放一次 Hit 动画。动画事件会回调 OnDrillingStarted 或 OnHitAnimationEnded。
        /// </summary>
        public void Play(Vector3 worldPosition, Action onHit, Action onComplete)
        {
            Stop();

            transform.position = worldPosition;
            gameObject.SetActive(true);

            m_OnHit = onHit;
            m_OnComplete = onComplete;
            m_HitInvoked = false;

            StopParticles();

            if (m_Animator != null)
            {
                m_Animator.Play(HitStateHash, 0, 0f);
                m_Animator.Update(0f);
            }

            m_PlayRoutine = StartCoroutine(FallbackCompleteRoutine());
        }

        public void Stop()
        {
            if (m_PlayRoutine != null)
            {
                StopCoroutine(m_PlayRoutine);
                m_PlayRoutine = null;
            }

            if (m_HideRoutine != null)
            {
                StopCoroutine(m_HideRoutine);
                m_HideRoutine = null;
            }

            m_OnHit = null;
            m_OnComplete = null;
            m_HitInvoked = false;
            StopParticles();
        }

        /// <summary>
        /// Drill 动画的命中事件。方法名需要和 AnimationClip 事件保持一致。
        /// </summary>
        public void OnDrillingStarted()
        {
            InvokeHit();
            PlayParticles();
        }

        /// <summary>
        /// Hammer、Drill、TNT 动画结束事件。没有独立命中事件时，会在这里执行命中逻辑。
        /// </summary>
        public void OnHitAnimationEnded()
        {
            InvokeHit();
            PlayParticles();
            Complete();
        }

        private IEnumerator FallbackCompleteRoutine()
        {
            yield return new WaitForSeconds(m_FallbackDuration);
            InvokeHit();
            PlayParticles();
            Complete();
        }

        private void InvokeHit()
        {
            if (m_HitInvoked)
            {
                return;
            }

            m_HitInvoked = true;
            m_OnHit?.Invoke();
        }

        private void Complete()
        {
            if (m_PlayRoutine != null)
            {
                StopCoroutine(m_PlayRoutine);
                m_PlayRoutine = null;
            }

            m_OnComplete?.Invoke();
            m_OnHit = null;
            m_OnComplete = null;

            // if (m_Animator != null)
            // {
            //     // m_Animator.Play(DefaultStateHash, 0, 0f);
            // }

            if (m_HideDelayAfterHit > 0f)
            {
                m_HideRoutine = StartCoroutine(HideAfterDelayRoutine());
                return;
            }

            gameObject.SetActive(false);
        }

        private IEnumerator HideAfterDelayRoutine()
        {
            yield return new WaitForSeconds(m_HideDelayAfterHit);
            m_HideRoutine = null;
            gameObject.SetActive(false);
        }

        private void PlayParticles()
        {
            if (m_Particles == null)
            {
                return;
            }

            for (int i = 0; i < m_Particles.Length; i++)
            {
                ParticleSystem particle = m_Particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.gameObject.SetActive(true);
                particle.Play(true);
            }
        }

        private void StopParticles()
        {
            if (m_Particles == null)
            {
                return;
            }

            for (int i = 0; i < m_Particles.Length; i++)
            {
                ParticleSystem particle = m_Particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void AutoBindReferences()
        {
            if (m_Animator == null)
            {
                m_Animator = GetComponentInChildren<Animator>(true);
            }

            if (m_Particles == null || m_Particles.Length == 0)
            {
                m_Particles = GetComponentsInChildren<ParticleSystem>(true);
            }

            if (m_Animator != null)
            {
                HexaAwayPropAnimationEventRelay relay = m_Animator.GetComponent<HexaAwayPropAnimationEventRelay>();
                if (relay == null)
                {
                    relay = m_Animator.gameObject.AddComponent<HexaAwayPropAnimationEventRelay>();
                }

                relay.Init(this);
            }
        }
    }
}
