using System;
using GameFramework;
using GameFramework.Event;

[Serializable]
public sealed class LevelPassedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(LevelPassedEventArgs).GetHashCode();

    public string levelId;

    public override int Id => EventId;

    public static LevelPassedEventArgs Create(string levelId)
    {
        var args = ReferencePool.Acquire<LevelPassedEventArgs>();
        args.levelId = levelId;
        return args;
    }

    public override void Clear()
    {
        levelId = null;
    }
}
