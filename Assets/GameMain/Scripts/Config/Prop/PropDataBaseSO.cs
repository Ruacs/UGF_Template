using System;
using System.Collections;
using System.Collections.Generic;
using GF_Mahjong;
using Lokas;
using UnityEngine;

[CreateAssetMenu(fileName = "PropDataBase",menuName = "Prop/DataBase")]
public class PropDataBaseSO : ScriptableObject
{
    [SerializeField] private List<PropData> m_propDatas;
    private Dictionary<PropType, PropData> m_propDic;


    private void InitDic()
    {
        m_propDic = new Dictionary<PropType, PropData>();
        foreach (var item in m_propDatas)
        {
            m_propDic.Add(item.propType, item);
        }
    }


    public bool TryGetPropData(PropType type, out PropData propData)
    {
        if (m_propDic == null)
        {
            InitDic();
        }
        return m_propDic.TryGetValue(type, out propData);
    }
}

[Serializable]
public class PropData
{
    public PropType propType;
    public Sprite sprite;

    public Sprite sprite_big;
}