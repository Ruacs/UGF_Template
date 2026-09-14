using System;
using System.Collections;
using System.Collections.Generic;
using ConfigSO;
using GF_Mahjong;
using Lokas;
using UnityEngine;
[CreateAssetMenu(fileName = "RewardDataSO", menuName = "RewardData/RewardData")]
public class RewardDataSO : IdOnlyConfigSO
{
    public List<RewardData> rewardDatas;
}

[Serializable]
public class RewardData
{
    public PropType propType;
    public int Count;

    public RewardData(PropType propType, int count)
    {
        this.propType = propType;
        this.Count = count;
    }

 
}



