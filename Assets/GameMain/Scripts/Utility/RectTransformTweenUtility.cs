using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Lokas
{
    public static class RectTransformTweenUtility
    {
        public static IEnumerator TweenAnchoredPositions(
            IReadOnlyList<RectTransform> rectTransforms,
            IReadOnlyList<Vector2> targetPositions,
            float duration,
            Ease ease = Ease.OutCubic)
        {
            if (rectTransforms == null || targetPositions == null || rectTransforms.Count != targetPositions.Count)
                yield break;

            Sequence sequence = DOTween.Sequence();
            bool hasTween = false;

            for (int index = 0; index < rectTransforms.Count; index++)
            {
                RectTransform rectTransform = rectTransforms[index];
                if (rectTransform == null)
                    continue;

                rectTransform.DOKill(false);
                sequence.Join(rectTransform.DOAnchorPos(targetPositions[index], duration).SetEase(ease));
                hasTween = true;
            }

            if (!hasTween)
                yield break;

            yield return sequence.WaitForCompletion();
        }

        public static IEnumerator PlayImpactMerge(
            RectTransform first,
            RectTransform second,
            float pullBackDistance,
            float pullBackDuration,
            float mergeDuration,
            float shrinkDuration,
            float squeezeScale = 0.82f)
        {
            if (first == null || second == null)
                yield break;

            first.DOKill(false);
            second.DOKill(false);

            Vector2 firstStart = first.anchoredPosition;
            Vector2 secondStart = second.anchoredPosition;
            Vector3 firstScale = first.localScale;
            Vector3 secondScale = second.localScale;

            Vector2 delta = secondStart - firstStart;
            Vector2 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
            Vector2 midpoint = (firstStart + secondStart) * 0.5f;
            Vector2 firstPullBack = firstStart - direction * pullBackDistance;
            Vector2 secondPullBack = secondStart + direction * pullBackDistance;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(first.DOAnchorPos(firstPullBack, pullBackDuration).SetEase(Ease.OutSine));
            sequence.Join(second.DOAnchorPos(secondPullBack, pullBackDuration).SetEase(Ease.OutSine));
            sequence.Join(first.DOScale(firstScale * 1.05f, pullBackDuration).SetEase(Ease.OutQuad));
            sequence.Join(second.DOScale(secondScale * 1.05f, pullBackDuration).SetEase(Ease.OutQuad));

            sequence.Append(first.DOAnchorPos(midpoint, mergeDuration).SetEase(Ease.InCubic));
            sequence.Join(second.DOAnchorPos(midpoint, mergeDuration).SetEase(Ease.InCubic));
            sequence.Join(first.DOScale(firstScale * squeezeScale, mergeDuration).SetEase(Ease.InQuad));
            sequence.Join(second.DOScale(secondScale * squeezeScale, mergeDuration).SetEase(Ease.InQuad));

            sequence.Append(first.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InBack));
            sequence.Join(second.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InBack));

            yield return sequence.WaitForCompletion();
        }
    }
}
