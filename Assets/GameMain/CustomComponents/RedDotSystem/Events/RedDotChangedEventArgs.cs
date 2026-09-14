using System;
using GameFramework;
using GameFramework.Event;

namespace Lokas
{
    [Serializable]
    public sealed class RedDotChangedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(RedDotChangedEventArgs).GetHashCode();

        public string RedDotPath;
        public int TotalCount;
        public bool IsActive;

        public override int Id => EventId;

        public static RedDotChangedEventArgs Create(string path, int totalCount, bool isActive)
        {
            RedDotChangedEventArgs args = ReferencePool.Acquire<RedDotChangedEventArgs>();
            args.RedDotPath = path;
            args.TotalCount = totalCount;
            args.IsActive = isActive;
            return args;
        }

        public override void Clear()
        {
            RedDotPath = null;
            TotalCount = 0;
            IsActive = false;
        }
    }
}
