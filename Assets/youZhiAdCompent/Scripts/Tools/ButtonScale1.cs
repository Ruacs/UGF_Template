using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YzAdComponent
{
    public class Scroll_V : MonoBehaviour
    {
        [Header("自动调整高度设置")]
        public bool autoResize = true;           // 是否自动调整高度
        public float topPadding = 0f;            // 顶部间距
        public float bottomPadding = 0f;         // 底部间距
        public float spacing = 0f;               // 子对象之间的间距
        public bool updateOnStart = true;        // 启动时是否更新
        
        private RectTransform rectTransform;
        private RectTransform contentRectTransform;

        void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            
            if (updateOnStart)
            {
                Invoke(nameof(RefreshSize), 0.1f); // 延迟执行以确保子对象已初始化
            }
        }

        void OnEnable()
        {
            if (autoResize && updateOnStart)
            {
                // 注册子对象变化事件
                StartCoroutine(DelayedRefresh());
            }
        }

        private void OnValidate()
        {
            if (autoResize && Application.isPlaying)
            {
                RefreshSize();
            }
        }

        /// <summary>
        /// 刷新容器大小以适应子对象
        /// </summary>
        public void RefreshSize()
        {
            if (!autoResize) return;
            
            float totalHeight = CalculateChildrenHeight();
            
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
                
            // 设置新的高度
            Vector2 newSize = rectTransform.sizeDelta;
            newSize.y = totalHeight;
            rectTransform.sizeDelta = newSize;
        }

        /// <summary>
        /// 计算所有子对象的总高度
        /// </summary>
        /// <returns>子对象总高度</returns>
        private float CalculateChildrenHeight()
        {
            float totalHeight = topPadding + bottomPadding;
            
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                
                // 检查子对象是否激活
                if (child.gameObject.activeInHierarchy)
                {
                    RectTransform childRect = child.GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        // 添加子对象的高度
                        totalHeight += childRect.sizeDelta.y;
                        
                        // 如果不是最后一个元素，添加间距
                        if (i < transform.childCount - 1)
                        {
                            totalHeight += spacing;
                        }
                    }
                }
            }
            
            return totalHeight;
        }

        /// <summary>
        /// 当子对象发生变化时调用
        /// </summary>
        private System.Collections.IEnumerator DelayedRefresh()
        {
            yield return new WaitForEndOfFrame(); // 等待当前帧结束
            RefreshSize();
        }

        /// <summary>
        /// 强制刷新大小（在运行时添加或删除子对象后调用）
        /// </summary>
        public void ForceRefresh()
        {
            RefreshSize();
        }

        // 监听子对象变化
        protected void OnTransformChildrenChanged()
        {
            if (autoResize)
            {
                Invoke(nameof(RefreshSize), 0.05f); // 小延迟以确保所有变换完成
            }
        }
    }
}