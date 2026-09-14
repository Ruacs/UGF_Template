using System;
using GameFramework;
using GameFramework.Event;

[Serializable]
public sealed class ItemUsedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ItemUsedEventArgs).GetHashCode();

    public string itemId;
    public int count;

    public override int Id => EventId;

    public static ItemUsedEventArgs Create(string itemId, int count = 1)
    {
        var args = ReferencePool.Acquire<ItemUsedEventArgs>();
        args.itemId = itemId;
        args.count = count;
        return args;
    }

    public override void Clear()
    {
        itemId = null;
        count = 0;
    }
}
