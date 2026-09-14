using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// 所有 Item Prefab 的基类。
    /// Prefab 中的 UI 结构在编辑器里搭好，脚本只做数据绑定。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class TestModeItemBase : MonoBehaviour { }
}
