using System;
using System.Collections.Generic;

namespace Lokas
{
    /// <summary>
    /// 红点管理器
    /// </summary>
    public class RedDotManager
    {
        private readonly Dictionary<string, RedDotNode> _nodes = new();
        private readonly RedDotNode _root;

        public RedDotManager()
        {
            _root = new RedDotNode("Root");
        }

        /// <summary>
        /// 根节点
        /// </summary>
        public RedDotNode Root => _root;

        /// <summary>
        /// 获取节点
        /// </summary>
        public RedDotNode GetNode(string name)
        {
            _nodes.TryGetValue(name, out var node);
            return node;
        }

        /// <summary>
        /// 注册节点（自动创建路径）
        /// </summary>
        public RedDotNode Register(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (_nodes.TryGetValue(path, out var existing))
                return existing;

            string[] segments = path.Split('/');
            RedDotNode current = _root;

            for (int i = 0; i < segments.Length; i++)
            {
                string segmentPath = string.Join("/", segments, 0, i + 1);

                if (!_nodes.TryGetValue(segmentPath, out var child))
                {
                    child = new RedDotNode(segmentPath);
                    _nodes[segmentPath] = child;
                    current.AddChild(child);
                }

                current = child;
            }

            return current;
        }

        /// <summary>
        /// 注册父子关系
        /// </summary>
        public void Register(string parentPath, string childPath)
        {
            var parent = Register(parentPath);
            var child = Register(childPath);

            if (child.Parent == null)
                parent.AddChild(child);
        }

        /// <summary>
        /// 设置红点数量
        /// </summary>
        public void SetCount(string path, int count)
        {
            var node = GetNode(path);
            if (node == null)
                throw new InvalidOperationException($"Node '{path}' not found. Register it first.");

            node.Count = count;
        }

        /// <summary>
        /// 增加红点数量
        /// </summary>
        public void AddCount(string path, int delta = 1)
        {
            var node = GetNode(path);
            if (node == null)
                throw new InvalidOperationException($"Node '{path}' not found. Register it first.");

            node.Count += delta;
        }

        /// <summary>
        /// 清除红点
        /// </summary>
        public void Clear(string path)
        {
            SetCount(path, 0);
        }

        /// <summary>
        /// 清除所有红点
        /// </summary>
        public void ClearAll()
        {
            foreach (var node in _nodes.Values)
            {
                node.Count = 0;
            }
        }

        /// <summary>
        /// 订阅节点变化
        /// </summary>
        public void Subscribe(string path, Action<RedDotNode> callback)
        {
            var node = GetNode(path);
            if (node == null)
                throw new InvalidOperationException($"Node '{path}' not found. Register it first.");

            node.OnChanged += callback;
        }

        /// <summary>
        /// 取消订阅节点变化
        /// </summary>
        public void Unsubscribe(string path, Action<RedDotNode> callback)
        {
            var node = GetNode(path);
            if (node != null)
                node.OnChanged -= callback;
        }

        /// <summary>
        /// 清理所有节点
        /// </summary>
        public void Shutdown()
        {
            foreach (var node in _nodes.Values)
            {
                node.Reset();
            }
            _nodes.Clear();
        }
    }
}
