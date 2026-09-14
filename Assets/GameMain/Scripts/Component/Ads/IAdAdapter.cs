using System.Collections;
using UnityEngine;

namespace Ads
{
    public interface IAdAdapter 
    {
        void Init();
        void ShowBanner();
        void HideBanner();
        void ShowInterstitial();
        void ShowRewarded(RewardCallBack rewardCallBack);
        bool IsRewardedReady();
    }
}