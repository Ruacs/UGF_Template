using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public static class TweenUtility
{
    /// <summary>
    /// 通用 Tween（自动取消 Destroy）
    /// </summary>
    public static async UniTask Tween(float duration,Action<float> onUpdate,AnimationCurve curve = null,CancellationToken token = default)
    {
        if (duration <= 0f)
        {
            onUpdate?.Invoke(1f);
            return;
        }

        float timer = 0f;
        curve ??= AnimationCurve.Linear(0, 0, 1, 1);

        while (timer < duration)
        {
            if (token.IsCancellationRequested)
                return;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            onUpdate?.Invoke(curve.Evaluate(t));

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        onUpdate?.Invoke(1f);
    }
}
