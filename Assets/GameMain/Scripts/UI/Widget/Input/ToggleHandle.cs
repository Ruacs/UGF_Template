using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToggleHandle : MonoBehaviour
{

    private const string SWITCH_TOGGLE_ON_ANIM_NAME = "ON";
    private const string SWITCH_TOGGLE_REVERSE_ANIM_NAME = "OFF";
    public RectTransform handle;
    public Toggle toggle;

    public bool hasAnimation;

    public Animator switchToggleAnim;

    [SerializeField] UnityEvent m_OnSelected;
    [SerializeField] UnityEvent m_OnUnselected;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    public void Init(bool isOn)
    {

        toggle.isOn = isOn;
        OnValueChanged(isOn);
    }


    public void OnValueChanged(bool isOn)
    {

        RefreshToggleState(isOn);
        // Debug.Log($"{name}  {(isOn ? "ON" : "OFF")}");
        if (!hasAnimation) return;
        if (!switchToggleAnim.isActiveAndEnabled || !switchToggleAnim.gameObject.activeInHierarchy)
        {
            return;
        }

        switchToggleAnim?.Play(isOn ? SWITCH_TOGGLE_ON_ANIM_NAME : SWITCH_TOGGLE_REVERSE_ANIM_NAME);
      

    }

    void RefreshToggleState(bool isOn)
    { 
        if (isOn)
        {
            m_OnSelected?.Invoke();
        }
        else
        {
            m_OnUnselected?.Invoke();
        }
    }

}
