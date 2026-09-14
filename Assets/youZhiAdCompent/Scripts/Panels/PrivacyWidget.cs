using UnityEngine;


namespace YzAdComponent
{
    public class PrivacyWidget : MonoBehaviour
    {
        public void OnBtnClickListener()
        {
            if (PlatUtils.isAndroid)
            {
                YzUtils.yzTool.jumpPravicy();
            }
        }
    }

}