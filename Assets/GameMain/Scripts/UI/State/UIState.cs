using System.Collections;
using UnityEngine;

public enum UIState
{
    Normal,        // 可点击
    Disabled,      // 不可点击
    Locked,        // 未解锁
    Highlighted,   // 高亮 / 推荐
    Selected       // 已选择
}