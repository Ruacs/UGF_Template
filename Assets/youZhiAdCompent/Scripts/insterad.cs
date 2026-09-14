using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YzAdComponent
{
    public class insterad : MonoBehaviour
    {
        public int pos;
        private void OnEnable()
        {
            YzUtils.adManager.showIntersititialAd(pos);
        }

        public void OnDisable()
        {
        }
    }
}