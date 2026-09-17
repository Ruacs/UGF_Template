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

        public IReadOnlyList<RewardEntry> rewardDatas
        {
            get;
            private set;
        }

        public static OnClaimRewardsEventArgs Create(int chestIndex, IReadOnlyList<RewardEntry> rewardDatas)
        {
            OnClaimRewardsEventArgs e = ReferencePool.Acquire<OnClaimRewardsEventArgs>();
            e.rewardDatas = new List<RewardEntry>(rewardDatas ?? System.Array.Empty<RewardEntry>()).AsReadOnly();
            e.ChestIndex = chestIndex;
            return e;
        }

        public override void Clear()
        {
            rewardDatas = null;
            ChestIndex = 0;
        }
    }

}
