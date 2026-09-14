using GameFramework;
using GameFramework.Event;

namespace Lokas
{
    public enum GameValueType
    {
        Score,
        Area
    }

    public sealed class ValueChangedEventArgs<T> : GameEventArgs
    {
        public static readonly int EventId = typeof(ValueChangedEventArgs<T>).GetHashCode();
        public override int Id => EventId;

        public GameValueType Type { get; private set; }
        public T OldValue { get; private set; }
        public T NewValue { get; private set; }

        public static ValueChangedEventArgs<T> Create(GameValueType type, T oldValue, T newValue)
        {
            var e = ReferencePool.Acquire<ValueChangedEventArgs<T>>();
            e.Type = type;
            e.OldValue = oldValue;
            e.NewValue = newValue;
            return e;
        }

        public override void Clear()
        {
            Type = GameValueType.Score;
            OldValue = default;
            NewValue = default;
        }
    }
}
