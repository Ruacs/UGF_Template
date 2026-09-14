using UnityEngine;


namespace YzAdComponent
{
    public class LanguageUtils : MonoBehaviour
    {


        [Header("英文"), Tooltip("海外节点")]
        public GameObject obj_en;

        [Header("中文"), Tooltip("中文节点")]
        public GameObject obj_zh;



        void Start()
        {
            if (YzUtils.curLanguage == "zh")
            {
                if (obj_en != null)
                    obj_en.SetActive(false);
                if (obj_zh != null)
                    obj_zh.SetActive(true);
            }
            else
            {
                if (obj_zh != null)
                    obj_zh.SetActive(false);
                if (obj_en != null)
                    obj_en.SetActive(true);
            }
        }



    }

}