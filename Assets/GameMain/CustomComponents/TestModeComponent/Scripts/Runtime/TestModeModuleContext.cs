using System;
using System.Threading;

namespace Lokas
{
    public sealed class TestModeModuleContext : IDisposable
    {
        private readonly CancellationTokenSource m_Cancellation = new CancellationTokenSource();
        private Action m_RequestRefresh;
        private Action<int> m_SimulateRankReward;

        public TestModeModuleContext(string ownerId, Action requestRefresh, Action<int> simulateRankReward = null)
        {
            OwnerId = ownerId;
            m_RequestRefresh = requestRefresh;
            m_SimulateRankReward = simulateRankReward;
            CancellationToken = m_Cancellation.Token;
        }

        public string OwnerId { get; }
        public bool IsActive { get; private set; } = true;
        public CancellationToken CancellationToken { get; }

        public void RequestRefresh()
        {
            if (IsActive) m_RequestRefresh?.Invoke();
        }

        public void SimulateRankReward(int rank)
        {
            if (IsActive) m_SimulateRankReward?.Invoke(rank);
        }

        public void Dispose()
        {
            if (!IsActive) return;
            // Invalidate before cancellation callbacks run.
            IsActive = false;
            m_RequestRefresh = null;
            m_SimulateRankReward = null;
            try { m_Cancellation.Cancel(); }
            finally { m_Cancellation.Dispose(); }
        }
    }
}
