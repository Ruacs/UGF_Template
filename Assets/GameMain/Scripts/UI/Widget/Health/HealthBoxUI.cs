using System.Collections;
using System.Collections.Generic;
using Lokas;
using UnityEngine;

public class HealthBoxUI : MonoBehaviour
{
    [SerializeField] private RectTransform _SelfRT;
    [SerializeField] private HeartItemUI[] m_heartUIs;

    private int m_CurrentHealth;

    public RectTransform GetRectTransform() => _SelfRT;

    private void Awake()
    {
        Reset();
    }

    public void Reduce(int value = 1)
    {
        if (value <= 0 || m_heartUIs == null || m_heartUIs.Length == 0)
        {
            return;
        }

        int targetHealth = Mathf.Max(0, m_CurrentHealth - value);
        SetHealth(targetHealth);
    }

    public void Recover(int value = 1)
    {
        if (value <= 0 || m_heartUIs == null || m_heartUIs.Length == 0)
        {
            return;
        }

        int targetHealth = Mathf.Min(m_heartUIs.Length, m_CurrentHealth + value);
        SetHealth(targetHealth);
    }

    public void Reset()
    {
        SetHealth(m_heartUIs == null ? 0 : m_heartUIs.Length);
    }

    public void SetHealthValue(int health, bool isBroken)
    {
        SetHealth(health, isBroken);
    }

    private void SetHealth(int health, bool isBroken = false)
    {
        if (m_heartUIs == null)
        {
            m_CurrentHealth = 0;
            return;
        }

        m_CurrentHealth = Mathf.Clamp(health, 0, m_heartUIs.Length);
        for (int i = 0; i < m_heartUIs.Length; i++)
        {
            HeartItemUI heartUI = m_heartUIs[i];
            if (heartUI == null)
            {
                continue;
            }

            if (i < m_CurrentHealth)
            {
                heartUI.PlayIdle();
            }
            else if (isBroken)
            {
                heartUI.PlayBroken();
            }
            else
            {
                heartUI.PlayBreak();

            }
        }
    }


    public void HideHeardById(int id)
    {
        m_heartUIs[id].PlayBroken();
    }

    public HeartItemUI GetHeartItemUIById(int id)
    {
        if (m_heartUIs == null || id < 0 || id >= m_heartUIs.Length)
        {
            return null;
        }

        return m_heartUIs[id];
    }

}
