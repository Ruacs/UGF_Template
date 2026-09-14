using GameFramework;
using GameFramework.Event;

namespace Lokas
{
    public enum CustomEventId
    {
        VoidEvent = 0,
        FrameSizeChanged = 1,
        Dead = 2  //生命值耗尽 
    }

    public sealed class VoidEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(VoidEventArgs).GetHashCode();

        public override int Id => EventId;
        public CustomEventId EventType;

        public static VoidEventArgs Create(CustomEventId eventId)
        {
            VoidEventArgs e = ReferencePool.Acquire<VoidEventArgs>();
            e.EventType = eventId;
            return e;
        }

        public override void Clear()
        {
            EventType = CustomEventId.VoidEvent;
        }
    }
}
