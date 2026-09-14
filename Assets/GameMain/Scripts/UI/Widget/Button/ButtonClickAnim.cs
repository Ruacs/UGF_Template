using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Unity.VisualScripting;

public class ButtonClickAnim : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public enum TriggerMode
    {
        Click,        // 点击播放一次完整动画
        PressRelease, // 按下缩小，抬起恢复（默认）
    }

    public enum AnimEffect
    {
        Scale,   // 普通缩放
        Bounce,  // 缩放+回弹
        Punch,   // 快速冲击（缩小→放大→恢复）
        Jelly,   // 果冻效果（X/Y不同步缩放）
    }

    [Header("模式")]
    public TriggerMode triggerMode = TriggerMode.PressRelease;

    [Header("动画效果")]
    public AnimEffect animEffect = AnimEffect.Bounce;

    [Header("参数")]
    [Range(0.7f, 0.95f)]
    public float pressScale = 0.85f;
    public bool useSeparateAxisScale = false;
    public Vector3 pressScaleXYZ = new Vector3(0.85f, 0.85f, 0.85f);
    public float animDuration = 0.1f;

    private Vector3 originScale;
    private Coroutine animCoroutine;

    void Awake()
    {
        originScale = transform.localScale;
    }
    void OnEnable()
    {
        transform.localScale = originScale;
    }

    // ────────── 事件 ──────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (triggerMode == TriggerMode.Click)
            PlayClickAnim();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (triggerMode == TriggerMode.PressRelease)
            PlayScale(GetPressScaleTarget());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (triggerMode == TriggerMode.PressRelease)
            PlayRelease();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (triggerMode == TriggerMode.PressRelease)
            PlayScale(originScale);
    }

    // ────────── 播放入口 ──────────

    void PlayClickAnim()
    {
        StopAnim();
        switch (animEffect)
        {
            case AnimEffect.Scale:
                animCoroutine = StartCoroutine(ClickScaleAnim());
                break;
            case AnimEffect.Bounce:
                animCoroutine = StartCoroutine(ClickBounceAnim());
                break;
            case AnimEffect.Punch:
                animCoroutine = StartCoroutine(PunchAnim());
                break;
            case AnimEffect.Jelly:
                animCoroutine = StartCoroutine(JellyAnim());
                break;
        }
    }

    void PlayRelease()
    {
        StopAnim();
        switch (animEffect)
        {
            case AnimEffect.Scale:
                animCoroutine = StartCoroutine(ScaleToTarget(originScale));
                break;
            case AnimEffect.Bounce:
                animCoroutine = StartCoroutine(BounceBackAnim());
                break;
            case AnimEffect.Punch:
                animCoroutine = StartCoroutine(BounceBackAnim());
                break;
            case AnimEffect.Jelly:
                animCoroutine = StartCoroutine(JellyBackAnim());
                break;
        }
    }

    void PlayScale(Vector3 target)
    {
        StopAnim();
        animCoroutine = StartCoroutine(ScaleToTarget(target));
    }

    void StopAnim()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
    }

    // ────────── 基础：平滑缩放到目标 ──────────

    IEnumerator ScaleToTarget(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float time = 0f;
        while (time < animDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = SmoothStep(time / animDuration);
            transform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.localScale = target;
    }

    // ────────── Click 模式动画 ──────────

    // Scale：缩小 → 恢复
    IEnumerator ClickScaleAnim()
    {
        yield return ScaleToTarget(GetPressScaleTarget());
        yield return ScaleToTarget(originScale);
    }

    // Bounce：缩小 → 恢复 + 回弹
    IEnumerator ClickBounceAnim()
    {
        yield return ScaleToTarget(GetPressScaleTarget());
        yield return BounceBackAnim();
    }

    // Punch：快速缩小 → 放大超过原始 → 回弹恢复
    IEnumerator PunchAnim()
    {
        float punchDur = animDuration * 0.6f;
        float overshootDur = animDuration * 0.8f;
        float settleDur = animDuration * 0.6f;

        Vector3 start = transform.localScale;
        Vector3 shrink = GetPressScaleTarget();
        Vector3 overshoot = originScale * 1.08f;

        // 缩小
        float time = 0f;
        while (time < punchDur)
        {
            time += Time.unscaledDeltaTime;
            float t = SmoothStep(time / punchDur);
            transform.localScale = Vector3.Lerp(start, shrink, t);
            yield return null;
        }

        // 放大超过
        time = 0f;
        while (time < overshootDur)
        {
            time += Time.unscaledDeltaTime;
            float t = SmoothStep(time / overshootDur);
            transform.localScale = Vector3.Lerp(shrink, overshoot, t);
            yield return null;
        }

        // 收回
        time = 0f;
        while (time < settleDur)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / settleDur);
            t = t * t;
            transform.localScale = Vector3.Lerp(overshoot, originScale, t);
            yield return null;
        }

        transform.localScale = originScale;
    }

    // Jelly：X/Y 不同步缩放，果冻感
    IEnumerator JellyAnim()
    {
        float dur = animDuration * 3f;
        float time = 0f;
        Vector3 start = transform.localScale;

        while (time < dur)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / dur);

            float decay = 1f - t;
            float freqX = 12f;
            float freqY = 14f;
            float ampX = 0.12f * decay;
            float ampY = 0.15f * decay;

            float sx = originScale.x * (1f + Mathf.Sin(t * freqX * Mathf.PI) * ampX);
            float sy = originScale.y * (1f + Mathf.Sin(t * freqY * Mathf.PI + 0.5f) * ampY);

            transform.localScale = new Vector3(sx, sy, originScale.z);
            yield return null;
        }

        transform.localScale = originScale;
    }

    // ────────── PressRelease 松手回弹 ──────────

    IEnumerator BounceBackAnim()
    {
        // 回到原始大小
        yield return ScaleToTarget(originScale);

        // 回弹：略大再收回
        float bounceDur = 0.08f;
        Vector3 bounceTarget = originScale * 1.04f;

        float time = 0f;
        while (time < bounceDur)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / bounceDur);
            transform.localScale = Vector3.Lerp(originScale, bounceTarget, t);
            yield return null;
        }

        time = 0f;
        while (time < bounceDur)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / bounceDur);
            t = t * t;
            transform.localScale = Vector3.Lerp(bounceTarget, originScale, t);
            yield return null;
        }

        transform.localScale = originScale;
    }

    // Jelly 松手恢复
    IEnumerator JellyBackAnim()
    {
        float dur = animDuration * 2f;
        float time = 0f;
        Vector3 start = transform.localScale;

        while (time < dur)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / dur);

            float decay = 1f - t;
            float sx = Mathf.Lerp(start.x, originScale.x, t) + Mathf.Sin(t * 10f * Mathf.PI) * 0.05f * decay * originScale.x;
            float sy = Mathf.Lerp(start.y, originScale.y, t) + Mathf.Sin(t * 12f * Mathf.PI + 0.5f) * 0.06f * decay * originScale.y;

            transform.localScale = new Vector3(sx, sy, originScale.z);
            yield return null;
        }

        transform.localScale = originScale;
    }

    // ────────── 工具 ──────────

    static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    Vector3 GetPressScaleTarget()
    {
        if (!useSeparateAxisScale)
            return originScale * pressScale;

        return Vector3.Scale(originScale, pressScaleXYZ);
    }
}
