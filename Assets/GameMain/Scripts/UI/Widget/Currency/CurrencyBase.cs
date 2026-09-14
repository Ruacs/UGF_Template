using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public abstract class CurrencyBase: MonoBehaviour,ICurrency
{
    public abstract int Amount { get; }
    public abstract string CurrencyName { get; protected set; }
    public virtual Sprite Icon { get; protected set; }
    public RectTransform rt_icon;
    public TMP_Text tmp_Amount;

    public event EventHandler<ICurrency.OnAmountChangeEvent> OnAmountChange;


    protected virtual void Awake()
    {
        rt_icon = transform.Find("Icon").GetComponent<RectTransform>();
        tmp_Amount = transform.Find("Amount").GetComponent<TMP_Text>();
        Icon = rt_icon.GetComponent<Image>().sprite;
    }

    public abstract void Refresh();

    protected void NotifyAmountChange()
    {
        OnAmountChange?.Invoke(this, new ICurrency.OnAmountChangeEvent(Amount));
    }

    public abstract void Add(int value);


    public abstract bool Spend(int value);
}
