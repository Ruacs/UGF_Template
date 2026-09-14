using System.Collections.Generic;

namespace GF_Mahjong.RedDot
{
    public sealed class RedDotRuleRegistry
    {
        private readonly Dictionary<string, IRedDotRule> m_Rules = new();

        public void Register(IRedDotRule rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Path))
                return;

            m_Rules[rule.Path] = rule;
        }

        public void Unregister(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            m_Rules.Remove(path);
        }

        public bool TryGet(string path, out IRedDotRule rule)
        {
            return m_Rules.TryGetValue(path, out rule);
        }

        public IEnumerable<IRedDotRule> GetAll()
        {
            return m_Rules.Values;
        }
    }
}
