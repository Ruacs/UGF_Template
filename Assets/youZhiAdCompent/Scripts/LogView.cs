using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YzAdComponent
{
    /// <summary>
    /// 日志面板视图，支持日志追加、显示/隐藏、清空、自动滚动等功能。
    /// </summary>
    public class LogView : MonoBehaviour
    {
        // 显示日志面板的按钮
        [SerializeField] GameObject BtnShow;
        // 隐藏日志面板的按钮
        [SerializeField] GameObject BtnHide;
        // 清空日志的按钮
        [SerializeField] GameObject BtnClear;
        // 日志滚动视图组件
        [SerializeField] ScrollRect scrollView;
        // 日志内容父节点（通常挂载VerticalLayoutGroup）
        [SerializeField] GameObject content;
        // 日志条目预制体（Text组件，需在Inspector中设置为未激活模板）
        [SerializeField] Text textItem;


        // 当前所有日志条目的GameObject列表，便于清空
        private List<GameObject> logItems = new List<GameObject>();

        private List<string> pendingLogs = new List<string>();

        void Awake()
        {
            BtnShow.GetComponent<Button>().onClick.AddListener(onBtnShow);
            BtnHide.GetComponent<Button>().onClick.AddListener(onBtnHide);
            BtnClear.GetComponent<Button>().onClick.AddListener(onBtnClear);
            gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            YzUtils.logOutView = null;
        }

        // 显示日志面板
        void onBtnShow()
        {
            scrollView.gameObject.SetActive(true);
            // 显示所有缓存的日志
            if (pendingLogs.Count > 0)
            {
                foreach (var log in pendingLogs)
                {
                    addLog(log);
                }
                pendingLogs.Clear();
            }
        }

        // 隐藏日志面板
        void onBtnHide()
        {
            scrollView.gameObject.SetActive(false);
        }

        // 清空所有日志条目
        void onBtnClear()
        {
            // 清除面板上的日志条目
            foreach (var item in logItems) Destroy(item);

            logItems.Clear();
            pendingLogs.Clear();
        }

        // 实际添加日志条目到面板
        private void addLog(string str)
        {
            if (textItem == null || content == null) return;
            GameObject newText = Instantiate(textItem.gameObject, content.transform);
            // 适配字体宽度
            var rect = newText.GetComponent<RectTransform>().sizeDelta;
            newText.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width - 80, rect.y);

            var txt = newText.GetComponent<Text>();
            if (txt != null) txt.text = str;
            logItems.Add(newText);
            newText.SetActive(true);
            Canvas.ForceUpdateCanvases();
            scrollView.verticalNormalizedPosition = 0f;
        }

        // 外部调用用于添加日志
        public void showLog(string str)
        {
            if (scrollView.gameObject.activeSelf)
                addLog(str);
            else
                pendingLogs.Add(str);

        }

    }
}