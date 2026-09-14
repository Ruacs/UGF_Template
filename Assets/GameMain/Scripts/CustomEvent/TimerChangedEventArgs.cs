using GameFramework;
using GameFramework.Event;

namespace Lokas
{
    public sealed class TimerChangedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(TimerChangedEventArgs).GetHashCode();
        public override int Id => EventId;
        public int RemainingTime { get; private set; }

        public static TimerChangedEventArgs Create(int time)
        {
            var e = ReferencePool.Acquire<TimerChangedEventArgs>();
            e.RemainingTime = time;
            return e;
        }
        public override void Clear() => RemainingTime = 0;
    }
}