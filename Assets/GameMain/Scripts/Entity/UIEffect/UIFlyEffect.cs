using DG.Tweening;
using UnityEngine;

namespace Lokas
{
    public enum FlyMoveMode
    {
        Curve,
        Straight,
    }

    public enum CurveDirection
    {
        Up,
        Down,
    }

    /// <summary>
    /// Moves a UI effect from a start screen position to a target UI/screen position.
    /// Visual components such as UIParticle or TrailRenderer should live on the moved node
    /// or its children and manage their own playback.
    /// </summary>
    public class UIFlyEffect : Entity
    {
        private const string DefaultMoveRootPath = "Emitter";
        private const float DefaultMoveDuration = 0.3f;
        private const float DefaultScaleInRatio = 0.3f;
        private const float DefaultMoveDirectionAngleOffset = -90f;
        private const float DefaultCurveStrengthMin = 120f;
        private const float DefaultCurveStrengthMax = 350f;

        private UIFlyEffectData m_Data;
        private UIFlyEffectConfig _config;
        private RectTransform _moveRect;
        private Transform _originalParent;
        private Sequence _seq;

        protected float m_KeepTime;
        protected bool m_canHide;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_Data = (UIFlyEffectData)userData;
            _config = GetComponentInChildren<UIFlyEffectConfig>(true);
            _moveRect = ResolveMoveRoot();
            _originalParent = transform.parent;
            m_KeepTime = 0f;
            m_canHide = false;

            if (m_Data.Parent != null)
            {
                transform.SetParent(m_Data.Parent, false);
            }

            FlyMoveMode moveMode = m_Data.MoveMode ?? _config?.MoveMode ?? FlyMoveMode.Curve;
            bool alignToMoveDirection = m_Data.AlignToMoveDirection ?? _config?.AlignToMoveDirection ?? true;
            float moveDirectionAngleOffset = m_Data.MoveDirectionAngleOffset ?? _config?.MoveDirectionAngleOffset ?? DefaultMoveDirectionAngleOffset;
            CurveDirection curveDirection = m_Data.CurveDirection ?? _config?.CurveDirection ?? CurveDirection.Up;
            float curveStrengthMin = m_Data.CurveStrengthMin ?? _config?.CurveStrengthMin ?? DefaultCurveStrengthMin;
            float curveStrengthMax = m_Data.CurveStrengthMax ?? _config?.CurveStrengthMax ?? DefaultCurveStrengthMax;
            Ease scaleInEase = _config != null ? _config.ScaleInEase : Ease.OutBack;
            float scaleInRatio = _config != null ? _config.ScaleInRatio : DefaultScaleInRatio;

            Vector2 startLocal = ScreenToLocal(m_Data.SpawnScreenPos);
            Vector2 endLocal = GetTargetLocal();

            _moveRect.anchoredPosition = startLocal;
            _moveRect.localScale = Vector3.one;
            _moveRect.localRotation = Quaternion.identity;

            float t = 0f;
            float dur = m_Data.MoveDuration ?? _config?.MoveDuration ?? DefaultMoveDuration;

            _seq = DOTween.Sequence();

            if (moveMode == FlyMoveMode.Curve)
            {
                Vector2 control = GetControlPoint(startLocal, endLocal, curveDirection, curveStrengthMin, curveStrengthMax);
                _seq.Append(
                    DOTween.To(() => t, x =>
                    {
                        t = x;
                        _moveRect.anchoredPosition = BezierPoint(t, startLocal, control, endLocal);
                        AlignMoveRootToDirection(BezierTangent(t, startLocal, control, endLocal), alignToMoveDirection, moveDirectionAngleOffset);
                    }, 1f, dur)
                    .SetEase(Ease.Linear)
                );
            }
            else
            {
                AlignMoveRootToDirection(endLocal - startLocal, alignToMoveDirection, moveDirectionAngleOffset);
                _seq.Append(_moveRect.DOAnchorPos(endLocal, dur).SetEase(Ease.Linear));
            }

            _seq.Join(_moveRect.DOScale(1.4f, dur * scaleInRatio).SetEase(scaleInEase));
            _seq.Append(_moveRect.DOScale(1f, dur * 0.2f));

            _seq.OnComplete(() =>
            {
                m_Data.OnAllCompleted?.Invoke();
            });
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (m_Data == null)
            {
                return;
            }

            m_KeepTime += elapseSeconds;
            if (!m_canHide && m_KeepTime >= Mathf.Max(0f, m_Data.KeepTime))
            {
                m_canHide = true;
                GameEntry.Entity.HideEntity(this);
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);

            _seq?.Kill();
            _seq = null;

            if (_moveRect != null)
            {
                _moveRect.localRotation = Quaternion.identity;
            }

            transform.SetParent(_originalParent);
            _config = null;
            m_Data = null;
        }

        private RectTransform ResolveMoveRoot()
        {
            if (_config != null && _config.TryGetComponent(out RectTransform configRect))
            {
                return configRect;
            }

            UIFlyEffectMoveRoot marker = GetComponentInChildren<UIFlyEffectMoveRoot>(true);
            if (marker != null && marker.TryGetComponent(out RectTransform markerRect))
            {
                return markerRect;
            }

            Transform moveRoot = FindMoveRootTransform(m_Data.MoveRootPath);
            if (moveRoot != null && moveRoot.TryGetComponent(out RectTransform moveRect))
            {
                return moveRect;
            }

            return GetComponent<RectTransform>();
        }

        private Transform FindMoveRootTransform(string moveRootPath)
        {
            string path = string.IsNullOrEmpty(moveRootPath) ? DefaultMoveRootPath : moveRootPath;
            Transform found = transform.Find(path);
            if (found != null)
            {
                return found;
            }

            if (path.Contains("/"))
            {
                return null;
            }

            return FindChildByName(transform, path);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform found = FindChildByName(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private Vector2 GetTargetLocal()
        {
            Vector2 screenPos = m_Data.TargetRect != null
                ? RectTransformUtility.WorldToScreenPoint(GameEntry.UI.UICamera, m_Data.TargetRect.position)
                : m_Data.TargetScreenPos;
            return ScreenToLocal(screenPos);
        }

        private static Vector2 BezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Vector2 BezierTangent(float t, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        }

        private void AlignMoveRootToDirection(Vector2 direction, bool alignToMoveDirection, float angleOffset)
        {
            if (!alignToMoveDirection || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
            _moveRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private Vector2 GetControlPoint(Vector2 start, Vector2 end, CurveDirection curveDirection, float curveStrengthMin, float curveStrengthMax)
        {
            Vector2 dir = (end - start).normalized;
            Vector2 normal = new(-dir.y, dir.x);

            float horizontalDist = Mathf.Abs(end.x - start.x);
            float curveStrength = Mathf.Clamp(horizontalDist, curveStrengthMin, curveStrengthMax);

            float sign = curveDirection == CurveDirection.Up
                ? Mathf.Sign(normal.y) == 0f ? 1f : Mathf.Sign(normal.y)
                : -Mathf.Sign(normal.y) == 0f ? -1f : -Mathf.Sign(normal.y);

            return (start + end) * 0.5f + curveStrength * sign * normal;
        }

        private Vector2 ScreenToLocal(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_Data.CanvasRect, screenPos, GameEntry.UI.UICamera, out Vector2 local);
            return local;
        }
    }
}
