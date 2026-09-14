using UnityEngine;


namespace YzAdComponent
{

    public enum ToolUtilsType
    {
        None,
        VideoIconStatus
    }

    class ToolUtils : MonoBehaviour
    {
        public ToolUtilsType type = ToolUtilsType.None;

        public GameObject target = null;

        void Awake()
        {
            // 操作目标
            if (target == null)
            {
                target = gameObject;
            }
            // 根据类型执行逻辑
            switch (type)
            {
                case ToolUtilsType.VideoIconStatus:
                    initVideoIconStatus();
                    break;
                default:
                    Debug.LogError("ServeToolType is None");
                    break;
            }
        }

        // 视频icon显示状态
        private void initVideoIconStatus()
        {
            // 0显示1隐藏
            if (YzUtils.getConfigIntValue("VideoIconStatus", 0) == 0)
            {
                this.target.SetActive(true);
            }
            else
            {
                this.target.SetActive(false);
            }
        }

    }

}
