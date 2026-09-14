using DG.Tweening;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [System.Serializable]
    public class FlyItemUIData
    {
        public Sprite sprite;
        public Vector2 startPos;
        public Vector2 endPos;

        public float height = 200f;
        public float duration = 0.8f;
        public Ease ease = Ease.OutCubic;
    }

}