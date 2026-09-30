using System;
using UnityEngine;

namespace Lokas.Activities.GalaxyChallenge.UI
{
    /// <summary>Prefab 中完整排版的一种步数模式；数组包含起点和每一个关卡平台。</summary>
    public sealed class GalaxyChallengeStepContainerView : MonoBehaviour
    {
        [SerializeField] private GalaxyChallengeStepView[] m_Steps = Array.Empty<GalaxyChallengeStepView>();

        public int Count => m_Steps?.Length ?? 0;

        public bool Supports(int stepCount) => Count == stepCount + 1;

        public GalaxyChallengeStepView GetStep(int progress)
        {
            if (m_Steps == null || m_Steps.Length == 0) return null;
            return m_Steps[Mathf.Clamp(progress, 0, m_Steps.Length - 1)];
        }

        public void Bind(GalaxyChallengeSnapshot snapshot, int displayedProgress)
        {
            if (snapshot == null || m_Steps == null) return;
            for (int index = 0; index < m_Steps.Length; index++)
            {
                GalaxyChallengeStepView step = m_Steps[index];
                if (step == null) continue;
                int people = index < snapshot.UserCountsInStep.Count ? snapshot.UserCountsInStep[index] : 0;
                step.Bind(index, index < displayedProgress, index == displayedProgress, people);
            }
        }
    }
}
