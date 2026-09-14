using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardDataBase", menuName = "RewardData/DataBase")]
public class RewardDataBaseSO : ScriptableObject
{
    public List<RewardDataSO> TaskRewardDataList;

    public List<RewardDataSO> ChestRewardList;

    /// <summary>
    /// 每日首胜奖励
    /// </summary>
    public List<RewardDataSO> FirstWinRewardList;


    public List<RewardDataSO> RankRewardList;



}
