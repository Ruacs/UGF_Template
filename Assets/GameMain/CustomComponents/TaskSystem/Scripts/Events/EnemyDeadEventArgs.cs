using System;
using GameFramework;
using GameFramework.Event;

[Serializable]
public sealed class EnemyDeadEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(EnemyDeadEventArgs).GetHashCode();

    public string enemyId;
    public int count;

    public override int Id => EventId;

    public static EnemyDeadEventArgs Create(string enemyId, int count = 1)
    {
        EnemyDeadEventArgs args = ReferencePool.Acquire<EnemyDeadEventArgs>();
        args.enemyId = enemyId;
        args.count = count;
        return args;
    }

    public override void Clear()
    {
        enemyId = null;
        count = 0;
    }

}
