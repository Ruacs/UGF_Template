using System;
using Lokas;
using UnityEngine;

public class UIItemBase : MonoBehaviour
{

    private void Awake()
    {
        OnInit();
    }

    protected virtual void OnInit()
    {
        InitLocalization();
    }
    /// <summary>
    /// 更新界面中静态文本的多语言文字
    /// </summary>
    public virtual void InitLocalization()
    {
        UnityGameFramework.Runtime.UIStringKey[] texts = GetComponentsInChildren<UnityGameFramework.Runtime.UIStringKey>(true);
        foreach (var t in texts)
        {
            if (t.TryGetComponent<TMPro.TextMeshProUGUI>(out var textMeshCom))
            {
                textMeshCom.text = GameEntry.Localization.GetString(t.Key);
            }
            else if (t.TryGetComponent<UnityEngine.UI.Text>(out var textCom))
            {
                textCom.text = GameEntry.Localization.GetString(t.Key);
            }
        }

        UILocalizedImageKey[] images = GetComponentsInChildren<UILocalizedImageKey>(true);
        foreach (var image in images)
        {
            image.ApplyLocalization();
        }
    }
}
