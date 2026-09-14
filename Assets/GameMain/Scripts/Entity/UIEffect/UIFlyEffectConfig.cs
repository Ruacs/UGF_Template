using DG.Tweening;
using UnityEngine;

namespace Lokas
{
    [DisallowMultipleComponent]
    public sealed class UIFlyEffectConfig : MonoBehaviour
    {
        [Header("Move Animation")]
        [SerializeField] private float m_MoveDuration = 0.3f;
        [SerializeField] private Ease m_ScaleInEase = Ease.OutBack;
        [SerializeField, Range(0f, 1f)] private float m_ScaleInRatio = 0.3f;
        [SerializeField] private FlyMoveMode m_MoveMode = FlyMoveMode.Curve;

        [SerializeField] private bool m_AlignToMoveDirection = true;
        [SerializeField] private float m_MoveDirectionAngleOffset = -90f;

        [Header("Curve")]
        [SerializeField] private CurveDirection m_CurveDirection = CurveDirection.Up;
        [SerializeField] private float m_CurveStrengthMin = 120f;
        [SerializeField] private float m_CurveStrengthMax = 350f;

        public float MoveDuration => m_MoveDuration;
        public Ease ScaleInEase => m_ScaleInEase;
        public float ScaleInRatio => m_ScaleInRatio;
        public FlyMoveMode MoveMode => m_MoveMode;
        public bool AlignToMoveDirection => m_AlignToMoveDirection;
        public float MoveDirectionAngleOffset => m_MoveDirectionAngleOffset;
        public CurveDirection CurveDirection => m_CurveDirection;
        public float CurveStrengthMin => m_CurveStrengthMin;
        public float CurveStrengthMax => m_CurveStrengthMax;
    }
}
