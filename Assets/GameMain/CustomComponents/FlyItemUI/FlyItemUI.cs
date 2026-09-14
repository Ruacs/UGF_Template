using DG.Tweening;
using GameFramework.ObjectPool;
using UnityEngine;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;

namespace UnityGameFramework.Runtime
{
    public class FlyItemUI : MonoBehaviour, IPoolable
    {
        public Image icon;

        [SerializeField] private RectTransform rect;
        private Tween tween;

        private Transform m_originParent;

        [SerializeField] private FlyItemUIData m_data;


        public void OnRecycle()
        {
            icon.sprite = null;
            if (rect != null)
            {
                rect.localScale = Vector3.zero;
                rect.anchoredPosition = Vector3.zero;
            }

            tween?.Kill();
            gameObject.SetActive(false);
        }

        public void OnSpawn()
        {
            rect ??= GetComponent<RectTransform>();

            rect.localScale = Vector3.zero;
        }

        public void Play(FlyItemUIData flyItemUIData, System.Action onComplete)
        {
            m_data = flyItemUIData;
            icon.sprite = flyItemUIData.sprite;

            rect.anchoredPosition = flyItemUIData.startPos;
            rect.localScale = Vector3.zero;
            gameObject.SetActive(true);

            tween?.Kill();

            Vector2 startPos = m_data.startPos;
            Vector2 endPos = m_data.endPos;

            // 生成控制点
            Vector2 control = GetControlPoint(startPos, endPos);

            float duration = m_data.duration;
            float t = 0;

            Sequence seq = DOTween.Sequence();

            // 曲线动画
            seq.Append(
                DOTween.To(() => t, x => t = x, 1f, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    rect.anchoredPosition = CalculateBezierPoint(t, startPos, control, endPos);
                })
            );

            // scale 动画
            seq.Join(rect.DOScale(1.4f, duration * 0.3f).SetEase(Ease.OutBack));
            seq.Append(rect.DOScale(1f, duration * 0.2f));

            tween = seq.OnComplete(() => onComplete?.Invoke());
        }





        /// <summary>
        /// 计算二阶贝塞尔点
        /// </summary>
        private Vector2 CalculateBezierPoint(
            float t,
            Vector2 p0,
            Vector2 p1,
            Vector2 p2)
        {
            float u = 1 - t;
            return u * u * p0 +
                2 * u * t * p1 +
                t * t * p2;
        }
        /// <summary>
        /// 自动生成控制点（竖屏适配版）
        /// </summary>
        private Vector2 GetControlPoint(Vector2 start, Vector2 end)
        {
            Vector2 dir = (end - start).normalized;

            // 垂直方向（法线）
            Vector2 normal = new Vector2(-dir.y, dir.x);

            // 判断左右
            float side = -Mathf.Sign(end.x - start.x);

            // 水平距离决定弧度
            float horizontalDistance = Mathf.Abs(end.x - start.x);
            float curveStrength = Mathf.Clamp(horizontalDistance, 120f, 350f);

            return (start + end) * 0.5f + normal * curveStrength * side;
        }



    }

}