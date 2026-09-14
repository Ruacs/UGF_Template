using System;
using System.Collections.Generic;

namespace UnityGameFramework.Runtime.RedDot
{
    public sealed class RedDotTree
    {
        private readonly RedDotNode m_Root = new(string.Empty, string.Empty, null);
        private readonly Dictionary<string, RedDotNode> m_Nodes = new();
        private readonly HashSet<RedDotNode> m_DirtyNodes = new();

        public event Action<RedDotChange> Changed;

        public RedDotNode GetOrCreate(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Red dot path can not be empty.", nameof(path));

            if (m_Nodes.TryGetValue(path, out RedDotNode cached))
                return cached;

            RedDotNode current = m_Root;
            string[] segments = path.Split('.');
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i].Trim();
                if (string.IsNullOrEmpty(segment))
                    throw new ArgumentException($"Invalid red dot path: {path}", nameof(path));

                current = current.GetOrAddChild(segment);
                if (!m_Nodes.ContainsKey(current.Path))
                    m_Nodes.Add(current.Path, current);
            }

            return current;
        }

        public bool TryGet(string path, out RedDotNode node)
        {
            return m_Nodes.TryGetValue(path, out node);
        }

        public int GetCount(string path)
        {
            return TryGet(path, out RedDotNode node) ? node.TotalCount : 0;
        }

        public bool IsActive(string path)
        {
            return GetCount(path) > 0;
        }

        public void SetCount(string path, int count)
        {
            RedDotNode node = GetOrCreate(path);
            if (!node.SetSelfCount(count))
                return;

            MarkDirty(node);
        }

        public void Clear(string path)
        {
            SetCount(path, 0);
        }

        public void Refresh()
        {
            if (m_DirtyNodes.Count == 0)
                return;

            List<RedDotNode> dirtySnapshot = new(m_DirtyNodes);
            m_DirtyNodes.Clear();

            HashSet<RedDotNode> affected = new();
            foreach (RedDotNode dirty in dirtySnapshot)
            {
                RedDotNode current = dirty;
                while (current != null && !string.IsNullOrEmpty(current.Path))
                {
                    affected.Add(current);
                    current = current.Parent;
                }
            }

            List<RedDotNode> ordered = new(affected);
            ordered.Sort((a, b) => GetDepth(b).CompareTo(GetDepth(a)));

            foreach (RedDotNode node in ordered)
            {
                int oldCount = node.TotalCount;
                int newCount = node.RecalculateTotalCount();
                if (oldCount != newCount)
                    Changed?.Invoke(new RedDotChange(node.Path, oldCount, newCount));
            }
        }

        public IEnumerable<RedDotNode> GetAllNodes()
        {
            return m_Nodes.Values;
        }

        private void MarkDirty(RedDotNode node)
        {
            m_DirtyNodes.Add(node);
        }

        private static int GetDepth(RedDotNode node)
        {
            int depth = 0;
            RedDotNode current = node;
            while (current != null && !string.IsNullOrEmpty(current.Path))
            {
                depth++;
                current = current.Parent;
            }

            return depth;
        }
    }
}
