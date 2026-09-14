using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// 红点显示类型
    /// </summary>
    public enum RedDotDisplayType
    {
        /// <summary>
        /// 红点（只显示有/无）
        /// </summary>
        Dot,

        /// <summary>
        /// 数字（显示具体数量）
        /// </summary>
        Count,

        /// <summary>
        /// 红点+数字（小于阈值显示红点，大于等于阈值显示数字）
        /// </summary>
        DotOrCount
    }

    /// <summary>
    /// 红点 UI 辅助组件
    /// 挂载到需要显示红点的 UI 元素上
    /// </summary>
    public class RedDotHelper : MonoBehaviour
    {
        [Header("红点配置")]
        [SerializeField]
        private string _redDotPath;

        [SerializeField]
        private RedDotDisplayType _displayType = RedDotDisplayType.Dot;

        [SerializeField]
        private int _countThreshold = 100;

        [Header("UI 引用")]
        [SerializeField]
        private GameObject _dotObject;

        [SerializeField]
        private TMP_Text _countText;

        [SerializeField]
        private Image _dotImage;

        private RedDotNode _node;
        private Action<RedDotNode> _onChanged;

        /// <summary>
        /// 红点路径
        /// </summary>
        public string RedDotPath
        {
            get => _redDotPath;
            set
            {
                _redDotPath = value;
                BindNode();
            }
        }

        /// <summary>
        /// 显示类型
        /// </summary>
        public RedDotDisplayType DisplayType
        {
            get => _displayType;
            set
            {
                _displayType = value;
                UpdateDisplay();
            }
        }

        private void Start()
        {
            BindNode();
        }

        private void OnDestroy()
        {
            UnbindNode();
        }

        private void OnDisable()
        {
            UnbindNode();
        }

        private void OnEnable()
        {
            BindNode();
        }

        private void BindNode()
        {
            UnbindNode();

            if (string.IsNullOrEmpty(_redDotPath)) return;

            var component = GameEntry.RedDot;
            if (component == null) return;

            _node = component.GetNode(_redDotPath);
            if (_node == null)
            {
                component.Register(_redDotPath);
                _node = component.GetNode(_redDotPath);
            }

            if (_node != null)
            {
                _onChanged = OnRedDotChanged;
                _node.OnChanged += _onChanged;
                UpdateDisplay();
            }
        }

        private void UnbindNode()
        {
            if (_node != null && _onChanged != null)
            {
                _node.OnChanged -= _onChanged;
                _onChanged = null;
            }
            _node = null;
        }

        private void OnRedDotChanged(RedDotNode node)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_node == null)
            {
                SetVisible(false);
                return;
            }

            int count = _node.TotalCount;
            bool hasRedDot = count > 0;

            switch (_displayType)
            {
                case RedDotDisplayType.Dot:
                    SetVisible(hasRedDot);
                    if (_countText != null) _countText.text = string.Empty;
                    break;

                case RedDotDisplayType.Count:
                    SetVisible(hasRedDot);
                    if (_countText != null)
                        _countText.text = count > _countThreshold ? $"{_countThreshold}+" : count.ToString();
                    break;

                case RedDotDisplayType.DotOrCount:
                    if (count >= _countThreshold)
                    {
                        SetVisible(true);
                        if (_dotObject != null) _dotObject.SetActive(false);
                        if (_countText != null)
                        {
                            _countText.gameObject.SetActive(true);
                            _countText.text = $"{_countThreshold}+";
                        }
                    }
                    else
                    {
                        SetVisible(hasRedDot);
                        if (_dotObject != null) _dotObject.SetActive(true);
                        if (_countText != null) _countText.gameObject.SetActive(false);
                    }
                    break;
            }
        }

        private void SetVisible(bool visible)
        {
            if (_dotObject != null)
                _dotObject.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            UpdateDisplay();
        }
#endif
    }
}
