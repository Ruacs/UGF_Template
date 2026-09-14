using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YzAdComponent
{
    public class bannerad : MonoBehaviour
    {
        public int pos;
        private void OnEnable()
        {
            YzUtils.adManager.showBannerAd(pos);
        }

        public void OnDisable()
        {
            YzUtils.adManager.hideBannerAd();
        }
    }
}