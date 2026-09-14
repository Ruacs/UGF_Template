#if DOTWEEN
using DG.Tweening;
using System;
using UnityEngine;
using static DOTweenSequence;
using UnityEngine.Events;
using UnityEngine.UI;

public class DOTweenSequence : MonoBehaviour
{
    [HideInInspector][SerializeField] SequenceAnimation[] m_Sequence;
    [SerializeField] bool m_PlayOnAwake = false;
    [SerializeField] bool m_ResetOnAwake = false;
    [SerializeField] float m_Delay = 0;
    [SerializeField] Ease m_Ease = Ease.OutQuad;
    [SerializeField] int m_Loops = 1;
    [SerializeField] LoopType m_LoopType = LoopType.Restart;
    [SerializeField] UpdateType m_UpdateType = UpdateType.Normal;

    [SerializeField] bool m_IgnoreTimeScale = true;
    [SerializeField] UnityEvent m_OnPlay = null;
    [SerializeField] UnityEvent m_OnUpdate = null;
    [SerializeField] UnityEvent m_OnComplete = null;

    private Tween m_Tween;
    private void Awake()
    {
        if (m_PlayOnAwake) DOPlay();
        else if (m_ResetOnAwake)
        {
            ResetToFromValue();
        }
    }

    private void ResetToFromValue()
    {
        foreach (var item in m_Sequence)
        {
            var useFromValue = item.UseFromValue;
            if (!useFromValue) continue;
            var targetCom = item.Target;
            var resetValue = item.FromValue;
            switch (item.AnimationType)
            {
                case DOTweenType.DOMove:
                    {
                        (targetCom as Transform).position = resetValue;
                        break;
                    }
                case DOTweenType.DOMoveX:
                    {
                        (targetCom as Transform).SetPositionX(resetValue.x);
                        break;
                    }
                case DOTweenType.DOMoveY:
                    {
                        (targetCom as Transform).SetPositionY(resetValue.x);
                        break;
                    }
                case DOTweenType.DOMoveZ:
                    {
                        (targetCom as Transform).SetPositionZ(resetValue.x);
                        break;
                    }
                case DOTweenType.DOLocalMove:
                    {
                        (targetCom as Transform).localPosition = resetValue;
                        break;
                    }
                case DOTweenType.DOLocalMoveX:
                    {
                        (targetCom as Transform).SetLocalPositionX(resetValue.x);
                        break;
                    }
                case DOTweenType.DOLocalMoveY:
                    {
                        (targetCom as Transform).SetLocalPositionY(resetValue.x);
                        break;
                    }
                case DOTweenType.DOLocalMoveZ:
                    {
                        (targetCom as Transform).SetLocalPositionZ(resetValue.x);
                        break;
                    }
                case DOTweenType.DOAnchorPos:
                    {
                        (targetCom as RectTransform).anchoredPosition = resetValue;
                        break;
                    }
                case DOTweenType.DOAnchorPosX:
                    {
                        (targetCom as RectTransform).SetAnchoredPositionX(resetValue.x);
                        break;
                    }
                case DOTweenType.DOAnchorPosY:
                    {
                        (targetCom as RectTransform).SetAnchoredPositionY(resetValue.x);
                        break;
                    }
                case DOTweenType.DOAnchorPosZ:
                    {
                        (targetCom as RectTransform).SetAnchoredPosition3DZ(resetValue.x);
                        break;
                    }
                case DOTweenType.DOAnchorPos3D:
                    {
                        (targetCom as RectTransform).anchoredPosition3D = resetValue;
                        break;
                    }
                case DOTweenType.DOColor:
                    {
                        (targetCom as UnityEngine.UI.Graphic).color = resetValue;
                        break;
                    }
                case DOTweenType.DOFade:
                    {
                        (targetCom as UnityEngine.UI.Graphic).SetColorAlpha(resetValue.x);
                        break;
                    }
                case DOTweenType.DOCanvasGroupFade:
                    {
                        (targetCom as UnityEngine.CanvasGroup).alpha = resetValue.x;
                        break;
                    }
                case DOTweenType.DOValue:
                    {
                        (targetCom as UnityEngine.UI.Slider).value = resetValue.x;
                        break;
                    }
                case DOTweenType.DOSizeDelta:
                    {
                        (targetCom as RectTransform).sizeDelta = resetValue;
                        break;
                    }
                case DOTweenType.DOFillAmount:
                    {
                        (targetCom as UnityEngine.UI.Image).fillAmount = resetValue.x;
                        break;
                    }
                case DOTweenType.DOFlexibleSize:
                    {
                        (targetCom as LayoutElement).SetFlexibleSize(resetValue);
                        break;
                    }
                case DOTweenType.DOMinSize:
                    {
                        (targetCom as LayoutElement).SetMinSize(resetValue);
                        break;
                    }
                case DOTweenType.DOPreferredSize:
                    {
                        (targetCom as LayoutElement).SetPreferredSize(resetValue);
                        break;
                    }
                case DOTweenType.DOScale:
                    {
                        (targetCom as Transform).localScale = resetValue;
                        break;
                    }
                case DOTweenType.DOScaleX:
                    {
                        (targetCom as Transform).SetLocalScaleX(resetValue.x);
                        break;
                    }
                case DOTweenType.DOScaleY:
                    {
                        (targetCom as Transform).SetLocalScaleY(resetValue.x);
                        break;
                    }
                case DOTweenType.DOScaleZ:
                    {
                        (targetCom as Transform).SetLocalScaleZ(resetValue.z);
                        break;
                    }
                case DOTweenType.DORotate:
                    {
                        var t = targetCom as Transform;
                        if (t is RectTransform)
                            t.localEulerAngles = resetValue;
                        else
                            t.eulerAngles = resetValue;
                        break;
                    }
                case DOTweenType.DOLocalRotate:
                    {
                        (targetCom as Transform).localEulerAngles = resetValue;
                        break;
                    }
            }
        }
    }
    private Tween CreateTween(bool reverse = false)
    {
        if (m_Sequence == null || m_Sequence.Length == 0)
        {
            return null;
        }
        var sequence = DOTween.Sequence();
        if (reverse)
        {
            for (int i = m_Sequence.Length - 1; i >= 0; i--)
            {
                var item = m_Sequence[i];
                var tweener = item.CreateTween(reverse);
                if (tweener == null)
                {
                    Debug.LogErrorFormat("Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}", i, item.AnimationType, item.Target == null ? "null" : item.Target.GetType().Name);
                    continue;
                }
                tweener.SetUpdate(!m_IgnoreTimeScale);
                switch (item.AddType)
                {
                    case AddType.Append:
                        sequence.Append(tweener);
                        break;
                    case AddType.Join:
                        sequence.Join(tweener);
                        break;
                }
            }
        }
        else
        {
            for (int i = 0; i < m_Sequence.Length; i++)
            {
                var item = m_Sequence[i];
                var tweener = item.CreateTween(reverse);
                if (tweener == null)
                {
                    Debug.LogErrorFormat("Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}", i, item.AnimationType, item.Target == null ? "null" : item.Target.GetType().Name);
                    continue;
                }
                tweener.SetUpdate(!m_IgnoreTimeScale);
                switch (item.AddType)
                {
                    case AddType.Append:
                        sequence.Append(tweener);
                        break;
                    case AddType.Join:
                        sequence.Join(tweener);
                        break;
                }
            }
        }
        sequence.SetEase(m_Ease).SetUpdate(m_UpdateType, m_IgnoreTimeScale).SetLoops(m_Loops, m_LoopType).SetDelay(m_Delay);
        if (m_OnPlay != null) sequence.OnPlay(m_OnPlay.Invoke);
        if (m_OnUpdate != null) sequence.OnUpdate(m_OnUpdate.Invoke);
        if (m_OnComplete != null) sequence.OnComplete(m_OnComplete.Invoke);
        sequence.SetAutoKill(true);
        return sequence;
    }
    public void Play()
    {
        if (gameObject.activeSelf)
            DOPlay();
    }
    public Tween DOPlay()
    {
        m_Tween = CreateTween();
        return m_Tween?.Play();
    }

    public Tween DORewind()
    {
        m_Tween = CreateTween(true);
        return m_Tween?.Play();
    }

    public void DOComplete(bool withCallback = false)
    {
        m_Tween?.Complete(withCallback);
    }

    public void DOKill()
    {
        m_Tween?.Kill();
        m_Tween = null;
    }
    [Serializable]
    public enum DOTweenType
    {
        DOMove,
        DOMoveX,
        DOMoveY,
        DOMoveZ,

        DOLocalMove,
        DOLocalMoveX,
        DOLocalMoveY,
        DOLocalMoveZ,

        DOScale,
        DOScaleX,
        DOScaleY,
        DOScaleZ,

        DORotate,
        DOLocalRotate,

        DOAnchorPos,
        DOAnchorPosX,
        DOAnchorPosY,
        DOAnchorPosZ,
        DOAnchorPos3D,


        DOColor,
        DOFade,
        DOCanvasGroupFade,
        DOFillAmount,
        DOFlexibleSize,
        DOMinSize,
        DOPreferredSize,
        DOSizeDelta,
        DOValue
    }

    [Serializable]
    public class SequenceAnimation
    {
        public AddType AddType = AddType.Append;
        public DOTweenType AnimationType = DOTweenType.DOMove;
        public Component Target = null;
        public Vector4 ToValue = Vector4.zero;

        public bool UseToTarget = false;
        public Component ToTarget = null;

        public bool UseFromValue = false;
        public Vector4 FromValue = Vector4.zero;
        public bool SpeedBased = false;
        public float DurationOrSpeed = 1;
        public float Delay = 0;
        public UpdateType UpdateType = UpdateType.Normal;
        public bool CustomEase = false;
        public AnimationCurve EaseCurve;
        public Ease Ease = Ease.OutQuad;
        public int Loops = 1;
        public LoopType LoopType = LoopType.Restart;
        public bool Snapping = false;
        public UnityEvent OnPlay = null;
        public UnityEvent OnUpdate = null;
        public UnityEvent OnComplete = null;
        public Tween CreateTween(bool reverse)
        {
            Tween result = null;
            float duration = this.DurationOrSpeed;

            switch (AnimationType)
            {
                case DOTweenType.DOMove:
                    {
                        var transform = Target as Transform;
                        Vector3 targetValue = UseToTarget ? (ToTarget as Transform).position : ToValue;
                        Vector3 startValue = UseFromValue ? FromValue : transform.position;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.position = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = transform.DOMove(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveX:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveY:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveZ:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetPositionZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveZ(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMove:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : transform.localPosition;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.localPosition = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMove(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveX:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetLocalPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveY:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetLocalPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveZ:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.SetLocalPositionZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveZ(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOScale:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : com.localScale;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.localScale = startValue;
                        if (SpeedBased) duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOScale(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleX:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetLocalScaleX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleX(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleY:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetLocalScaleY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleY(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleZ:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetLocalScaleZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleZ(targetValue, duration);
                    }
                    break;
                case DOTweenType.DORotate:
                    {
                        var com = Target as Transform;
                        if (com is RectTransform)
                        {
                            var targetValue = UseToTarget ? (ToTarget as Transform).localEulerAngles : (Vector3)ToValue;
                            var startValue = UseFromValue ? (Vector3)FromValue : com.localEulerAngles;
                            if (reverse)
                            {
                                (targetValue, startValue) = (startValue, targetValue);
                            }
                            com.localEulerAngles = startValue;
                            if (SpeedBased)
                                duration = GetEulerAnglesAngle(targetValue, startValue) / this.DurationOrSpeed;
                            result = com.DOLocalRotate(targetValue, duration, RotateMode.FastBeyond360);
                        }
                        else
                        {
                            var targetValue = UseToTarget ? (ToTarget as Transform).eulerAngles : (Vector3)ToValue;
                            var startValue = UseFromValue ? (Vector3)FromValue : com.eulerAngles;
                            if (reverse)
                            {
                                (targetValue, startValue) = (startValue, targetValue);
                            }
                            com.eulerAngles = startValue;
                            if (SpeedBased)
                                duration = GetEulerAnglesAngle(targetValue, startValue) / this.DurationOrSpeed;
                            result = com.DORotate(targetValue, duration, RotateMode.FastBeyond360);
                        }
                    }
                    break;
                case DOTweenType.DOLocalRotate:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localEulerAngles : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : com.localEulerAngles;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.localEulerAngles = startValue;
                        if (SpeedBased)
                            duration = GetEulerAnglesAngle(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOLocalRotate(targetValue, duration, RotateMode.FastBeyond360);
                    }
                    break;
                case DOTweenType.DOAnchorPos:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : rectTransform.anchoredPosition;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.anchoredPosition = startValue;
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosX:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.SetAnchoredPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPosX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosY:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition.y;
                        if (reverse)
                        {
                            var swapValue = startValue;
                            startValue = targetValue;
                            targetValue = swapValue;
                        }
                        rectTransform.SetAnchoredPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPosY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosZ:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition3D.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition3D.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.SetAnchoredPosition3DZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos3DZ(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPos3D:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition3D : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : rectTransform.anchoredPosition3D;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.anchoredPosition3D = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos3D(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOSizeDelta:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).sizeDelta : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : rectTransform.sizeDelta;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        rectTransform.sizeDelta = startValue;
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOSizeDelta(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOColor:
                    {
                        var com = Target as UnityEngine.UI.Graphic;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Graphic).color : (Color)ToValue;
                        var startValue = UseFromValue ? (Color)FromValue : com.color;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.color = startValue;
                        if (SpeedBased)
                            duration = Vector4.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOColor(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOFade:
                    {
                        var com = Target as UnityEngine.UI.Graphic;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Graphic).color.a : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.color.a;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetColorAlpha(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFade(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOCanvasGroupFade:
                    {
                        var com = Target as UnityEngine.CanvasGroup;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.CanvasGroup).alpha : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.alpha;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.alpha = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFade(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOValue:
                    {
                        var com = Target as UnityEngine.UI.Slider;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Slider).value : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.value;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.value = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOValue(targetValue, duration, Snapping);
                    }
                    break;

                case DOTweenType.DOFillAmount:
                    {
                        var com = Target as UnityEngine.UI.Image;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Image).fillAmount : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.fillAmount;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.fillAmount = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFillAmount(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOFlexibleSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetFlexibleSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetFlexibleSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetFlexibleSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOFlexibleSize(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMinSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetMinSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetMinSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetMinSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOMinSize(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOPreferredSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetPreferredSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetPreferredSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        com.SetPreferredSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOPreferredSize(targetValue, duration, Snapping);
                    }
                    break;
            }

            if (result != null)
            {
                result.SetAutoKill(true).SetTarget(Target.gameObject).SetLoops(Loops, LoopType).SetUpdate(UpdateType);
                if (Delay > 0) result.SetDelay(Delay);
                if (CustomEase) result.SetEase(EaseCurve);
                else result.SetEase(Ease);

                if (OnPlay != null) result.OnPlay(OnPlay.Invoke);
                if (OnUpdate != null) result.OnUpdate(OnUpdate.Invoke);
                if (OnComplete != null) result.OnComplete(OnComplete.Invoke);
            }
            return result;
        }
        public static float GetEulerAnglesAngle(Vector3 euler1, Vector3 euler2)
        {
            // 计算差值
            Vector3 delta = euler2 - euler1;
            delta.x = Mathf.DeltaAngle(euler1.x, euler2.x);
            delta.y = Mathf.DeltaAngle(euler1.y, euler2.y);
            delta.z = Mathf.DeltaAngle(euler1.z, euler2.z);

            float angle = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            return (angle + 360) % 360;
        }
    }
    [Serializable]
    public enum AddType
    {
        Append,
        Join
    }
}
#endif