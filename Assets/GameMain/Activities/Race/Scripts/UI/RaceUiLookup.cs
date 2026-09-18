using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas.Activities.Race.UI
{
    /// <summary>页面脚本通过分析文档中的稳定节点名读取 Prefab，不在运行时拼装 UI。</summary>
    internal static class RaceUiLookup
    {
        public static TMP_Text Text(Transform root, string path) => Find<TMP_Text>(root, path);
        public static Button Button(Transform root, string path) => Find<Button>(root, path);
        public static Image Image(Transform root, string path) => Find<Image>(root, path);
        public static T Component<T>(Transform root, string path) where T : Component => Find<T>(root, path);

        public static GameObject Object(Transform root, string path)
        {
            Transform target = root.Find(path);
            return target == null ? null : target.gameObject;
        }

        private static T Find<T>(Transform root, string path) where T : Component
        {
            Transform target = root.Find(path);
            return target == null ? null : target.GetComponent<T>();
        }
    }
}
