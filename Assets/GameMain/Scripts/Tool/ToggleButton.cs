using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    GameObject on;
    GameObject off;
    GameObject off_icon;
    GameObject on_bg;
    GameObject on_icon;
    Vector2 on_icon_originPos;
    Vector2 off_icon_originPos;
    bool canClick = true;
    float tweenTime = 0.5f;
    public Action<bool> onClickCallBack;

    /// <summary>
    /// 设置ui初始化时调用
    /// </summary>
    public void Init(Action<bool> onClickCallBackArg)
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        on = transform.Find("on").gameObject;
        off = transform.Find("off").gameObject;
        off_icon = off.transform.Find("icon").gameObject;
        on_bg = on.transform.Find("bg").gameObject;
        on_icon = on.transform.Find("icon").gameObject;
        on_icon_originPos = on_icon.transform.localPosition;
        off_icon_originPos = off_icon.transform.localPosition;
        onClickCallBack = onClickCallBackArg;
    }

    /// <summary>
    /// 设置ui打开时调用
    /// </summary>
    /// <param name="isOn"></param>
    public void SetState(bool isOn)
    {
        //Debug.Log("isOn: "+isOn);

        if (isOn)
        {
            on.SetActive(true);
            off.SetActive(false);
            on_icon.transform.localPosition = on_icon_originPos;
            on_bg.GetComponent<Image>().fillAmount = 1;
        }
        else
        {
            on.SetActive(false);
            off.SetActive(true);
            on_icon.transform.localPosition = off_icon_originPos;
            on_bg.GetComponent<Image>().fillAmount = 0;
        }
    }

    void OnClick()
    {
        if(!canClick)
        {
            return;
        }

        canClick = false;

        bool isOn = on.activeInHierarchy;


        if (isOn)
        {
            Tween t1 = on_icon.transform.DOLocalMove(off_icon_originPos, tweenTime);
            Tween t2 = on_bg.GetComponent<Image>().DOFillAmount(0,tweenTime);
            t1.OnComplete(() =>
            {
                on.SetActive(false);
                off.SetActive(true);
                canClick = true;
                onClickCallBack?.Invoke(false);
            });
        }
        else
        {
            on.SetActive(true);
            off.SetActive(false);
            Tween t1 = on_icon.transform.DOLocalMove(on_icon_originPos, tweenTime);
            Tween t2 = on_bg.GetComponent<Image>().DOFillAmount(1, tweenTime);
            t1.OnComplete(() =>
            {
                canClick = true;
                onClickCallBack?.Invoke(true);
            });
        }
    }

    
}
