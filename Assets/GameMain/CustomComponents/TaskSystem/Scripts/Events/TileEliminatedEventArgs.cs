using System;
using GameFramework;
using GameFramework.Event;

[Serializable]
public sealed class TileEliminatedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(TileEliminatedEventArgs).GetHashCode();

    public int tileId;
    public int count;

    public override int Id => EventId;

    public static TileEliminatedEventArgs Create(int tileId, int count = 1)
    {
        var args = ReferencePool.Acquire<TileEliminatedEventArgs>();
        args.tileId = tileId;
        args.count = count;
        return args;
    }

    public override void Clear()
    {
        tileId = 0;
        count = 0;
    }
}
