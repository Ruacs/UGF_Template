using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lokas
{
    /// <summary>Owned by TestModeComponent; contains no concrete game dependencies.</summary>
    public sealed class TestModeModuleRegistry : IDisposable
    {
        public const string CommonOwnerId = "Common";
        private readonly List<ITestModeModule> m_Modules = new List<ITestModeModule>();
        private readonly ReadOnlyCollection<ITestModeModule> m_ReadOnlyModules;
        private readonly Dictionary<(string, string), TestModeModuleContext> m_Contexts =
            new Dictionary<(string, string), TestModeModuleContext>();
        private readonly Action<int> m_SimulateRankReward;

        public TestModeModuleRegistry(Action<int> simulateRankReward = null)
        {
            m_ReadOnlyModules = m_Modules.AsReadOnly();
            m_SimulateRankReward = simulateRankReward;
        }

        public IReadOnlyList<ITestModeModule> Modules => m_ReadOnlyModules;
        public event Action Changed;

        public bool Register(ITestModeModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (string.IsNullOrWhiteSpace(module.OwnerId) || string.IsNullOrWhiteSpace(module.ModuleName)
                || string.IsNullOrWhiteSpace(module.PageName))
                throw new ArgumentException("A test module needs an owner, module name and page name.", nameof(module));

            var key = (module.OwnerId, module.ModuleName);
            if (m_Contexts.ContainsKey(key)) return false;
            var context = new TestModeModuleContext(module.OwnerId, RequestRefresh, m_SimulateRankReward);
            m_Contexts.Add(key, context);
            m_Modules.Add(module);
            try
            {
                (module as ITestModeModuleLifecycle)?.OnRegistered(context);
            }
            catch
            {
                Unregister(module);
                throw;
            }
            m_Modules.Sort((left, right) =>
            {
                int order = left.Order.CompareTo(right.Order);
                if (order != 0) return order;
                int owner = string.CompareOrdinal(left.OwnerId, right.OwnerId);
                return owner != 0 ? owner : string.CompareOrdinal(left.ModuleName, right.ModuleName);
            });
            RequestRefresh();
            return true;
        }

        public TestModeModuleContext GetContext(ITestModeModule module)
        {
            if (module == null || !m_Modules.Exists(item => ReferenceEquals(item, module))) return null;
            m_Contexts.TryGetValue((module.OwnerId, module.ModuleName), out var context);
            return context;
        }

        public bool Unregister(ITestModeModule module)
        {
            int index = m_Modules.FindIndex(item => ReferenceEquals(item, module));
            if (index < 0) return false;
            var key = (module.OwnerId, module.ModuleName);
            TestModeModuleContext context = m_Contexts[key];
            m_Modules.RemoveAt(index);
            m_Contexts.Remove(key);
            try
            {
                context.Dispose();
            }
            finally
            {
                try { (module as ITestModeModuleLifecycle)?.OnUnregistered(); }
                finally { RequestRefresh(); }
            }
            return true;
        }

        public void UnregisterOwner(string ownerId)
        {
            RemoveModules(module => module.OwnerId == ownerId);
        }

        public void RequestRefresh() => Changed?.Invoke();

        public void Dispose()
        {
            try { RemoveModules(module => true); }
            finally { Changed = null; }
        }

        private void RemoveModules(Predicate<ITestModeModule> predicate)
        {
            List<Exception> errors = null;
            foreach (ITestModeModule module in m_Modules.ToArray())
            {
                if (!predicate(module)) continue;
                try { Unregister(module); }
                catch (Exception exception)
                {
                    if (errors == null) errors = new List<Exception>();
                    errors.Add(exception);
                }
            }
            if (errors != null) throw new AggregateException("Test module cleanup failed.", errors);
        }
    }
}
