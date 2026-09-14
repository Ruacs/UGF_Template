using System;
using UnityEngine;



namespace YzAdComponent
{

    /// <summary>
    /// 节点配置，用于创建组件内节点 
    /// parent:父节点，color：颜色， left:左侧对齐，居父节点左边距离，right:右侧对齐，居父节点右边距离，top:顶部对齐，居父节点顶部距离，bottom：底部对齐
    /// </summary>
    public class YzAdParame
    {

        public YzAdParame() { }


        /// <summary>
        /// 位置
        /// </summary>
        public int location;

        /// <summary>
        /// 是否定时器刷新
        /// </summary>
        public bool isTimeRefresh;

        /// <summary>
        /// 父lei
        /// </summary>
        public Transform parent;

        /// <summary>
        /// 距离父节点左边距离
        /// </summary>
        public float left = -1;
        /// <summary>
        /// 距离父节点上边距离
        /// </summary>
        public float top = -1;
        /// <summary>
        /// 距离父节点右边距离
        /// </summary>
        public float right = -1;
        /// <summary>
        /// 距离父节点下边距离
        /// </summary>
        public float bottom = -1;

        /// <summary>
        /// 宽度
        /// </summary>
        public float width = -1;
        /// <summary>
        /// 高度
        /// </summary>
        public float height = -1;

        /// <summary>
        /// 缩放值
        /// </summary>
        public float scale = 1;

        /// <summary>
        /// 屏幕的宽度
        /// </summary>
        public int winSizeWidth = Screen.width;

        /// <summary>
        /// 屏幕的高度
        /// </summary>
        public int winSizeHeight = Screen.height;


        /// <summary>
        /// 文本语言
        /// </summary>
        public string language = "zh";

        /// <summary>
        /// 颜色
        /// </summary>
        public Color color = Color.black;

        public YzAdParame(int location, bool isTimeRefresh = false)
        {
            this.location = location;
            this.isTimeRefresh = isTimeRefresh;
        }

        /// <summary>
        /// 节点配置，用于创建组件内节点 
        /// parent:父节点， left:左侧对齐，居父节点左边距离，right:右侧对齐，居父节点右边距离，top:顶部对齐，居父节点顶部距离，bottom：底部对齐
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="left">left:左侧对齐，居父节点左边距离，，，bottom：</param>
        /// <param name="top">顶部对齐，居父节点顶部距离 </param>
        /// <param name="right">右侧对齐，居父节点右边距离</param>
        /// <param name="bottom">底部对齐，居父节点底部的距离</param>
        /// <param name="scale">缩放值</param>
        public YzAdParame(Transform parent, float left = -1, float top = 1, float right = 1, float bottom = -1)
        {
            this.parent = parent;
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }


        /// <summary>
        /// 节点配置，用于创建组件内节点 
        /// parent:父节点， left:左侧对齐，居父节点左边距离，right:右侧对齐，居父节点右边距离，top:顶部对齐，居父节点顶部距离，bottom：底部对齐
        /// scale:缩放值
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="left">left:左侧对齐，居父节点左边距离，，，bottom：</param>
        /// <param name="top">顶部对齐，居父节点顶部距离 </param>
        /// <param name="right">右侧对齐，居父节点右边距离</param>
        /// <param name="bottom">底部对齐，居父节点底部的距离</param>
        /// <param name="scale">缩放值</param>
        public YzAdParame(Transform parent, float left = -1, float top = 1, float right = 1, float bottom = -1, float scale = 1)
        {
            this.parent = parent;
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.scale = scale;
        }

        /// <summary>
        /// 节点配置，用于创建组件内节点 
        /// parent:父节点，color：颜色， left:左侧对齐，居父节点左边距离，right:右侧对齐，居父节点右边距离，top:顶部对齐，居父节点顶部距离，bottom：底部对齐
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="left">left:左侧对齐，居父节点左边距离，，，bottom：</param>
        /// <param name="top">顶部对齐，居父节点顶部距离 </param>
        /// <param name="right">右侧对齐，居父节点右边距离</param>
        /// <param name="bottom">底部对齐，居父节点底部的距离</param>
        /// <param name="scale">缩放值</param>
        /// <param name="color">颜色</param>
        //public YzAdParame(Transform parent, Color color, float left = -1, float top = 1, float right = 1, float bottom = -1, float scale = 1)
        //{
        //    this.parent = parent;
        //    this.left = left;
        //    this.top = top;
        //    this.right = right;
        //    this.bottom = bottom;
        //    this.scale = scale;
        //    this.color = color;
        //}


        public override string ToString()
        {
            return "adParame: #location=" + location + "  #parent=" + parent + " #left=" + left + " #top=" + top + " #right=" + right + " #bottom=" + bottom + " #scale=" + scale;
        }
    }

}