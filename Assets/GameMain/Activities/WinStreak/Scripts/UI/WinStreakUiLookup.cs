using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lokas.Activities.WinStreak.UI
{
    internal static class WinStreakUiLookup
    {
        public static T Component<T>(Transform root, string path) where T : Component
        {
            Transform target = root == null ? null : root.Find(path);
            return target == null ? null : target.GetComponent<T>();
        }

        public static TMP_Text Text(Transform root, string path) => Component<TMP_Text>(root, path);
        public static Button Button(Transform root, string path) => Component<Button>(root, path);
        public static Image Image(Transform root, string path) => Component<Image>(root, path);
        public static GameObject Object(Transform root, string path)
        {
            Transform target = root == null ? null : root.Find(path);
            return target == null ? null : target.gameObject;
        }
    }
}
