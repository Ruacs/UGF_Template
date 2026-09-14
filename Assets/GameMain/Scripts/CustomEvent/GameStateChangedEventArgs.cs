using GameFramework;
using GameFramework.Event;


namespace Lokas
{
    public sealed class GameStateChangedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(GameStateChangedEventArgs).GetHashCode();
        public override int Id => EventId;
        public GameState Previous { get; private set; }
        public GameState Current { get; private set; }

        public static GameStateChangedEventArgs Create(GameState prev, GameState cur)
        {
            var e = ReferencePool.Acquire<GameStateChangedEventArgs>();
            e.Previous = prev;
            e.Current = cur;
            return e;
        }
        public override void Clear() { Previous = Current = GameState.None; }
    }
}