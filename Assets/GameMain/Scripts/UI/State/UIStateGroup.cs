using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStateGroup : MonoBehaviour
{
    [SerializeField] private UIState defaultState;
    [SerializeField] private List<UIStateObject> stateObjects;

    private Dictionary<UIState, GameObject> m_StateMap;

    private void Awake()
    {
        m_StateMap = new Dictionary<UIState, GameObject>();

        foreach (var item in stateObjects)
        {
            if (item.target != null)
                m_StateMap[item.state] = item.target;
        }

        SetState(defaultState);
    }

    public void SetState(UIState state)
    {
        foreach (var kv in m_StateMap)
        {
            kv.Value.SetActive(kv.Key == state);
        }
    }
}
