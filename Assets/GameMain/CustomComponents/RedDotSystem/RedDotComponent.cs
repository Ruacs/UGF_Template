using System;
using UnityEngine;
using UnityGameFramework.Runtime;
using UnityGameFramework.Runtime.RedDot;

namespace Lokas
{
    /// <summary>
    /// 绾㈢偣绯荤粺缁勪欢
    /// </summary>
    public class RedDotComponent : GameFrameworkComponent
    {
        private RedDotManager _manager;
        public event Action<RedDotChange> Changed;

        [Serializable]
        public class RedDotConfig
        {
            public string path;
            public string parentPath;
        }

        [SerializeField]
        private RedDotConfig[] _configs = Array.Empty<RedDotConfig>();

        protected override void Awake()
        {
            base.Awake();
            _manager = new RedDotManager();
            InitializeConfigs();
        }

        private void InitializeConfigs()
        {
            if (_configs == null) return;

            foreach (var config in _configs)
            {
                if (string.IsNullOrEmpty(config.path)) continue;

                if (string.IsNullOrEmpty(config.parentPath))
                    _manager.Register(config.path);
                else
                    _manager.Register(config.parentPath, config.path);
            }
        }

        /// <summary>
        /// 绾㈢偣绠＄悊鍣?
        /// </summary>
        public RedDotManager Manager => _manager;

        /// <summary>
        /// 娉ㄥ唽绾㈢偣璺緞
        /// </summary>
        public RedDotNode Register(string path)
        {
            return _manager.Register(path);
        }

        /// <summary>
        /// 娉ㄥ唽鐖跺瓙鍏崇郴
        /// </summary>
        public void Register(string parentPath, string childPath)
        {
            _manager.Register(parentPath, childPath);
        }

        /// <summary>
        /// 璁剧疆绾㈢偣鏁伴噺
        /// </summary>
        public void SetCount(string path, int count)
        {
            int oldCount = GetCount(path);
            _manager.SetCount(path, count);
            NotifyChanged(path, oldCount);
        }

        /// <summary>
        /// 澧炲姞绾㈢偣鏁伴噺
        /// </summary>
        public void AddCount(string path, int delta = 1)
        {
            int oldCount = GetCount(path);
            _manager.AddCount(path, delta);
            NotifyChanged(path, oldCount);
        }

        /// <summary>
        /// 娓呴櫎绾㈢偣
        /// </summary>
        public void Clear(string path)
        {
            int oldCount = GetCount(path);
            _manager.Clear(path);
            NotifyChanged(path, oldCount);
        }

        /// <summary>
        /// 娓呴櫎鎵€鏈夌孩鐐?
        /// </summary>
        public void ClearAll()
        {
            _manager.ClearAll();
        }

        /// <summary>
        /// 鑾峰彇鑺傜偣
        /// </summary>
        public RedDotNode GetNode(string path)
        {
            return _manager.GetNode(path);
        }
        public int GetCount(string path)
        {
            return _manager?.GetNode(path)?.TotalCount ?? 0;
        }

        private void NotifyChanged(string path, int oldCount)
        {
            int newCount = GetCount(path);
            if (oldCount != newCount)
                Changed?.Invoke(new RedDotChange(path, oldCount, newCount));
        }
        /// <summary>
        /// 璁㈤槄鑺傜偣鍙樺寲
        /// </summary>
        public void Subscribe(string path, Action<RedDotNode> callback)
        {
            _manager.Subscribe(path, callback);
        }

        /// <summary>
        /// 鍙栨秷璁㈤槄鑺傜偣鍙樺寲
        /// </summary>
        public void Unsubscribe(string path, Action<RedDotNode> callback)
        {
            _manager.Unsubscribe(path, callback);
        }

        private void OnDestroy()
        {
            _manager?.Shutdown();
        }
    }
}

