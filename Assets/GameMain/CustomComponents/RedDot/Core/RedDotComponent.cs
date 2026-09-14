using System;
using System.Collections.Generic;
using UnityGameFramework.Runtime;
using UnityGameFramework.Runtime.RedDot;
namespace GF_Mahjong.RedDot
{
    public sealed class RedDotComponent : GameFrameworkComponent
    {
        private readonly RedDotTree m_Tree = new();
        private readonly RedDotRuleRegistry m_RuleRegistry = new();
        private bool m_NeedRefresh;

        public event Action<RedDotChange> Changed;

        protected override void Awake()
        {
            base.Awake();
            m_Tree.Changed += OnTreeChanged;
        }

        private void LateUpdate()
        {
            if (!m_NeedRefresh)
                return;

            m_NeedRefresh = false;
            m_Tree.Refresh();
        }

        public void SetActive(string path, bool active)
        {
            SetCount(path, active ? 1 : 0);
        }

        public void SetCount(string path, int count)
        {
            m_Tree.SetCount(path, count);
            m_NeedRefresh = true;
        }

        public void Clear(string path)
        {
            SetCount(path, 0);
        }

        public bool IsActive(string path)
        {
            return m_Tree.IsActive(path);
        }

        public int GetCount(string path)
        {
            return m_Tree.GetCount(path);
        }

        public void RegisterRule(IRedDotRule rule, bool refreshImmediately = true)
        {
            m_RuleRegistry.Register(rule);
            if (refreshImmediately && rule != null)
                RefreshRule(rule.Path);
        }

        public void UnregisterRule(string path)
        {
            m_RuleRegistry.Unregister(path);
        }

        public void RefreshRule(string path)
        {
            if (!m_RuleRegistry.TryGet(path, out IRedDotRule rule))
                return;

            SetCount(path, rule.EvaluateCount());
        }

        public void RefreshAllRules()
        {
            foreach (IRedDotRule rule in m_RuleRegistry.GetAll())
                SetCount(rule.Path, rule.EvaluateCount());
        }

        public IEnumerable<RedDotNode> GetAllNodes()
        {
            return m_Tree.GetAllNodes();
        }

        private void OnTreeChanged(RedDotChange change)
        {
            Changed?.Invoke(change);
        }

        private void OnDestroy()
        {
            m_Tree.Changed -= OnTreeChanged;
        }
    }
}
