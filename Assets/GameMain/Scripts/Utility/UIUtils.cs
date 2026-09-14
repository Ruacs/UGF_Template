using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FVLG
{
    public static class UIUtils 
    {

        /// <summary>
        /// 判断鼠标是否悬停在 UI 元素上
        /// </summary>
        /// <returns></returns>
        public static bool IsPointerOverUI()
        {
            // 如果没有 EventSystem 或没有鼠标点击，直接返回 false
            if (EventSystem.current == null || !Input.GetMouseButton(0))
                return false;

            // 创建 GraphicRaycaster 检测
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            // 检查所有 Canvas
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            // 如果检测到 UI 元素，返回 true
            return results.Count > 0;
        }

       
    }
}