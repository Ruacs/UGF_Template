using System;
using System.Collections.Generic;

namespace Lokas
{
    /// <summary>
    /// 红点节点
    /// </summary>
    public class RedDotNode
    {
        private readonly string _name;
        private int _count;
        private bool _active;
        private RedDotNode _parent;
        private readonly List<RedDotNode> _children = new();

        /// <summary>
        /// 节点名称（唯一标识）
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// 当前节点红点数量（不包含子节点）
        /// </summary>
        public int Count
        {
            get => _count;
            set
            {
                if (_count == value) return;
                _count = value;
                Refresh();
            }
        }

        /// <summary>
        /// 红点总数（包含所有子节点）
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// 是否激活（有红点）
        /// </summary>
        public bool Active => TotalCount > 0;

        /// <summary>
        /// 父节点
        /// </summary>
        public RedDotNode Parent => _parent;

        /// <summary>
        /// 子节点列表
        /// </summary>
        public IReadOnlyList<RedDotNode> Children => _children;

        /// <summary>
        /// 数量变化回调
        /// </summary>
        public event Action<RedDotNode> OnChanged;

        public RedDotNode(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddChild(RedDotNode child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child._parent != null)
                throw new InvalidOperationException($"Node '{child._name}' already has a parent.");

            child._parent = this;
            _children.Add(child);
            Refresh();
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        public bool RemoveChild(RedDotNode child)
        {
            if (child == null || child._parent != this) return false;

            child._parent = null;
            bool removed = _children.Remove(child);
            if (removed) Refresh();
            return removed;
        }

        /// <summary>
        /// 递归刷新红点数量
        /// </summary>
        public void Refresh()
        {
            int total = _count;
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Refresh();
                total += _children[i].TotalCount;
            }

            TotalCount = total;
            OnChanged?.Invoke(this);
            _parent?.Refresh();
        }

        /// <summary>
        /// 重置节点
        /// </summary>
        public void Reset()
        {
            _count = 0;
            TotalCount = 0;
            _children.Clear();
            _parent = null;
            OnChanged = null;
        }
    }
}
