using System.Collections;
using System.Collections.Generic;
using Lokas;
using UnityEngine;

public class HeartItemUI : MonoBehaviour
{
    private const string IdleStateName = "RedHeart_idle";
    private const string BreakStateName = "RedHeart_Break";
    private const string BrokenStatNAme = "RedHeart_Broken";
    private const string ReviveStateName = "RedHeart_Reset";

    [SerializeField] private Animator m_Animator;

    [SerializeField] private int m_IdleHash;
    [SerializeField] private int m_BreakHash;
    [SerializeField] private int m_BrokenHash;
    [SerializeField] private int m_ReviveHash;

    [SerializeField] private RectTransform m_IconRootRT;

    private bool m_isBreak;
    private bool m_HasPendingState;
    private int m_PendingStateHash;


    public RectTransform RootRT
    {
        get
        {
            return m_IconRootRT;
        }
    }

    private void Awake()
    {
        InitHash();
    }

    private void OnEnable()
    {
        if (m_HasPendingState)
        {
            Play(m_PendingStateHash);
        }
    }

    private void OnValidate()
    {
        InitHash();
    }

    public void PlayIdle()
    {
        InitHash();
        m_isBreak = false;
        Play(m_IdleHash);
    }

    public void PlayBreak()
    {
        InitHash();
        if (m_isBreak) return;

        m_isBreak = true;
        Play(m_BreakHash);
    }

    public void PlayBroken()
    {
        InitHash();
        if (m_isBreak) return;

        m_isBreak = true;
        Play(m_BrokenHash);
    }

    public void PlayRevive()
    {
        InitHash();
        m_isBreak = false;
        Play(m_ReviveHash);

        GameEntry.Sound.PlaySound(SoundId.SFX_correct);
    }

    private void InitHash()
    {
        if (m_IdleHash == 0)
        {
            m_IdleHash = Animator.StringToHash(IdleStateName);
        }

        if (m_BreakHash == 0)
        {
            m_BreakHash = Animator.StringToHash(BreakStateName);
        }

        if (m_BrokenHash == 0)
        {
            m_BrokenHash = Animator.StringToHash(BrokenStatNAme);
        }

        if(m_ReviveHash == 0)
        {
            m_ReviveHash = Animator.StringToHash(ReviveStateName);
        }
    }

    private void Play(int stateHash)
    {
        m_HasPendingState = true;
        m_PendingStateHash = stateHash;

        if (m_Animator == null)
        {
            return;
        }

        if (!m_Animator.isActiveAndEnabled || !m_Animator.gameObject.activeInHierarchy)
        {
            return;
        }

        m_Animator.Play(stateHash, 0, 0f);
    }


}
