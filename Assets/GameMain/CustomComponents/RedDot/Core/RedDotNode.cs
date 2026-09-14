using System.Collections.Generic;

namespace UnityGameFramework.Runtime.RedDot
{
    public sealed class RedDotNode
    {
        private readonly Dictionary<string, RedDotNode> m_Children = new();

        public RedDotNode(string name, string path, RedDotNode parent)
        {
            Name = name;
            Path = path;
            Parent = parent;
        }

        public string Name { get; }
        public string Path { get; }
        public RedDotNode Parent { get; }
        public int SelfCount { get; private set; }
        public int TotalCount { get; private set; }
        public bool IsActive => TotalCount > 0;
        public IReadOnlyDictionary<string, RedDotNode> Children => m_Children;

        public RedDotNode GetOrAddChild(string name)
        {
            if (m_Children.TryGetValue(name, out RedDotNode child))
                return child;

            string childPath = string.IsNullOrEmpty(Path) ? name : $"{Path}.{name}";
            child = new RedDotNode(name, childPath, this);
            m_Children.Add(name, child);
            return child;
        }

        public bool SetSelfCount(int count)
        {
            if (count < 0)
                count = 0;

            if (SelfCount == count)
                return false;

            SelfCount = count;
            return true;
        }

        public int RecalculateTotalCount()
        {
            int total = SelfCount;
            foreach (RedDotNode child in m_Children.Values)
                total += child.TotalCount;

            TotalCount = total;
            return TotalCount;
        }
    }
}
