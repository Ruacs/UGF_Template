using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleActiveController : MonoBehaviour
{
    [SerializeField] private Toggle m_Toggle;
    [SerializeField] private GameObject[] m_OnTargets;
    [SerializeField] private GameObject[] m_OffTargets;

    private void Awake()
    {
        if (m_Toggle == null)
            m_Toggle = GetComponent<Toggle>();

        if (m_Toggle != null)
            m_Toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnEnable()
    {
        RefreshState();
    }

    private void OnDestroy()
    {
        if (m_Toggle != null)
            m_Toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        SetTargetsActive(m_OnTargets, isOn);
        SetTargetsActive(m_OffTargets, !isOn);
    }

    public void RefreshState()
    {
        if (m_Toggle == null)
            m_Toggle = GetComponent<Toggle>();

        if (m_Toggle == null) return;

        OnToggleValueChanged(m_Toggle.isOn);
    }

    private static void SetTargetsActive(GameObject[] targets, bool active)
    {
        if (targets == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(active);
        }
    }
}
