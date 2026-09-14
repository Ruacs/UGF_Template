using GameFramework;
using GameFramework.Event;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lokas
{
    public sealed class OnClaimRewardsEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(OnClaimRewardsEventArgs).GetHashCode();
        public override int Id
        {
            get
            {
                return EventId;
            }
        }

        public int ChestIndex
        {
            get;
            private set;
        }

        public List<RewardData> rewardDatas
        {
            get;
            private set;
        }

        public static OnClaimRewardsEventArgs Create(int chestIndex,List<RewardData> rewardDatas)
        {
            OnClaimRewardsEventArgs e = ReferencePool.Acquire<OnClaimRewardsEventArgs>();
            e.rewardDatas = rewardDatas;
            e.ChestIndex = chestIndex;
            return e;
        }

        public override void Clear()
        {
            rewardDatas = null;
        }
    }

}