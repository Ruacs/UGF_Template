#if DOTWEEN
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DOTweenSequenceAnimator : MonoBehaviour
{
    [SerializeField] private SequenceState[] m_States = null;
    [SerializeField] private bool m_PlayOnAwake = false;
    [SerializeField] private string m_DefaultStateName = string.Empty;
    [SerializeField] private PlayConflictMode m_PlayConflictMode = PlayConflictMode.KillCurrent;
    [SerializeField] private StateEvent m_OnPlay = null;

    private readonly Dictionary<string, DOTweenSequence> m_StateMap = new Dictionary<string, DOTweenSequence>(StringComparer.Ordinal);
    private string m_CurrentStateName = string.Empty;
    private DOTweenSequence m_CurrentSequence = null;
    private Tween m_CurrentTween = null;

    public string CurrentStateName => m_CurrentStateName;
    public DOTweenSequence CurrentSequence => m_CurrentSequence;
    public Tween CurrentTween => m_CurrentTween;

    private void Awake()
    {
        RebuildStateMap();

        if (m_PlayOnAwake && !string.IsNullOrEmpty(m_DefaultStateName))
        {
            Play(m_DefaultStateName);
        }
    }

    private void OnValidate()
    {
        if (m_States == null) return;

        for (int i = 0; i < m_States.Length; i++)
        {
            if (m_States[i] == null) continue;
            m_States[i].TrimName();
        }
    }

    public bool HasState(string stateName)
    {
        EnsureStateMap();
        return !string.IsNullOrEmpty(stateName) && m_StateMap.ContainsKey(stateName);
    }

    public DOTweenSequence GetSequence(string stateName)
    {
        EnsureStateMap();

        DOTweenSequence sequence;
        return !string.IsNullOrEmpty(stateName) && m_StateMap.TryGetValue(stateName, out sequence) ? sequence : null;
    }

    public void Play()
    {
        Play(m_DefaultStateName);
    }

    public Tween Play(string stateName)
    {
        return DOPlay(stateName);
    }

    public Tween DOPlay(string stateName)
    {
        var sequence = GetSequenceOrLog(stateName);
        if (sequence == null) return null;

        PrepareForPlay();
        m_CurrentStateName = stateName;
        m_CurrentSequence = sequence;
        m_CurrentTween = sequence.DOPlay();
        m_OnPlay?.Invoke(stateName);
        return m_CurrentTween;
    }

    public Tween Rewind(string stateName)
    {
        return DORewind(stateName);
    }

    public Tween DORewind(string stateName)
    {
        var sequence = GetSequenceOrLog(stateName);
        if (sequence == null) return null;

        PrepareForPlay();
        m_CurrentStateName = stateName;
        m_CurrentSequence = sequence;
        m_CurrentTween = sequence.DORewind();
        m_OnPlay?.Invoke(stateName);
        return m_CurrentTween;
    }

    public void Complete(bool withCallback = false)
    {
        m_CurrentSequence?.DOComplete(withCallback);
    }

    public void Complete(string stateName, bool withCallback = false)
    {
        GetSequenceOrLog(stateName)?.DOComplete(withCallback);
    }

    public void Kill()
    {
        m_CurrentSequence?.DOKill();
        ClearCurrent();
    }

    public void Kill(string stateName)
    {
        var sequence = GetSequenceOrLog(stateName);
        if (sequence == null) return;

        sequence.DOKill();
        if (sequence == m_CurrentSequence)
        {
            ClearCurrent();
        }
    }

    public void KillAll()
    {
        EnsureStateMap();

        foreach (var sequence in m_StateMap.Values)
        {
            sequence?.DOKill();
        }

        ClearCurrent();
    }

    public void RebuildStateMap()
    {
        m_StateMap.Clear();

        if (m_States == null) return;

        for (int i = 0; i < m_States.Length; i++)
        {
            var state = m_States[i];
            if (state == null || string.IsNullOrEmpty(state.Name) || state.Sequence == null) continue;

            if (m_StateMap.ContainsKey(state.Name))
            {
                Debug.LogWarningFormat(this, "Duplicate DOTweenSequence state name: {0}", state.Name);
                continue;
            }

            m_StateMap.Add(state.Name, state.Sequence);
        }
    }

    private void EnsureStateMap()
    {
        if (m_StateMap.Count == 0)
        {
            RebuildStateMap();
        }
    }

    private DOTweenSequence GetSequenceOrLog(string stateName)
    {
        var sequence = GetSequence(stateName);
        if (sequence != null) return sequence;

        Debug.LogWarningFormat(this, "DOTweenSequence state not found: {0}", stateName);
        return null;
    }

    private void PrepareForPlay()
    {
        switch (m_PlayConflictMode)
        {
            case PlayConflictMode.None:
                break;
            case PlayConflictMode.KillCurrent:
                if (m_CurrentSequence != null)
                {
                    m_CurrentSequence.DOKill();
                }
                break;
            case PlayConflictMode.KillAll:
                KillAll();
                break;
        }
    }

    private void ClearCurrent()
    {
        m_CurrentStateName = string.Empty;
        m_CurrentSequence = null;
        m_CurrentTween = null;
    }

    [Serializable]
    public class SequenceState
    {
        [SerializeField] private string m_Name = string.Empty;
        [SerializeField] private DOTweenSequence m_Sequence = null;

        public string Name => m_Name;
        public DOTweenSequence Sequence => m_Sequence;

        public void TrimName()
        {
            if (m_Name != null)
            {
                m_Name = m_Name.Trim();
            }
        }
    }

    public enum PlayConflictMode
    {
        None,
        KillCurrent,
        KillAll
    }

    [Serializable]
    public class StateEvent : UnityEvent<string>
    {
    }
}
#endif
