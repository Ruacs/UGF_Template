using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public static class TweenExtension 
{
    /// <summary>
    /// 自动随 Component Destroy 取消 Tween
    /// </summary>
    public static CancellationToken GetTweenToken(this Component component)
    {
        return component.GetCancellationTokenOnDestroy();
    }

    /// <summary>
    /// 自动随 GameObject Destroy 取消 Tween
    /// </summary>
    public static CancellationToken GetTweenToken(this GameObject gameObject)
    {
        return gameObject.GetCancellationTokenOnDestroy();
    }



}