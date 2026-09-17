using System;
using GameFramework;
using GameFramework.Event;

namespace Lokas
{
    /// <summary>GF 池化的是外壳，Fact 必须是可在事件分发后保留的不可变值对象。</summary>
    public sealed class ActivityGameFactEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ActivityGameFactEventArgs).GetHashCode();
        public override int Id => EventId;
        public IActivityGameFact Fact { get; private set; }

        public static ActivityGameFactEventArgs Create(IActivityGameFact fact)
        {
            if (fact == null) throw new ArgumentNullException(nameof(fact));
            var args = ReferencePool.Acquire<ActivityGameFactEventArgs>();
            args.Fact = fact;
            return args;
        }

        public override void Clear() { Fact = null; }
    }

    /// <summary>复用 GF Event，不存储事件历史、不生成 FactId、不推断当前游戏。</summary>
    public sealed class GameFrameworkActivityFacts : IActivityGameFacts
    {
        private readonly IEventManager m_Events;
        public GameFrameworkActivityFacts(IEventManager events)
        {
            m_Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public IDisposable Subscribe<TFact>(Action<TFact> handler) where TFact : class, IActivityGameFact
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return new Subscription<TFact>(m_Events, handler);
        }

        private sealed class Subscription<TFact> : IDisposable where TFact : class, IActivityGameFact
        {
            private readonly IEventManager m_Events;
            private Action<TFact> m_Handler;

            public Subscription(IEventManager events, Action<TFact> handler)
            {
                m_Events = events;
                m_Handler = handler;
                m_Events.Subscribe(ActivityGameFactEventArgs.EventId, OnFact);
            }

            private void OnFact(object sender, GameEventArgs args)
            {
                // 只传递不可变事实，不将池化 EventArgs 交给模块异步持有。
                if (args is ActivityGameFactEventArgs activity && activity.Fact is TFact fact) m_Handler?.Invoke(fact);
            }

            public void Dispose()
            {
                m_Handler = null;
                if (m_Events.Check(ActivityGameFactEventArgs.EventId, OnFact))
                    m_Events.Unsubscribe(ActivityGameFactEventArgs.EventId, OnFact);
            }
        }
    }
}
