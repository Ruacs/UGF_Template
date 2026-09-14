//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using Cysharp.Threading.Tasks;
using GameFramework.DataTable;
using GameFramework.Resource;
using GameFramework.UI; 
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityGameFramework.Runtime;
using Slider = UnityEngine.UI.Slider;
using GameEntry = Lokas.GameEntry;
using DG.Tweening;
using Lokas;

/// <summary>
/// UI控制类
/// </summary>
public static class UIExtension
{
    public static void AddSafeClick(this UnityEngine.UI.Button button, System.Action action, float interval = 0.5f)
    {
        if (button == null)
        {
            return;
        }

        float lastClickTime = -999f;
        button.onClick.AddListener(() =>
        {
            if (Time.unscaledTime - lastClickTime < interval)
            {
                return;
            }

            GameEntry.Vibrate(10);

            lastClickTime = Time.unscaledTime;
            action?.Invoke();
        });
    }

    public static void AddOnceClick(this UnityEngine.UI.Button button, System.Action action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.AddListener(() =>
        {
            if (!button.interactable)
            {
                return;
            }

            button.interactable = false;
            action?.Invoke();
        });
    }

    /// <summary>
    /// 把 sourceRt 的位置转换到 targetRt 的局部坐标系下
    /// </summary>
    /// <param name="sourceRt">源 RectTransform</param>
    /// <param name="targetRt">目标 RectTransform</param>
    /// <param name="camera">UI 相机</param>
    /// <returns>目标局部坐标</returns>
    public static Vector2 WorldToLocalPointInRect(this RectTransform sourceRt, RectTransform targetRt, Camera camera)
    {
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(camera, sourceRt.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRt, screenPos, camera, out Vector2 localPos);
        return localPos;
    }





    public static IEnumerator FadeToAlpha(this CanvasGroup canvasGroup, float alpha, float duration)
    {
        float time = 0f;
        float originalAlpha = canvasGroup.alpha;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(originalAlpha, alpha, time / duration);
            yield return new WaitForEndOfFrame();
        }

        canvasGroup.alpha = alpha;
    }
    public static IEnumerator SmoothValue(this Slider slider, float value, float duration)
    {
        float time = 0f;
        float originalValue = slider.value;
        while (time < duration)
        {
            time += Time.deltaTime;
            slider.value = Mathf.Lerp(originalValue, value, time / duration);
            yield return new WaitForEndOfFrame();
        }

        slider.value = value;
    }

    public static IEnumerator ValueTween(this TMP_Text text, int startValue, int endValue, float duration = 0.5f)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float value = Mathf.Lerp(startValue, endValue, t);
            text.text = Mathf.FloorToInt(value).ToString(); // 取整显示
            yield return null;
        }

        text.text = Mathf.FloorToInt(endValue).ToString(); // 最终值
    }

    public static IEnumerator ValueTween(this TMP_Text text, int startValue, int endValue, System.Func<int, string> formatFunc, float duration = 0.5f)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            int value = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
            text.text = formatFunc(value);
            yield return null;
        }

        text.text = formatFunc(endValue);
    }

    public static UniTask TweenSpacing(this VerticalLayoutGroup layout, float endValue, float duration = 0.5f)
    {
        if (!layout) return UniTask.CompletedTask;

        float start = layout.spacing;
        var token = layout.GetTweenToken();

        return TweenUtility.Tween(
            duration,
            t =>
            {
                if (!layout) return;
                layout.spacing = Mathf.Lerp(start, endValue, t);
            },
            token: token
        );
    }

    public static IEnumerator ValueTween(this Text text, int startValue, int endValue, float duration = 0.5f)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float value = Mathf.Lerp(startValue, endValue, t);
            text.text = Mathf.FloorToInt(value).ToString(); // 取整显示
            yield return null;
        }

        text.text = Mathf.FloorToInt(endValue).ToString(); // 最终值
    }

    public static async UniTask PlayScale(this RectTransform rectTransform, float from, float to, float duration)
    {
        if (rectTransform == null) return;
        rectTransform.localScale = Vector3.one;
        if (rectTransform == null)
            return;

        rectTransform.DOKill();

        rectTransform.localScale = Vector3.one * from;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            rectTransform
                .DOScale(Vector3.one * to, duration * 0.5f)
                .SetEase(Ease.OutQuad));

        sequence.Append(
            rectTransform
                .DOScale(Vector3.one * from, duration * 0.5f)
                .SetEase(Ease.InQuad));

        await sequence.AsyncWaitForCompletion();
    }

    public static bool HasUIForm(this UIComponent uiComponent, UIFormId uiFormId, string uiGroupName = null)
    {
        return uiComponent.HasUIForm((int)uiFormId, uiGroupName);
    }

    public static bool HasUIForm(this UIComponent uiComponent, int uiFormId, string uiGroupName = null)
    {
        if (uiComponent == null || GameEntry.DataTable == null)
        {
            return false;
        }

        IDataTable<DRUIForm> dtUIForm = GameEntry.DataTable.GetDataTable<DRUIForm>();
        if (dtUIForm == null)
        {
            return false;
        }

        DRUIForm drUIForm = dtUIForm.GetDataRow(uiFormId);
        if (drUIForm == null)
        {
            return false;
        }

        string assetName = AssetUtility.GetUIFormAsset(drUIForm.AssetName);
        if (string.IsNullOrEmpty(assetName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(uiGroupName))
        {
            return uiComponent.HasUIForm(assetName);
        }

        IUIGroup uiGroup = uiComponent.GetUIGroup(uiGroupName);
        if (uiGroup == null)
        {
            return false;
        }

        return uiGroup.HasUIForm(assetName);
    }

    public static UGuiForm GetUIForm(this UIComponent uiComponent, UIFormId uiFormId, string uiGroupName = null)
    {
        return uiComponent.GetUIForm((int)uiFormId, uiGroupName);
    }

    public static UGuiForm GetUIForm(this UIComponent uiComponent, int uiFormId, string uiGroupName = null)
    {
        if (uiComponent == null || GameEntry.DataTable == null)
        {
            return null;
        }

        IDataTable<DRUIForm> dtUIForm = GameEntry.DataTable.GetDataTable<DRUIForm>();
        if (dtUIForm == null)
        {
            return null;
        }

        DRUIForm drUIForm = dtUIForm.GetDataRow(uiFormId);
        if (drUIForm == null)
        {
            return null;
        }

        string assetName = AssetUtility.GetUIFormAsset(drUIForm.AssetName);
        if (string.IsNullOrEmpty(assetName))
        {
            return null;
        }

        UIForm uiForm = null;
        if (string.IsNullOrEmpty(uiGroupName))
        {
            uiForm = uiComponent.GetUIForm(assetName);
            if (uiForm == null)
            {
                return null;
            }

            return (UGuiForm)uiForm.Logic;
        }

        IUIGroup uiGroup = uiComponent.GetUIGroup(uiGroupName);
        if (uiGroup == null)
        {
            return null;
        }

        uiForm = (UIForm)uiGroup.GetUIForm(assetName);
        if (uiForm == null)
        {
            return null;
        }

        return (UGuiForm)uiForm.Logic;
    }

    public static void CloseUIForm(this UIComponent uiComponent, UGuiForm uiForm)
    {
        uiComponent.CloseUIForm(uiForm.UIForm);
    }

    public static int? OpenUIForm(this UIComponent uiComponent, UIFormId uiFormId, object userData = null)
    {
        return uiComponent.OpenUIForm((int)uiFormId, userData);
    }

    public static int? OpenUIForm(this UIComponent uiComponent, int uiFormId, object userData = null)
    {
        if (uiComponent == null || GameEntry.DataTable == null)
        {
            Log.Warning("Can not open UI form '{0}', UI component or data table component is not ready.", uiFormId.ToString());
            return null;
        }

        IDataTable<DRUIForm> dtUIForm = GameEntry.DataTable.GetDataTable<DRUIForm>();
        if (dtUIForm == null)
        {
            Log.Warning("Can not open UI form '{0}', DRUIForm data table is not ready.", uiFormId.ToString());
            return null;
        }

        DRUIForm drUIForm = dtUIForm.GetDataRow(uiFormId);
        if (drUIForm == null)
        {
            Log.Warning("Can not load UI form '{0}' from data table.", uiFormId.ToString());
            return null;
        }

        string assetName = AssetUtility.GetUIFormAsset(drUIForm.AssetName);
        if (string.IsNullOrEmpty(assetName))
        {
            Log.Warning("Can not open UI form '{0}', asset name is empty.", uiFormId.ToString());
            return null;
        }

        if (!drUIForm.AllowMultiInstance)
        {
            if (uiComponent.IsLoadingUIForm(assetName))
            {
                return null;
            }

            if (uiComponent.HasUIForm(assetName))
            {
                return null;
            }
        }

        return uiComponent.OpenUIForm(assetName, drUIForm.UIGroupName, Constant.AssetPriority.UIFormAsset, drUIForm.PauseCoveredUIForm, userData);
    }



    /// <summary>
    /// 刷新所有UI的多语言文本(当语言切换时需调用),用于即时改变多语言文本
    /// </summary>
    /// <param name="uiCom"></param>
    public static void UpdateLocalizationTexts(this UIComponent uiCom)
    {
        foreach (UIForm uiForm in uiCom.GetAllLoadedUIForms())
        {
            (uiForm.Logic as UGuiForm).InitLocalization();
        }
        var uiObjectPool = GameEntry.ObjectPool.GetObjectPool(pool => pool.FullName == "GameFramework.UI.UIManager+UIFormInstanceObject.UI Instance Pool");
        if (uiObjectPool != null)
        {
            uiObjectPool.ReleaseAllUnused();
        }
    }
    //public static void OpenDialog(this UIComponent uiComponent, DialogParams dialogParams)
    //{
    //    if (((ProcedureBase)GameEntry.Procedure.CurrentProcedure).UseNativeDialog)
    //    {
    //        OpenNativeDialog(dialogParams);
    //    }
    //    else
    //    {
    //        uiComponent.OpenUIForm(UIFormId.DialogForm, dialogParams);
    //    }
    //}

    //private static void OpenNativeDialog(DialogParams dialogParams)
    //{
    //    // TODO：这里应该弹出原生对话框，先简化实现为直接按确认按钮
    //    if (dialogParams.OnClickConfirm != null)
    //    {
    //        dialogParams.OnClickConfirm(dialogParams.UserData);
    //    }
    //}


    #region Unity UI Extension
    public static void SetAnchoredPositionX(this RectTransform rectTransform, float anchoredPositionX)
    {
        var value = rectTransform.anchoredPosition;
        value.x = anchoredPositionX;
        rectTransform.anchoredPosition = value;
    }
    public static void SetAnchoredPositionY(this RectTransform rectTransform, float anchoredPositionY)
    {
        var value = rectTransform.anchoredPosition;
        value.y = anchoredPositionY;
        rectTransform.anchoredPosition = value;
    }
    public static void SetAnchoredPosition3DZ(this RectTransform rectTransform, float anchoredPositionZ)
    {
        var value = rectTransform.anchoredPosition3D;
        value.z = anchoredPositionZ;
        rectTransform.anchoredPosition3D = value;
    }
    public static void SetColorAlpha(this UnityEngine.UI.Graphic graphic, float alpha)
    {
        var value = graphic.color;
        value.a = alpha;
        graphic.color = value;
    }
    public static void SetFlexibleSize(this LayoutElement layoutElement, Vector2 flexibleSize)
    {
        layoutElement.flexibleWidth = flexibleSize.x;
        layoutElement.flexibleHeight = flexibleSize.y;
    }
    public static Vector2 GetFlexibleSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.flexibleWidth, layoutElement.flexibleHeight);
    }
    public static void SetMinSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
    }
    public static Vector2 GetMinSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.minWidth, layoutElement.minHeight);
    }
    public static void SetPreferredSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
    }
    public static Vector2 GetPreferredSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
    }
    #endregion
}

