using System;

public interface ICurrency
{
    string CurrencyName { get; }
    /// <summary>
    /// 总数
    /// </summary>
    int Amount { get; }
    /// <summary>
    /// 增加
    /// </summary>
    /// <param name="value"></param>
    void Add(int value);
    /// <summary>
    /// 花费
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    bool Spend(int value);

    public event EventHandler<OnAmountChangeEvent> OnAmountChange;

    public class OnAmountChangeEvent: EventArgs
    {
        public int Amount { get; private set; }
        public OnAmountChangeEvent(int newAmount)
        {
            Amount = newAmount;
        }
    }
}
