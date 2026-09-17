using System;
using GameFramework.Event;
using NUnit.Framework;

namespace Lokas.Editor.Tests
{
    public sealed class ActivityGameFactsTests
    {
        [Test]
        public void GameFrameworkDispatchRetainsImmutableFactAfterPooledEnvelopeIsCleared()
        {
            // 独立 GF EventManager，避免修改实际游戏运行时的全局事件管理器。
            Type managerType = typeof(IEventManager).Assembly.GetType("GameFramework.Event.EventManager", true);
            var events = (IEventManager)Activator.CreateInstance(managerType, true);
            var facts = new GameFrameworkActivityFacts(events);
            ActivityLevelCompletedFact received = null;
            int calls = 0;
            var fact = new ActivityLevelCompletedFact("fact-1", "GameA", "round-1", 3, DateTimeOffset.UtcNow, "level-1", true);
            using (facts.Subscribe<ActivityLevelCompletedFact>(value => { received = value; calls++; }))
            {
                var envelope = ActivityGameFactEventArgs.Create(fact);
                events.FireNow(this, envelope);
                Assert.That(envelope.Fact, Is.Null);
                Assert.That(received, Is.SameAs(fact));
                Assert.That(received.FactId, Is.EqualTo("fact-1"));
                Assert.That(events.EventHandlerCount, Is.EqualTo(1));
            }
            events.FireNow(this, ActivityGameFactEventArgs.Create(fact));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(events.EventHandlerCount, Is.Zero);
        }
    }
}
